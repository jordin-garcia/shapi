namespace Shapi.Trabajador.VerificacionDominios;

public sealed class TrabajoVerificacionDominios(
    IServiceScopeFactory alcances, OpcionesVerificacionDominios opciones,
    ILogger<TrabajoVerificacionDominios> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(opciones.Intervalo);
        while (await Esperar(temporizador, stoppingToken))
        {
            try
            {
                await using var alcance = alcances.CreateAsyncScope();
                await alcance.ServiceProvider.GetRequiredService<VerificarDominiosPendientes>().EjecutarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) when (ex is StackExchange.Redis.RedisException or StackExchange.Redis.RedisTimeoutException)
            {
                registro.LogWarning("No se pudieron verificar los dominios; se reintenta en {Intervalo}: {Tipo}", opciones.Intervalo, ex.GetType().Name);
            }
            catch (Exception ex)
            {
                registro.LogError(ex, "No se pudieron verificar los dominios pendientes; se reintenta en {Intervalo}", opciones.Intervalo);
            }
        }
    }

    private static async Task<bool> Esperar(PeriodicTimer temporizador, CancellationToken ct)
    {
        try { return await temporizador.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
