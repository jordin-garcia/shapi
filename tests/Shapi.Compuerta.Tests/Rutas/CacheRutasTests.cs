using Shapi.Compuerta.Rutas;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Rutas;

// Criterio 3 de JG-05 y 08 §8: api:{id}:rutas se guarda en memoria 5 segundos como máximo, junto con su version.
public class CacheRutasTests
{
    private static readonly Guid ApiId = Guid.NewGuid();

    private static readonly string Json = RutaCache.Serializar([EntornoCompuerta.Ruta("GET", "/guias")]);

    [Fact]
    public void RNF_02_Vigentes_SinGuardar_DevuelveNull()
    {
        new CacheRutas(new RelojManual()).Vigentes(ApiId).Should().BeNull();
    }

    [Fact]
    public void RNF_02_Vigentes_AntesDeCincoSegundos_DevuelveLasRutasYSuVersion()
    {
        var reloj = new RelojManual();
        var cache = new CacheRutas(reloj);
        cache.Guardar(ApiId, 7, Json);

        reloj.Avanzar(TimeSpan.FromSeconds(4.9));
        var vigentes = cache.Vigentes(ApiId);

        vigentes.Should().NotBeNull();
        vigentes!.Version.Should().Be(7);
        vigentes.Tabla.Buscar("GET", "/guias").Should().NotBeNull();
    }

    [Fact]
    public void RNF_02_Vigentes_ACincoSegundos_YaNoLasDevuelve()
    {
        var reloj = new RelojManual();
        var cache = new CacheRutas(reloj);
        cache.Guardar(ApiId, 7, Json);

        reloj.Avanzar(CacheRutas.Duracion);

        cache.Vigentes(ApiId).Should().BeNull();
    }

    [Fact]
    public void RNF_02_Guardar_OtraVez_ReemplazaLaVersionYReiniciaElTiempo()
    {
        var reloj = new RelojManual();
        var cache = new CacheRutas(reloj);
        cache.Guardar(ApiId, 7, Json);
        reloj.Avanzar(TimeSpan.FromSeconds(4));

        cache.Guardar(ApiId, 8, "[]");
        reloj.Avanzar(TimeSpan.FromSeconds(4));

        var vigentes = cache.Vigentes(ApiId);
        vigentes!.Version.Should().Be(8);
        vigentes.Tabla.Buscar("GET", "/guias").Should().BeNull();
    }

    private sealed class RelojManual : TimeProvider
    {
        private long _marca = 1_000;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _marca;

        public void Avanzar(TimeSpan tiempo) => _marca += tiempo.Ticks;
    }
}
