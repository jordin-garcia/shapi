using Shapi.Compuerta.Contexto;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;
using Shapi.Compuerta.Rutas;
using Shapi.Contratos.Red;
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

        // YARP registra la URL de destino con su query ("Proxying to …"), que puede traer datos del consumidor
        // (08 §1). Solo se conservan sus advertencias y errores.
        services.AddLogging(registros => registros.AddFilter("Yarp.ReverseProxy.Forwarder.HttpForwarder", LogLevel.Warning));
        services.AddSingleton(TiemposOrigen.PorDefecto);
        services.AddSingleton<ValidadorDireccionOrigen>();
        services.AddSingleton(sp => ProteccionOrigen.DesdeConfiguracion(sp.GetRequiredService<IConfiguration>()));
        services.AddSingleton<ConexionOrigen>();
        services.AddSingleton(sp => ReenvioOrigen.CrearInvocador(
            sp.GetRequiredService<TiemposOrigen>(), sp.GetRequiredService<ConexionOrigen>()));
        services.AddSingleton<IReenvioOrigen, ReenvioOrigen>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<CacheRutas>();
        services.AddSingleton<ILectorContexto, LectorContexto>();

        foreach (var filtro in TuberiaCompuerta.Orden)
        {
            services.AddSingleton(filtro);
        }

        services.AddSingleton(sp => new TuberiaCompuerta(
            sp.GetRequiredService<ILectorContexto>(),
            TuberiaCompuerta.Orden.Select(filtro => (IFiltroCompuerta)sp.GetRequiredService(filtro)),
            sp.GetRequiredService<IReenvioOrigen>(),
            sp.GetRequiredService<ILogger<TuberiaCompuerta>>()));
        return services;
    }
}
