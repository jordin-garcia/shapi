using System.Globalization;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Medicion;

public sealed class LatidoCompuerta(
    IConnectionMultiplexer redis, TimeProvider reloj, ILogger<LatidoCompuerta> registro) : BackgroundService
{
    public string Instancia { get; } = Guid.NewGuid().ToString();

    public Task EscribirAsync(CancellationToken cancelacion) => redis.GetDatabase().StringSetAsync(
        LlavesRedis.SaludCompuerta(Instancia), reloj.GetUtcNow().ToString("O", CultureInfo.InvariantCulture),
        TimeSpan.FromSeconds(30)).WaitAsync(cancelacion);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(10), reloj);
        try
        {
            do
            {
                try
                {
                    await EscribirAsync(stoppingToken);
                }
                catch (RedisException excepcion)
                {
                    registro.LogWarning("No se pudo escribir el latido de la compuerta: {Tipo}", excepcion.GetType().Name);
                }
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
