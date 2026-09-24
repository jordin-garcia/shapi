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
        cliente.DefaultRequestHeaders.Add("X-Shapi-Secreto", "secreto-de-prueba");
        var conSecreto = await cliente.GetAsync("/salud");

        sinSecreto.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        conSecreto.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
