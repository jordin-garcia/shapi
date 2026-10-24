using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shapi.Api.Suscripciones;
using Shapi.Api.Tests.Cache;
using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Comun;
using Shapi.Trabajador.CierreCiclo;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Comun;

[Collection(nameof(RedisCache))]
public sealed class RelojRedisIntegracionTests(RedisCache redis)
{
    // RF-22, RF-34: API y trabajador comparten Redis real; el cierre se avisa con la hora ya adelantada.
    [Fact]
    public async Task Adelantar_RedisReal_ActualizaAmbosProcesosYCierreEnMenosDeUnSegundo()
    {
        await redis.VaciarAsync();
        await redis.Conexion.GetDatabase().StringSetAsync(LlavesRedis.DemoRelojDesplazamiento, 30L * 86400);
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "true",
        }).Build();
        using var relojApi = new RelojSistema(TimeProvider.System, configuracion, redis.Conexion);
        using var relojTrabajador = new RelojSistema(TimeProvider.System, configuracion, redis.Conexion);
        await relojApi.StartAsync(CancellationToken.None);
        await relojTrabajador.StartAsync(CancellationToken.None);
        var inicial = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cierreAdelantado = new TaskCompletionSource<DateTimeOffset>(TaskCreationOptions.RunContinuationsAsynchronously);
        var alcances = Substitute.For<IServiceScopeFactory>();
        alcances.CreateScope().Returns(_ =>
        {
            var ahora = relojTrabajador.Ahora;
            if (ahora > DateTimeOffset.UtcNow.AddDays(59))
            {
                cierreAdelantado.TrySetResult(ahora);
            }

            inicial.TrySetResult();
            throw new RedisTimeoutException(CommandFlags.None, "Fallo del trabajo simulado", CommandStatus.Sent);
        });
        using var trabajo = new TrabajoCierreCiclo(alcances, relojTrabajador,
            new OpcionesCierreCiclo { Intervalo = TimeSpan.FromMinutes(1) }, NullLogger<TrabajoCierreCiclo>.Instance);
        await trabajo.StartAsync(CancellationToken.None);
        try
        {
            await inicial.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var antes = DateTimeOffset.UtcNow;
            var api = new RelojDemoApi(configuracion, redis.Conexion, relojApi);

            await api.Adelantar(new PeticionRelojDemo(30), CancellationToken.None);
            var horaCierre = await cierreAdelantado.Task.WaitAsync(TimeSpan.FromSeconds(1));

            (DateTimeOffset.UtcNow - antes).Should().BeLessThan(TimeSpan.FromSeconds(1));
            relojApi.Ahora.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(60), TimeSpan.FromSeconds(1));
            horaCierre.Should().BeCloseTo(antes.AddDays(60), TimeSpan.FromSeconds(1));
            trabajo.ExecuteTask!.IsCompleted.Should().BeFalse();
        }
        finally
        {
            await trabajo.StopAsync(CancellationToken.None);
            await relojTrabajador.StopAsync(CancellationToken.None);
            await relojApi.StopAsync(CancellationToken.None);
        }
    }
}
