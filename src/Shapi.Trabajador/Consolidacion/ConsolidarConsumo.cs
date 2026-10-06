using System.Runtime.ExceptionServices;
using Shapi.Infraestructura.Consumo;

namespace Shapi.Trabajador.Consolidacion;

public sealed class ConsolidarConsumo(LotesMetricasRedis lotes, RepositorioConsolidacion repositorio)
{
    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        Exception? fallo = null;
        async Task<bool> IntentarAsync(Guid lote)
        {
            try
            {
                await AplicarAsync(lote, cancelacion);
                return true;
            }
            catch (Exception excepcion) when (excepcion is not OperationCanceledException)
            {
                // Conservar el lote fallido y procesar los demás. El trabajo registra el error al final del ciclo.
                fallo ??= excepcion;
                return false;
            }
        }
        // Se recupera también en cada intervalo: una caída temporal de PostgreSQL no requiere reiniciar el proceso.
        foreach (var lote in await lotes.LotesPendientesAsync(cancelacion))
        {
            await IntentarAsync(lote);
        }
        // Una sola lectura acota el ciclo incluso con tráfico continuo. Las llaves nuevas esperan al próximo intervalo.
        var pendientes = await lotes.LlavesPendientesAsync(cancelacion);
        foreach (var llaves in pendientes.Chunk(256))
        {
            cancelacion.ThrowIfCancellationRequested();
            var lote = Guid.NewGuid();
            if (await lotes.SepararAsync(lote, llaves, cancelacion) == 0)
            {
                continue;
            }
            if (!await IntentarAsync(lote))
            {
                // Si PostgreSQL falla, el resto permanece en met:pendientes en vez de multiplicar instantáneas.
                break;
            }
        }
        if (fallo is not null)
        {
            ExceptionDispatchInfo.Capture(fallo).Throw();
        }
    }

    private async Task AplicarAsync(Guid lote, CancellationToken cancelacion)
    {
        if (!await repositorio.YaAplicadoAsync(lote, cancelacion))
        {
            await repositorio.GuardarAsync(lote, await lotes.LeerAsync(lote, cancelacion), cancelacion);
        }
        await lotes.BorrarAsync(lote, cancelacion);
    }
}
