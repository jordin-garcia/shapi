using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shapi.Aplicacion.Cache;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Cache;

/// <summary>Lo que se publica de una API: su hash (con la versión en 0), sus rutas y sus hosts.</summary>
public sealed record ApiCache(ContextoApi Contexto, IReadOnlyList<RutaCache> Rutas, IReadOnlyList<string> Hosts);

/// <summary>
/// Lo que se publica de una clave. <see cref="Contexto"/> es <c>null</c> si la clave no debe estar en Redis
/// (revocada o con la rotación vencida); <see cref="ExpiraEn"/> tiene valor si está rotada.
/// </summary>
public sealed record ClaveCache(string Hash, ContextoClave? Contexto, DateTimeOffset? ExpiraEn);

/// <summary>
/// Lee de PostgreSQL lo que se publica en Redis y lo arma con <see cref="ArmadoCache"/>. Es un componente del sistema:
/// lee sin el filtro por organización (10 §2), como el trabajador. Lo usan el publicador y la resincronización.
/// </summary>
public sealed class LectorCacheBaseDatos(
    ShapiDbContext db,
    IProtectorSecretoOrigen protector,
    IConfiguration configuracion,
    IReloj reloj,
    ILogger<LectorCacheBaseDatos> registro)
{
    private readonly string _dominioBase =
        configuracion["SHAPI_DOMINIO_BASE"]?.Trim().TrimEnd('.').ToLowerInvariant() is { Length: > 0 } dominio
            ? dominio
            : "shapi.localhost";

    // ---------- APIs ----------

    /// <returns><c>null</c> si la API no existe.</returns>
    /// <exception cref="CryptographicException">Si no se puede descifrar el secreto de origen.</exception>
    public async Task<ApiCache?> ApiAsync(Guid apiId, CancellationToken cancelacion)
    {
        var api = await Sin<Api>().SingleOrDefaultAsync(a => a.Id == apiId, cancelacion);
        if (api is null)
        {
            return null;
        }

        var rutas = await Sin<Ruta>().Where(r => r.ApiId == apiId).ToListAsync(cancelacion);
        var dominio = await Sin<DominioPropio>().SingleOrDefaultAsync(d => d.ApiId == apiId, cancelacion);
        return Armar(api, rutas, dominio);
    }

    /// <summary>
    /// Las APIs publicadas (07 §4). Una API cuyo secreto no se puede descifrar se registra como error y se omite:
    /// publicarla sin su secreto haría que el origen rechace el tráfico, o que lo acepte sin validarlo.
    /// </summary>
    public async Task<IReadOnlyList<ApiCache>> ApisPublicadasAsync(CancellationToken cancelacion)
    {
        var apis = await Sin<Api>().Where(a => a.Estado == EstadoApi.Publicada).ToListAsync(cancelacion);
        var ids = apis.Select(a => a.Id).ToList();
        var rutas = (await Sin<Ruta>().Where(r => ids.Contains(r.ApiId)).ToListAsync(cancelacion)).ToLookup(r => r.ApiId);
        var dominios = (await Sin<DominioPropio>().Where(d => ids.Contains(d.ApiId)).ToListAsync(cancelacion))
            .ToDictionary(d => d.ApiId);

        var resultado = new List<ApiCache>();
        foreach (var api in apis)
        {
            try
            {
                resultado.Add(Armar(api, rutas[api.Id], dominios.GetValueOrDefault(api.Id)));
            }
            catch (CryptographicException)
            {
                registro.LogError("No se pudo descifrar el secreto de origen de la API {ApiId}; no se publica", api.Id);
            }
        }

        return resultado;
    }

    // ---------- Claves ----------

    /// <returns><c>null</c> si la clave no existe.</returns>
    public async Task<ClaveCache?> ClaveAsync(Guid claveId, CancellationToken cancelacion)
    {
        var fila = await (
            from c in Sin<Clave>()
            join s in Sin<SuscripcionApi>() on c.SuscripcionId equals s.Id
            join a in Sin<Api>() on s.ApiId equals a.Id
            where c.Id == claveId
            select new { Clave = c, Suscripcion = s, a.OrganizacionId }).SingleOrDefaultAsync(cancelacion);

        return fila is null ? null : Armar(fila.Clave, fila.Suscripcion, fila.OrganizacionId);
    }

    /// <summary>Las claves activas y las rotadas que siguen vigentes (07 §4).</summary>
    public async Task<IReadOnlyList<ClaveCache>> ClavesVigentesAsync(CancellationToken cancelacion)
    {
        var ahora = reloj.Ahora;
        var filas = await (
            from c in Sin<Clave>()
            join s in Sin<SuscripcionApi>() on c.SuscripcionId equals s.Id
            join a in Sin<Api>() on s.ApiId equals a.Id
            where c.Estado == EstadoClave.Activa || (c.Estado == EstadoClave.Rotada && c.ExpiraEn > ahora)
            select new { Clave = c, Suscripcion = s, a.OrganizacionId }).ToListAsync(cancelacion);

        return [.. filas.Select(f => Armar(f.Clave, f.Suscripcion, f.OrganizacionId))];
    }

    /// <summary>El estado actual de las claves con estos hashes, para corregir las que cambiaron durante una resincronización.</summary>
    public async Task<IReadOnlyList<ClaveCache>> ClavesAsync(IReadOnlyCollection<string> hashes, CancellationToken cancelacion)
    {
        var filas = await (
            from c in Sin<Clave>()
            join s in Sin<SuscripcionApi>() on c.SuscripcionId equals s.Id
            join a in Sin<Api>() on s.ApiId equals a.Id
            where hashes.Contains(c.HashSha256)
            select new { Clave = c, Suscripcion = s, a.OrganizacionId }).ToListAsync(cancelacion);

        return [.. filas.Select(f => Armar(f.Clave, f.Suscripcion, f.OrganizacionId))];
    }

    // ---------- Suscripciones ----------

    /// <returns>
    /// Si es una suscripción de API y, en ese caso, su contexto, que es <c>null</c> si está finalizada.
    /// </returns>
    public async Task<(bool Existe, ContextoSuscripcion? Contexto)> SuscripcionApiAsync(Guid suscripcionId, CancellationToken cancelacion)
    {
        var fila = await (
            from s in Sin<SuscripcionApi>()
            join p in Sin<PlanApi>() on s.PlanId equals p.Id
            where s.Id == suscripcionId
            select new { Suscripcion = s, Plan = p }).SingleOrDefaultAsync(cancelacion);

        if (fila is null)
        {
            return (false, null);
        }

        return (true, fila.Suscripcion.Estado == EstadoSuscripcion.Finalizada
            ? null
            : ArmadoCache.Suscripcion(fila.Suscripcion, fila.Plan));
    }

    /// <returns>La organización de la suscripción de plataforma, o <c>null</c> si no es una suscripción de plataforma.</returns>
    public Task<Guid?> OrganizacionDeSuscripcionPlataformaAsync(Guid suscripcionId, CancellationToken cancelacion) =>
        Sin<SuscripcionPlataforma>().Where(s => s.Id == suscripcionId).Select(s => (Guid?)s.OrganizacionId)
            .SingleOrDefaultAsync(cancelacion);

    /// <summary>Las suscripciones de API sin finalizar (07 §4).</summary>
    public async Task<IReadOnlyList<ContextoSuscripcion>> SuscripcionesVigentesAsync(CancellationToken cancelacion)
    {
        var filas = await (
            from s in Sin<SuscripcionApi>()
            join p in Sin<PlanApi>() on s.PlanId equals p.Id
            where s.Estado != EstadoSuscripcion.Finalizada
            select new { Suscripcion = s, Plan = p }).ToListAsync(cancelacion);

        return [.. filas.Select(f => ArmadoCache.Suscripcion(f.Suscripcion, f.Plan))];
    }

    // ---------- Organizaciones ----------

    /// <returns><c>null</c> si la organización no existe.</returns>
    public async Task<ContextoOrganizacion?> OrganizacionAsync(Guid organizacionId, CancellationToken cancelacion)
    {
        var organizacion = await Sin<Organizacion>().SingleOrDefaultAsync(o => o.Id == organizacionId, cancelacion);
        if (organizacion is null)
        {
            return null;
        }

        var vigente = await SuscripcionesPlataformaVigentes(organizacionId).SingleOrDefaultAsync(cancelacion);
        return ArmadoCache.Organizacion(organizacion, vigente?.Suscripcion, vigente?.Plan);
    }

    public async Task<IReadOnlyList<ContextoOrganizacion>> OrganizacionesAsync(CancellationToken cancelacion)
    {
        var organizaciones = await Sin<Organizacion>().ToListAsync(cancelacion);
        var vigentes = (await SuscripcionesPlataformaVigentes().ToListAsync(cancelacion))
            .ToDictionary(f => f.Suscripcion.OrganizacionId);

        return
        [
            .. organizaciones.Select(o =>
            {
                var vigente = vigentes.GetValueOrDefault(o.Id);
                return ArmadoCache.Organizacion(o, vigente?.Suscripcion, vigente?.Plan);
            }),
        ];
    }

    // Hay a lo más una suscripción de plataforma sin finalizar por organización (índice único parcial, 07 §3.3).
    private IQueryable<SuscripcionConPlan> SuscripcionesPlataformaVigentes(Guid? organizacionId = null) =>
        from s in Sin<SuscripcionPlataforma>()
        where s.Estado != EstadoSuscripcion.Finalizada && (organizacionId == null || s.OrganizacionId == organizacionId)
        join p in Sin<PlanPlataforma>() on s.PlanId equals p.Id
        select new SuscripcionConPlan(s, p);

    private ApiCache Armar(Api api, IEnumerable<Ruta> rutas, DominioPropio? dominio) => new(
        ArmadoCache.Api(api, protector.Descifrar(api.SecretoOrigenCifrado), _dominioBase),
        ArmadoCache.Rutas(rutas),
        ArmadoCache.Hosts(api, dominio, _dominioBase));

    private ClaveCache Armar(Clave clave, SuscripcionApi suscripcion, Guid organizacionId) =>
        ArmadoCache.SePublica(clave, reloj.Ahora, out var expiraEn)
            ? new ClaveCache(clave.HashSha256, ArmadoCache.Clave(clave, suscripcion, organizacionId), expiraEn)
            : new ClaveCache(clave.HashSha256, null, null);

    private IQueryable<T> Sin<T>()
        where T : class => db.Set<T>().IgnoreQueryFilters().AsNoTracking();

    private sealed record SuscripcionConPlan(SuscripcionPlataforma Suscripcion, PlanPlataforma Plan);
}
