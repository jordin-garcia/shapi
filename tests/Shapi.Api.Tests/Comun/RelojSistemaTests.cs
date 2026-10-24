using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Comun;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Comun;

public sealed class RelojSistemaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 18, 0, 0, TimeSpan.Zero);

    // RNF-06, RF-22: un GET pendiente o atrasado no bloquea Ahora ni deshace un mensaje nuevo.
    [Fact]
    public async Task Sincronizacion_LecturaAnteriorAlMensaje_NoSobrescribeHoraNueva()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        var sub = Substitute.For<ISubscriber>();
        redis.GetDatabase().ReturnsForAnyArgs(db);
        redis.GetSubscriber().ReturnsForAnyArgs(sub);
        Action<RedisChannel, RedisValue>? recibir = null;
        sub.SubscribeAsync(Arg.Any<RedisChannel>(), Arg.Do<Action<RedisChannel, RedisValue>>(h => recibir = h)).Returns(Task.CompletedTask);
        var lectura = new TaskCompletionSource<RedisValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        db.StringGetAsync(Arg.Any<RedisKey>()).ReturnsForAnyArgs(lectura.Task);
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SHAPI_MODO_DEMO"] = "true" }).Build();
        using var reloj = new RelojSistema(new ProveedorTiempoFijo(Ahora), configuracion, redis);
        var inicio = reloj.StartAsync(CancellationToken.None);
        reloj.Ahora.Should().Be(Ahora);
        recibir!(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), 30L * 86400);
        lectura.SetResult(86400);
        await inicio;
        try
        {
            reloj.Ahora.Should().Be(Ahora.AddDays(30));
        }
        finally
        {
            await reloj.StopAsync(CancellationToken.None);
        }
    }

    // RNF-06: reintenta la suscripción inicial, conserva el último total si falla el respaldo y se recupera.
    [Fact]
    public async Task Sincronizacion_FallosDeSuscripcionYLectura_ReintentaSinDetenerse()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        var sub = Substitute.For<ISubscriber>();
        redis.GetDatabase().ReturnsForAnyArgs(db);
        redis.GetSubscriber().ReturnsForAnyArgs(sub);
        var fallo = new RedisTimeoutException(CommandFlags.None, "Timeout simulado", CommandStatus.Sent);
        var suscripciones = 0;
        sub.SubscribeAsync(Arg.Any<RedisChannel>(), Arg.Any<Action<RedisChannel, RedisValue>>()).Returns(_ =>
            Interlocked.Increment(ref suscripciones) == 1 ? Task.FromException(fallo) : Task.CompletedTask);
        var lecturas = 0;
        DateTimeOffset? horaDuranteFallo = null;
        var primera = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recuperada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SHAPI_MODO_DEMO"] = "true" }).Build();
        using var reloj = new RelojSistema(new TiempoRapido(Ahora), configuracion, redis);
        db.StringGetAsync(Arg.Any<RedisKey>()).ReturnsForAnyArgs(_ =>
        {
            var lectura = Interlocked.Increment(ref lecturas);
            if (lectura == 2)
            {
                horaDuranteFallo = reloj.Ahora;
                return Task.FromException<RedisValue>(fallo);
            }
            return Task.FromResult<RedisValue>(lectura == 1 ? 86400 : 172800);
        });
        reloj.DesplazamientoActualizado += () =>
        {
            if (reloj.Ahora == Ahora.AddDays(1))
            {
                primera.TrySetResult();
            }

            if (reloj.Ahora == Ahora.AddDays(2))
            {
                recuperada.TrySetResult();
            }
        };
        await reloj.StartAsync(CancellationToken.None);
        try
        {
            await primera.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await recuperada.Task.WaitAsync(TimeSpan.FromSeconds(5));
            reloj.Ahora.Should().Be(Ahora.AddDays(2));
            reloj.ExecuteTask!.IsCompleted.Should().BeFalse();
            suscripciones.Should().Be(2);
            lecturas.Should().BeGreaterThanOrEqualTo(3);
            horaDuranteFallo.Should().Be(Ahora.AddDays(1));
        }
        finally
        {
            await reloj.StopAsync(CancellationToken.None);
        }
    }

    private sealed class TiempoRapido(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            System.CreateTimer(callback, state, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20));
    }

    // RF-44: el modo demo agrega el desplazamiento compartido en Redis al reloj real.
    [Fact]
    public async Task Ahora_ModoDemo_LeeDesplazamientoDeRedis()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        redis.GetSubscriber(Arg.Any<object?>()).Returns(Substitute.For<ISubscriber>());
        database.StringGetAsync(LlavesRedis.DemoRelojDesplazamiento, CommandFlags.None).Returns((RedisValue)"86400");
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "true",
        }).Build();

        using var reloj = new RelojSistema(new ProveedorTiempoFijo(Ahora), configuracion, redis);
        var servicio = reloj;
        await servicio.StartAsync(CancellationToken.None);

        reloj.Ahora.Should().Be(Ahora.AddDays(1));
        reloj.Ahora.Should().Be(Ahora.AddDays(1));
        database.DidNotReceive().StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        await database.Received(1).StringGetAsync(LlavesRedis.DemoRelojDesplazamiento, CommandFlags.None);
        await servicio.StopAsync(CancellationToken.None);
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

        using var reloj = new RelojSistema(new ProveedorTiempoFijo(Ahora), configuracion, redis);

        reloj.Ahora.Should().Be(Ahora);
        redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
    }

    private sealed class ProveedorTiempoFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    // RNF-06: también se conserva la hora conocida si falla la lectura asíncrona de respaldo.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Ahora_RedisNoDisponible_ConservaDesplazamientoSinConsultarRedis(bool timeout)
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        var subscriber = Substitute.For<ISubscriber>();
        redis.GetDatabase().ReturnsForAnyArgs(database);
        redis.GetSubscriber().ReturnsForAnyArgs(subscriber);
        Action<RedisChannel, RedisValue>? recibir = null;
        subscriber.SubscribeAsync(Arg.Any<RedisChannel>(), Arg.Do<Action<RedisChannel, RedisValue>>(h => recibir = h))
            .Returns(Task.CompletedTask);
        Exception fallo = timeout
            ? new RedisTimeoutException(CommandFlags.None, "Timeout simulado", CommandStatus.Sent)
            : new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Sin conexión simulada");
        database.StringGetAsync(Arg.Any<RedisKey>()).ReturnsForAnyArgs(Task.FromException<RedisValue>(fallo));
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "true",
        }).Build();
        using var reloj = new RelojSistema(new ProveedorTiempoFijo(Ahora), configuracion, redis);
        var servicio = reloj;
        await servicio.StartAsync(CancellationToken.None);
        try
        {
            recibir.Should().NotBeNull();
            recibir!(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), "86400");
            redis.ClearReceivedCalls();
            database.ClearReceivedCalls();

            reloj.Ahora.Should().Be(Ahora.AddDays(1));
            reloj.Ahora.Should().Be(Ahora.AddDays(1));
            redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
            database.ReceivedCalls().Should().BeEmpty();
        }
        finally
        {
            await servicio.StopAsync(CancellationToken.None);
        }
    }

    // RF-22: un mensaje actualiza la hora antes de avisar al cierre y no hace otra consulta.
    [Fact]
    public async Task Ahora_MensajeDuplicadoOAnterior_NoRetrocedeNiSumaDosVeces()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var subscriber = Substitute.For<ISubscriber>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase().ReturnsForAnyArgs(database);
        redis.GetSubscriber().ReturnsForAnyArgs(subscriber);
        Action<RedisChannel, RedisValue>? recibir = null;
        subscriber.SubscribeAsync(Arg.Any<RedisChannel>(), Arg.Do<Action<RedisChannel, RedisValue>>(h => recibir = h))
            .Returns(Task.CompletedTask);
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "true",
        }).Build();
        using var reloj = new RelojSistema(new ProveedorTiempoFijo(Ahora), configuracion, redis);
        var servicio = reloj;
        await servicio.StartAsync(CancellationToken.None);
        try
        {
            var canal = RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento);
            recibir!(canal, "2592000");
            recibir(canal, "2592000");
            recibir(canal, "86400");
            recibir(canal, "incorrecto");
            recibir(canal, long.MaxValue);
            reloj.Ahora.Should().Be(Ahora.AddDays(30));
            await database.Received(1).StringGetAsync(LlavesRedis.DemoRelojDesplazamiento, CommandFlags.None);
            database.DidNotReceive().StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        }
        finally
        {
            await servicio.StopAsync(CancellationToken.None);
        }
    }
}
