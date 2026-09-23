namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Portal. Solo lo edita su dueño (convenciones §2).</summary>
public static class PortalModulo
{
    public static IServiceCollection AgregarModuloPortal(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloPortal(this WebApplication app)
    {
        return app;
    }
}
