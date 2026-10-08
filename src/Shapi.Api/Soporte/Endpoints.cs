using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Soporte;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Soporte;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsSoporte(this IEndpointRouteBuilder app)
    {
        var proveedor = app.MapGroup("/api/casos").RequireAuthorization(Permisos.AdministrarCasos);
        proveedor.MapGet("", ListarProveedor);
        proveedor.MapPost("", AbrirProveedor);
        proveedor.MapGet("/{numero:int}", ConsultarProveedor);
        proveedor.MapPost("/{numero:int}/mensajes", ResponderProveedor);

        var administracion = app.MapGroup("/api/admin/casos").RequireAuthorization(Permisos.AdministrarCasosSoporte);
        administracion.MapGet("", ListarAdministracion);
        administracion.MapPost("", AbrirAdministracion);
        administracion.MapGet("/organizaciones", ListarOrganizaciones);
        administracion.MapGet("/{numero:int}", ConsultarAdministracion);
        administracion.MapPost("/{numero:int}/asignar", Asignar);
        administracion.MapPost("/{numero:int}/mensajes", ResponderAdministracion);
        administracion.MapPost("/{numero:int}/cerrar", Cerrar);
        administracion.MapGet("/{numero:int}/organizacion", ConsultarOrganizacion)
            .RequireAuthorization(Permisos.VerDatosOrganizacionSoporte);
        return app;
    }

    private static async Task<IResult> ListarProveedor(
        HttpContext http, ListarCasos casoUso, CancellationToken cancelacion) =>
        TypedResults.Ok(await casoUso.Proveedor(OrganizacionId(http.User), cancelacion));

    private static async Task<IResult> AbrirProveedor(
        HttpContext http,
        [FromBody] SolicitudAbrirCaso solicitud,
        AbrirCaso casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Proveedor(
            OrganizacionId(http.User), UsuarioId(http.User), Nombre(http.User), solicitud, Ip(http), cancelacion);
        return resultado.EsExito
            ? TypedResults.Created($"/api/casos/{resultado.Valor.Numero}", resultado.Valor)
            : Problema(resultado.Error);
    }

    private static async Task<IResult> ConsultarProveedor(
        int numero, HttpContext http, ConsultarCaso casoUso, CancellationToken cancelacion)
    {
        var caso = await casoUso.Proveedor(numero, OrganizacionId(http.User), cancelacion);
        return caso is null ? NoEncontrado() : TypedResults.Ok(caso);
    }

    private static async Task<IResult> ResponderProveedor(
        int numero,
        HttpContext http,
        [FromBody] SolicitudMensajeCaso solicitud,
        ResponderCaso casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Proveedor(
            numero, OrganizacionId(http.User), UsuarioId(http.User), solicitud, cancelacion);
        if (!resultado.EsExito)
        {
            return Problema(resultado.Error);
        }

        return resultado.Valor is null
            ? TypedResults.NotFound()
            : TypedResults.Created($"/api/casos/{numero}", resultado.Valor);
    }

    private static async Task<IResult> ListarAdministracion(
        ListarCasos casoUso, CancellationToken cancelacion) =>
        TypedResults.Ok(await casoUso.Administracion(cancelacion));

    private static async Task<IResult> ListarOrganizaciones(
        ListarOrganizacionesParaCaso casoUso, CancellationToken cancelacion) =>
        TypedResults.Ok(await casoUso.Ejecutar(cancelacion));

    private static async Task<IResult> AbrirAdministracion(
        HttpContext http,
        [FromBody] SolicitudAbrirCasoAdministracion solicitud,
        AbrirCaso casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Administracion(
            UsuarioId(http.User), Nombre(http.User), solicitud, Ip(http), cancelacion);
        return resultado.EsExito
            ? TypedResults.Created($"/api/admin/casos/{resultado.Valor.Numero}", resultado.Valor)
            : Problema(resultado.Error);
    }

    private static async Task<IResult> ConsultarAdministracion(
        int numero, ConsultarCaso casoUso, CancellationToken cancelacion)
    {
        var caso = await casoUso.Administracion(numero, cancelacion);
        return caso is null ? NoEncontrado() : TypedResults.Ok(caso);
    }

    private static async Task<IResult> Asignar(
        int numero, HttpContext http, AsignarCaso casoUso, CancellationToken cancelacion) =>
        await casoUso.Ejecutar(numero, UsuarioId(http.User), cancelacion)
            ? TypedResults.NoContent()
            : NoEncontrado();

    private static async Task<IResult> ResponderAdministracion(
        int numero,
        HttpContext http,
        [FromBody] SolicitudMensajeCaso solicitud,
        ResponderCaso casoUso,
        CancellationToken cancelacion)
    {
        var resultado = await casoUso.Administracion(numero, UsuarioId(http.User), solicitud, cancelacion);
        if (!resultado.EsExito)
        {
            return Problema(resultado.Error);
        }

        return resultado.Valor is null
            ? TypedResults.NotFound()
            : TypedResults.Created($"/api/admin/casos/{numero}", resultado.Valor);
    }

    private static async Task<IResult> Cerrar(
        int numero, HttpContext http, CerrarCaso casoUso, CancellationToken cancelacion) =>
        await casoUso.Ejecutar(numero, UsuarioId(http.User), Nombre(http.User), Ip(http), cancelacion)
            ? TypedResults.NoContent()
            : NoEncontrado();

    private static async Task<IResult> ConsultarOrganizacion(
        int numero, ConsultarOrganizacionCaso casoUso, CancellationToken cancelacion)
    {
        var resumen = await casoUso.Ejecutar(numero, cancelacion);
        return resumen is null ? NoEncontrado() : TypedResults.Ok(resumen);
    }

    private static IResult Problema(Error error)
    {
        var estado = error.Codigo switch
        {
            CodigosError.DatosInvalidos => StatusCodes.Status400BadRequest,
            CodigosError.CasoCerrado => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };
        return Problemas.Crear(estado, error.Codigo, error.Mensaje, error.Detalle as IDictionary<string, string[]>);
    }

    private static IResult NoEncontrado() => TypedResults.NotFound();

    private static Guid OrganizacionId(ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion)!);

    private static Guid UsuarioId(ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string Nombre(ClaimsPrincipal usuario) =>
        usuario.FindFirstValue(ClaimTypes.Name) ?? usuario.FindFirstValue(ClaimTypes.Email) ?? "Usuario";

    private static string? Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();
}
