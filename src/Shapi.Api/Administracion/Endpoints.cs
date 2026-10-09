using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Administracion;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Administracion;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsAdministracion(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/admin/organizaciones");
        grupo.MapGet("", Listar).RequireAuthorization(Permisos.VerOrganizaciones);
        grupo.MapPost("/{id:guid}/suspender", Suspender).RequireAuthorization(Permisos.AdministrarOrganizaciones);
        grupo.MapPost("/{id:guid}/reactivar", Reactivar).RequireAuthorization(Permisos.AdministrarOrganizaciones);
        return app;
    }

    private static async Task<IResult> Listar(GestionarOrganizaciones servicio, CancellationToken cancelacion) =>
        TypedResults.Ok(await servicio.Listar(cancelacion));

    private static async Task<IResult> Suspender(
        Guid id,
        [FromBody] SolicitudSuspenderOrganizacion solicitud,
        HttpContext http,
        GestionarOrganizaciones servicio,
        CancellationToken cancelacion) =>
        Respuesta(await servicio.Suspender(id, solicitud.Motivo, UsuarioId(http.User), Nombre(http.User), Ip(http), cancelacion));

    private static async Task<IResult> Reactivar(
        Guid id,
        HttpContext http,
        GestionarOrganizaciones servicio,
        CancellationToken cancelacion) =>
        Respuesta(await servicio.Reactivar(id, UsuarioId(http.User), Nombre(http.User), Ip(http), cancelacion));

    private static IResult Respuesta(ResultadoCambioOrganizacion resultado) => resultado switch
    {
        ResultadoCambioOrganizacion.Exito => TypedResults.NoContent(),
        ResultadoCambioOrganizacion.MotivoInvalido => Problemas.Crear(
            StatusCodes.Status400BadRequest,
            CodigosError.DatosInvalidos,
            "El motivo de la suspensión es obligatorio.",
            new Dictionary<string, string[]> { ["motivo"] = ["Escriba el motivo administrativo."] }),
        _ => Problemas.Crear(
            StatusCodes.Status404NotFound,
            CodigosError.DatosInvalidos,
            "No se encontró la organización."),
    };

    private static Guid UsuarioId(ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string Nombre(ClaimsPrincipal usuario) =>
        usuario.FindFirstValue(ClaimTypes.Name) ?? usuario.FindFirstValue(ClaimTypes.Email) ?? "Usuario";

    private static string? Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();
}
