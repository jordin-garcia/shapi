namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Cache. Solo lo edita su dueño (convenciones §2).</summary>
public static class CacheModulo
{
    public static IServiceCollection AgregarModuloCache(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloCache(this WebApplication app)
    {
        return app;
    }
}
