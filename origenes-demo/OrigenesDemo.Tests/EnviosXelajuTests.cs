using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using OrigenesDemo.EnviosXelaju;

namespace OrigenesDemo.Tests;

public sealed class EnviosXelajuTests(WebApplicationFactory<EnviosXelajuAplicacion> fabrica)
    : IClassFixture<WebApplicationFactory<EnviosXelajuAplicacion>>
{
    [Fact]
    public async Task Cotizaciones_EjemploDelMockup_DevuelveTarifaYEntrega()
    {
        // JZ-02 CA1
        using var cliente = fabrica.CreateClient();
        var solicitud = new
        {
            origen = "0901",
            destino = "0301",
            peso_kg = 2.5m,
            tipo_servicio = "normal",
        };

        var respuesta = await cliente.PostAsJsonAsync("/cotizaciones", solicitud);
        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        contenido.GetProperty("origen").GetString().Should().Be("Quetzaltenango");
        contenido.GetProperty("destino").GetString().Should().Be("Antigua Guatemala");
        contenido.GetProperty("tarifa").GetString().Should().Be("Q 38.50");
        contenido.GetProperty("entrega_estimada").GetString().Should().Be("2 días hábiles");
    }

    [Fact]
    public async Task Guias_DatosValidos_DevuelveNumeroDeGuia()
    {
        // JZ-02 CA1
        using var cliente = fabrica.CreateClient();
        var solicitud = new
        {
            origen = "0901",
            destino = "0301",
            destinatario = "Mercadito Antigua",
            direccion = "5a avenida norte 12, Antigua Guatemala",
            peso_kg = 2.5m,
        };

        var respuesta = await cliente.PostAsJsonAsync("/guias", solicitud);
        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        contenido.GetProperty("numero_guia").GetString().Should().Be("GX-2026-0001");
    }

    [Theory]
    [InlineData("/tarifas", "tarifas")]
    [InlineData("/rastreo?guia=GX-2026-0001", "eventos")]
    [InlineData("/cobertura?municipio=0301", "cubierto")]
    public async Task Consultas_RutaDisponible_DevuelveDatos(string ruta, string propiedad)
    {
        // JZ-02 CA1
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
