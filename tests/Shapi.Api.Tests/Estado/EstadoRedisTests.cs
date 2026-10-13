using Shapi.Api.Tests.Cache;
using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Estado;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Estado;

[Collection(nameof(RedisCache))]
public sealed class EstadoRedisTests(RedisCache redis)
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 16, 40, 23, TimeSpan.Zero);

    // RF-39: ventana móvil con precisión de un segundo, cruza la hora y excluye el día acumulado.
    [Fact]
    public async Task RF_39_Ventana_SoloSumaMuestrasDeLaUltimaHora()
    {
        await redis.VaciarAsync();
        var db = redis.Conexion.GetDatabase();
        var segundo = Ahora.ToUnixTimeSeconds();
        await Medir(db, segundo - 3601, 9, 10000);
        await Medir(db, segundo - 3600, 9, 10000);
        await Medir(db, segundo - 3599, 1, 50);
        await Medir(db, segundo, 1, 50);
        await Medir(db, segundo + 1, 9, 10000);
        var salud = await Lector().LeerAsync(CancellationToken.None);
        Assert.Equal(9.75m, salud.Latencia.Milisegundos);
        Assert.False(salud.Latencia.Supera2500);
        // Avanzar una hora elimina todas las muestras válidas anteriores; el rango abierto sigue representado.
        var despues = new LectorEstadoRedis(redis.Conexion, new Tiempo(Ahora.AddHours(1)));
        Assert.True((await despues.LeerAsync(CancellationToken.None)).Latencia.Supera2500);
    }

    [Fact]
    public async Task RF_39_Latidos_ExigenTtlYNoComparanLasFechasDeDemostracion()
    {
        await redis.VaciarAsync();
        var db = redis.Conexion.GetDatabase();
        await db.StringSetAsync(LlavesRedis.SaludTrabajador, Ahora.AddYears(1).ToString("O"), TimeSpan.FromSeconds(30));
        await db.StringSetAsync(LlavesRedis.SaludCompuerta("a"), "marca", TimeSpan.FromSeconds(30));
        var salud = await Lector().LeerAsync(CancellationToken.None);
        Assert.True(salud.Trabajador);
        Assert.True(salud.Compuerta);
        await db.KeyPersistAsync(LlavesRedis.SaludCompuerta("a"));
        await db.KeyDeleteAsync(LlavesRedis.SaludTrabajador);
        salud = await Lector().LeerAsync(CancellationToken.None);
        Assert.False(salud.Trabajador);
        Assert.False(salud.Compuerta);
        Assert.Null(salud.Latencia.Milisegundos);
    }

    [Fact]
    public async Task RF_39_Pendientes_IgnoraUuidNuloYDetectaLotesSeparados()
    {
        await redis.VaciarAsync();
        var db = redis.Conexion.GetDatabase();
        var global = LlavesRedis.Metricas(new(2026, 10, 12), Guid.Empty, null, null, "produccion");
        await db.SetAddAsync(LlavesRedis.MetricasPendientes, global);
        Assert.False((await Lector().LeerAsync(CancellationToken.None)).Pendientes);
        var identificada = LlavesRedis.Metricas(new(2026, 10, 12), Guid.NewGuid(), null, null, "produccion");
        await db.SetAddAsync(LlavesRedis.MetricasPendientes, identificada);
        Assert.True((await Lector().LeerAsync(CancellationToken.None)).Pendientes);
        await db.SetRemoveAsync(LlavesRedis.MetricasPendientes, identificada);
        await db.HashSetAsync(LlavesRedis.LoteMetricas(Guid.NewGuid(), identificada), "peticiones", 1);
        Assert.True((await Lector().LeerAsync(CancellationToken.None)).Pendientes);
    }

    private LectorEstadoRedis Lector() => new(redis.Conexion, new Tiempo(Ahora));
    private static Task Medir(IDatabase db, long segundo, int rango, int cantidad) =>
        db.HashSetAsync(LlavesRedis.LatenciaCompuerta(segundo / 60), $"{segundo}:{rango}", cantidad);
    private sealed class Tiempo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
