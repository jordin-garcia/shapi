namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Soporte. Solo lo edita su dueño (convenciones §2).</summary>
public static class SoporteModulo
{
    public static IServiceCollection AgregarModuloSoporte(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloSoporte(this WebApplication app)
    {
        return app;
    }
}
