namespace Shapi.Aplicacion.Comun;

/// <summary>
/// Cifra y descifra el secreto de origen de una API con ASP.NET Data Protection, propósito <c>Shapi.SecretoOrigen</c>
/// (07 §3.2, 10 §3). La API de control y el trabajador comparten el anillo de llaves (<c>SHAPI_DPKEYS_DIR</c>).
/// </summary>
public interface IProtectorSecretoOrigen
{
    string Cifrar(string secreto);

    /// <exception cref="System.Security.Cryptography.CryptographicException">Si el texto no se puede descifrar.</exception>
    string Descifrar(string secretoCifrado);
}
