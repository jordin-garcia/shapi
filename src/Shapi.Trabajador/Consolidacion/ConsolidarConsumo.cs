using Shapi.Infraestructura.Consumo;

namespace Shapi.Trabajador.Consolidacion;

public sealed class ConsolidarConsumo(LotesMetricasRedis lotes, RepositorioConsolidacion repositorio)
{
    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        // Se recupera también en cada intervalo: una caída temporal de PostgreSQL no requiere reiniciar el proceso.
        foreach (var lote in await lotes.LotesPendientesAsync(cancelacion))
        {
            await AplicarAsync(lote, cancelacion);
        }
        while (true)
        {
            cancelacion.ThrowIfCancellationRequested();
            var lote = Guid.NewGuid();
            if (await lotes.SepararAsync(lote, cancelacion) == 0)
            {
                break;
            }
            await AplicarAsync(lote, cancelacion);
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
