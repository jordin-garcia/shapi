using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Persistencia;
using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Identidad;

public class PersonalAutenticacionOpciones : AuthenticationSchemeOptions
{
    public const string Esquema = "Personal";
}

public class PersonalAutenticacionHandler : AuthenticationHandler<PersonalAutenticacionOpciones>
{
    private readonly ShapiDbContext _db;
    private readonly IReloj _reloj;

    public PersonalAutenticacionHandler(
        IOptionsMonitor<PersonalAutenticacionOpciones> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ShapiDbContext db,
        IReloj reloj) 
        : base(options, logger, encoder)
    {
        _db = db;
        _reloj = reloj;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue("shapi_sesion", out var tokenCookie) || string.IsNullOrEmpty(tokenCookie))
        {
            return AuthenticateResult.NoResult();
        }

        var hash = SeguridadTokens.HashearToken(tokenCookie);
        var ahora = _reloj.Ahora;

        // Ignoramos el filtro global aquí porque estamos resolviendo el usuario primero
        var sesion = await _db.Set<Sesion>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.HashIdentificador == hash);

        if (sesion == null)
        {
            return AuthenticateResult.Fail("Sesión no encontrada.");
        }

        if (sesion.RevocadaEn.HasValue || sesion.ExpiraEn < ahora || sesion.UltimoUsoEn.AddHours(8) < ahora)
        {
            return AuthenticateResult.Fail("Sesión inválida o expirada.");
        }

        // Actualizar último uso si pasó más de 1 minuto (debouncing)
        if (ahora - sesion.UltimoUsoEn > TimeSpan.FromMinutes(1))
        {
            sesion.UltimoUsoEn = ahora;
            await _db.SaveChangesAsync(); // Se podría hacer en un job de fondo o sin await, pero esto es seguro.
        }

        var usuario = await _db.Set<Usuario>().FirstOrDefaultAsync(u => u.Id == sesion.UsuarioId);
        if (usuario == null) return AuthenticateResult.Fail("Usuario no encontrado.");

        var membresia = await _db.Set<Shapi.Dominio.Organizaciones.Membresia>()
                                 .FirstOrDefaultAsync(m => m.UsuarioId == usuario.Id);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, membresia?.Rol.ToString() ?? "sin_rol"),
            new Claim("OrganizacionId", membresia?.OrganizacionId.ToString() ?? Guid.Empty.ToString()),
            new Claim("Ambito", AmbitoSesion.Personal.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
