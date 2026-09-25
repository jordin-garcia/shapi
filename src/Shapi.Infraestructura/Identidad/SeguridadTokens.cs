using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Shapi.Infraestructura.Identidad;

/// <summary>
/// Clase utilitaria para generar tokens aleatorios seguros y calcular su hash SHA-256.
/// </summary>
public static class SeguridadTokens
{
    /// <summary>
    /// Genera un token aleatorio criptográficamente seguro y lo devuelve codificado en Base64Url.
    /// </summary>
    /// <param name="bytes">Cantidad de bytes (por defecto 32)</param>
    /// <returns>Token codificado (e.g. jZ2n...)</returns>
    public static string GenerarToken(int bytes = 32)
    {
        var buffer = new byte[bytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buffer);
        return Base64UrlTextEncoder.Encode(buffer);
    }

    /// <summary>
    /// Calcula el hash SHA-256 de una cadena UTF-8 y lo devuelve codificado en Base64Url,
    /// o como cadena hexadecimal si se prefiere. El requisito pide SHA-256 (32 bytes).
    /// </summary>
    /// <param name="token">Token en claro</param>
    /// <returns>Hash en formato Base64Url</returns>
    public static string HashearToken(string token)
    {
        var inputBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(inputBytes);
        return Base64UrlTextEncoder.Encode(hashBytes);
    }
}
