using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Bitacora;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Persistencia;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Bitacora;

/// <summary>Consulta administrativa de la bitácora (RF-41).</summary>
public static class Endpoints
{
    private static readonly TimeSpan DesfaseGuatemala = TimeSpan.FromHours(-6);

    public static IEndpointRouteBuilder MapearEndpointsBitacora(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/bitacora", Consultar)
            .RequireAuthorization(Permisos.VerBitacora);
        return app;
    }

    private static async Task<IResult> Consultar(
        HttpRequest peticion,
        ShapiDbContext db,
        IReloj reloj,
        CancellationToken cancelacion)
    {
        if (!LeerFecha(peticion.Query["desde"], out var desde)
            || !LeerFecha(peticion.Query["hasta"], out var hasta)
            || !LeerEntero(peticion.Query["pagina"], 1, int.MaxValue, 1, out var pagina)
            || !LeerEntero(peticion.Query["tamano"], 1, 100, 20, out var tamano))
        {
            return DatosInvalidos("Use fechas AAAA-MM-DD, pagina mayor que cero y tamano entre 1 y 100.");
        }

        var hoy = DateOnly.FromDateTime(reloj.Ahora.ToOffset(DesfaseGuatemala).DateTime);
        hasta ??= hoy;
        desde ??= hasta.Value.AddDays(-6);
        if (desde > hasta || pagina - 1 > int.MaxValue / tamano)
        {
            return DatosInvalidos("El periodo o la paginación no son válidos.");
        }

        var inicio = InicioDia(desde.Value);
        var finExclusivo = InicioDia(hasta.Value.AddDays(1));
        var consulta = db.Set<EntradaBitacoraDominio>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.Fecha >= inicio && e.Fecha < finExclusivo);

        var total = await consulta.CountAsync(cancelacion);
        var filas = await consulta
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(e => new Fila(e.Fecha, e.ActorTipo, e.ActorId, e.ActorNombre, e.Accion, e.Descripcion))
            .ToListAsync(cancelacion);

        var actoresUsuario = filas
            .Where(e => e.ActorTipo == ActorTipo.Usuario && e.ActorId.HasValue)
            .Select(e => e.ActorId!.Value)
            .Distinct()
            .ToArray();
        var actoresConsumidor = filas
            .Where(e => e.ActorTipo == ActorTipo.Consumidor && e.ActorId.HasValue)
            .Select(e => e.ActorId!.Value)
            .Distinct()
            .ToArray();

        var usuarios = await (
            from membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking()
            join organizacion in db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
                on membresia.OrganizacionId equals organizacion.Id
            where actoresUsuario.Contains(membresia.UsuarioId)
            select new ActorUsuario(membresia.UsuarioId, membresia.Rol, organizacion.Nombre))
            .ToDictionaryAsync(x => x.Id, cancelacion);

        var consumidores = await (
            from consumidor in db.Set<Consumidor>().IgnoreQueryFilters().AsNoTracking()
            join organizacion in db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
                on consumidor.OrganizacionId equals organizacion.Id
            where actoresConsumidor.Contains(consumidor.Id)
            select new ActorConsumidor(consumidor.Id, organizacion.Nombre))
            .ToDictionaryAsync(x => x.Id, cancelacion);

        var elementos = filas.Select(fila => new EntradaBitacoraListado(
            fila.Fecha,
            Actor(fila, usuarios, consumidores),
            fila.Accion,
            fila.Descripcion)).ToArray();

        return TypedResults.Ok(new PaginaBitacora(elementos, total));
    }

    private static ActorBitacora Actor(
        Fila fila,
        IReadOnlyDictionary<Guid, ActorUsuario> usuarios,
        IReadOnlyDictionary<Guid, ActorConsumidor> consumidores)
    {
        if (fila.ActorTipo == ActorTipo.Usuario && fila.ActorId is Guid usuarioId && usuarios.TryGetValue(usuarioId, out var usuario))
        {
            return new(fila.ActorNombre, usuario.Rol.ToString().ToLowerInvariant(), usuario.Organizacion);
        }

        if (fila.ActorTipo == ActorTipo.Consumidor && fila.ActorId is Guid consumidorId && consumidores.TryGetValue(consumidorId, out var consumidor))
        {
            return new(fila.ActorNombre, "consumidor", consumidor.Organizacion);
        }

        return new(fila.ActorNombre, fila.ActorTipo == ActorTipo.Sistema ? "sistema" : "usuario", null);
    }

    private static bool LeerFecha(string? valor, out DateOnly? fecha)
    {
        fecha = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }
        if (!DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var leida))
        {
            return false;
        }
        fecha = leida;
        return true;
    }

    private static bool LeerEntero(string? valor, int minimo, int maximo, int predeterminado, out int numero)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            numero = predeterminado;
            return true;
        }

        return int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out numero)
            && numero >= minimo && numero <= maximo;
    }

    private static DateTimeOffset InicioDia(DateOnly fecha) =>
        new DateTimeOffset(fecha.ToDateTime(TimeOnly.MinValue), DesfaseGuatemala).ToUniversalTime();

    private static ProblemHttpResult DatosInvalidos(string titulo) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: titulo,
            type: "about:blank",
            extensions: new Dictionary<string, object?> { ["codigo"] = "datos_invalidos" });

    private sealed record Fila(
        DateTimeOffset Fecha,
        ActorTipo ActorTipo,
        Guid? ActorId,
        string ActorNombre,
        string Accion,
        string Descripcion);

    private sealed record ActorUsuario(Guid Id, Rol Rol, string Organizacion);

    private sealed record ActorConsumidor(Guid Id, string Organizacion);
}
