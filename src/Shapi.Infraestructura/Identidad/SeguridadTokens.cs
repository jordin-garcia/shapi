using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Shapi.Infraestructura.Identidad;

/// <summary>
/// Valores aleatorios de las cookies de sesión y de los enlaces de un solo uso (10 §1).
/// La base de datos solo guarda su SHA-256 en hex minúsculas (07 §3.1: <c>char(64)</c>).
/// </summary>
public static class SeguridadTokens
{
    /// <summary>Genera 32 bytes aleatorios en Base64Url, aptos para una cookie o un enlace.</summary>
    public static string GenerarToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    /// <summary>SHA-256 del valor en UTF-8, en hex minúsculas (64 caracteres).</summary>
    public static string HashearToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
