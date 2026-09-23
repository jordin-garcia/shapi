using System.Security.Cryptography;
using System.Text;

namespace Shapi.Contratos.Redis;

/// <summary>
/// Contenido del hash <c>clave:{sha256}</c> (07 §4). Lo escribe la API de control y lo lee la compuerta;
/// los nombres de los campos solo se definen aquí.
/// </summary>
/// <param name="Tipo"><see cref="TipoProduccion"/> o <see cref="TipoPruebas"/>.</param>
public sealed record ContextoClave(
    Guid ClaveId,
    Guid SuscripcionId,
    Guid ApiId,
    Guid OrganizacionId,
    Guid ConsumidorId,
    string Tipo)
{
    public const string TipoProduccion = "produccion";
    public const string TipoPruebas = "pruebas";

    public const string CampoClaveId = "clave_id";
    public const string CampoSuscripcionId = "suscripcion_id";
    public const string CampoApiId = "api_id";
    public const string CampoOrganizacionId = "organizacion_id";
    public const string CampoConsumidorId = "consumidor_id";
    public const string CampoTipo = "tipo";

    /// <summary>El valor de <c>X-Shapi-Entorno</c> hacia el origen, que coincide con el tipo de clave (RF-45).</summary>
    public string Entorno => Tipo;

    /// <summary>SHA-256 de la clave completa en UTF-8, en hex minúsculas (08 §2, ADR-01).</summary>
    public static string CalcularHash(string clave) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(clave)));

    public IReadOnlyDictionary<string, string> ACampos() => new Dictionary<string, string>
    {
        [CampoClaveId] = ClaveId.ToString(),
        [CampoSuscripcionId] = SuscripcionId.ToString(),
        [CampoApiId] = ApiId.ToString(),
        [CampoOrganizacionId] = OrganizacionId.ToString(),
        [CampoConsumidorId] = ConsumidorId.ToString(),
        [CampoTipo] = Tipo,
    };

    /// <returns><c>null</c> si falta algún campo o tiene un formato inválido.</returns>
    public static ContextoClave? DesdeCampos(IReadOnlyDictionary<string, string> campos)
    {
        if (!TryGuid(campos, CampoClaveId, out var claveId)
            || !TryGuid(campos, CampoSuscripcionId, out var suscripcionId)
            || !TryGuid(campos, CampoApiId, out var apiId)
            || !TryGuid(campos, CampoOrganizacionId, out var organizacionId)
            || !TryGuid(campos, CampoConsumidorId, out var consumidorId)
            || !campos.TryGetValue(CampoTipo, out var tipo)
            || tipo is not (TipoProduccion or TipoPruebas))
        {
            return null;
        }

        return new ContextoClave(claveId, suscripcionId, apiId, organizacionId, consumidorId, tipo);
    }

    private static bool TryGuid(IReadOnlyDictionary<string, string> campos, string campo, out Guid valor)
    {
        valor = Guid.Empty;
        return campos.TryGetValue(campo, out var texto) && Guid.TryParse(texto, out valor);
    }
}
