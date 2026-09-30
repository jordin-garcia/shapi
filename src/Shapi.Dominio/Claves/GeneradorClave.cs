using System.Security.Cryptography;
using System.Text;

namespace Shapi.Dominio.Claves;

/// <summary>Una clave recién generada: el valor en claro, que se muestra una sola vez, y lo que se guarda (ADR-01).</summary>
public sealed record ClaveGenerada(string EnClaro, string Prefijo, string Ultimos4, string HashSha256);

/// <summary>Genera claves de API con el formato de 08 §2: <c>{prefijo}{26 caracteres base62}</c>.</summary>
public static class GeneradorClave
{
    public const string PrefijoProduccion = "shp_prod_";
    public const string PrefijoPruebas = "shp_prueba_";

    /// <summary>26 caracteres base62: unos 154 bits de entropía (08 §2).</summary>
    public const int LongitudAleatoria = 26;

    private const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static ClaveGenerada Generar(TipoClave tipo)
    {
        var prefijo = Prefijo(tipo);
        // GetString elige cada carácter de forma uniforme, sin el sesgo del módulo.
        var enClaro = prefijo + RandomNumberGenerator.GetString(Base62, LongitudAleatoria);
        return new ClaveGenerada(enClaro, prefijo, enClaro[^4..], CalcularHash(enClaro));
    }

    public static string Prefijo(TipoClave tipo) => tipo switch
    {
        TipoClave.Produccion => PrefijoProduccion,
        TipoClave.Pruebas => PrefijoPruebas,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };

    /// <summary>
    /// SHA-256 de la clave completa en UTF-8, en hex minúsculas. Es el mismo cálculo que hace la compuerta con
    /// <c>ContextoClave.CalcularHash</c>, en <c>Shapi.Contratos</c>.
    /// </summary>
    public static string CalcularHash(string clave) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(clave)));
}
