using System.Runtime.ExceptionServices;
using Shapi.Infraestructura.Consumo;

namespace Shapi.Trabajador.Consolidacion;

public sealed class ConsolidarConsumo(LotesMetricasRedis lotes, RepositorioConsolidacion repositorio)
{
    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        Exception? fallo = null;
        async Task IntentarAsync(Guid lote)
        {
            try
            {
                await AplicarAsync(lote, cancelacion);
            }
            catch (Exception excepcion) when (excepcion is not OperationCanceledException)
            {
                // Conservar el lote fallido y procesar los demás. El trabajo registra el error al final del ciclo.
                fallo ??= excepcion;
            }
        }
        // Se recupera también en cada intervalo: una caída temporal de PostgreSQL no requiere reiniciar el proceso.
        foreach (var lote in await lotes.LotesPendientesAsync(cancelacion))
        {
            await IntentarAsync(lote);
        }
        while (true)
        {
            cancelacion.ThrowIfCancellationRequested();
            var lote = Guid.NewGuid();
            if (await lotes.SepararAsync(lote, cancelacion) == 0)
            {
                break;
            }
            await IntentarAsync(lote);
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
