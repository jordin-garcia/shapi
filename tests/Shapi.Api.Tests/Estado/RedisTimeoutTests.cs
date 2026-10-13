using NSubstitute;
using Shapi.Aplicacion.Estado;
using Shapi.Infraestructura.Estado;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Estado;

public sealed class RedisTimeoutTests
{
    // RF-39: RedisTimeoutException hereda de TimeoutException, no de RedisException.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RF_39_RedisNoResponde_DevuelveFueraDeServicioSinPropagarLaExcepcion(bool durantePing)
    {
        var conexion = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        conexion.GetDatabase().ReturnsForAnyArgs(db);
        db.PingAsync().ReturnsForAnyArgs(Task.FromResult(TimeSpan.FromMilliseconds(1)));
        db.HashGetAllAsync(default).ReturnsForAnyArgs(Task.FromResult(Array.Empty<HashEntry>()));
        var excepcion = new RedisTimeoutException(CommandFlags.None, "Plazo agotado simulado", CommandStatus.Sent);
        if (durantePing)
        {
            db.PingAsync().ReturnsForAnyArgs(Task.FromException<TimeSpan>(excepcion));
        }
        else
        {
            db.HashGetAllAsync(default).ReturnsForAnyArgs(Task.FromException<HashEntry[]>(excepcion));
        }

        var salud = await new LectorEstadoRedis(conexion, TimeProvider.System).LeerAsync(CancellationToken.None);

        Assert.Null(salud.Milisegundos);
        Assert.Null(salud.Latencia.Milisegundos);
        Assert.False(salud.Compuerta);
        Assert.False(salud.Trabajador);
        Assert.Equal("fuera_de_servicio", ReglasEstado.PorLatencia("redis", salud.Milisegundos));
    }
}
