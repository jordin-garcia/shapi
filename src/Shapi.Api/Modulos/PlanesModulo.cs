namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Planes. Solo lo edita su dueño (convenciones §2).</summary>
public static class PlanesModulo
{
    public static IServiceCollection AgregarModuloPlanes(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloPlanes(this WebApplication app)
    {
        return app;
    }
}
