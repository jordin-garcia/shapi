using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Shapi.Infraestructura.Cache;

/// <summary>Esperas entre los reintentos de una publicación en Redis (06 §5.2).</summary>
public sealed class OpcionesCacheRedis
{
    /// <summary>Una espera por reintento: con las tres de siempre, el publicador intenta una vez y reintenta 3 veces.</summary>
    public IReadOnlyList<TimeSpan> EsperasReintento { get; init; } =
        [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1)];
}

/// <summary>
/// Reintenta una escritura en Redis cuando falla la conexión o se agota el tiempo (06 §5.2). Otros errores, como un
/// error del servidor o de la base de datos, no se reintentan: se propagan.
/// </summary>
public static class ReintentosRedis
{
    /// <returns><c>false</c> si Redis siguió fallando después del último reintento.</returns>
    public static async Task<bool> EjecutarAsync(
        Func<Task> escritura, IReadOnlyList<TimeSpan> esperas, ILogger registro, string operacion,
        CancellationToken cancelacion)
    {
        for (var reintento = 0; ; reintento++)
        {
            try
            {
                await escritura();
                return true;
            }
            catch (Exception excepcion) when (EsFallaDeRedis(excepcion) && reintento < esperas.Count)
            {
                // Sin la excepción completa: el multiplexor ya no pone llaves en sus mensajes, pero así queda seguro.
                registro.LogWarning(
                    "Redis falló en {Operacion} ({Falla}); se reintenta en {EsperaMs} ms (reintento {Reintento} de {Reintentos})",
                    operacion, excepcion.GetType().Name, esperas[reintento].TotalMilliseconds, reintento + 1, esperas.Count);
                await Task.Delay(esperas[reintento], cancelacion);
            }
            catch (Exception excepcion) when (EsFallaDeRedis(excepcion))
            {
                return false;
            }
        }
    }

    private static bool EsFallaDeRedis(Exception excepcion) =>
        excepcion is RedisConnectionException or RedisTimeoutException;
}
