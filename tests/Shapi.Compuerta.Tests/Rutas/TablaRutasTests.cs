using Shapi.Compuerta.Rutas;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Rutas;

// Criterio 3 de JG-05: coincidencia con patrones OpenAPI y la regla de especificidad de 08 §1.
public class TablaRutasTests
{
    [Theory]
    [InlineData("/guias/{numero}", "/guias/GT123", true)]
    [InlineData("/guias/{numero}", "/guias/", false)]
    [InlineData("/guias/{numero}", "/guias", false)]
    [InlineData("/guias/{numero}", "/guias/GT123/eventos", false)]
    [InlineData("/guias/{numero}/eventos", "/guias/GT123/eventos", true)]
    [InlineData("/guias", "/guias", true)]
    [InlineData("/guias", "/Guias", false)]
    [InlineData("/guias", "/guias/", false)]
    [InlineData("/", "/", true)]
    [InlineData("/archivos/{nombre}.json", "/archivos/reporte.json", true)]
    [InlineData("/archivos/{nombre}.json", "/archivos/reporte.xml", false)]
    [InlineData("/archivos/{nombre}.json", "/archivos/.json", false)]
    [InlineData("/precios/{a}-{b}", "/precios/maiz-frijol", true)]
    [InlineData("/precios/{producto}", "/precios/ma%C3%ADz", true)]
    public void RF_29_Buscar_PatronOpenApi_CoincideSoloConElCaminoCorrespondiente(string patron, string camino, bool coincide)
    {
        var tabla = TablaRutas.Crear([EntornoCompuerta.Ruta("GET", patron)]);

        var ruta = tabla.Buscar("GET", camino);

        (ruta is not null).Should().Be(coincide);
    }

    [Fact]
    public void RF_29_Buscar_OtroMetodo_NoCoincide()
    {
        var tabla = TablaRutas.Crear([EntornoCompuerta.Ruta("GET", "/guias")]);

        tabla.Buscar("POST", "/guias").Should().BeNull();
        tabla.Buscar("get", "/guias").Should().NotBeNull("el método HTTP no distingue mayúsculas al compararse");
    }

    [Fact]
    public void RF_29_Buscar_DosPatronesCoinciden_GanaElDeMasSegmentosLiterales()
    {
        // 08 §1: primero el que tiene más segmentos literales.
        var general = EntornoCompuerta.Ruta("GET", "/guias/{numero}");
        var literal = EntornoCompuerta.Ruta("GET", "/guias/recientes");
        var tabla = TablaRutas.Crear([general, literal]);

        tabla.Buscar("GET", "/guias/recientes").Should().Be(literal);
        tabla.Buscar("GET", "/guias/GT123").Should().Be(general);
    }

    [Fact]
    public void RF_29_Buscar_EmpateEnLiterales_GanaElDeMenosParametros()
    {
        // 08 §1: si empatan en segmentos literales, el que tiene menos parámetros.
        var dosParametros = EntornoCompuerta.Ruta("GET", "/precios/{producto}-{mercado}");
        var unParametro = EntornoCompuerta.Ruta("GET", "/precios/{clave}");
        var tabla = TablaRutas.Crear([dosParametros, unParametro]);

        tabla.Buscar("GET", "/precios/maiz-terminal").Should().Be(unParametro);
    }

    [Fact]
    public void RF_29_Buscar_LaMasEspecificaEstaOculta_DevuelveLaOcultaParaQueSeRechace()
    {
        // RF-10: ocultar /guias/recientes no sirve de nada si /guias/{numero} la deja pasar.
        var oculta = EntornoCompuerta.Ruta("GET", "/guias/recientes", expuesta: false);
        var tabla = TablaRutas.Crear([EntornoCompuerta.Ruta("GET", "/guias/{numero}"), oculta]);

        tabla.Buscar("GET", "/guias/recientes").Should().Be(oculta);
    }

    [Fact]
    public void RF_29_Buscar_EmpateTotalEntreExpuestaYOculta_DevuelveLaOculta()
    {
        // Dos patrones igual de específicos que coinciden: ante la duda, no se deja pasar.
        var oculta = EntornoCompuerta.Ruta("GET", "/{a}/fijo", expuesta: false);
        var tabla = TablaRutas.Crear([EntornoCompuerta.Ruta("GET", "/fijo/{b}"), oculta]);

        tabla.Buscar("GET", "/fijo/fijo").Should().Be(oculta);
    }

    [Fact]
    public void RF_29_Crear_PatronInvalido_SeIgnora()
    {
        var valida = EntornoCompuerta.Ruta("GET", "/guias");
        var tabla = TablaRutas.Crear([EntornoCompuerta.Ruta("GET", "sin-barra"), EntornoCompuerta.Ruta("GET", "/a/{abierta"), valida]);

        tabla.Buscar("GET", "/guias").Should().Be(valida);
        tabla.Buscar("GET", "sin-barra").Should().BeNull();
    }

    [Fact]
    public void RF_29_MetodosExpuestos_SoloLosDeLasRutasExpuestasSinRepetir()
    {
        // 08 §6: Access-Control-Allow-Methods lleva los métodos de las rutas expuestas.
        var tabla = TablaRutas.Crear(
        [
            EntornoCompuerta.Ruta("POST", "/cotizaciones"),
            EntornoCompuerta.Ruta("GET", "/rastreo"),
            EntornoCompuerta.Ruta("GET", "/cobertura"),
            EntornoCompuerta.Ruta("DELETE", "/guias/{numero}", expuesta: false),
        ]);

        tabla.MetodosExpuestos.Should().Equal("GET", "POST");
    }

    [Fact]
    public void RF_29_Vacia_NoCoincideConNada()
    {
        TablaRutas.Vacia.Buscar("GET", "/").Should().BeNull();
        TablaRutas.Vacia.MetodosExpuestos.Should().BeEmpty();
    }

    [Fact]
    public void RF_29_Crear_DesdeElJsonDeRedis_UsaTodasLasRutas()
    {
        var rutas = new[] { EntornoCompuerta.Ruta("GET", "/a"), EntornoCompuerta.Ruta("GET", "/b", expuesta: false) };

        var tabla = TablaRutas.DesdeJson(RutaCache.Serializar(rutas));

        tabla.Buscar("GET", "/a").Should().Be(rutas[0]);
        tabla.Buscar("GET", "/b").Should().Be(rutas[1]);
        TablaRutas.DesdeJson("no es json").Buscar("GET", "/a").Should().BeNull();
        TablaRutas.DesdeJson(null).Buscar("GET", "/a").Should().BeNull();
    }
}
