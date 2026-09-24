using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace OrigenesDemo.Tests;

public sealed class OpenApiTests
{
    public static TheoryData<string, string[]> Especificaciones => new()
    {
        {
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml",
            ["/cotizaciones", "/guias", "/tarifas", "/rastreo", "/cobertura", "/salud"]
        },
        {
            "origenes-demo/agro-precios/openapi.yaml",
            ["/precios", "/productos", "/mercados", "/historial", "/salud"]
        },
    };

    [Theory]
    [MemberData(nameof(Especificaciones))]
    public async Task Especificacion_OpenApi303_ValidaYDocumentaRutas(string rutaRelativa, string[] rutas)
    {
        // JZ-02 CA3
        var ruta = Path.Combine(RaizRepositorio.Ruta, rutaRelativa);
        var configuracion = new OpenApiReaderSettings();
        configuracion.AddYamlReader();
        var resultado = await OpenApiDocument.LoadAsync(ruta, configuracion);

        var diagnostico = resultado.Diagnostic;
        diagnostico.Should().NotBeNull();
        diagnostico!.Errors.Should().BeEmpty();
        diagnostico.SpecificationVersion.Should().Be(OpenApiSpecVersion.OpenApi3_0);

        var documento = resultado.Document;
        documento.Should().NotBeNull();
        documento!.Paths.Keys.Should().Contain(rutas);

        if (rutaRelativa.Contains("envios-xelaju", StringComparison.Ordinal))
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

        foreach (var camino in documento.Paths.Values)
        {
            camino.Operations.Should().NotBeNull();
            camino.Operations!.Values.Should().OnlyContain(operacion =>
                !string.IsNullOrWhiteSpace(operacion.Description));
        }

        var yaml = await File.ReadAllTextAsync(ruta);
        yaml.Should().Contain("required:");
        yaml.Should().Contain("type:");
        yaml.Should().Contain("example:");
    }
}
