using NSubstitute;
using Shapi.Contratos.Redis;
using Shapi.Trabajador.Consolidacion;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Consumo;

public sealed class LotesMetricasRedisTests
{
    // RNF-05, RF-34: SCAN puede repetir elementos; la unicidad del lote no protege duplicados dentro del lote.
    [Fact]
    public async Task RNF_05_ScanRepiteUnaLlave_LeeYSumaElHashUnaSolaVez()
    {
        var lote = Guid.NewGuid();
        var original = LlavesRedis.Metricas(new DateOnly(2026, 10, 5), Guid.NewGuid(), null, null, "produccion");
        RedisKey llave = LlavesRedis.LoteMetricas(lote, original);
        var conexion = Substitute.For<IConnectionMultiplexer>();
        var servidor = Substitute.For<IServer>();
        var db = Substitute.For<IDatabase>();
        conexion.GetServers().Returns([servidor]);
        conexion.GetDatabase().ReturnsForAnyArgs(db);
        servidor.KeysAsync().ReturnsForAnyArgs(ConDuplicados(llave));
        HashEntry[] campos = [new("peticiones", 7), new("h_t_0", 7), new("h_c_0", 7)];
        db.HashGetAllAsync(llave).Returns(Task.FromResult(campos));

        var filas = await new LotesMetricasRedis(conexion).LeerAsync(lote, CancellationToken.None);

        filas.Should().ContainSingle();
        filas.Sum(x => x.Contador("peticiones")).Should().Be(7);
        filas.Sum(x => x.Histograma("h_t_").Sum()).Should().Be(7);
        await db.Received(1).HashGetAllAsync(llave);
    }

    private static async IAsyncEnumerable<RedisKey> ConDuplicados(RedisKey llave)
    {
        yield return llave;
        await Task.Yield();
        yield return llave;
    }
}
