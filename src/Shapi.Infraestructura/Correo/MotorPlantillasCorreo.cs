using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Shapi.Infraestructura.Correo;

public sealed partial class MotorPlantillasCorreo
{
    private const string PrefijoRecursos = "Shapi.Infraestructura.Correo.Plantillas";
    private readonly string _dominioBase;

    public MotorPlantillasCorreo(IConfiguration configuracion)
    {
        _dominioBase = configuracion["SHAPI_DOMINIO_BASE"]?.Trim().TrimEnd('.') ?? "shapi.localhost";
        if (string.IsNullOrWhiteSpace(_dominioBase))
        {
            throw new InvalidOperationException("SHAPI_DOMINIO_BASE no puede estar vacío.");
        }
    }

    public CorreoRenderizado Renderizar(string plantilla, string datosJson)
    {
        var datos = LeerDatos(datosJson);
        AgregarEnlace(plantilla, datos);

        var html = Reemplazar(Cargar(plantilla, "html"), datos, escaparHtml: true);
        var texto = Reemplazar(Cargar(plantilla, "txt"), datos, escaparHtml: false);
        datos.TryGetValue("nombrePortal", out var nombrePortal);

        return new CorreoRenderizado(html, texto, nombrePortal);
    }

    private void AgregarEnlace(string plantilla, IDictionary<string, string> datos)
    {
        if (plantilla is not ("verificacion_correo" or "recuperacion"))
        {
            return;
        }

        if (!datos.TryGetValue("token", out var token) || string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException($"Falta el dato 'token' para la plantilla '{plantilla}'.");
        }

        var ruta = plantilla == "verificacion_correo" ? "verificar-correo" : "restablecer";
        datos["enlace"] = $"https://{_dominioBase}/{ruta}?token={Uri.EscapeDataString(token)}";
    }

    private static Dictionary<string, string> LeerDatos(string datosJson)
    {
        using var documento = JsonDocument.Parse(datosJson);
        if (documento.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Los datos de una plantilla de correo deben ser un objeto JSON.");
        }

        return documento.RootElement.EnumerateObject().ToDictionary(
            propiedad => propiedad.Name,
            propiedad => propiedad.Value.ValueKind == JsonValueKind.String
                ? propiedad.Value.GetString() ?? string.Empty
                : propiedad.Value.ToString(),
            StringComparer.Ordinal);
    }

    private static string Reemplazar(
        string plantilla,
        IReadOnlyDictionary<string, string> datos,
        bool escaparHtml) => Marcador().Replace(plantilla, coincidencia =>
    {
        var nombre = coincidencia.Groups[1].Value;
        if (!datos.TryGetValue(nombre, out var valor))
        {
            throw new InvalidOperationException($"Falta el dato '{nombre}' requerido por la plantilla.");
        }

        return escaparHtml ? WebUtility.HtmlEncode(valor) : valor;
    });

    private static string Cargar(string plantilla, string extension)
    {
        if (!NombrePlantilla().IsMatch(plantilla))
        {
            throw new InvalidOperationException($"El nombre de plantilla '{plantilla}' no es válido.");
        }

        var nombreRecurso = $"{PrefijoRecursos}.{plantilla}.{extension}";
        using var flujo = typeof(MotorPlantillasCorreo).Assembly.GetManifestResourceStream(nombreRecurso)
            ?? throw new InvalidOperationException($"No existe la plantilla '{plantilla}'.");
        using var lector = new StreamReader(flujo);
        return lector.ReadToEnd();
    }

    [GeneratedRegex(@"{{\s*([A-Za-z][A-Za-z0-9_]*)\s*}}", RegexOptions.CultureInvariant)]
    private static partial Regex Marcador();

    [GeneratedRegex(@"^[a-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex NombrePlantilla();
}
