using Shapi.Aplicacion.Portal;
using Shapi.Dominio.Apis;

namespace Shapi.Api.Portal;

/// <summary>Configuración pública del portal de marca blanca (RF-15 y RF-16).</summary>
public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsPortal(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/portal/configuracion", Configuracion)
            .AllowAnonymous();
        app.MapGet("/api/portal/logo", Logo)
            .AllowAnonymous();
        app.MapGet("/api/portal/documentacion", Documentacion)
            .AllowAnonymous();
        return app;
    }

    private static async Task<IResult> Configuracion(
        HttpRequest peticion,
        IResolutorPortal resolutor,
        CancellationToken cancelacion)
    {
        var portal = await resolutor.Resolver(peticion.Host.Host, cancelacion);
        if (portal is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ConfiguracionPortal(
            portal.NombrePortal,
            portal.ColorPrincipal,
            portal.Logo is null ? null : "/api/portal/logo",
            portal.Bienvenida,
            portal.NombreApi,
            portal.DescripcionApi,
            portal.HostPortal,
            portal.HostApi,
            portal.NombreOrganizacion));
    }

    private static async Task<IResult> Logo(
        HttpRequest peticion,
        HttpResponse respuesta,
        IResolutorPortal resolutor,
        CancellationToken cancelacion)
    {
        var portal = await resolutor.Resolver(peticion.Host.Host, cancelacion);
        if (portal?.Logo is null || portal.TipoLogo is null)
        {
            return TypedResults.NotFound();
        }

        respuesta.Headers.XContentTypeOptions = "nosniff";
        if (portal.TipoLogo == LogoTipo.Svg)
        {
            respuesta.Headers.ContentSecurityPolicy = "sandbox";
        }

        var tipoContenido = portal.TipoLogo == LogoTipo.Svg ? "image/svg+xml" : "image/png";
        return TypedResults.File(portal.Logo, tipoContenido);
    }

    private static async Task<IResult> Documentacion(
        HttpRequest peticion,
        IResolutorPortal resolutor,
        IConsultorDocumentacionPortal consultor,
        CancellationToken cancelacion)
    {
        var portal = await resolutor.Resolver(peticion.Host.Host, cancelacion);
        if (portal is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(await consultor.Consultar(portal, cancelacion));
    }

    private sealed record ConfiguracionPortal(
        string NombrePortal,
        string ColorPrincipal,
        string? UrlLogo,
        string? Bienvenida,
        string NombreApi,
        string? DescripcionApi,
        string HostPortal,
        string HostApi,
        string NombreOrganizacion);
}
