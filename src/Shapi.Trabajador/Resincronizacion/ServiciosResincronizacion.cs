using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Infraestructura.Cache;

namespace Shapi.Trabajador.Resincronizacion;

public static class ServiciosResincronizacion
{
    /// <summary>
    /// Registra el publicador de la caché (que el trabajador también usa al cerrar ciclos) y la resincronización
    /// periódica de Redis.
    /// </summary>
    public static IServiceCollection AgregarResincronizacion(this IServiceCollection services)
    {
        services.AgregarCacheRedis();
        services.TryAddSingleton(new OpcionesResincronizacion());
        services.AddScoped<ResincronizarCache>();
        services.AddHostedService<TrabajoResincronizacion>();
        return services;
    }
}
