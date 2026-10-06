using Shapi.Compuerta.Filtros;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Medicion;

public interface IMedicionPeticion
{
    Task MedirAsync(ContextoPeticion contexto, Func<Task> siguiente);
}

/// <summary>Filtro 9: se ejecuta después de cualquier respuesta, incluidos los rechazos, sin esperar a Redis.</summary>
public sealed class MedicionMiddleware(
    IConnectionMultiplexer redis, TimeProvider reloj, ILogger<MedicionMiddleware> registro) : IMedicionPeticion
{
    public async Task MedirAsync(ContextoPeticion contexto, Func<Task> siguiente)
    {
        var http = contexto.Http;
        var inicio = reloj.GetTimestamp();
        var fecha = FiltroLimitesYCuotas.DiaGuatemala(reloj.GetUtcNow());
        var entradaOriginal = http.Request.Body;
        var salidaOriginal = http.Response.Body;
        using var entrada = new FlujoContado(entradaOriginal);
        using var salida = new FlujoContado(salidaOriginal);
        http.Request.Body = entrada;
        http.Response.Body = salida;
        try
        {
            await siguiente();
        }
        finally
        {
            var total = reloj.GetElapsedTime(inicio);
            http.Request.Body = entradaOriginal;
            http.Response.Body = salidaOriginal;
            Registrar(contexto, fecha, total, entrada.Leidos, salida.Escritos);
        }
    }

    public void Registrar(ContextoPeticion contexto, DateOnly fecha, TimeSpan total, long bytesEntrada, long bytesSalida)
    {
        var compuerta = total - contexto.TiempoEsperaOrigen;
        if (compuerta < TimeSpan.Zero)
        {
            compuerta = TimeSpan.Zero;
        }
        var clave = contexto.ClaveValidada ? contexto.Clave : null;
        var llave = LlavesRedis.Metricas(fecha, contexto.Api?.ApiId ?? Guid.Empty,
            contexto.Ruta?.RutaId, clave?.SuscripcionId, clave?.Entorno ?? "produccion");
        var campos = new Dictionary<string, long>
        {
            ["peticiones"] = 1,
            ["llamadas"] = contexto.LlamadasDescontadas,
            ["bytes_entrada"] = bytesEntrada,
            ["bytes_salida"] = bytesSalida,
            [$"h_t_{HistogramaMetricas.Rango(total)}"] = 1,
            [$"h_c_{HistogramaMetricas.Rango(compuerta)}"] = 1,
            ["lt_suma"] = (long)Math.Round(total.TotalMilliseconds),
            ["lc_suma"] = (long)Math.Round(compuerta.TotalMilliseconds),
        };
        var codigo = ContadorRespuesta(contexto);
        if (codigo is not null)
        {
            campos[codigo] = 1;
        }
        try
        {
            // MULTI/EXEC envía los HINCRBY y el SADD en un pipeline indivisible: RENAME nunca parte una petición.
            // FireAndForget tanto en los comandos como en EXEC: no se espera ninguna respuesta de Redis.
            var lote = redis.GetDatabase().CreateTransaction();
            foreach (var (campo, incremento) in campos)
            {
                _ = lote.HashIncrementAsync(llave, campo, incremento, CommandFlags.FireAndForget);
            }
            _ = lote.SetAddAsync(LlavesRedis.MetricasPendientes, llave, CommandFlags.FireAndForget);
            _ = lote.ExecuteAsync(CommandFlags.FireAndForget);
        }
        catch (RedisException excepcion)
        {
            // La respuesta ya se produjo: la medición no debe convertirla en otro error ni registrar datos sensibles.
            registro.LogWarning("No se pudieron enviar las métricas a Redis: {Tipo}", excepcion.GetType().Name);
        }
    }

    private static string? ContadorRespuesta(ContextoPeticion contexto)
    {
        if (contexto.FalloOrigen)
        {
            return "ofallo";
        }
        var codigo = contexto.Http.Response.StatusCode;
        if (contexto.RespondioOrigen)
        {
            return (codigo / 100) switch { 2 => "o2xx", 3 => "o3xx", 4 => "o4xx", 5 => "o5xx", _ => null };
        }
        return codigo switch { 401 => "r401", 403 => "r403", 404 => "r404", 429 => "r429", _ => null };
    }
}
