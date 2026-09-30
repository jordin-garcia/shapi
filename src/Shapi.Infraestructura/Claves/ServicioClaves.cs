using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Claves;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Claves;

/// <summary>
/// Emisión, rotación y revocación de claves (RF-26 a RF-28). Cada cambio se guarda en PostgreSQL y, después del
/// <i>commit</i>, se publica en Redis (06 §5.4). Todas las consultas usan el filtro global por organización (10 §2).
/// Nunca se registra una clave completa ni su hash (10 §3).
/// </summary>
public sealed class ServicioClaves(
    ShapiDbContext db,
    IReloj reloj,
    IPublicadorCache publicador,
    IBitacora bitacora) : IServicioClaves
{
    private static readonly TipoClave[] Tipos = [TipoClave.Produccion, TipoClave.Pruebas];

    public async Task<IReadOnlyList<ClaveEmitida>> EmitirClavesParaSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default)
    {
        var existe = await db.Set<SuscripcionApi>()
            .AnyAsync(s => s.Id == suscripcionId && s.Estado != EstadoSuscripcion.Finalizada, cancelacion);
        if (!existe)
        {
            throw new InvalidOperationException(
                $"La suscripción {suscripcionId} no existe, es de otra organización o está finalizada.");
        }

        var conClaveActiva = await db.Set<Clave>()
            .Where(c => c.SuscripcionId == suscripcionId && c.Estado == EstadoClave.Activa)
            .Select(c => c.Tipo)
            .ToListAsync(cancelacion);

        var emitidas = Tipos.Where(t => !conClaveActiva.Contains(t))
            .Select(tipo => Clave.Emitir(suscripcionId, tipo))
            .ToList();
        db.AddRange(emitidas.Select(e => e.Clave));
        await db.SaveChangesAsync(cancelacion);

        foreach (var (clave, _) in emitidas)
        {
            await publicador.PublicarClave(clave.Id, cancelacion);
        }

        return [.. emitidas.Select(e => Emitida(e.Clave, e.EnClaro))];
    }

    public async Task<IReadOnlyList<VistaClave>> ClavesDelConsumidor(ConsumidorDelPortal consumidor, CancellationToken cancelacion = default)
    {
        var suscripcionId = await SuscripcionVigente(consumidor, cancelacion);
        if (suscripcionId is null)
        {
            return [];
        }

        var claves = await db.Set<Clave>().AsNoTracking()
            .Where(c => c.SuscripcionId == suscripcionId)
            .ToListAsync(cancelacion);
        return Visibles(claves, reloj.Ahora);
    }

    public async Task<Resultado<ClaveEmitida>> Emitir(ConsumidorDelPortal consumidor, TipoClave tipo, CancellationToken cancelacion = default)
    {
        var suscripcionId = await SuscripcionVigente(consumidor, cancelacion);
        if (suscripcionId is null)
        {
            return ErroresClaves.NoEncontrada;
        }

        if (await db.Set<Clave>().AnyAsync(
            c => c.SuscripcionId == suscripcionId && c.Tipo == tipo && c.Estado == EstadoClave.Activa, cancelacion))
        {
            return ErroresClaves.ActivaExistente;
        }

        var (clave, enClaro) = Clave.Emitir(suscripcionId.Value, tipo);
        db.Add(clave);
        try
        {
            await db.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException excepcion) when (ViolaUnicidad(excepcion))
        {
            // Otra petición emitió la misma clave al mismo tiempo: el índice único de clave activa por tipo lo impide.
            return ErroresClaves.ActivaExistente;
        }

        await publicador.PublicarClave(clave.Id, cancelacion);
        return Emitida(clave, enClaro);
    }

    public async Task<Resultado<ClaveRotada>> Rotar(ConsumidorDelPortal consumidor, Guid claveId, CancellationToken cancelacion = default)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var clave = await BloquearClaveDelConsumidor(consumidor, claveId, cancelacion);
        if (clave is null)
        {
            return ErroresClaves.NoEncontrada;
        }

        if (!clave.PuedeRotarse)
        {
            return ErroresClaves.NoRotable;
        }

        // RF-27: como máximo dos claves vigentes del mismo tipo. Si la anterior rotada sigue dentro de sus 24 horas, deja
        // de funcionar ahora; solo se cambia su expira_en, así que una revocación simultánea de esa clave no se pierde.
        var ahora = reloj.Ahora;
        var rotadasVigentes = await db.Set<Clave>()
            .Where(c => c.SuscripcionId == clave.SuscripcionId && c.Tipo == clave.Tipo && c.Id != clave.Id
                && c.Estado == EstadoClave.Rotada && c.ExpiraEn > ahora)
            .ToListAsync(cancelacion);
        foreach (var rotada in rotadasVigentes)
        {
            rotada.TerminarRotacion(ahora);
        }

        var (nueva, enClaro) = clave.Rotar(ahora);
        db.Add(nueva);
        await RegistrarAccionDelConsumidor(consumidor, clave, AccionesBitacora.ClaveRotada, "rotó", cancelacion);
        await transaccion.CommitAsync(cancelacion);

        foreach (var rotada in rotadasVigentes)
        {
            await publicador.EliminarClave(rotada.HashSha256, cancelacion);
        }

        await publicador.PublicarClave(nueva.Id, cancelacion);
        await publicador.ExpirarClave(clave.HashSha256, clave.ExpiraEn!.Value, cancelacion);
        return new ClaveRotada(nueva.Id, Tipo(nueva.Tipo), enClaro, nueva.Enmascarada, Estado(nueva.Estado), Vista(clave));
    }

    public async Task<Resultado<VistaClave>> RevocarPropia(ConsumidorDelPortal consumidor, Guid claveId, CancellationToken cancelacion = default)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var clave = await BloquearClaveDelConsumidor(consumidor, claveId, cancelacion);
        if (clave is null)
        {
            return ErroresClaves.NoEncontrada;
        }

        if (!clave.Revocar(RevocadaPor.Consumidor, reloj.Ahora))
        {
            return Vista(clave);
        }

        await RegistrarAccionDelConsumidor(consumidor, clave, AccionesBitacora.ClaveRevocadaPorConsumidor, "revocó", cancelacion);
        await transaccion.CommitAsync(cancelacion);

        await publicador.EliminarClave(clave.HashSha256, cancelacion);
        return Vista(clave);
    }

    public async Task<Resultado<PaginaClavesDeApi>> ClavesDeApi(Guid apiId, int pagina, int tamano, CancellationToken cancelacion = default)
    {
        if (!await db.Set<Api>().AnyAsync(a => a.Id == apiId, cancelacion))
        {
            return ErroresClaves.NoEncontrada;
        }

        var suscripciones =
            from s in db.Set<SuscripcionApi>()
            where s.ApiId == apiId && s.Estado != EstadoSuscripcion.Finalizada
            join c in db.Set<Consumidor>() on s.ConsumidorId equals c.Id
            join p in db.Set<PlanApi>() on s.PlanId equals p.Id
            select new { s.Id, s.Inicio, ConsumidorId = c.Id, c.NombreEmpresa, Plan = p.Nombre };

        var total = await suscripciones.CountAsync(cancelacion);
        var filas = await suscripciones
            .OrderBy(s => s.Inicio).ThenBy(s => s.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync(cancelacion);

        var ids = filas.Select(f => f.Id).ToList();
        var claves = await db.Set<Clave>().AsNoTracking()
            .Where(c => ids.Contains(c.SuscripcionId))
            .ToListAsync(cancelacion);
        var porSuscripcion = claves.ToLookup(c => c.SuscripcionId);

        var ahora = reloj.Ahora;
        var elementos = filas
            .Select(f => new ClavesDeConsumidor(f.ConsumidorId, f.NombreEmpresa, f.Plan, Visibles([.. porSuscripcion[f.Id]], ahora)))
            .ToList();
        return new PaginaClavesDeApi(elementos, total);
    }

    public async Task<Resultado<VistaClave>> RevocarDeConsumidor(
        MiembroDelPanel miembro, Guid apiId, Guid claveId, CancellationToken cancelacion = default)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        await Bloquear(claveId, cancelacion);
        var clave = await db.Set<Clave>()
            .Where(c => c.Id == claveId && db.Set<SuscripcionApi>().Any(s => s.Id == c.SuscripcionId && s.ApiId == apiId))
            .SingleOrDefaultAsync(cancelacion);
        if (clave is null)
        {
            return ErroresClaves.NoEncontrada;
        }

        if (!clave.Revocar(RevocadaPor.Proveedor, reloj.Ahora))
        {
            return Vista(clave);
        }

        var datos = await DatosDeLaClave(clave.SuscripcionId, cancelacion);
        var usuario = await db.Set<Usuario>().Where(u => u.Id == miembro.UsuarioId).Select(u => u.Nombre).SingleAsync(cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario, miembro.UsuarioId, usuario, datos.OrganizacionId, AccionesBitacora.ClaveRevocadaPorProveedor,
            $"Revocó la clave {TipoEnTexto(clave.Tipo)} de {datos.Empresa} en {EnLaApi(datos.Api)}")
        {
            ObjetivoTipo = "clave",
            ObjetivoId = clave.Id,
            Ip = miembro.Ip,
        }, cancelacion);
        await transaccion.CommitAsync(cancelacion);

        await publicador.EliminarClave(clave.HashSha256, cancelacion);
        return Vista(clave);
    }

    // ---------- Consultas ----------

    /// <summary>La suscripción sin finalizar del consumidor en la API del portal (hay una como máximo, 07 §3.3).</summary>
    private Task<Guid?> SuscripcionVigente(ConsumidorDelPortal consumidor, CancellationToken cancelacion) =>
        db.Set<SuscripcionApi>()
            .Where(s => s.ConsumidorId == consumidor.ConsumidorId && s.ApiId == consumidor.ApiId
                && s.Estado != EstadoSuscripcion.Finalizada)
            .Select(s => (Guid?)s.Id)
            .SingleOrDefaultAsync(cancelacion);

    /// <summary>
    /// Bloquea la fila de la clave hasta el final de la transacción (<c>FOR UPDATE</c>). Así, dos rotaciones o una
    /// rotación y una revocación de la misma clave se hacen una después de la otra y la segunda ve el estado nuevo.
    /// </summary>
    private Task Bloquear(Guid claveId, CancellationToken cancelacion) =>
        db.Database.ExecuteSqlAsync($"SELECT 1 FROM clave WHERE id = {claveId} FOR UPDATE", cancelacion);

    private async Task<Clave?> BloquearClaveDelConsumidor(ConsumidorDelPortal consumidor, Guid claveId, CancellationToken cancelacion)
    {
        await Bloquear(claveId, cancelacion);
        return await db.Set<Clave>()
            .Where(c => c.Id == claveId && db.Set<SuscripcionApi>().Any(s => s.Id == c.SuscripcionId
                && s.ConsumidorId == consumidor.ConsumidorId && s.ApiId == consumidor.ApiId
                && s.Estado != EstadoSuscripcion.Finalizada))
            .SingleOrDefaultAsync(cancelacion);
    }

    private sealed record DatosClave(Guid OrganizacionId, string Api, string Empresa, string NombreConsumidor);

    private Task<DatosClave> DatosDeLaClave(Guid suscripcionId, CancellationToken cancelacion) =>
        (from s in db.Set<SuscripcionApi>()
         where s.Id == suscripcionId
         join a in db.Set<Api>() on s.ApiId equals a.Id
         join c in db.Set<Consumidor>() on s.ConsumidorId equals c.Id
         select new DatosClave(a.OrganizacionId, a.Nombre, c.NombreEmpresa, c.Nombre))
        .SingleAsync(cancelacion);

    /// <summary>Registra en la bitácora la acción del consumidor; guarda también los cambios pendientes de la clave.</summary>
    private async Task RegistrarAccionDelConsumidor(
        ConsumidorDelPortal consumidor, Clave clave, string accion, string verbo, CancellationToken cancelacion)
    {
        var datos = await DatosDeLaClave(clave.SuscripcionId, cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Consumidor, consumidor.ConsumidorId, datos.NombreConsumidor, datos.OrganizacionId, accion,
            $"{datos.Empresa} {verbo} su clave {TipoEnTexto(clave.Tipo)} en {EnLaApi(datos.Api)}")
        {
            ObjetivoTipo = "clave",
            ObjetivoId = clave.Id,
            Ip = consumidor.Ip,
        }, cancelacion);
    }

    /// <summary>
    /// Las claves que se muestran (A4.3 y B2.3): las activas, las rotadas durante sus 24 horas y, por cada tipo sin clave
    /// activa, la última revocada, para que se vea que se puede emitir otra. Primero producción y luego pruebas; dentro
    /// de cada tipo, activa, rotada y revocada.
    /// </summary>
    internal static IReadOnlyList<VistaClave> Visibles(IReadOnlyCollection<Clave> claves, DateTimeOffset ahora)
    {
        var visibles = claves.Where(c => c.EsVigente(ahora)).ToList();
        foreach (var tipo in Tipos.Where(t => !claves.Any(c => c.Tipo == t && c.Estado == EstadoClave.Activa)))
        {
            var ultimaRevocada = claves
                .Where(c => c.Tipo == tipo && c.Estado == EstadoClave.Revocada)
                .MaxBy(c => c.RevocadaEn);
            if (ultimaRevocada is not null)
            {
                visibles.Add(ultimaRevocada);
            }
        }

        return [.. visibles.OrderBy(c => c.Tipo).ThenBy(c => c.Estado).ThenByDescending(c => c.CreadoEn).Select(Vista)];
    }

    // ---------- Traducción ----------

    private static bool ViolaUnicidad(DbUpdateException excepcion) =>
        excepcion.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };

    private static ClaveEmitida Emitida(Clave clave, string enClaro) =>
        new(clave.Id, Tipo(clave.Tipo), enClaro, clave.Enmascarada, Estado(clave.Estado));

    private static VistaClave Vista(Clave clave) =>
        new(clave.Id, Tipo(clave.Tipo), clave.Enmascarada, Estado(clave.Estado), clave.ExpiraEn, clave.RevocadaEn);

    private static string Tipo(TipoClave tipo) => tipo == TipoClave.Produccion
        ? Contratos.Redis.ContextoClave.TipoProduccion
        : Contratos.Redis.ContextoClave.TipoPruebas;

    private static string Estado(EstadoClave estado) => estado switch
    {
        EstadoClave.Activa => "activa",
        EstadoClave.Rotada => "rotada",
        _ => "revocada",
    };

    private static string TipoEnTexto(TipoClave tipo) => tipo == TipoClave.Produccion ? "de producción" : "de pruebas";

    /// <summary>"en la API de Cotización de Envíos" (10 §7). Si el nombre no empieza con "API", se le antepone.</summary>
    private static string EnLaApi(string nombre) =>
        nombre.StartsWith("API ", StringComparison.OrdinalIgnoreCase) ? $"la {nombre}" : $"la API {nombre}";
}
