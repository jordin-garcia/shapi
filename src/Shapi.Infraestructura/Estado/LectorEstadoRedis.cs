using System.Globalization;
using Shapi.Aplicacion.Consumo;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Infraestructura.Estado;

public sealed record SaludRedis(double? Milisegundos, bool Compuerta, bool Trabajador, bool Pendientes, Percentil95 Latencia);

public sealed class LectorEstadoRedis(IConnectionMultiplexer redis, TimeProvider tiempo)
{
    public async Task<SaludRedis> LeerAsync(CancellationToken cancelacion)
    {
        try
        {
            var db = redis.GetDatabase();
            var ms = (await db.PingAsync().WaitAsync(cancelacion)).TotalMilliseconds;
            var segundo = tiempo.GetUtcNow().ToUnixTimeSeconds();
            var tareas = Enumerable.Range(0, 61)
                .Select(i => db.HashGetAllAsync(LlavesRedis.LatenciaCompuerta(segundo / 60 - i))).ToArray();
            var histogramas = await Task.WhenAll(tareas).WaitAsync(cancelacion);
            var histograma = new int[10];
            foreach (var campo in histogramas.SelectMany(x => x))
            {
                var partes = campo.Name.ToString().Split(':');
                if (partes.Length == 2 && long.TryParse(partes[0], CultureInfo.InvariantCulture, out var instante)
                    && int.TryParse(partes[1], CultureInfo.InvariantCulture, out var rango) && rango is >= 0 and < 10
                    && instante > segundo - 3600 && instante <= segundo)
                {
                    histograma[rango] = checked(histograma[rango] + (int)campo.Value);
                }
            }
            var trabajador = await VigenteAsync(db, LlavesRedis.SaludTrabajador, cancelacion);
            var compuerta = false;
            var lotes = false;
            foreach (var servidor in redis.GetServers().Where(x => !x.IsReplica))
            {
                await foreach (var llave in servidor.KeysAsync(db.Database, LlavesRedis.SaludCompuerta("*")).WithCancellation(cancelacion))
                {
                    compuerta |= await VigenteAsync(db, llave, cancelacion);
                }
                await foreach (var _ in servidor.KeysAsync(db.Database, LlavesRedis.PatronLotes).WithCancellation(cancelacion))
                {
                    lotes = true;
                    break;
                }
            }
            var pendientes = await db.SetMembersAsync(LlavesRedis.MetricasPendientes).WaitAsync(cancelacion);
            var identificadas = pendientes.Any(x => MetricasDiarias.DesdeLlave(x.ToString(), new Dictionary<string, long>()).ApiId != Guid.Empty);
            return new(ms, compuerta, trabajador, lotes || identificadas, HistogramaLatencia.CalcularP95([histograma]));
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            return new(null, false, false, false, new(null));
        }
    }

    private static async Task<bool> VigenteAsync(IDatabase db, RedisKey llave, CancellationToken cancelacion) =>
        await db.KeyTimeToLiveAsync(llave).WaitAsync(cancelacion) is { } ttl && ttl > TimeSpan.Zero;
}
