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

        var sinCabecera = await cliente.GetAsync("/tarifas");
        cliente.DefaultRequestHeaders.Add("X-Shapi-Secreto", "cualquier-valor");
        var conCabecera = await cliente.GetAsync("/tarifas");

        sinCabecera.StatusCode.Should().Be(HttpStatusCode.OK);
        conCabecera.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(2.5, "normal", "Q 38.50", "2 días hábiles")]
    [InlineData(2.5, "urgente", "Q 57.75", "1 día hábil")]
    [InlineData(10, null, "Q 100.00", "2 días hábiles")]
    [InlineData(0.5, "normal", "Q 22.10", "2 días hábiles")]
    [InlineData(200, "normal", "Q 1,658.00", "2 días hábiles")]
    public async Task Cotizaciones_CalculaLaTarifaConElPesoYLasTarifas(
        decimal pesoKg,
        string? tipoServicio,
        string tarifa,
        string entrega)
    {
        // JZ-02 CA1 (auditoría H-117): precio_base + precio_por_kg × peso_kg, con la tabla de /tarifas.
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/cotizaciones", new
        {
            origen = "0901",
            destino = "0301",
            peso_kg = pesoKg,
            tipo_servicio = tipoServicio,
        });
        var contenido = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        contenido.GetProperty("tarifa").GetString().Should().Be(tarifa);
        contenido.GetProperty("entrega_estimada").GetString().Should().Be(entrega);
    }

    [Fact]
    public async Task Tarifas_DevuelveLasDosTarifasEnQuetzales()
    {
        // JZ-02 CA1 (auditoría H-77)
        using var cliente = fabrica.CreateClient();

        var contenido = await cliente.GetFromJsonAsync<JsonElement>("/tarifas");

        contenido.GetProperty("moneda").GetString().Should().Be("GTQ");
        var tarifas = contenido.GetProperty("tarifas");
        tarifas.GetArrayLength().Should().Be(2);
        tarifas[0].GetProperty("tipo_servicio").GetString().Should().Be("normal");
        tarifas[0].GetProperty("precio_base").GetDecimal().Should().Be(18.00m);
        tarifas[0].GetProperty("precio_por_kg").GetDecimal().Should().Be(8.20m);
        tarifas[1].GetProperty("tipo_servicio").GetString().Should().Be("urgente");
        tarifas[1].GetProperty("precio_base").GetDecimal().Should().Be(27.00m);
        tarifas[1].GetProperty("precio_por_kg").GetDecimal().Should().Be(12.30m);
    }

    [Fact]
    public async Task Rastreo_DevuelveLaGuiaYSusEventosEnOrden()
    {
        // JZ-02 CA1 (auditoría H-77)
        using var cliente = fabrica.CreateClient();

        var contenido = await cliente.GetFromJsonAsync<JsonElement>("/rastreo?guia=GX-2026-0042");

        contenido.GetProperty("numero_guia").GetString().Should().Be("GX-2026-0042");
        contenido.GetProperty("estado").GetString().Should().Be("en_transito");
        var eventos = contenido.GetProperty("eventos");
        eventos.GetArrayLength().Should().Be(2);
        eventos[0].GetProperty("descripcion").GetString().Should().Be("Guía creada");
        eventos[0].GetProperty("ubicacion").GetString().Should().Be("Quetzaltenango");
        eventos[1].GetProperty("descripcion").GetString().Should().Be("Paquete en tránsito");
        eventos[1].GetProperty("ubicacion").GetString().Should().Be("Chimaltenango");
    }

    [Theory]
    [InlineData("0901", "Quetzaltenango", true, 1)]
    [InlineData("0301", "Antigua Guatemala", true, 2)]
    [InlineData("0101", "Ciudad de Guatemala", true, 2)]
    [InlineData("1701", "1701", false, null)]
    public async Task Cobertura_DevuelveElMunicipioYSiTieneCobertura(
        string municipio,
        string nombre,
        bool cubierto,
        int? diasHabiles)
    {
        // JZ-02 CA1 (auditoría H-77)
        using var cliente = fabrica.CreateClient();

        var contenido = await cliente.GetFromJsonAsync<JsonElement>($"/cobertura?municipio={municipio}");

        contenido.GetProperty("municipio").GetString().Should().Be(municipio);
        contenido.GetProperty("nombre").GetString().Should().Be(nombre);
        contenido.GetProperty("cubierto").GetBoolean().Should().Be(cubierto);
        if (diasHabiles is not null)
        {
            contenido.GetProperty("dias_habiles").GetInt32().Should().Be(diasHabiles);
        }
    }
}
