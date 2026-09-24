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
            foreach (var operacion in camino.Operations!.Values)
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
        }

        VerificarParametrosObligatorios(documento, rutaRelativa);
    }

    private static void VerificarParametrosObligatorios(OpenApiDocument documento, string rutaRelativa)
    {
        if (rutaRelativa.Contains("envios-xelaju", StringComparison.Ordinal))
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
