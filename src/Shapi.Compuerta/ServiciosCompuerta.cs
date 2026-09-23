using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;
using StackExchange.Redis;

namespace Shapi.Compuerta;

public static class ServiciosCompuerta
{
    /// <summary>Cadena de conexión de Redis (formato de StackExchange.Redis).</summary>
    public const string VariableRedis = "SHAPI_REDIS";

    public const string RedisPorDefecto = "localhost:6379";

    /// <summary>Registra Redis, el reenvío con YARP y los filtros en el orden de <see cref="TuberiaCompuerta.Orden"/>.</summary>
    public static IServiceCollection AgregarCompuerta(this IServiceCollection services)
    {
        // Una sola conexión multiplexada (08 §8). No aborta si Redis todavía no está listo al arrancar.
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var opciones = ConfigurationOptions.Parse(
                sp.GetRequiredService<IConfiguration>()[VariableRedis] ?? RedisPorDefecto);
            opciones.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(opciones);
        });

        services.AddHttpForwarder();
        services.AddSingleton(_ => ReenvioOrigen.CrearInvocador());
        services.AddSingleton<IReenvioOrigen, ReenvioOrigen>();

        foreach (var filtro in TuberiaCompuerta.Orden)
        {
            services.AddSingleton(filtro);
        }

        services.AddSingleton(sp => new TuberiaCompuerta(
            TuberiaCompuerta.Orden.Select(filtro => (IFiltroCompuerta)sp.GetRequiredService(filtro)),
            sp.GetRequiredService<IReenvioOrigen>()));
        return services;
    }
}
