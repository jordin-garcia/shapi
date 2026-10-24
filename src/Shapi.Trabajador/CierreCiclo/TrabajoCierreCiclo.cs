using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shapi.Infraestructura.Comun;
using StackExchange.Redis;

namespace Shapi.Trabajador.CierreCiclo;

public sealed class TrabajoCierreCiclo(
    IServiceScopeFactory alcances,
    RelojSistema reloj,
    OpcionesCierreCiclo opciones,
    ILogger<TrabajoCierreCiclo> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var solicitudes = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });
        void Solicitar() => solicitudes.Writer.TryWrite(true);
        reloj.DesplazamientoActualizado += Solicitar;
        try
        {
            do
            {
                await EjecutarSeguroAsync(stoppingToken);
                using var espera = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                espera.CancelAfter(opciones.Intervalo);
                try
                {
                    await solicitudes.Reader.ReadAsync(espera.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Sin mensajes, se cierra el ciclo en el intervalo normal.
                }
            }
            while (!stoppingToken.IsCancellationRequested);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            reloj.DesplazamientoActualizado -= Solicitar;
            solicitudes.Writer.TryComplete();
        }
    }

    private async Task EjecutarSeguroAsync(CancellationToken ct)
    {
        try
        {
            await using var alcance = alcances.CreateAsyncScope();
            await alcance.ServiceProvider.GetRequiredService<CerrarCiclo>().EjecutarAsync(ct);
        }
        catch (Exception excepcion) when (excepcion is RedisException or RedisTimeoutException)
        {
            registro.LogWarning("No se pudo cerrar el ciclo; se reintenta en el siguiente intervalo: {Tipo}", excepcion.GetType().Name);
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            registro.LogError(excepcion, "No se pudo cerrar el ciclo de suscripciones; se volverá a intentar en el siguiente intervalo");
        }
    }
}
