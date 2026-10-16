using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shapi.Trabajador.AvisosCuota;

public sealed class TrabajoAvisosCuota(IServiceScopeFactory alcances, ILogger<TrabajoAvisosCuota> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            do
            {
                try
                {
                    await using var alcance = alcances.CreateAsyncScope();
                    await alcance.ServiceProvider.GetRequiredService<AvisosCuotaPlataforma>().EjecutarAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    registro.LogError(ex, "No se pudieron enviar los avisos de cuota; se volverá a intentar en el siguiente intervalo");
                }
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
