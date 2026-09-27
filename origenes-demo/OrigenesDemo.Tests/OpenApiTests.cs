using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using OrigenesDemo.AgroPrecios;
using OrigenesDemo.EnviosXelaju;

namespace OrigenesDemo.Tests;

public sealed class OpenApiTests(
    WebApplicationFactory<EnviosXelajuAplicacion> fabricaEnvios,
    WebApplicationFactory<AgroPreciosAplicacion> fabricaAgro)
    : IClassFixture<WebApplicationFactory<EnviosXelajuAplicacion>>,
        IClassFixture<WebApplicationFactory<AgroPreciosAplicacion>>
{
    private const string ArchivoEnvios = "origenes-demo/envios-xelaju/cotizacion-envios.yaml";
    private const string ArchivoAgro = "origenes-demo/agro-precios/openapi.yaml";

    /// <summary>Las rutas que muestran A3.3 (Envíos) y A5.1 (Agro): sin <c>/salud</c>, que no es parte de la API.</summary>
    public static TheoryData<string, string[]> Especificaciones => new()
    {
        { ArchivoEnvios, ["/cotizaciones", "/guias", "/tarifas", "/rastreo", "/cobertura"] },
        { ArchivoAgro, ["/precios", "/productos", "/mercados", "/historial"] },
    };

    public static TheoryData<string> Archivos => new() { ArchivoEnvios, ArchivoAgro };

    [Theory]
    [MemberData(nameof(Especificaciones))]
    public async Task Especificacion_OpenApi303_ValidaYDocumentaRutas(string rutaRelativa, string[] rutas)
    {
        // JZ-02 CA3
        var documento = await Cargar(rutaRelativa);

        documento.Paths.Keys.Should().BeEquivalentTo(rutas);

        if (rutaRelativa == ArchivoEnvios)
        {
            documento.Paths["/cotizaciones"].Operations!.Should().ContainKey(HttpMethod.Post);
            documento.Paths["/guias"].Operations!.Should().ContainKey(HttpMethod.Post);
            documento.Paths["/tarifas"].Operations!.Should().ContainKey(HttpMethod.Get);
            documento.Paths["/rastreo"].Operations!.Should().ContainKey(HttpMethod.Get);
            documento.Paths["/cobertura"].Operations!.Should().ContainKey(HttpMethod.Get);
        }
        else
        {
            documento.Paths["/precios"].Operations!.Should().ContainKey(HttpMethod.Get);
            documento.Paths["/productos"].Operations!.Should().ContainKey(HttpMethod.Get);
            documento.Paths["/mercados"].Operations!.Should().ContainKey(HttpMethod.Get);
            documento.Paths["/historial"].Operations!.Should().ContainKey(HttpMethod.Get);
        }

        foreach (var operacion in Operaciones(documento).Select(par => par.Operacion))
        {
            operacion.Description.Should().NotBeNullOrWhiteSpace();

            foreach (var parametro in operacion.Parameters ?? [])
            {
                parametro.Description.Should().NotBeNullOrWhiteSpace();
                parametro.Schema.Should().NotBeNull();
                parametro.Schema!.Type.Should().NotBeNull();
                parametro.Example.Should().NotBeNull();
            }

            if (operacion.RequestBody is not null)
            {
                operacion.RequestBody.Required.Should().BeTrue();
                var peticionJson = operacion.RequestBody.Content!["application/json"];
                peticionJson.Schema.Should().NotBeNull();
                peticionJson.Example.Should().NotBeNull();
            }

            operacion.Responses.Should().ContainKey("200");
            var respuestaJson = operacion.Responses!["200"].Content!["application/json"];
            respuestaJson.Schema.Should().NotBeNull();
            respuestaJson.Example.Should().NotBeNull();
        }

        VerificarParametrosObligatorios(documento, rutaRelativa);
    }

    [Theory]
    [MemberData(nameof(Archivos))]
    public async Task Especificacion_NoDocumentaElSecretoDeOrigen(string rutaRelativa)
    {
        // JZ-02 CA3 (auditoría H-74): el consumidor ve esta documentación en A5.1 y nunca envía el secreto de
        // origen; lo agrega la compuerta.
        var documento = await Cargar(rutaRelativa);

        foreach (var (ruta, metodo, operacion) in Operaciones(documento))
        {
            (operacion.Parameters ?? []).Should().NotContain(
                parametro => parametro.Name == "X-Shapi-Secreto",
                $"{metodo} {ruta} no debe documentar X-Shapi-Secreto");
            operacion.Responses!.Keys.Should().NotContain("401", $"{metodo} {ruta} no debe documentar el 401 del secreto");
        }
    }

    [Fact]
    public async Task Especificacion_TextosDeA51_CoincidenConElMockup()
    {
        // JZ-02 CA3: los textos que muestran A5.1 y A5 (InicioAgro).
        var envios = await Cargar(ArchivoEnvios);
        var cotizaciones = envios.Paths["/cotizaciones"].Operations![HttpMethod.Post];
        cotizaciones.Description.Should().Be("Calcula el costo de un envío según origen, destino, peso y tipo de servicio.");
        var solicitud = cotizaciones.RequestBody!.Content!["application/json"].Schema!.Properties!;
        solicitud["origen"].Description.Should().Be("Código del municipio de origen");
        solicitud["destino"].Description.Should().Be("Código del municipio de destino");
        solicitud["peso_kg"].Description.Should().Be("Peso del paquete en kilogramos");
        solicitud["tipo_servicio"].Description.Should().Be("Normal o urgente. Si se omite, se cotiza como normal");

        var agro = await Cargar(ArchivoAgro);
        agro.Paths["/precios"].Operations![HttpMethod.Get].Description
            .Should().Be("Devuelve el precio del día de un producto en un mercado.");
        agro.Paths["/productos"].Operations![HttpMethod.Get].Description
            .Should().Be("Lista los productos que tienen precio publicado.");
        agro.Paths["/mercados"].Operations![HttpMethod.Get].Description
            .Should().Be("Lista los mercados mayoristas donde se registran precios.");
        agro.Paths["/historial"].Operations![HttpMethod.Get].Description
            .Should().Be("Devuelve los precios de un producto en un rango de fechas.");
        Parametro(agro, "/precios", "producto").Description.Should().Be("Clave del producto");
        Parametro(agro, "/precios", "mercado").Description.Should().Be("Clave del mercado mayorista");
        Parametro(agro, "/precios", "fecha").Description.Should().Be("Fecha del precio. Si se omite, se devuelve el precio del día");
    }

    [Fact]
    public async Task EjemplosDeEnvios_CoincidenConLasRespuestasReales()
    {
        // JZ-02 CA3 (auditoría H-77)
        using var cliente = fabricaEnvios.CreateClient();
        await CompararEjemplos(cliente, await Cargar(ArchivoEnvios));
    }

    [Fact]
    public async Task EjemplosDeAgro_CoincidenConLasRespuestasReales()
    {
        // JZ-02 CA3 (auditoría H-77)
        using var cliente = fabricaAgro.CreateClient();
        await CompararEjemplos(cliente, await Cargar(ArchivoAgro));
    }

    /// <summary>
    /// Arma cada petición con los ejemplos de sus parámetros y de su cuerpo, y exige que la respuesta real sea
    /// igual al ejemplo de la respuesta 200.
    /// </summary>
    private static async Task CompararEjemplos(HttpClient cliente, OpenApiDocument documento)
    {
        foreach (var (ruta, metodo, operacion) in Operaciones(documento))
        {
            var consulta = string.Join('&', (operacion.Parameters ?? [])
                .Where(parametro => parametro.In == ParameterLocation.Query)
                .Select(parametro => $"{parametro.Name}={Uri.EscapeDataString(Texto(parametro.Example!))}"));
            using var peticion = new HttpRequestMessage(metodo, consulta.Length == 0 ? ruta : $"{ruta}?{consulta}");
            if (operacion.RequestBody is not null)
            {
                peticion.Content = JsonContent(operacion.RequestBody.Content!["application/json"].Example!);
            }

            using var respuesta = await cliente.SendAsync(peticion);
            var cuerpo = await respuesta.Content.ReadAsStringAsync();

            respuesta.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, $"{metodo} {ruta} con los ejemplos debe responder 200");
            var esperado = operacion.Responses!["200"].Content!["application/json"].Example!;
            JsonNode.DeepEquals(JsonNode.Parse(cuerpo), esperado).Should().BeTrue(
                $"la respuesta de {metodo} {ruta} debe ser igual a su ejemplo.\nReal: {cuerpo}\nEjemplo: {esperado.ToJsonString()}");
        }
    }

    private static StringContent JsonContent(JsonNode ejemplo) =>
        new(ejemplo.ToJsonString(), System.Text.Encoding.UTF8, "application/json");

    private static string Texto(JsonNode valor) =>
        valor.GetValueKind() == JsonValueKind.String ? valor.GetValue<string>() : valor.ToJsonString();

    private static async Task<OpenApiDocument> Cargar(string rutaRelativa)
    {
        var ruta = Path.Combine(RaizRepositorio.Ruta, rutaRelativa);
        var configuracion = new OpenApiReaderSettings();
        configuracion.AddYamlReader();
        var resultado = await OpenApiDocument.LoadAsync(ruta, configuracion);

        var diagnostico = resultado.Diagnostic;
        diagnostico.Should().NotBeNull();
        diagnostico!.Errors.Should().BeEmpty();
        diagnostico.SpecificationVersion.Should().Be(OpenApiSpecVersion.OpenApi3_0);
        resultado.Document.Should().NotBeNull();
        return resultado.Document!;
    }

    private static IEnumerable<(string Ruta, HttpMethod Metodo, OpenApiOperation Operacion)> Operaciones(
        OpenApiDocument documento) =>
        documento.Paths.SelectMany(camino => camino.Value.Operations!
            .Select(operacion => (camino.Key, operacion.Key, operacion.Value)));

    private static void VerificarParametrosObligatorios(OpenApiDocument documento, string rutaRelativa)
    {
        if (rutaRelativa == ArchivoEnvios)
        {
            Parametro(documento, "/rastreo", "guia").Required.Should().BeTrue();
            Parametro(documento, "/cobertura", "municipio").Required.Should().BeTrue();
            return;
        }

        Parametro(documento, "/precios", "producto").Required.Should().BeTrue();
        Parametro(documento, "/precios", "mercado").Required.Should().BeTrue();
        Parametro(documento, "/precios", "fecha").Required.Should().BeFalse();
        Parametro(documento, "/historial", "producto").Required.Should().BeTrue();
        Parametro(documento, "/historial", "mercado").Required.Should().BeTrue();
        Parametro(documento, "/historial", "desde").Required.Should().BeFalse();
        Parametro(documento, "/historial", "hasta").Required.Should().BeFalse();
    }

    private static IOpenApiParameter Parametro(OpenApiDocument documento, string ruta, string nombre)
    {
        return documento.Paths[ruta].Operations![HttpMethod.Get].Parameters!
            .Single(parametro => parametro.Name == nombre);
    }
}
