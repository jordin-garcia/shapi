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

    private static IResult Problema(Error error)
    {
        var estado = error.Codigo switch
        {
            CodigosError.DatosInvalidos => StatusCodes.Status400BadRequest,
            CodigosError.SubdominioOcupado => StatusCodes.Status409Conflict,
            CodigosError.OrigenNoPermitido or CodigosError.OrigenInaccesible => StatusCodes.Status422UnprocessableEntity,
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
}
