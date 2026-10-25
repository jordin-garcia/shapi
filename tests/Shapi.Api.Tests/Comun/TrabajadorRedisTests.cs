using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shapi.Api.Tests.Cache;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Comun;
using Shapi.Trabajador.AvisosCuota;
using Shapi.Trabajador.CierreCiclo;
using Shapi.Trabajador.Consolidacion;
using Shapi.Trabajador.Correo;
using Shapi.Trabajador.Resincronizacion;
using Shapi.Trabajador.VerificacionDominios;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Comun;

public sealed class TrabajadorRedisTests
{
    // RNF-06: un timeout no apaga el latido y la siguiente iteración vuelve a intentar.
    [Fact]
    public async Task Latido_TimeoutRedis_ContinuaYRegistraAdvertencia()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        redis.GetDatabase().ReturnsForAnyArgs(db);
        var reloj = Substitute.For<IReloj>();
        reloj.Ahora.Returns(DateTimeOffset.UtcNow);
        var registro = Substitute.For<ILogger<LatidoTrabajador>>();
        var intentos = 0;
        var repetido = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        redis.When(x => x.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())).Do(_ =>
        {
            if (Interlocked.Increment(ref intentos) == 1)
            {
                throw new RedisTimeoutException(CommandFlags.None, "Timeout simulado", CommandStatus.Sent);
            }

            repetido.TrySetResult();
        });
        using var servicio = new LatidoTrabajador(redis, reloj,
            new OpcionesConsolidacion { Intervalo = TimeSpan.FromMilliseconds(20) }, registro);

        await servicio.StartAsync(CancellationToken.None);
        try
        {
            await repetido.Task.WaitAsync(TimeSpan.FromSeconds(3));
            servicio.ExecuteTask!.IsCompleted.Should().BeFalse();
            registro.ReceivedCalls().Should().Contain(c => c.GetMethodInfo().Name == "Log"
                && (LogLevel)c.GetArguments()[0]! == LogLevel.Warning
                && c.GetArguments()[2]!.ToString()!.Contains(nameof(RedisTimeoutException)));
        }
        finally
        {
            await servicio.StopAsync(CancellationToken.None);
        }
    }

    // RNF-06: cada servicio conserva el bucle y registra el tipo de fallo de Redis.
    [Theory]
    [InlineData("consolidacion", true)]
    [InlineData("consolidacion", false)]
    [InlineData("resincronizacion", true)]
    [InlineData("resincronizacion", false)]
    [InlineData("cierre", true)]
    [InlineData("cierre", false)]
    [InlineData("avisos", true)]
    [InlineData("avisos", false)]
    [InlineData("correo", true)]
    [InlineData("correo", false)]
    [InlineData("dominios", true)]
    [InlineData("dominios", false)]
    public async Task Trabajo_FalloRedis_ContinuaYReintenta(string tipo, bool timeout)
    {
        var alcances = Substitute.For<IServiceScopeFactory>();
        var repetido = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var intentos = 0;
        Exception fallo = timeout
            ? new RedisTimeoutException(CommandFlags.None, "Timeout simulado", CommandStatus.Sent)
            : new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Sin conexión simulada");
        alcances.CreateScope().Returns(_ =>
        {
            if (Interlocked.Increment(ref intentos) > 1)
            {
                repetido.TrySetResult();
            }

            throw fallo;
        });
        using var registros = new RegistrosCapturados();
        using var fabrica = LoggerFactory.Create(b => b.AddProvider(registros));
        var intervalo = TimeSpan.FromMilliseconds(20);
        using var reloj = new RelojSistema(TimeProvider.System, new ConfigurationBuilder().Build());
        using BackgroundService servicio = tipo switch
        {
            "consolidacion" => new TrabajoConsolidacion(alcances, new OpcionesConsolidacion { Intervalo = intervalo }, fabrica.CreateLogger<TrabajoConsolidacion>()),
            "resincronizacion" => new TrabajoResincronizacion(alcances, new OpcionesResincronizacion { Intervalo = intervalo }, fabrica.CreateLogger<TrabajoResincronizacion>()),
            "cierre" => new TrabajoCierreCiclo(alcances, reloj, new OpcionesCierreCiclo { Intervalo = intervalo }, fabrica.CreateLogger<TrabajoCierreCiclo>()),
            "avisos" => new TrabajoAvisosCuota(alcances, fabrica.CreateLogger<TrabajoAvisosCuota>(), new TiempoRapido()),
            "correo" => new DespachadorCorreos(alcances, new TiempoRapido(), fabrica.CreateLogger<DespachadorCorreos>()),
            "dominios" => new TrabajoVerificacionDominios(alcances, new OpcionesVerificacionDominios { Intervalo = intervalo }, fabrica.CreateLogger<TrabajoVerificacionDominios>()),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
        };
        await servicio.StartAsync(CancellationToken.None);
        try
        {
            await repetido.Task.WaitAsync(TimeSpan.FromSeconds(5));
            servicio.ExecuteTask!.IsCompleted.Should().BeFalse();
            registros.Entradas.Should().Contain(e => e.Nivel == LogLevel.Warning && e.Texto.Contains(fallo.GetType().Name));
        }
        finally
        {
            await servicio.StopAsync(CancellationToken.None);
        }
    }

    private sealed class TiempoRapido : TimeProvider
    {
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            System.CreateTimer(callback, state, TimeSpan.FromMilliseconds(20),
                period == Timeout.InfiniteTimeSpan ? period : TimeSpan.FromMilliseconds(20));
    }
}
