namespace Shapi.Compuerta.Filtros;

public interface IFiltroCompuerta
{
    ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto);
}
