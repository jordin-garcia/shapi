using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;

namespace Shapi.Compuerta;

public sealed class TuberiaCompuerta(IEnumerable<IFiltroCompuerta> filtros, IReenvioOrigen reenvio)
{
    public static IReadOnlyList<Type> Orden { get; } = [];
    public Task ProcesarAsync(HttpContext http) => throw new NotImplementedException(filtros.ToString() + reenvio);
}
