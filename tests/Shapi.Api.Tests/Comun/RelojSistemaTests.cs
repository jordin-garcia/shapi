using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Comun;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Comun;

public sealed class RelojSistemaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 18, 0, 0, TimeSpan.Zero);

    // RF-44: el modo demo agrega el desplazamiento compartido en Redis al reloj real.
    [Fact]
    public void Ahora_ModoDemo_LeeDesplazamientoDeRedis()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        database.StringGet(LlavesRedis.DemoRelojDesplazamiento, CommandFlags.None).Returns((RedisValue)"86400");
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "true",
        }).Build();

        var reloj = new RelojSistema(new ProveedorTiempoFijo(Ahora), configuracion, redis);

        reloj.Ahora.Should().Be(Ahora.AddDays(1));
    }

    // RF-44: fuera del modo demo el reloj ignora el desplazamiento y no consulta Redis.
    [Fact]
    public void Ahora_ModoDemoDesactivado_DevuelveHoraReal()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "false",
        }).Build();

        var reloj = new RelojSistema(new ProveedorTiempoFijo(Ahora), configuracion, redis);

        reloj.Ahora.Should().Be(Ahora);
        redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
    }

    private sealed class ProveedorTiempoFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
