using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Portal;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Planes;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsPlanes(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/apis/{apiId:guid}/planes");
        grupo.MapGet("", Listar).RequireAuthorization(Permisos.VerApis);
        grupo.MapPost("", Crear).RequireAuthorization(Permisos.ConfigurarApis);
        grupo.MapPut("/{planId:guid}", Editar).RequireAuthorization(Permisos.ConfigurarApis);
        grupo.MapPost("/{planId:guid}/desactivar", Desactivar).RequireAuthorization(Permisos.ConfigurarApis);

        app.MapGet("/api/portal/planes", PortalPlanes).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> Listar(
        Guid apiId,
        HttpContext contexto,
        ListarPlanes casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Ejecutar(apiId, OrganizacionId(contexto.User), cancelacion);
        return resultado.EsExito ? TypedResults.Ok(resultado.Valor) : Problema(resultado.Error);
    }

    private static async Task<IResult> Crear(
        Guid apiId,
        [FromBody] SolicitudPlanApi solicitud,
        HttpContext contexto,
        CrearPlan casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Ejecutar(apiId, OrganizacionId(contexto.User), solicitud, cancelacion);
        return resultado.EsExito ? TypedResults.Ok(resultado.Valor) : Problema(resultado.Error);
    }

    private static async Task<IResult> Editar(
        Guid apiId,
        Guid planId,
        [FromBody] SolicitudPlanApi solicitud,
        HttpContext contexto,
        EditarPlan casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Ejecutar(apiId, planId, OrganizacionId(contexto.User), solicitud, cancelacion);
        return resultado.EsExito ? TypedResults.Ok(resultado.Valor) : Problema(resultado.Error);
    }

    private static async Task<IResult> Desactivar(
        Guid apiId,
        Guid planId,
        HttpContext contexto,
        DesactivarPlan casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Ejecutar(apiId, planId, OrganizacionId(contexto.User), cancelacion);
        return resultado.EsExito ? TypedResults.Ok() : Problema(resultado.Error);
    }

    private static async Task<IResult> PortalPlanes(
        HttpContext contexto,
        ListarPlanes casoUso,
        IResolutorPortal resolutor,
        CancellationToken cancelacion)
    {
        var portal = await resolutor.Resolver(contexto.Request.Host.Host, cancelacion);
        if (portal is null)
        {
            return Problema(new Error(CodigosError.ApiNoEncontrada, "No se encontró el portal."));
        }

        var resultado = await casoUso.Ejecutar(portal.ApiId, portal.OrganizacionId, cancelacion);
        return resultado.EsExito ? TypedResults.Ok(resultado.Valor) : Problema(resultado.Error);
    }

    private static Guid OrganizacionId(ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion)!);

    private static IResult Problema(Error error)
    {
        var estado = error.Codigo switch
        {
            CodigosError.DatosInvalidos => StatusCodes.Status400BadRequest,
            CodigosError.ApiNoEncontrada => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };
        var extensiones = new Dictionary<string, object?> { ["codigo"] = error.Codigo };
        if (error.Detalle is not null)
        {
            extensiones["detalle"] = error.Detalle;
        }

        return TypedResults.Problem(
            statusCode: estado,
            title: error.Mensaje,
            type: "about:blank",
            extensions: extensiones);
    }
}
