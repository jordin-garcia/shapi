namespace Shapi.Compuerta;

/// <summary>Nombres de las cabeceras de la compuerta (08 §1, §5 y §6).</summary>
public static class CabecerasCompuerta
{
    public const string ApiKey = "X-Api-Key";
    public const string Consumidor = "X-Shapi-Consumidor";
    public const string Entorno = "X-Shapi-Entorno";
    public const string Secreto = "X-Shapi-Secreto";

    /// <summary>El origen no recibe ninguna cabecera con este prefijo que no haya puesto la compuerta (08 §5).</summary>
    public const string PrefijoShapi = "X-Shapi-";

    /// <summary>Las cookies de sesión de Shapi, que nunca llegan al origen (08 §5): personal y consumidor.</summary>
    public static readonly IReadOnlyList<string> CookiesShapi = ["shapi_sesion", "portal_sesion"];

    /// <summary><c>Access-Control-Allow-Headers</c> (08 §6).</summary>
    public static readonly IReadOnlyList<string> PermitidasCors = [ApiKey, "Content-Type", "Accept"];

    /// <summary><c>Access-Control-Expose-Headers</c>: todas las de 08 §5.</summary>
    public static readonly IReadOnlyList<string> ExpuestasCors =
    [
        "X-Shapi-Plan", "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset", "X-Cuota-Limite",
        "X-Cuota-Restante", "X-Cuota-Reinicio", "X-Shapi-Cache",
    ];
}
