using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Aplicacion.Comun;
using StackExchange.Redis;

namespace Shapi.Infraestructura.Cache;

public static class ServiciosCache
{
    /// <summary>Cadena de conexión de Redis (formato de StackExchange.Redis), la misma que usa la compuerta.</summary>
    public const string VariableRedis = "SHAPI_REDIS";

    public const string RedisPorDefecto = "localhost:6379";

    /// <summary>
    /// Registra la conexión a Redis y el publicador de la caché (<see cref="IPublicadorCache"/>). Lo usan la API de
    /// control (<c>CacheModulo</c>) y el trabajador.
    /// </summary>
    public static IServiceCollection AgregarCacheRedis(this IServiceCollection services)
    {
        // Una sola conexión multiplexada. No aborta si Redis no está listo al arrancar, y falla de inmediato mientras
        // no hay conexión (en vez de encolar el comando hasta que se agote el tiempo), para que los reintentos del
        // publicador no alarguen la respuesta HTTP. Los mensajes de error no llevan los nombres de las llaves, que
        // incluyen el hash de las claves (10 §3).
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var opciones = ConfigurationOptions.Parse(
                sp.GetRequiredService<IConfiguration>()[VariableRedis] ?? RedisPorDefecto);
            opciones.AbortOnConnectFail = false;
            opciones.BacklogPolicy = BacklogPolicy.FailFast;
            opciones.IncludeDetailInExceptions = false;

            // Si Redis se cuelga sin cortar la conexión, cada intento espera a lo más 1 s (y no 5 s).
            opciones.AsyncTimeout = 1000;
            opciones.SyncTimeout = 1000;
            return ConnectionMultiplexer.Connect(opciones);
        });

        services.TryAddSingleton(new OpcionesCacheRedis());
        services.TryAddSingleton<EscritorCacheRedis>();
        services.TryAddScoped<LectorCacheBaseDatos>();
        services.AddScoped<IPublicadorCache, PublicadorCacheRedis>();
        return services;
    }
}
