using Shapi.Dominio.Consumo;

namespace Shapi.Aplicacion.Consumo;

/// <summary>Consultas base del consumo consolidado, con periodo inclusivo y aislamiento de organización.</summary>
public interface IConsultaConsumo
{
    Task<ConsumoPeriodo> PorApiAsync(Guid organizacionId, Guid apiId, DateOnly desde, DateOnly hasta,
        EntornoConsumo? entorno = null, CancellationToken cancelacion = default);
    Task<ConsumoPeriodo> PorSuscripcionAsync(Guid organizacionId, Guid suscripcionId, DateOnly desde, DateOnly hasta,
        EntornoConsumo? entorno = null, CancellationToken cancelacion = default);
}

public sealed record ConsumoPeriodo(IReadOnlyList<ConsumoDiario> Dias)
{
    public long Peticiones => Dias.Sum(x => x.Peticiones);
    public long Llamadas => Dias.Sum(x => x.Llamadas);
    public Percentil95 LatenciaTotalP95 => HistogramaLatencia.CalcularP95(Dias.Select(x => (IReadOnlyList<int>)x.HistLatenciaTotal));
    public Percentil95 LatenciaCompuertaP95 => HistogramaLatencia.CalcularP95(Dias.Select(x => (IReadOnlyList<int>)x.HistLatenciaCompuerta));
}
