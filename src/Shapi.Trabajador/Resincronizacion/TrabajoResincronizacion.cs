namespace Shapi.Trabajador.Resincronizacion;

public sealed class OpcionesResincronizacion
{
    /// <summary>Cada cuánto se resincroniza la caché, además de al arrancar (07 §4).</summary>
    public TimeSpan Intervalo { get; init; } = TimeSpan.FromMinutes(5);
}

/// <summary>Ejecuta <see cref="ResincronizarCache"/> al arrancar el trabajador y luego cada 5 minutos (07 §4, RNF-05).</summary>
public sealed class TrabajoResincronizacion(
    IServiceScopeFactory alcances,
    OpcionesResincronizacion opciones,
    ILogger<TrabajoResincronizacion> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(opciones.Intervalo);
        do
        {
            await ResincronizarAsync(stoppingToken);
        }
        while (await EsperarAsync(temporizador, stoppingToken));
    }

    private async Task ResincronizarAsync(CancellationToken cancelacion)
    {
        try
        {
            await using var alcance = alcances.CreateAsyncScope();
            await alcance.ServiceProvider.GetRequiredService<ResincronizarCache>().EjecutarAsync(cancelacion);
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
        }
        catch (Exception excepcion)
        {
            // Si Redis o PostgreSQL no responden, se vuelve a intentar en el siguiente intervalo.
            registro.LogError(excepcion, "No se pudo resincronizar la caché de Redis; se reintenta en {Intervalo}", opciones.Intervalo);
        }
    }

    private static async Task<bool> EsperarAsync(PeriodicTimer temporizador, CancellationToken cancelacion)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(cancelacion);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
