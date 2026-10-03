using StackExchange.Redis;

namespace Shapi.Compuerta;

/// <summary>
/// Abre la conexión a Redis al arrancar la compuerta, antes de atender peticiones (08 §8). Si se abriera con la primera
/// petición, varias peticiones simultáneas quedarían bloqueadas esperando al singleton mientras
/// <c>ConnectionMultiplexer.Connect</c>, que es síncrono, necesita hilos del pool para terminar: el pool se agotaba
/// durante segundos y todo el proceso se frenaba (JG-18, pruebas de la compuerta en la CI del PR #59).
/// </summary>
public sealed class ConexionRedisAlArrancar(IServiceProvider servicios) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        servicios.GetRequiredService<IConnectionMultiplexer>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
