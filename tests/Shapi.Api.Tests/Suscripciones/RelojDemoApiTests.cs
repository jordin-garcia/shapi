using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shapi.Api.Suscripciones;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Suscripciones;

public sealed class RelojDemoApiTests
{
    // RF-44: el administrador adelanta el reloj compartido y solicita de inmediato el cierre del ciclo.
    [Fact]
    public async Task Adelantar_ModoDemo_ActualizaDesplazamientoYPublicaSolicitud()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "true",
        }).Build();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        var subscriber = Substitute.For<ISubscriber>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        redis.GetSubscriber(Arg.Any<object?>()).Returns(subscriber);
        var api = new RelojDemoApi(configuracion, redis);

        var respuesta = await api.Adelantar(new PeticionRelojDemo(30), CancellationToken.None);

        respuesta.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        await database.Received(1).StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, 30L * 24 * 60 * 60, CommandFlags.None);
        await subscriber.Received(1).PublishAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), "30", CommandFlags.None);
    }

    // RF-44: el endpoint no existe cuando se desactiva el modo demo.
    [Fact]
    public async Task Adelantar_ModoDemoDesactivado_Devuelve404SinCambiarRedis()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "false",
        }).Build();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var api = new RelojDemoApi(configuracion, redis);

        var respuesta = await api.Adelantar(new PeticionRelojDemo(30), CancellationToken.None);

        respuesta.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
        redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
    }
}
