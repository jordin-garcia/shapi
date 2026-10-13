using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Medicion;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Medicion;

public sealed class LatenciaEstadoTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>
{
    // RF-39: muestra global independiente de las métricas diarias, incluso para rechazos sin API.
    [Fact]
    public async Task RF_39_Medicion_AgregaLaLatenciaSinEsperaDelOrigenYConVencimiento()
    {
        var fecha = new DateTimeOffset(2026, 10, 12, 15, 40, 23, TimeSpan.Zero);
        var reloj = new RelojFijo(fecha);
        var llave = LlavesRedis.LatenciaCompuerta(fecha.ToUnixTimeSeconds() / 60);
        var db = entorno.Redis.GetDatabase();
        await db.KeyDeleteAsync(llave);
        var medicion = new MedicionMiddleware(entorno.Redis, reloj, NullLogger<MedicionMiddleware>.Instance);
        var contexto = new ContextoPeticion(new DefaultHttpContext()) { TiempoEsperaOrigen = TimeSpan.FromMilliseconds(100) };
        medicion.Registrar(contexto, DateOnly.FromDateTime(fecha.DateTime), TimeSpan.FromMilliseconds(110), 0, 0);
        // El PING de la misma conexión espera a los comandos FireAndForget anteriores.
        await db.PingAsync();
        var campos = await db.HashGetAllAsync(llave);
        Assert.Equal(1, (long)Assert.Single(campos).Value);
        Assert.Equal($"{fecha.ToUnixTimeSeconds()}:1", campos[0].Name.ToString());
        var ttl = await db.KeyTimeToLiveAsync(llave);
        Assert.InRange(ttl!.Value.TotalMinutes, 64, 65);
        // Una segunda instancia acumula en la misma ventana.
        new MedicionMiddleware(entorno.Redis, reloj, NullLogger<MedicionMiddleware>.Instance)
            .Registrar(contexto, DateOnly.FromDateTime(fecha.DateTime), TimeSpan.FromMilliseconds(110), 0, 0);
        await db.PingAsync();
        Assert.Equal(2, (long)await db.HashGetAsync(llave, $"{fecha.ToUnixTimeSeconds()}:1"));
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
