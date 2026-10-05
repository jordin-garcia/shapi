using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Bitacora;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
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

    /// <summary>10 §7: el personal de la organización de plataforma se muestra como «Plataforma Shapi», como en B3.2.</summary>
    public const string NombrePlataforma = "Plataforma Shapi";

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
        // convenciones §5: el 400 lleva el error de cada parámetro en `errores`.
        var errores = new Dictionary<string, string[]>();
        if (!LeerFecha(peticion.Query["desde"], out var desde))
        {
            errores["desde"] = ["Use una fecha AAAA-MM-DD."];
        }
        if (!LeerFecha(peticion.Query["hasta"], out var hasta))
        {
            errores["hasta"] = ["Use una fecha AAAA-MM-DD."];
        }
        if (!LeerEntero(peticion.Query["pagina"], 1, int.MaxValue, 1, out var pagina))
        {
            errores["pagina"] = ["La página debe ser mayor que cero."];
        }
        if (!LeerEntero(peticion.Query["tamano"], 1, 100, 20, out var tamano))
        {
            errores["tamano"] = ["El tamaño debe estar entre 1 y 100."];
        }
        if (errores.Count > 0)
        {
            return DatosInvalidos(errores);
        }

        var hoy = DateOnly.FromDateTime(reloj.Ahora.ToOffset(DesfaseGuatemala).DateTime);
        hasta ??= hoy;
        if (hasta == DateOnly.MaxValue)
        {
            errores["hasta"] = ["La fecha no es válida."];
        }
        else if (desde is null)
        {
            if (hasta.Value < DateOnly.MinValue.AddDays(6))
            {
                errores["hasta"] = ["La fecha no es válida."];
            }
            else
            {
                desde = hasta.Value.AddDays(-6);
            }
        }
        if (desde > hasta)
        {
            errores["desde"] = ["El primer día no puede ser posterior al último."];
        }
        if (pagina - 1 > int.MaxValue / tamano)
        {
            errores["pagina"] = ["La página es demasiado grande."];
        }
        if (errores.Count > 0)
        {
            return DatosInvalidos(errores);
        }

        var inicio = InicioDia(desde!.Value);
        var finExclusivo = InicioDia(hasta!.Value.AddDays(1));
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
            select new ActorUsuario(membresia.UsuarioId, membresia.Rol, NombreVisible(organizacion.Nombre, organizacion.Tipo)))
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

    // Se evalúa en memoria, después de traer las filas: EF no traduce este método, así que va en la proyección final.
    private static string NombreVisible(string nombre, TipoOrganizacion tipo) =>
        tipo == TipoOrganizacion.Plataforma ? NombrePlataforma : nombre;

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

    private static ProblemHttpResult DatosInvalidos(Dictionary<string, string[]> errores) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "El periodo o la paginación no son válidos.",
            type: "about:blank",
            extensions: new Dictionary<string, object?> { ["codigo"] = CodigosError.DatosInvalidos, ["errores"] = errores });

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
