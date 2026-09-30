using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Portal;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Identidad;

public sealed class ConsumidorAutenticacionOpciones : AuthenticationSchemeOptions
{
    public const string Esquema = "Consumidor";
    public const string Cookie = "portal_sesion";
}

/// <summary>Elige el ámbito de sesión según la cookie que presenta el navegador.</summary>
public static class EsquemaAutenticacionPortal
{
    public const string Esquema = "SesionShapi";
}

/// <summary>Autentica sesiones de consumidor y las vincula al host y organización actuales.</summary>
public sealed class ConsumidorAutenticacionHandler(
    IOptionsMonitor<ConsumidorAutenticacionOpciones> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ShapiDbContext db,
    IReloj reloj,
    IResolutorPortal resolutor,
    IConfiguration configuracion)
    : AuthenticationHandler<ConsumidorAutenticacionOpciones>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(ConsumidorAutenticacionOpciones.Cookie, out var valor) || string.IsNullOrEmpty(valor))
        {
            return AuthenticateResult.NoResult();
        }

        var portal = await resolutor.Resolver(Request.Host.Host, Context.RequestAborted);
        if (portal is null)
        {
            return AuthenticateResult.Fail("Portal no disponible.");
        }

        var hash = SeguridadTokens.HashearToken(valor);
        var ahora = reloj.Ahora;
        var inactividad = TimeSpan.FromHours(configuracion.GetValue<int?>("Sesion:InactividadHoras") ?? 8);
        var sesion = await db.Set<Sesion>().IgnoreQueryFilters().FirstOrDefaultAsync(s =>
            s.HashIdentificador == hash && s.Ambito == AmbitoSesion.Consumidor && s.ConsumidorId != null && s.Host == Request.Host.Host);
        if (sesion is null || !sesion.EstaVigente(ahora, inactividad))
        {
            return AuthenticateResult.Fail("Sesión inválida para este portal.");
        }

        var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == sesion.ConsumidorId);
        if (consumidor is null || consumidor.Estado != EstadoCuenta.Activo || consumidor.OrganizacionId != portal.OrganizacionId)
        {
            return AuthenticateResult.Fail("Consumidor no disponible en este portal.");
        }

        if (sesion.RegistrarUso(ahora))
        {
            await db.SaveChangesAsync(Context.RequestAborted);
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, consumidor.Id.ToString()),
            new(ClaimTypes.Email, consumidor.Correo),
            new(ClaimTypes.Name, consumidor.Nombre),
            new(PoliticasAutorizacion.ClaimOrganizacion, consumidor.OrganizacionId.ToString()),
            new(PoliticasAutorizacion.ClaimAmbito, AmbitoSesion.Consumidor.ToString()),
            new("HostPortal", Request.Host.Host),
        ];
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }
}
