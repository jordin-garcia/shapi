using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Identidad;

public class PersonalAutenticacionOpciones : AuthenticationSchemeOptions
{
    public const string Esquema = "Personal";
    public const string Cookie = "shapi_sesion";
}

/// <summary>
/// Autentica al personal con la cookie <c>shapi_sesion</c> contra la tabla <c>sesion</c> (10 §1) y resuelve
/// su organización y su rol desde la membresía (10 §2).
/// </summary>
public class PersonalAutenticacionHandler(
    IOptionsMonitor<PersonalAutenticacionOpciones> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ShapiDbContext db,
    IReloj reloj,
    IConfiguration configuracion)
    : AuthenticationHandler<PersonalAutenticacionOpciones>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(PersonalAutenticacionOpciones.Cookie, out var valorCookie) || string.IsNullOrEmpty(valorCookie))
        {
            return AuthenticateResult.NoResult();
        }

        var hash = SeguridadTokens.HashearToken(valorCookie);
        var ahora = reloj.Ahora;
        var inactividad = TimeSpan.FromHours(configuracion.GetValue<int?>("Sesion:InactividadHoras") ?? 8);

        // La sesión, el usuario y la membresía no dependen de la organización de la petición: son los que la definen.
        // Por eso se leen sin el filtro global (10 §2), que aquí todavía no tiene organización y ocultaría todo.
        var sesion = await db.Set<Sesion>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HashIdentificador == hash && s.Ambito == AmbitoSesion.Personal && s.UsuarioId != null);
        if (sesion is null || !sesion.EstaVigente(ahora, inactividad))
        {
            return AuthenticateResult.Fail("Sesión inexistente, revocada o vencida.");
        }

        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == sesion.UsuarioId);
        if (usuario is null || usuario.Estado == EstadoCuenta.Desactivado)
        {
            return AuthenticateResult.Fail("Usuario inexistente o desactivado.");
        }

        var membresia = await db.Set<Membresia>().IgnoreQueryFilters().FirstOrDefaultAsync(m => m.UsuarioId == usuario.Id);
        if (membresia is null)
        {
            return AuthenticateResult.Fail("El usuario no tiene membresía.");
        }

        if (sesion.RegistrarUso(ahora))
        {
            await db.SaveChangesAsync(Context.RequestAborted);
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, membresia.Rol.ToString()),
            new(PoliticasAutorizacion.ClaimOrganizacion, membresia.OrganizacionId.ToString()),
            new(PoliticasAutorizacion.ClaimAmbito, AmbitoSesion.Personal.ToString()),
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
