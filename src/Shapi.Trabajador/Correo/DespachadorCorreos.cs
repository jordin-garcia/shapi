namespace Shapi.Trabajador.Correo;

public sealed class DespachadorCorreos(
    IServiceScopeFactory fabricaAlcances,
    TimeProvider proveedorTiempo,
    ILogger<DespachadorCorreos> registro) : BackgroundService
{
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var alcance = fabricaAlcances.CreateAsyncScope();
                var procesador = alcance.ServiceProvider.GetRequiredService<ProcesadorCorreos>();
                await procesador.ProcesarPendientes(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception excepcion)
            {
                registro.LogError(excepcion, "No se pudo procesar la bandeja de salida");
            }

            await Task.Delay(Intervalo, proveedorTiempo, stoppingToken);
        }
    }
}
