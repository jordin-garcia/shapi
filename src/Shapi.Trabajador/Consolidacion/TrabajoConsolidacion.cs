using System.Globalization;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Trabajador.Consolidacion;

public sealed class OpcionesConsolidacion
{
    public TimeSpan Intervalo { get; init; } = TimeSpan.FromSeconds(10);
}

public sealed class TrabajoConsolidacion(
    IServiceScopeFactory alcances, OpcionesConsolidacion opciones,
    ILogger<TrabajoConsolidacion> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(opciones.Intervalo);
        try
        {
            do
            {
                try
                {
                    await using var alcance = alcances.CreateAsyncScope();
                    await alcance.ServiceProvider.GetRequiredService<ConsolidarConsumo>().EjecutarAsync(stoppingToken);
                }
                catch (Exception excepcion) when (excepcion is not OperationCanceledException)
                {
                    registro.LogError(excepcion, "No se pudo consolidar el consumo; el lote se recuperará en el siguiente intervalo");
                }
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}

// Independiente de la consolidación: un trabajo lento o PostgreSQL caído no debe apagar el latido del proceso.
public sealed class LatidoTrabajador(
    IConnectionMultiplexer redis, IReloj reloj, OpcionesConsolidacion opciones,
    ILogger<LatidoTrabajador> registro) : BackgroundService
{
    public Task EscribirAsync(CancellationToken cancelacion) => redis.GetDatabase().StringSetAsync(
        LlavesRedis.SaludTrabajador, reloj.Ahora.ToString("O", CultureInfo.InvariantCulture), TimeSpan.FromSeconds(30))
        .WaitAsync(cancelacion);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(opciones.Intervalo);
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
                    registro.LogWarning("No se pudo escribir el latido del trabajador: {Tipo}", excepcion.GetType().Name);
                }
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
