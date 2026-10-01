using Shapi.Compuerta.Contexto;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;
using Shapi.Contratos;
using StackExchange.Redis;

namespace Shapi.Compuerta;

/// <summary>
/// La tubería de la compuerta (08 §3): lee de Redis el contexto de la petición (08 §8), evalúa los filtros en orden
/// y, si todos dejan pasar la petición, la reenvía al origen. El primer rechazo detiene la cadena y se responde con
/// el error JSON.
/// </summary>
public sealed class TuberiaCompuerta(
    ILectorContexto lector,
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

    /// <summary>El cuerpo más grande que se acepta (08 §1): 10 MB.</summary>
    public const long LimiteCuerpo = 10 * 1024 * 1024;

    /// <summary>
    /// El orden de los filtros, definido solo aquí. Agregar una regla es agregar su clase y una línea (RNF-13).
    /// </summary>
    public static IReadOnlyList<Type> Orden { get; } =
    [
        typeof(FiltroCors), // 0 · CORS: OPTIONS → 204
        typeof(FiltroApi), // 1 · host → API publicada
        typeof(FiltroClave), // 2 · X-Api-Key → hash → Redis
        typeof(FiltroOrganizacion), // 3 · organización activa
        typeof(FiltroSuscripcion), // 4 · suscripción activa o en gracia
        typeof(FiltroRuta), // 5 · método + patrón expuestos
    ];

    public async Task ProcesarAsync(HttpContext http)
    {
        var contexto = new ContextoPeticion(http);
        var resultado = await EvaluarAsync(contexto);
        if (!resultado.Continua)
        {
            await ResponderAsync(http, resultado);
            return;
        }

        await reenvio.ReenviarAsync(contexto);
    }

    private async Task<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        try
        {
            await lector.LeerAsync(contexto);
            foreach (var filtro in _filtros)
            {
                var resultado = await filtro.EvaluarAsync(contexto);
                if (!resultado.Continua)
                {
                    return resultado;
                }
            }

            return ResultadoFiltro.Continuar;
        }
        catch (Exception excepcion) when (excepcion is RedisConnectionException or RedisTimeoutException)
        {
            // Solo la caída o la lentitud de Redis; un error de datos (RedisServerException) no es un 503.
            // El mensaje de StackExchange.Redis no incluye la clave: solo la llave de Redis, que lleva su hash.
            // Sin la traza: mientras Redis esté caído, esto se registra en cada petición.
            registro.LogWarning("Redis no está disponible, la compuerta responde 503: {Motivo}", excepcion.Message);
            return RedisNoDisponible;
        }
    }

    private static Task ResponderAsync(HttpContext http, ResultadoFiltro resultado)
    {
        if (resultado.EsError)
        {
            return RespuestaError.EscribirAsync(http, resultado);
        }

        http.Response.StatusCode = resultado.Estado;
        return Task.CompletedTask;
    }
}
