using Shapi.Infraestructura.Cache;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Cache. Solo lo edita su dueño (convenciones §2).</summary>
public static class CacheModulo
{
    /// <summary>El publicador de la configuración en Redis que usan los casos de uso de los demás módulos (JG-04).</summary>
    public static IServiceCollection AgregarModuloCache(this IServiceCollection services)
    {
        return services.AgregarCacheRedis();
    }

    public static WebApplication MapearModuloCache(this WebApplication app)
    {
        return app;
    }
}
