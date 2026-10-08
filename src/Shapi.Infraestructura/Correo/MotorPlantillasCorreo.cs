using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Shapi.Dominio.Apis;

namespace Shapi.Infraestructura.Correo;

public sealed partial class MotorPlantillasCorreo
{
    private const string PrefijoRecursos = "Shapi.Infraestructura.Correo.Plantillas";
    private const string ColorShapi = "#3B6FF0";

    /// <summary>Nombre visible del remitente en los correos del personal (10 §6).</summary>
    public const string NombreRemitentePersonal = "Shapi";
    private readonly string _dominioBase;
    private readonly Regex _hostPortal;

    public MotorPlantillasCorreo(IConfiguration configuracion)
    {
        _dominioBase = configuracion["SHAPI_DOMINIO_BASE"]?.Trim().TrimEnd('.').ToLowerInvariant() ?? "shapi.localhost";
        if (string.IsNullOrWhiteSpace(_dominioBase))
        {
            throw new InvalidOperationException("SHAPI_DOMINIO_BASE no puede estar vacío.");
        }

        // El portal solo vive en {sub}.{dominio_base} (06 §4): una etiqueta ASCII y el dominio base, nada más.
        // Además, {sub} no puede ser un subdominio reservado (HostDelEnlace).
        _hostPortal = new Regex(
            $@"^[a-z0-9](?:[a-z0-9-]{{0,61}}[a-z0-9])?\.{Regex.Escape(_dominioBase)}$",
            RegexOptions.CultureInvariant);
    }

    public CorreoRenderizado Renderizar(string plantilla, string datosJson)
    {
        var datos = LeerDatos(datosJson);
        AgregarEnlace(plantilla, datos);
        var marca = CrearMarca(datos);

        var html = AplicarMarcaHtml(Reemplazar(Cargar(plantilla, "html"), datos, escaparHtml: true), marca);
        var texto = AplicarMarcaTexto(Reemplazar(Cargar(plantilla, "txt"), datos, escaparHtml: false), marca);

        return new CorreoRenderizado(html, texto, marca.Nombre);
    }

    private MarcaCorreo CrearMarca(IReadOnlyDictionary<string, string> datos)
    {
        if (!datos.TryGetValue("nombrePortal", out var nombrePortal))
        {
            return new MarcaCorreo(NombreRemitentePersonal, ColorShapi, null);
        }

        if (string.IsNullOrWhiteSpace(nombrePortal))
        {
            throw new InvalidOperationException("El dato 'nombrePortal' no puede estar vacío.");
        }

        var hostPortal = HostPortalRequerido(datos);
        if (!datos.TryGetValue("colorPortal", out var colorPortal) || !ColorHexadecimal().IsMatch(colorPortal))
        {
            throw new InvalidOperationException("El dato 'colorPortal' debe tener el formato #RRGGBB.");
        }

        var logoPortal = datos.TryGetValue("logoPortal", out var tieneLogo)
            && string.Equals(tieneLogo, "true", StringComparison.Ordinal)
                ? $"https://{hostPortal}/api/portal/logo"
                : null;

        return new MarcaCorreo(nombrePortal, colorPortal, logoPortal);
    }

    private static string AplicarMarcaHtml(string html, MarcaCorreo marca)
    {
        const string aperturaCuerpo = "<body>";
        if (!html.Contains(aperturaCuerpo, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La plantilla HTML debe contener la etiqueta <body>.");
        }

        var nombre = WebUtility.HtmlEncode(marca.Nombre);
        var logo = marca.LogoUrl is null
            ? string.Empty
            : $"  <img src=\"{WebUtility.HtmlEncode(marca.LogoUrl)}\" alt=\"{nombre}\" style=\"display: block; max-width: 200px; max-height: 48px; margin-bottom: 12px;\">\n";
        var encabezado =
            $"<body style=\"margin: 0; color: #0B1220; font-family: 'IBM Plex Sans', Arial, sans-serif; line-height: 1.6;\">\n" +
            $"<header style=\"border-top: 4px solid {marca.Color}; padding: 20px 0 16px; margin-bottom: 24px;\">\n" +
            logo +
            $"  <strong style=\"color: {marca.Color}; font-family: Sora, Arial, sans-serif; font-size: 24px; font-weight: 500;\">{nombre}</strong>\n" +
            "</header>";

        return html.Replace(aperturaCuerpo, encabezado, StringComparison.Ordinal);
    }

    private static string AplicarMarcaTexto(string texto, MarcaCorreo marca) => $"{marca.Nombre}\n\n{texto}";

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

        var host = HostDelEnlace(datos);
        var ruta = plantilla == "verificacion_correo" ? "verificar-correo" : "restablecer";
        datos["enlace"] = $"https://{host}/{ruta}?token={Uri.EscapeDataString(token)}";
    }

    /// <summary>
    /// Los correos de un consumidor traen <c>hostPortal</c> (<c>{sub}.{dominio_base}</c>): su enlace lleva a las
    /// pantallas del portal (A5.8 y A5.10). Los del personal llevan al dominio base (A1.2 y A1.4b).
    /// </summary>
    private string HostDelEnlace(IDictionary<string, string> datos)
    {
        if (!datos.TryGetValue("hostPortal", out var hostPortal))
        {
            return _dominioBase;
        }

        return ValidarHostPortal(hostPortal);
    }

    private string HostPortalRequerido(IReadOnlyDictionary<string, string> datos)
    {
        if (!datos.TryGetValue("hostPortal", out var hostPortal) || string.IsNullOrWhiteSpace(hostPortal))
        {
            throw new InvalidOperationException("Falta el dato 'hostPortal' para la marca del portal.");
        }

        return ValidarHostPortal(hostPortal);
    }

    private string ValidarHostPortal(string hostPortal)
    {
        var host = hostPortal.Trim().ToLowerInvariant();
        if (!_hostPortal.IsMatch(host) || SubdominiosReservados.Contiene(host[..host.IndexOf('.')]))
        {
            throw new InvalidOperationException(
                $"El dato 'hostPortal' debe ser un subdominio de {_dominioBase}: '{hostPortal}'.");
        }

        return host;
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

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorHexadecimal();

    private sealed record MarcaCorreo(string Nombre, string Color, string? LogoUrl);
}
