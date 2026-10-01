namespace Shapi.Compuerta.Filtros;

public sealed class FiltroSuscripcion : IFiltroCompuerta
{
    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto) => ValueTask.FromResult(ResultadoFiltro.Continuar);
}
