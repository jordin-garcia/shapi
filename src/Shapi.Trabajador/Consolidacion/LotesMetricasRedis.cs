using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Trabajador.Consolidacion;

/// <summary>Instantáneas cerradas, recuperables mediante SCAN aunque el proceso muera antes de recibir el RENAME.</summary>
public sealed class LotesMetricasRedis(IConnectionMultiplexer redis)
{
    // Validar primero evita que un error de tipo deje un lote abierto a medias (Lua no revierte comandos anteriores).
    private const string Separar = """
        for i = 2, #KEYS, 2 do
            local tipo = redis.call('TYPE', KEYS[i]).ok
            if tipo ~= 'none' and tipo ~= 'hash' then return redis.error_reply('Metrica no es un hash') end
            if redis.call('EXISTS', KEYS[i+1]) == 1 then return redis.error_reply('El lote ya existe') end
        end
        local cantidad = 0
        for i = 2, #KEYS, 2 do
            if redis.call('SREM', KEYS[1], KEYS[i]) == 1 then
                cantidad = cantidad + 1
                if redis.call('EXISTS', KEYS[i]) == 1 then
                    redis.call('RENAME', KEYS[i], KEYS[i+1])
                end
            end
        end
        return cantidad
        """;

    public async Task<int> SepararAsync(Guid loteId, CancellationToken cancelacion)
    {
        var db = redis.GetDatabase();
        var pendientes = await db.SetMembersAsync(LlavesRedis.MetricasPendientes).WaitAsync(cancelacion);
        // Sin API identificada no existe una FK válida: esas métricas globales se conservan en Redis.
        var llaves = pendientes.Select(x => x.ToString())
            .Where(x => MetricasDiarias.DesdeLlave(x, new Dictionary<string, long>()).ApiId != Guid.Empty)
            .Order(StringComparer.Ordinal).Take(256).ToArray();
        if (llaves.Length == 0)
        {
            return 0;
        }
        RedisKey[] claves = [LlavesRedis.MetricasPendientes,
            .. llaves.SelectMany(x => new RedisKey[] { x, LlavesRedis.LoteMetricas(loteId, x) })];
        return (int)await db.ScriptEvaluateAsync(Separar, claves).WaitAsync(cancelacion);
    }

    public async Task<IReadOnlyList<Guid>> LotesPendientesAsync(CancellationToken cancelacion)
    {
        var lotes = new HashSet<Guid>();
        await foreach (var llave in BuscarAsync(LlavesRedis.PatronLotes, cancelacion))
        {
            lotes.Add(Guid.Parse(llave.ToString().Split(':')[2]));
        }
        return [.. lotes.Order()];
    }

    public async Task<IReadOnlyList<MetricasDiarias>> LeerAsync(Guid loteId, CancellationToken cancelacion)
    {
        var filas = new List<MetricasDiarias>();
        var prefijo = LlavesRedis.LoteMetricas(loteId, "");
        await foreach (var llave in BuscarAsync(LlavesRedis.PatronLote(loteId), cancelacion))
        {
            var campos = await redis.GetDatabase().HashGetAllAsync(llave).WaitAsync(cancelacion);
            if (campos.Length > 0)
            {
                filas.Add(MetricasDiarias.DesdeLlave(llave.ToString()[prefijo.Length..],
                    campos.ToDictionary(x => x.Name.ToString(), x => (long)x.Value)));
            }
        }
        return filas;
    }

    public async Task BorrarAsync(Guid loteId, CancellationToken cancelacion)
    {
        await foreach (var llave in BuscarAsync(LlavesRedis.PatronLote(loteId), cancelacion))
        {
            await redis.GetDatabase().KeyDeleteAsync(llave).WaitAsync(cancelacion);
        }
    }

    private async IAsyncEnumerable<RedisKey> BuscarAsync(string patron,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancelacion)
    {
        foreach (var servidor in redis.GetServers().Where(x => !x.IsReplica))
        {
            await foreach (var llave in servidor.KeysAsync(database: redis.GetDatabase().Database, pattern: patron)
                .WithCancellation(cancelacion))
            {
                yield return llave;
            }
        }
    }
}
