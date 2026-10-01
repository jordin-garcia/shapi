namespace Shapi.Compuerta.Filtros;

public sealed class FiltroRuta : IFiltroCompuerta
{
    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto) => ValueTask.FromResult(ResultadoFiltro.Continuar);
}
