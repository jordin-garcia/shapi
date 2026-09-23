namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Una regla de la compuerta (08 §3). Deja pasar la petición o la detiene con un error; no conoce a los demás
/// filtros. Agregar una regla es agregar una clase y una línea en <see cref="TuberiaCompuerta.Orden"/> (RNF-13).
/// </summary>
public interface IFiltroCompuerta
{
    ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto);
}
