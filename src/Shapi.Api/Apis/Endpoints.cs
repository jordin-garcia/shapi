using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Apis;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsApis(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/apis");
        grupo.MapGet("", Listar).RequireAuthorization(Permisos.VerApis);
        grupo.MapPost("", Registrar).RequireAuthorization(Permisos.ConfigurarApis);
        grupo.MapPut("/{id:guid}/especificacion", CargarEspecificacionArchivo)
            .DisableAntiforgery()
            .RequireAuthorization(Permisos.ConfigurarApis);
        // 04: el lector puede ver las APIs y sus rutas; solo cambiarlas exige ConfigurarApis.
        grupo.MapGet("/{id:guid}/rutas", ObtenerRutas).RequireAuthorization(Permisos.VerApis);
        grupo.MapPut("/{id:guid}/rutas/exposicion", ActualizarExposicion)
            .RequireAuthorization(Permisos.ConfigurarApis);
        return app;
    }

    private static async Task<IResult> Listar(
        HttpContext contexto,
        ListarApis casoUso,
        CancellationToken cancelacion)
    {
        var lista = await casoUso.Ejecutar(OrganizacionId(contexto.User), cancelacion);
        return TypedResults.Ok(new RespuestaListaApis(
            lista.Elementos.Select(a => new RespuestaApiListado(
                a.Id,
                a.Nombre,
                a.Subdominio,
                a.Estado.ToString().ToLowerInvariant())).ToArray(),
            lista.Total,
            lista.PlanNombre,
            lista.MaxApis));
    }

    private static async Task<IResult> Registrar(
        [FromBody] SolicitudRegistroApi solicitud,
        HttpContext contexto,
        RegistrarApi casoUso,
        CancellationToken cancelacion)
    {
        var usuarioId = Guid.Parse(contexto.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var actor = new ActorRegistroApi(
            usuarioId,
            contexto.User.FindFirstValue(ClaimTypes.Name)
                ?? contexto.User.FindFirstValue(ClaimTypes.Email)
                ?? "Usuario",
            contexto.Connection.RemoteIpAddress?.ToString());
        var resultado = await casoUso.Ejecutar(OrganizacionId(contexto.User), actor, solicitud, cancelacion);
        if (!resultado.EsExito)
        {
            return Problema(resultado.Error);
        }

        var api = resultado.Valor;
        return TypedResults.Created(
            $"/api/apis/{api.Id}/especificacion",
            new RespuestaApiRegistrada(
                api.Id,
                api.Nombre,
                api.Subdominio,
                api.Estado.ToString().ToLowerInvariant(),
                api.SecretoOrigen,
                api.ConexionMilisegundos));
    }

    private static Guid OrganizacionId(ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion)!);

    private static async Task<IResult> CargarEspecificacionArchivo(
        Guid id,
        IFormFile? archivo,
        HttpContext contexto,
        CargarEspecificacion casoUso,
        CancellationToken cancelacion)
    {
        if (archivo is null)
        {
            return Problema(new Error(
                CodigosError.DatosInvalidos,
                "Revise los datos del formulario.",
                new Dictionary<string, string[]> { ["archivo"] = ["Elija el archivo de la especificación."] }));
        }

        if (archivo.Length > CargarEspecificacion.MaximoBytes)
        {
            return Problema(new Error(
                CodigosError.EspecificacionInvalida,
                "La especificación OpenAPI no es válida.",
                new { ubicacion = "archivo", mensaje = "El archivo no puede superar 2 MB." }));
        }

        using var lector = new StreamReader(archivo.OpenReadStream());
        var contenido = await lector.ReadToEndAsync(cancelacion);
        var resultado = await casoUso.Ejecutar(id, OrganizacionId(contexto.User), archivo.FileName, contenido, cancelacion);
        return resultado.EsExito ? TypedResults.Ok(Respuesta(resultado.Valor)) : Problema(resultado.Error);
    }

    private static async Task<IResult> ObtenerRutas(
        Guid id,
        HttpContext contexto,
        ListarRutas casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Ejecutar(id, OrganizacionId(contexto.User), cancelacion);
        return resultado.EsExito ? TypedResults.Ok(Respuesta(resultado.Valor)) : Problema(resultado.Error);
    }

    private static async Task<IResult> ActualizarExposicion(
        Guid id,
        [FromBody] CambioExposicionRuta?[] cambios,
        HttpContext contexto,
        ActualizarExposicionRutas casoUso,
        CancellationToken cancelacion)
    {
        var usuarioId = Guid.Parse(contexto.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var actor = new ActorRegistroApi(
            usuarioId,
            contexto.User.FindFirstValue(ClaimTypes.Name)
                ?? contexto.User.FindFirstValue(ClaimTypes.Email)
                ?? "Usuario",
            contexto.Connection.RemoteIpAddress?.ToString());
        var resultado = await casoUso.Ejecutar(
            id,
            OrganizacionId(contexto.User),
            new SolicitudExposicionRutas(cambios, actor),
            cancelacion);
        return resultado.EsExito ? TypedResults.Ok(Respuesta(resultado.Valor)) : Problema(resultado.Error);
    }

    private static RespuestaListaRutas Respuesta(ListaRutas lista) => new(
        lista.ApiId,
        lista.ApiNombre,
        lista.Elementos.Select(Respuesta).ToArray(),
        lista.TotalExpuestas,
        lista.TotalOcultas,
        lista.Especificacion is null
            ? null
            : new RespuestaResumenEspecificacion(
                lista.Especificacion.Titulo,
                lista.Especificacion.Version,
                lista.Especificacion.VersionOpenApi,
                lista.Especificacion.Formato,
                lista.Especificacion.CargadaEn,
                lista.Especificacion.TamanoBytes));

    private static RespuestaEspecificacionCargada Respuesta(EspecificacionCargada especificacion) => new(
        especificacion.ApiId,
        especificacion.ApiNombre,
        especificacion.Titulo,
        especificacion.Descripcion,
        especificacion.Version,
        especificacion.VersionOpenApi,
        especificacion.Formato,
        especificacion.CargadaEn,
        especificacion.TotalRutas,
        especificacion.Rutas.Select(Respuesta).ToArray());

    private static RespuestaRuta Respuesta(RutaAdministrada ruta) => new(
        ruta.Id, ruta.Metodo, ruta.Patron, ruta.Resumen, ruta.Descripcion, ruta.Expuesta);

    private static IResult Problema(Error error)
    {
        var estado = error.Codigo switch
        {
            CodigosError.DatosInvalidos => StatusCodes.Status400BadRequest,
            CodigosError.ApiNoEncontrada => StatusCodes.Status404NotFound,
            CodigosError.SubdominioOcupado => StatusCodes.Status409Conflict,
            CodigosError.OrigenNoPermitido or CodigosError.OrigenInaccesible or CodigosError.EspecificacionInvalida
                => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };
        var extensiones = new Dictionary<string, object?> { ["codigo"] = error.Codigo };
        if (error.Codigo == CodigosError.DatosInvalidos && error.Detalle is IDictionary<string, string[]> errores)
        {
            extensiones["errores"] = errores;
        }
        else if (error.Detalle is not null)
        {
            extensiones["detalle"] = error.Detalle;
        }

        return TypedResults.Problem(
            statusCode: estado,
            title: error.Mensaje,
            type: "about:blank",
            extensions: extensiones);
    }

    private sealed record RespuestaApiListado(Guid Id, string Nombre, string Subdominio, string Estado);

    private sealed record RespuestaListaApis(
        IReadOnlyList<RespuestaApiListado> Elementos,
        int Total,
        string PlanNombre,
        int? MaxApis);

    private sealed record RespuestaApiRegistrada(
        Guid Id,
        string Nombre,
        string Subdominio,
        string Estado,
        string SecretoOrigen,
        long ConexionMilisegundos);

    private sealed record RespuestaRuta(
        Guid Id,
        string Metodo,
        string Patron,
        string? Resumen,
        string? Descripcion,
        bool Expuesta);

    private sealed record RespuestaResumenEspecificacion(
        string Titulo,
        string Version,
        string VersionOpenApi,
        string Formato,
        DateTimeOffset CargadaEn,
        int TamanoBytes);

    private sealed record RespuestaListaRutas(
        Guid ApiId,
        string ApiNombre,
        IReadOnlyList<RespuestaRuta> Elementos,
        int TotalExpuestas,
        int TotalOcultas,
        RespuestaResumenEspecificacion? Especificacion);

    private sealed record RespuestaEspecificacionCargada(
        Guid ApiId,
        string ApiNombre,
        string Titulo,
        string? Descripcion,
        string Version,
        string VersionOpenApi,
        string Formato,
        DateTimeOffset CargadaEn,
        int TotalRutas,
        IReadOnlyList<RespuestaRuta> Rutas);
}
