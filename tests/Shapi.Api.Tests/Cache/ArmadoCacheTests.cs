using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Aplicacion.Cache;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Cache;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Cache;

// JG-04: cálculo del estado efectivo (07 §3.1) y reintentos del publicador (criterio 3).
public sealed class ArmadoCacheTests
{
    [Theory]
    [InlineData(EstadoAdmin.Activa, null, "activa")]
    [InlineData(EstadoAdmin.Activa, EstadoSuscripcion.Activa, "activa")]
    [InlineData(EstadoAdmin.Activa, EstadoSuscripcion.EnGracia, "activa")]
    [InlineData(EstadoAdmin.Activa, EstadoSuscripcion.Suspendida, "suspendida")]
    [InlineData(EstadoAdmin.Suspendida, null, "suspendida")]
    [InlineData(EstadoAdmin.Suspendida, EstadoSuscripcion.Activa, "suspendida")]
    [InlineData(EstadoAdmin.Suspendida, EstadoSuscripcion.Suspendida, "suspendida")]
    public void RF_38_EstadoEfectivo_SuspendidaSiLaSuspendioElAdministradorOSuSuscripcionDePlataforma(
        EstadoAdmin estadoAdmin, EstadoSuscripcion? suscripcion, string esperado)
    {
        // RF-38 y RF-23
        ArmadoCache.EstadoEfectivo(estadoAdmin, suscripcion).Should().Be(esperado);
    }

    [Fact]
    public void RF_11_Hosts_UsanElSubdominioYElDominioBaseEnMinusculas()
    {
        // RF-11
        ArmadoCache.HostApi("envios", "Shapi.Localhost").Should().Be("envios.api.shapi.localhost");
        ArmadoCache.HostPortal("envios", "Shapi.Localhost").Should().Be("envios.shapi.localhost");
    }

    [Fact]
    public async Task RNF_05_Reintentos_FallaDosVecesYLuegoFunciona_TerminaBienAlTercerIntento()
    {
        // RNF-05
        var intentos = 0;

        var exito = await ReintentosRedis.EjecutarAsync(() =>
        {
            intentos++;
            return intentos < 3 ? throw new RedisConnectionException(ConnectionFailureType.SocketFailure, CommandFlags.None, "caído", null, CommandStatus.Unknown) : Task.CompletedTask;
        }, [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero], NullLogger.Instance, "PublicarApi", CancellationToken.None);

        exito.Should().BeTrue();
        intentos.Should().Be(3);
    }

    [Fact]
    public async Task RNF_05_Reintentos_RedisSigueCaido_IntentaUnaVezYReintentaTresVecesMas()
    {
        // RNF-05
        var intentos = 0;

        var exito = await ReintentosRedis.EjecutarAsync(() =>
        {
            intentos++;
            throw new RedisTimeoutException(CommandFlags.None, "tiempo agotado", CommandStatus.Unknown);
        }, [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero], NullLogger.Instance, "PublicarApi", CancellationToken.None);

        exito.Should().BeFalse();
        intentos.Should().Be(4);
    }

    [Fact]
    public async Task RNF_05_Reintentos_OtroError_NoSeReintentaYSePropaga()
    {
        // RNF-05: solo se reintentan las fallas de Redis.
        var intentos = 0;

        var ejecutar = () => ReintentosRedis.EjecutarAsync(() =>
        {
            intentos++;
            throw new InvalidOperationException("otro error");
        }, [TimeSpan.Zero], NullLogger.Instance, "PublicarApi", CancellationToken.None);

        await ejecutar.Should().ThrowAsync<InvalidOperationException>();
        intentos.Should().Be(1);
    }
}
