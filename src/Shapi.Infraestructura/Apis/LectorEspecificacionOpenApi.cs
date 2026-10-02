using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Shapi.Aplicacion.Apis;
using Shapi.Dominio.Apis;
using SharpYaml.Serialization;

namespace Shapi.Infraestructura.Apis;

public sealed class LectorEspecificacionOpenApi : ILectorEspecificacionOpenApi
{
    public async Task<ResultadoLecturaEspecificacion> Leer(
        string contenido,
        string nombreArchivo,
        CancellationToken cancelacion = default)
    {
        var formato = contenido.AsSpan().TrimStart().StartsWith("{") ? "json" : "yaml";
        try
        {
            var opciones = new OpenApiReaderSettings();
            opciones.AddYamlReader();
            await using var flujo = new MemoryStream(Encoding.UTF8.GetBytes(contenido));
            var resultado = await OpenApiDocument.LoadAsync(flujo, formato, opciones, cancelacion);
            var diagnostico = resultado.Diagnostic;
            if (resultado.Document is null || diagnostico is null || diagnostico.Errors.Count > 0)
            {
                var error = diagnostico?.Errors.FirstOrDefault();
                return Fallo(error?.Pointer ?? nombreArchivo, error?.Message ?? "No se pudo leer el documento.");
            }

            if (diagnostico.SpecificationVersion is not (OpenApiSpecVersion.OpenApi3_0 or OpenApiSpecVersion.OpenApi3_1))
            {
                return Fallo("openapi", "Solo se admiten documentos OpenAPI 3.0 o 3.1.");
            }

            var documento = resultado.Document;
            if (documento.Info is null || string.IsNullOrWhiteSpace(documento.Info.Title))
            {
                return Fallo("info.title", "El título de la especificación es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(documento.Info.Version))
            {
                return Fallo("info.version", "La versión de la API es obligatoria.");
            }

            var operaciones = new List<OperacionEspecificacion>();
            var orden = 0;
            if (documento.Paths is not null)
            {
                foreach (var camino in documento.Paths)
                {
                    if (camino.Value.Operations is null)
                    {
                        continue;
                    }

                    foreach (var par in camino.Value.Operations)
                    {
                        if (!ConvertirMetodo(par.Key, out var metodo))
                        {
                            return Fallo($"paths.{camino.Key}", $"El método {par.Key} no es compatible con Shapi.");
                        }

                        operaciones.Add(new OperacionEspecificacion(
                            metodo,
                            camino.Key,
                            par.Value.Summary,
                            par.Value.Description,
                            await Definicion(camino.Value, par.Value, orden++, diagnostico.SpecificationVersion, cancelacion),
                            orden - 1));
                    }
                }
            }

            return new ResultadoLecturaEspecificacion(
                new EspecificacionLeida(
                    formato == "json" ? EspecificacionFormato.Json : EspecificacionFormato.Yaml,
                    ObtenerVersionExacta(contenido, formato),
                    documento.Info.Title,
                    documento.Info.Description,
                    documento.Info.Version,
                    operaciones),
                null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Fallo(Ubicacion(ex.Message, nombreArchivo), ex.Message);
        }
    }

    private static async Task<string> Definicion(
        IOpenApiPathItem camino,
        OpenApiOperation operacion,
        int orden,
        OpenApiSpecVersion version,
        CancellationToken cancelacion)
    {
        var completa = new OpenApiOperation(operacion);
        if (camino.Parameters is { Count: > 0 })
        {
            completa.Parameters = [.. camino.Parameters, .. operacion.Parameters ?? []];
        }

        using var texto = new StringWriter(CultureInfo.InvariantCulture);
        var escritor = new OpenApiJsonWriter(texto, new OpenApiWriterSettings
        {
            InlineLocalReferences = true,
            InlineExternalReferences = true,
        });
        if (version == OpenApiSpecVersion.OpenApi3_1)
        {
            completa.SerializeAsV31(escritor);
        }
        else
        {
            completa.SerializeAsV3(escritor);
        }
        await escritor.FlushAsync(cancelacion);
        var original = JsonNode.Parse(texto.ToString())!.AsObject();
        var definicion = new JsonObject
        {
            ["orden"] = orden,
            ["parametros"] = original["parameters"]?.DeepClone() ?? new JsonArray(),
            ["cuerpo"] = original["requestBody"]?.DeepClone(),
            ["respuestas"] = original["responses"]?.DeepClone() ?? new JsonObject(),
        };
        return definicion.ToJsonString();
    }

    private static bool ConvertirMetodo(HttpMethod origen, out MetodoHttp metodo)
    {
        if (origen == HttpMethod.Get)
        {
            metodo = MetodoHttp.Get;
        }
        else if (origen == HttpMethod.Post)
        {
            metodo = MetodoHttp.Post;
        }
        else if (origen == HttpMethod.Put)
        {
            metodo = MetodoHttp.Put;
        }
        else if (origen == HttpMethod.Patch)
        {
            metodo = MetodoHttp.Patch;
        }
        else if (origen == HttpMethod.Delete)
        {
            metodo = MetodoHttp.Delete;
        }
        else if (origen == HttpMethod.Head)
        {
            metodo = MetodoHttp.Head;
        }
        else if (origen == HttpMethod.Options)
        {
            metodo = MetodoHttp.Options;
        }
        else
        {
            metodo = default;
            return false;
        }
        return true;
    }

    private static ResultadoLecturaEspecificacion Fallo(string ubicacion, string mensaje) =>
        new(null, new ErrorLecturaEspecificacion(string.IsNullOrWhiteSpace(ubicacion) ? "documento" : ubicacion, mensaje));

    private static string Ubicacion(string mensaje, string archivo)
    {
        var indice = mensaje.IndexOf("line", StringComparison.OrdinalIgnoreCase);
        return indice >= 0 ? mensaje[indice..] : archivo;
    }

    private static string ObtenerVersionExacta(string contenido, string formato)
    {
        if (formato == "json")
        {
            return JsonNode.Parse(contenido)?["openapi"]?.GetValue<string>()
                ?? throw new InvalidOperationException("El documento no declara su versión OpenAPI.");
        }

        var yaml = new YamlStream();
        yaml.Load(new StringReader(contenido));
        if (yaml.Documents.FirstOrDefault()?.RootNode is YamlMappingNode raiz
            && raiz.Children.TryGetValue(new YamlScalarNode("openapi"), out var nodo)
            && nodo is YamlScalarNode version
            && !string.IsNullOrWhiteSpace(version.Value))
        {
            return version.Value;
        }

        throw new InvalidOperationException("El documento no declara su versión OpenAPI.");
    }
}
