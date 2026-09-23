using System.Globalization;

namespace Shapi.Contratos.Redis;

/// <summary>
/// Contenido del hash <c>api:{api_id}</c> (07 §4). Lo escribe la API de control y lo lee la compuerta;
/// los nombres de los campos solo se definen aquí.
/// </summary>
/// <param name="Secreto">Se envía al origen en <c>X-Shapi-Secreto</c>; <c>null</c> si la API no tiene.</param>
/// <param name="PortalHost">Host del portal de la API, para CORS (08 §6).</param>
public sealed record ContextoApi(
    Guid ApiId,
    Guid OrganizacionId,
    string Estado,
    string UrlOrigen,
    string? Secreto,
    string? PortalHost,
    long Version)
{
    public const string EstadoPublicada = "publicada";

    public const string CampoOrganizacionId = "organizacion_id";
    public const string CampoEstado = "estado";
    public const string CampoUrlOrigen = "url_origen";
    public const string CampoSecreto = "secreto";
    public const string CampoPortalHost = "portal_host";
    public const string CampoVersion = "version";

    public bool EstaPublicada => Estado == EstadoPublicada;

    /// <summary>Los campos del hash. Los opcionales vacíos no se escriben.</summary>
    public IReadOnlyDictionary<string, string> ACampos()
    {
        var campos = new Dictionary<string, string>
        {
            [CampoOrganizacionId] = OrganizacionId.ToString(),
            [CampoEstado] = Estado,
            [CampoUrlOrigen] = UrlOrigen,
            [CampoVersion] = Version.ToString(CultureInfo.InvariantCulture),
        };
        if (!string.IsNullOrEmpty(Secreto))
        {
            campos[CampoSecreto] = Secreto;
        }

        if (!string.IsNullOrEmpty(PortalHost))
        {
            campos[CampoPortalHost] = PortalHost;
        }

        return campos;
    }

    /// <returns><c>null</c> si falta algún campo obligatorio o tiene un formato inválido.</returns>
    public static ContextoApi? DesdeCampos(Guid apiId, IReadOnlyDictionary<string, string> campos)
    {
        if (!campos.TryGetValue(CampoOrganizacionId, out var organizacion)
            || !Guid.TryParse(organizacion, out var organizacionId)
            || !campos.TryGetValue(CampoEstado, out var estado)
            || !campos.TryGetValue(CampoUrlOrigen, out var urlOrigen)
            || !campos.TryGetValue(CampoVersion, out var textoVersion)
            || !long.TryParse(textoVersion, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
        {
            return null;
        }

        return new ContextoApi(apiId, organizacionId, estado, urlOrigen, Opcional(campos, CampoSecreto),
            Opcional(campos, CampoPortalHost), version);
    }

    private static string? Opcional(IReadOnlyDictionary<string, string> campos, string campo) =>
        campos.TryGetValue(campo, out var valor) && valor.Length > 0 ? valor : null;
}
