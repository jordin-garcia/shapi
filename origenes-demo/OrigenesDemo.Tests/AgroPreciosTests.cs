using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using OrigenesDemo.AgroPrecios;

namespace OrigenesDemo.Tests;

public sealed class AgroPreciosTests(WebApplicationFactory<AgroPreciosAplicacion> fabrica)
    : IClassFixture<WebApplicationFactory<AgroPreciosAplicacion>>
{
    [Fact]
    public async Task Precios_EjemploDelMockup_DevuelveFrijolEnCenma()
    {
        // JZ-02 CA2
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/precios?producto=frijol_negro&mercado=cenma");
        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        contenido.GetProperty("producto").GetString().Should().Be("Frijol negro");
        contenido.GetProperty("mercado").GetString().Should().Be("CENMA");
        contenido.GetProperty("unidad").GetString().Should().Be("quintal");
        contenido.GetProperty("precio").GetString().Should().Be("Q 510.00");
        contenido.GetProperty("fecha").GetString().Should().Be("10 sep 2026");
    }

    [Theory]
    [InlineData("/productos", "productos")]
    [InlineData("/mercados", "mercados")]
    [InlineData("/historial?producto=frijol_negro&mercado=cenma&desde=2026-09-08&hasta=2026-09-10", "precios")]
    public async Task CatalogosEHistorial_RutaDisponible_DevuelveDatos(string ruta, string propiedad)
    {
        // JZ-02 CA2
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(ruta);
        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        contenido.TryGetProperty(propiedad, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Historial_RangoDeUnDia_SoloDevuelveEseDia()
    {
        // JZ-02 CA2
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(
            "/historial?producto=frijol_negro&mercado=cenma&desde=2026-09-10&hasta=2026-09-10");
        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var precios = contenido.GetProperty("precios");
        precios.GetArrayLength().Should().Be(1);
        precios[0].GetProperty("fecha").GetString().Should().Be("2026-09-10");
        precios[0].GetProperty("precio").GetString().Should().Be("Q 510.00");
    }

    [Fact]
    public async Task Salud_AplicacionDisponible_Devuelve200()
    {
        // JZ-02 CA5
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecretoOrigen_Configurado_ExigeCabeceraCorrecta()
    {
        // JZ-02 CA4
        await using var fabricaSegura = fabrica.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracion) =>
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SECRETO_ORIGEN"] = "secreto-de-prueba",
                })));
        using var cliente = fabricaSegura.CreateClient();

        var sinSecreto = await cliente.GetAsync("/salud");
        cliente.DefaultRequestHeaders.Add("X-Shapi-Secreto", "secreto-incorrecto");
        var conSecretoIncorrecto = await cliente.GetAsync("/salud");
        cliente.DefaultRequestHeaders.Remove("X-Shapi-Secreto");
        cliente.DefaultRequestHeaders.Add("X-Shapi-Secreto", "secreto-de-prueba");
        var conSecreto = await cliente.GetAsync("/salud");

        sinSecreto.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        conSecretoIncorrecto.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        conSecreto.StatusCode.Should().Be(HttpStatusCode.OK);
        var error = await sinSecreto.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("Secreto de origen inválido.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SecretoOrigen_NoConfigurado_AceptaTodo(string? secretoOrigen)
    {
        // JZ-02 CA4 (auditoría H-77): sin SECRETO_ORIGEN, o vacío como lo pasa infra/compose.yml, no se exige nada.
        await using var fabricaAbierta = fabrica.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracion) =>
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SECRETO_ORIGEN"] = secretoOrigen,
                })));
        using var cliente = fabricaAbierta.CreateClient();

        var sinCabecera = await cliente.GetAsync("/productos");
        cliente.DefaultRequestHeaders.Add("X-Shapi-Secreto", "cualquier-valor");
        var conCabecera = await cliente.GetAsync("/productos");

        sinCabecera.StatusCode.Should().Be(HttpStatusCode.OK);
        conCabecera.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("2026-09-08", "Q 505.00", "8 sep 2026")]
    [InlineData("2026-09-09", "Q 508.00", "9 sep 2026")]
    [InlineData("2026-09-10", "Q 510.00", "10 sep 2026")]
    public async Task Precios_ConFecha_DevuelveElPrecioDeEseDia(string fecha, string precio, string fechaMostrada)
    {
        // JZ-02 CA2 (auditoría H-76)
        using var cliente = fabrica.CreateClient();

        var contenido = await cliente.GetFromJsonAsync<JsonElement>(
            $"/precios?producto=frijol_negro&mercado=cenma&fecha={fecha}");

        contenido.GetProperty("precio").GetString().Should().Be(precio);
        contenido.GetProperty("fecha").GetString().Should().Be(fechaMostrada);
    }

    [Fact]
    public async Task Precios_CadaProductoYMercadoDelCatalogo_CoincideConElHistorial()
    {
        // JZ-02 CA2 (auditoría H-76 y H-116): todo lo que listan /productos y /mercados tiene precio, y /precios
        // devuelve para cada fecha el mismo precio que /historial.
        using var cliente = fabrica.CreateClient();
        var productos = (await cliente.GetFromJsonAsync<JsonElement>("/productos")).GetProperty("productos");
        var mercados = (await cliente.GetFromJsonAsync<JsonElement>("/mercados")).GetProperty("mercados");
        productos.GetArrayLength().Should().Be(3);
        mercados.GetArrayLength().Should().Be(3);

        foreach (var producto in productos.EnumerateArray())
        {
            foreach (var mercado in mercados.EnumerateArray())
            {
                var consulta = $"producto={producto.GetProperty("clave").GetString()}&mercado={mercado.GetProperty("clave").GetString()}";
                var historial = (await cliente.GetFromJsonAsync<JsonElement>($"/historial?{consulta}"))
                    .GetProperty("precios");
                historial.GetArrayLength().Should().Be(3, $"{consulta} debe tener precio los 3 días");

                foreach (var dia in historial.EnumerateArray())
                {
                    var precio = await cliente.GetFromJsonAsync<JsonElement>(
                        $"/precios?{consulta}&fecha={dia.GetProperty("fecha").GetString()}");
                    precio.GetProperty("precio").GetString().Should().Be(dia.GetProperty("precio").GetString());
                    precio.GetProperty("producto").GetString().Should().Be(producto.GetProperty("nombre").GetString());
                    precio.GetProperty("mercado").GetString().Should().Be(mercado.GetProperty("nombre").GetString());
                    precio.GetProperty("unidad").GetString().Should().Be(producto.GetProperty("unidad").GetString());
                }

                var delDia = await cliente.GetFromJsonAsync<JsonElement>($"/precios?{consulta}");
                delDia.GetProperty("precio").GetString().Should().Be(historial[2].GetProperty("precio").GetString());
                delDia.GetProperty("fecha").GetString().Should().Be("10 sep 2026");
            }
        }
    }

    [Theory]
    [InlineData("/precios?producto=frijol_negro&mercado=cenma&fecha=2026-09-11", HttpStatusCode.NotFound)]
    [InlineData("/precios?producto=frijol_negro&mercado=cenma&fecha=2026-09-07", HttpStatusCode.NotFound)]
    [InlineData("/precios?producto=cafe&mercado=cenma", HttpStatusCode.NotFound)]
    [InlineData("/precios?producto=frijol_negro&mercado=otro", HttpStatusCode.NotFound)]
    [InlineData("/precios?producto=frijol_negro&mercado=cenma&fecha=no-es-fecha", HttpStatusCode.BadRequest)]
    public async Task Precios_SinPrecioOFechaInvalida_Rechaza(string ruta, HttpStatusCode esperado)
    {
        // JZ-02 CA2 (auditoría H-76)
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(ruta);

        respuesta.StatusCode.Should().Be(esperado);
        if (esperado == HttpStatusCode.NotFound)
        {
            var error = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
            error.GetProperty("error").GetString().Should().Be("No hay precio para ese producto, mercado y fecha.");
        }
    }
}
