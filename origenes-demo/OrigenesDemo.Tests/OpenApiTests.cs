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
        var resultado = await OpenApiDocument.LoadAsync(ruta, new OpenApiReaderSettings());

        var diagnostico = resultado.Diagnostic;
        diagnostico.Should().NotBeNull();
        diagnostico!.Errors.Should().BeEmpty();
        diagnostico.SpecificationVersion.Should().Be(OpenApiSpecVersion.OpenApi3_0);

        var documento = resultado.Document;
        documento.Should().NotBeNull();
        documento!.Paths.Keys.Should().Contain(rutas);

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
