using Shapi.Api.Planes;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Planes. Solo lo edita su dueño (convenciones §2).</summary>
public static class PlanesModulo
{
    public static IServiceCollection AgregarModuloPlanes(this IServiceCollection services)
    {
        services.AddScoped<ListarPlanes>();
        services.AddScoped<CrearPlan>();
        services.AddScoped<EditarPlan>();
        services.AddScoped<DesactivarPlan>();
        return services;
    }

    public static WebApplication MapearModuloPlanes(this WebApplication app)
    {
        app.MapearEndpointsPlanes();
        return app;
    }
}
