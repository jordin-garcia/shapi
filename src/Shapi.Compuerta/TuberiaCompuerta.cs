using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;

namespace Shapi.Compuerta;

/// <summary>
/// La tubería de la compuerta (08 §3): evalúa los filtros en orden y, si todos dejan pasar la petición,
/// la reenvía al origen. El primer rechazo detiene la cadena y se responde con el error JSON.
/// </summary>
public sealed class TuberiaCompuerta(IEnumerable<IFiltroCompuerta> filtros, IReenvioOrigen reenvio)
{
    private readonly IFiltroCompuerta[] _filtros = [.. filtros];

    /// <summary>
    /// El orden de los filtros, definido solo aquí. Agregar una regla es agregar su clase y una línea (RNF-13).
    /// </summary>
    public static IReadOnlyList<Type> Orden { get; } =
    [
        typeof(FiltroApi), // 1 · host → API publicada
        typeof(FiltroClave), // 2 · X-Api-Key → hash → Redis
    ];

    public async Task ProcesarAsync(HttpContext http)
    {
        var contexto = new ContextoPeticion(http);
        foreach (var filtro in _filtros)
        {
            var resultado = await filtro.EvaluarAsync(contexto);
            if (!resultado.Continua)
            {
                await RespuestaError.EscribirAsync(http, resultado);
                return;
            }
        }

        await reenvio.ReenviarAsync(contexto);
    }
}
