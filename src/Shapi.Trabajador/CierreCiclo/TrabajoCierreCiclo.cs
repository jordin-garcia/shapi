using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Trabajador.CierreCiclo;

public sealed class TrabajoCierreCiclo(
    IServiceScopeFactory alcances,
    IConnectionMultiplexer redis,
    OpcionesCierreCiclo opciones,
    ILogger<TrabajoCierreCiclo> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cola = await redis.GetSubscriber().SubscribeAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento));
        cola.OnMessage(async _ => await EjecutarSeguroAsync(stoppingToken));

        using var temporizador = new PeriodicTimer(opciones.Intervalo);
        try
        {
            do
            {
                await EjecutarSeguroAsync(stoppingToken);
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await cola.UnsubscribeAsync();
        }
    }

    private async Task EjecutarSeguroAsync(CancellationToken ct)
    {
        try
        {
            await using var alcance = alcances.CreateAsyncScope();
            await alcance.ServiceProvider.GetRequiredService<CerrarCiclo>().EjecutarAsync(ct);
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            registro.LogError(excepcion, "No se pudo cerrar el ciclo de suscripciones; se volverá a intentar en el siguiente intervalo");
        }
    }
}
