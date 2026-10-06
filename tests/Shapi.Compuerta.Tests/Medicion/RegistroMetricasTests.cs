using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shapi.Compuerta.Medicion;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Tests.Medicion;

public sealed class RegistroMetricasTests
{
    [Fact]
    public void RF_34_Medicion_NoEsperaRespuestasDeRedisYDescuentaLaEsperaDelOrigen()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        var transaccion = Substitute.For<ITransaction>();
        redis.GetDatabase().ReturnsForAnyArgs(db);
        db.CreateTransaction().ReturnsForAnyArgs(transaccion);
        var respuestaNuncaLlega = new TaskCompletionSource<bool>();
        transaccion.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(respuestaNuncaLlega.Task);
        var medicion = new MedicionMiddleware(redis, TimeProvider.System, NullLogger<MedicionMiddleware>.Instance);
        var contexto = new ContextoPeticion(new DefaultHttpContext())
        {
            TiempoEsperaOrigen = TimeSpan.FromMilliseconds(95),
        };
        medicion.Registrar(contexto, new DateOnly(2026, 10, 5), TimeSpan.FromMilliseconds(100), 3, 4);
        _ = transaccion.Received().HashIncrementAsync(Arg.Any<RedisKey>(), "h_t_4", 1, CommandFlags.FireAndForget);
        _ = transaccion.Received().HashIncrementAsync(Arg.Any<RedisKey>(), "h_c_0", 1, CommandFlags.FireAndForget);
        _ = transaccion.Received().HashIncrementAsync(Arg.Any<RedisKey>(), "lc_suma", 5, CommandFlags.FireAndForget);
        _ = transaccion.Received().SetAddAsync(LlavesRedis.MetricasPendientes, Arg.Any<RedisValue>(), CommandFlags.FireAndForget);
        _ = transaccion.Received().ExecuteAsync(CommandFlags.FireAndForget);
        respuestaNuncaLlega.Task.IsCompleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(401, false, false, "r401")]
    [InlineData(403, false, false, "r403")]
    [InlineData(404, false, false, "r404")]
    [InlineData(429, false, false, "r429")]
    [InlineData(200, true, false, "o2xx")]
    [InlineData(301, true, false, "o3xx")]
    [InlineData(401, true, false, "o4xx")]
    [InlineData(502, true, false, "o5xx")]
    [InlineData(502, false, true, "ofallo")]
    [InlineData(504, false, true, "ofallo")]
    [InlineData(413, false, true, null)]
    [InlineData(200, true, true, "o2xx")]
    [InlineData(502, true, true, "o5xx")]
    public void RF_33_Respuesta_DistingueRechazosCodigosDelOrigenYFallos(int estado, bool respondio, bool fallo, string? contador)
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        var transaccion = Substitute.For<ITransaction>();
        redis.GetDatabase().ReturnsForAnyArgs(db);
        db.CreateTransaction().ReturnsForAnyArgs(transaccion);
        var contexto = new ContextoPeticion(new DefaultHttpContext()) { RespondioOrigen = respondio, FalloOrigen = fallo };
        contexto.Http.Response.StatusCode = estado;
        new MedicionMiddleware(redis, TimeProvider.System, NullLogger<MedicionMiddleware>.Instance)
            .Registrar(contexto, new DateOnly(2026, 10, 5), TimeSpan.FromMilliseconds(1), 0, 0);
        if (contador is null)
        {
            _ = transaccion.DidNotReceive().HashIncrementAsync(Arg.Any<RedisKey>(), "ofallo", 1, CommandFlags.FireAndForget);
            _ = transaccion.Received().HashIncrementAsync(Arg.Any<RedisKey>(), "peticiones", 1, CommandFlags.FireAndForget);
        }
        else
        {
            _ = transaccion.Received().HashIncrementAsync(Arg.Any<RedisKey>(), contador, 1, CommandFlags.FireAndForget);
        }
    }
}
