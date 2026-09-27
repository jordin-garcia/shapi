using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;
using Shapi.Contratos;
using StackExchange.Redis;

namespace Shapi.Compuerta;

/// <summary>
/// La tubería de la compuerta (08 §3): evalúa los filtros en orden y, si todos dejan pasar la petición,
/// la reenvía al origen. El primer rechazo detiene la cadena y se responde con el error JSON.
/// </summary>
public sealed class TuberiaCompuerta(
    IEnumerable<IFiltroCompuerta> filtros,
    IReenvioOrigen reenvio,
    ILogger<TuberiaCompuerta> registro)
{
    /// <summary>08 §4: sin Redis no se puede validar nada; el cliente puede reintentar en unos segundos.</summary>
    private static readonly ResultadoFiltro RedisNoDisponible = ResultadoFiltro.Rechazar(
        StatusCodes.Status503ServiceUnavailable, CodigosError.ServicioNoDisponible,
        "Shapi no puede atender su petición en este momento. Intente de nuevo en unos segundos.",
        new Dictionary<string, string> { ["Retry-After"] = "5" });

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
            ResultadoFiltro resultado;
            try
            {
                resultado = await filtro.EvaluarAsync(contexto);
            }
            catch (Exception excepcion) when (excepcion is RedisConnectionException or RedisTimeoutException)
            {
                // Solo la caída o la lentitud de Redis; un error de datos (RedisServerException) no es un 503.
                // El mensaje de StackExchange.Redis no incluye la clave: solo la llave de Redis, que lleva su hash.
                // Sin la traza: mientras Redis esté caído, esto se registra en cada petición.
                registro.LogWarning("Redis no está disponible, la compuerta responde 503: {Motivo}", excepcion.Message);
                resultado = RedisNoDisponible;
            }

            if (!resultado.Continua)
            {
                await RespuestaError.EscribirAsync(http, resultado);
                return;
            }
        }

        await reenvio.ReenviarAsync(contexto);
    }
}
