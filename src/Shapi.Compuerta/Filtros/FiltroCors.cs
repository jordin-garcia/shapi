namespace Shapi.Compuerta.Filtros;

public sealed class FiltroCors : IFiltroCompuerta
{
    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto) => ValueTask.FromResult(ResultadoFiltro.Continuar);
}
