using Microsoft.AspNetCore.DataProtection;
using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Comun;

/// <summary>Secreto de origen cifrado con ASP.NET Data Protection (07 §3.2, 10 §3).</summary>
public sealed class ProtectorSecretoOrigen(IDataProtectionProvider proveedor) : IProtectorSecretoOrigen
{
    public const string Proposito = "Shapi.SecretoOrigen";

    private readonly IDataProtector _protector = proveedor.CreateProtector(Proposito);

    public string Cifrar(string secreto) => _protector.Protect(secreto);

    public string Descifrar(string secretoCifrado) => _protector.Unprotect(secretoCifrado);
}
