using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Identidad;
using Shapi.Aplicacion.Portal;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Identidad;

/// <summary>Autenticación del portal de consumidores (EM-05).</summary>
public static class EndpointsPortal
{
    public const string Ruta = "/api/portal/auth";

    public static IEndpointRouteBuilder MapearEndpointsIdentidadPortal(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup(Ruta);
        g.MapPost("/registro", Registrar).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        g.MapPost("/verificar-correo", Verificar).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        g.MapPost("/reenviar-verificacion", Reenviar).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        g.MapPost("/entrar", Entrar).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        g.MapPost("/salir", Salir).AllowAnonymous();
        g.MapGet("/sesion", SesionActual).RequireAuthorization(p => p.AddAuthenticationSchemes(ConsumidorAutenticacionOpciones.Esquema).RequireAuthenticatedUser());
        g.MapPost("/recuperar", Recuperar).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        g.MapPost("/restablecer", Restablecer).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        g.MapGet("/invitacion/{token}", Invitacion).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        g.MapPost("/invitacion/{token}/aceptar", AceptarInvitacion).AllowAnonymous().RequireRateLimiting(Endpoints.PoliticaLimiteAutenticacion);
        return app;
    }

    private static async Task<IResult> Registrar(PeticionRegistroConsumidor p, IResolutorPortal resolver, HttpContext http,
        ShapiDbContext db, IPasswordHasher<Consumidor> hasher, IColaCorreo cola, IReloj reloj, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is null)
        {
            return PortalNoDisponible();
        }

        var errores = ValidarRegistro(p);
        if (errores.Count > 0)
        {
            return DatosInvalidos(errores);
        }

        var correo = Usuario.NormalizarCorreo(p.Correo!);
        if (await db.Set<Consumidor>().IgnoreQueryFilters().AnyAsync(c => c.OrganizacionId == portal.OrganizacionId && c.Correo == correo, ct))
        {
            return Problemas.Crear(409, CodigosError.CorreoYaRegistrado, "Ya existe una cuenta con ese correo.");
        }

        var provisional = new Consumidor(portal.OrganizacionId, p.Nombre!, p.NombreEmpresa!, correo, "pendiente");
        provisional.DefinirHashContrasena(hasher.HashPassword(provisional, p.Contrasena!));
        var valorToken = SeguridadTokens.GenerarToken();
        var ahora = reloj.Ahora;
        db.AddRange(provisional, Token.VerificacionCorreoConsumidor(SeguridadTokens.HashearToken(valorToken), provisional, ahora));
        try
        {
            await cola.Encolar("verificacion_correo", correo, DatosPortal(portal, provisional.Nombre, valorToken), ct);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, TableName: "consumidor" })
        {
            return Problemas.Crear(409, CodigosError.CorreoYaRegistrado, "Ya existe una cuenta con ese correo.");
        }
        return TypedResults.Ok();
    }

    private static async Task<IResult> Verificar(PeticionToken p, IResolutorPortal resolver, HttpContext http, ShapiDbContext db, IReloj reloj, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is null)
        {
            return PortalNoDisponible();
        }

        var token = await db.Set<Token>().IgnoreQueryFilters().FirstOrDefaultAsync(t => t.HashToken == SeguridadTokens.HashearToken(p.Token ?? "") && t.Tipo == TipoToken.VerificacionCorreo && t.ConsumidorId != null, ct);
        if (token is null || !token.EsValido(reloj.Ahora))
        {
            return TokenInvalido();
        }

        var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(c => c.Id == token.ConsumidorId, ct);
        if (consumidor.OrganizacionId != portal.OrganizacionId)
        {
            return TokenInvalido();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var ahora = reloj.Ahora;
        var actualizados = await db.Set<Token>().IgnoreQueryFilters().Where(t => t.Id == token.Id && t.UsadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), ct);
        if (actualizados == 0)
        {
            return TokenInvalido();
        }

        consumidor.VerificarCorreo(ahora);
        await db.SaveChangesAsync(ct);
        if (consumidor.Estado == EstadoCuenta.Activo)
        {
            await CrearSesion(db, consumidor, http, ahora, ct);
        }

        await tx.CommitAsync(ct);
        return consumidor.Estado == EstadoCuenta.Activo ? TypedResults.Ok() : Problemas.Crear(403, CodigosError.CuentaDesactivada, "Cuenta desactivada.");
    }

    private static async Task<IResult> Reenviar(PeticionCorreo p, IResolutorPortal resolver, HttpContext http, ShapiDbContext db, IColaCorreo cola, IReloj reloj, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is null || string.IsNullOrWhiteSpace(p.Correo))
        {
            return TypedResults.Ok();
        }

        var correo = Usuario.NormalizarCorreo(p.Correo);
        var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.OrganizacionId == portal.OrganizacionId && c.Correo == correo, ct);
        if (consumidor is null || consumidor.CorreoVerificadoEn is not null || consumidor.Estado != EstadoCuenta.Activo)
        {
            return TypedResults.Ok();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM consumidor WHERE id = {consumidor.Id} FOR UPDATE", ct);
        var ahora = reloj.Ahora;
        var haceUnaHora = ahora.AddHours(-1);
        var enlacesRecientes = await db.Set<Token>().IgnoreQueryFilters()
            .CountAsync(t => t.ConsumidorId == consumidor.Id && t.Tipo == TipoToken.VerificacionCorreo && t.CreadoEn > haceUnaHora, ct);
        var reenviosRecientes = enlacesRecientes - (consumidor.CreadoEn > haceUnaHora ? 1 : 0);
        if (reenviosRecientes >= 3)
        {
            return TypedResults.Ok();
        }

        var token = SeguridadTokens.GenerarToken();
        db.Add(Token.VerificacionCorreoConsumidor(SeguridadTokens.HashearToken(token), consumidor, ahora));
        await cola.Encolar("verificacion_correo", correo, DatosPortal(portal, consumidor.Nombre, token), ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> Entrar(PeticionEntrar p, IResolutorPortal resolver, HttpContext http, ShapiDbContext db, IPasswordHasher<Consumidor> hasher, IReloj reloj, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is null)
        {
            return PortalNoDisponible();
        }

        if (string.IsNullOrWhiteSpace(p.Correo) || string.IsNullOrEmpty(p.Contrasena))
        {
            return CredencialesInvalidas();
        }

        var correo = Usuario.NormalizarCorreo(p.Correo);
        var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.OrganizacionId == portal.OrganizacionId && c.Correo == correo, ct);
        if (consumidor is null)
        {
            hasher.VerifyHashedPassword(ConsumidorFicticio, HashFicticio, p.Contrasena);
            await RegistrarIntentoFallido(db, Guid.Empty, reloj.Ahora, ct);
            return CredencialesInvalidas();
        }
        var ahora = reloj.Ahora;
        if (consumidor.EstaBloqueado(ahora))
        {
            return Problemas.Crear(423, CodigosError.CuentaBloqueada, "La cuenta está bloqueada por intentos fallidos. Intente de nuevo en 15 minutos.");
        }

        var verificacion = hasher.VerifyHashedPassword(consumidor, consumidor.HashContrasena, p.Contrasena);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            await db.Set<Consumidor>().IgnoreQueryFilters().Where(c => c.Id == consumidor.Id).ExecuteUpdateAsync(s => s
                .SetProperty(c => c.BloqueadoHasta, c => c.IntentosFallidos + 1 >= Consumidor.IntentosAntesDeBloquear ? ahora.Add(Consumidor.DuracionBloqueo) : c.BloqueadoHasta)
                .SetProperty(c => c.IntentosFallidos, c => c.IntentosFallidos + 1 >= Consumidor.IntentosAntesDeBloquear ? 0 : c.IntentosFallidos + 1)
                .SetProperty(c => c.ActualizadoEn, ahora), ct);
            return CredencialesInvalidas();
        }
        if (consumidor.Estado != EstadoCuenta.Activo)
        {
            return Problemas.Crear(403, CodigosError.CuentaDesactivada, "Cuenta desactivada.");
        }

        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            consumidor.DefinirHashContrasena(hasher.HashPassword(consumidor, p.Contrasena));
        }

        consumidor.RegistrarInicioExitoso();
        await CrearSesion(db, consumidor, http, ahora, ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> Salir(HttpContext http, ShapiDbContext db, IReloj reloj, CancellationToken ct)
    {
        if (http.Request.Cookies.TryGetValue(ConsumidorAutenticacionOpciones.Cookie, out var valor) && !string.IsNullOrEmpty(valor))
        {
            var sesion = await db.Set<Sesion>().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.HashIdentificador == SeguridadTokens.HashearToken(valor) && s.Ambito == AmbitoSesion.Consumidor && s.Host == http.Request.Host.Host, ct);
            if (sesion is not null) { sesion.Revocar(reloj.Ahora); await db.SaveChangesAsync(ct); }
        }
        http.Response.Cookies.Delete(ConsumidorAutenticacionOpciones.Cookie, OpcionesCookie());
        return TypedResults.Ok();
    }

    private static async Task<IResult> SesionActual(ClaimsPrincipal user, IResolutorPortal resolver, HttpContext http, ShapiDbContext db, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        var id = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // Con la sesión, el filtro global por organización ya aplica (10 §2): no hace falta IgnoreQueryFilters.
        var consumidor = await db.Set<Consumidor>().SingleAsync(c => c.Id == id, ct);
        if (portal is null || consumidor.OrganizacionId != portal.OrganizacionId)
        {
            return PortalNoDisponible();
        }

        // 10 §1 (EM-18): a /cuenta/suscripcion si tiene una suscripción vigente (no finalizada) a la API de este portal;
        // si no, a /planes. Acotado al consumidor y a la API del host, que ya se comprobó que son de la misma organización.
        var tieneSuscripcion = await db.Set<SuscripcionApi>()
            .AnyAsync(s => s.ConsumidorId == consumidor.Id && s.ApiId == portal.ApiId && s.Estado != EstadoSuscripcion.Finalizada, ct);

        return TypedResults.Ok(new
        {
            consumidor = new { nombre = consumidor.Nombre, nombreEmpresa = consumidor.NombreEmpresa },
            correoVerificado = consumidor.CorreoVerificadoEn is not null,
            destino = tieneSuscripcion ? "/cuenta/suscripcion" : "/planes",
        });
    }

    private static async Task<IResult> Recuperar(PeticionCorreo p, IResolutorPortal resolver, HttpContext http, IServicioRecuperacion recuperacion, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is not null && !string.IsNullOrWhiteSpace(p.Correo))
        {
            await recuperacion.Solicitar(p.Correo, AmbitoSesion.Consumidor, portal.HostPortal, portal.NombrePortal, portal.ColorPrincipal, portal.Logo is not null, ct, portal.OrganizacionId);
        }

        return TypedResults.Ok();
    }

    private static async Task<IResult> Restablecer(PeticionRestablecer p, IResolutorPortal resolver, HttpContext http, IServicioRecuperacion recuperacion, ShapiDbContext db, IReloj reloj, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is null)
        {
            return PortalNoDisponible();
        }

        string? correo = null;
        if (!string.IsNullOrWhiteSpace(p.Token))
        {
            correo = await db.Set<Token>().IgnoreQueryFilters().Where(t => t.HashToken == SeguridadTokens.HashearToken(p.Token)
                && t.Tipo == TipoToken.Recuperacion && t.ConsumidorId != null && t.UsuarioId == null && t.OrganizacionId == portal.OrganizacionId)
                .Select(t => t.Correo).FirstOrDefaultAsync(ct);
        }

        var error = ValidarContrasena(p.Contrasena, correo);
        if (error is not null)
        {
            return DatosInvalidos(new Dictionary<string, string[]> { ["contrasena"] = [error] });
        }

        var resultado = await recuperacion.Restablecer(p.Token ?? "", p.Contrasena!, ct, portal.OrganizacionId);
        if (resultado?.ConsumidorId is not Guid consumidorId || resultado.OrganizacionId != portal.OrganizacionId)
        {
            return TokenInvalido();
        }

        var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(c => c.Id == consumidorId, ct);
        await CrearSesion(db, consumidor, http, reloj.Ahora, ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> Invitacion(string token, IResolutorPortal resolver, HttpContext http, ShapiDbContext db, IReloj reloj, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is null)
        {
            return PortalNoDisponible();
        }

        var invitacion = await BuscarInvitacion(db, token, portal.OrganizacionId, reloj.Ahora, ct);
        return invitacion is null ? TokenInvalido() : TypedResults.Ok(new { correo = invitacion.Correo });
    }

    private static async Task<IResult> AceptarInvitacion(string token, PeticionAceptarInvitacion p, IResolutorPortal resolver, HttpContext http, ShapiDbContext db, IPasswordHasher<Consumidor> hasher, IReloj reloj, CancellationToken ct)
    {
        var portal = await Resolver(resolver, http, ct);
        if (portal is null)
        {
            return PortalNoDisponible();
        }

        var invitacion = await BuscarInvitacion(db, token, portal.OrganizacionId, reloj.Ahora, ct);
        if (invitacion is null)
        {
            return TokenInvalido();
        }

        var error = ValidarContrasena(p.Contrasena, invitacion.Correo);
        var errores = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(p.Nombre) || p.Nombre.Trim().Length > 120)
        {
            errores["nombre"] = ["Escriba un nombre de hasta 120 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(p.NombreEmpresa) || p.NombreEmpresa.Trim().Length is < 2 or > 120)
        {
            errores["nombreEmpresa"] = ["Escriba el nombre de su empresa."];
        }

        if (error is not null)
        {
            errores["contrasena"] = [error];
        }

        if (errores.Count > 0)
        {
            return DatosInvalidos(errores);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var ahora = reloj.Ahora;
        var usados = await db.Set<Token>().IgnoreQueryFilters().Where(t => t.Id == invitacion.Id && t.UsadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), ct);
        if (usados == 0)
        {
            return TokenInvalido();
        }

        var consumidor = new Consumidor(portal.OrganizacionId, p.Nombre!, p.NombreEmpresa!, invitacion.Correo, "pendiente");
        consumidor.DefinirHashContrasena(hasher.HashPassword(consumidor, p.Contrasena!));
        consumidor.VerificarCorreo(ahora);
        db.Add(consumidor);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, TableName: "consumidor" })
        { return Problemas.Crear(409, CodigosError.CorreoYaRegistrado, "Ya existe una cuenta con ese correo."); }
        await CrearSesion(db, consumidor, http, ahora, ct);
        await tx.CommitAsync(ct);
        return TypedResults.Ok();
    }

    private static Task<Token?> BuscarInvitacion(ShapiDbContext db, string valor, Guid organizacionId, DateTimeOffset ahora, CancellationToken ct) =>
        db.Set<Token>().IgnoreQueryFilters().FirstOrDefaultAsync(t => t.HashToken == SeguridadTokens.HashearToken(valor)
            && t.Tipo == TipoToken.InvitacionConsumidor && t.OrganizacionId == organizacionId && t.ConsumidorId == null
            && t.UsadoEn == null && t.ExpiraEn > ahora, ct);

    private static async Task<PortalResuelto?> Resolver(IResolutorPortal resolver, HttpContext http, CancellationToken ct) => await resolver.Resolver(http.Request.Host.Host, ct);

    private static async Task CrearSesion(ShapiDbContext db, Consumidor consumidor, HttpContext http, DateTimeOffset ahora, CancellationToken ct)
    {
        var valor = SeguridadTokens.GenerarToken();
        var sesion = Sesion.IniciarConsumidor(SeguridadTokens.HashearToken(valor), consumidor.Id, http.Request.Host.Host,
            http.Connection.RemoteIpAddress, http.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null, ahora);
        db.Add(sesion);
        await db.SaveChangesAsync(ct);
        var opciones = OpcionesCookie(); opciones.Expires = sesion.ExpiraEn;
        http.Response.Cookies.Append(ConsumidorAutenticacionOpciones.Cookie, valor, opciones);
    }

    private static Task<int> RegistrarIntentoFallido(ShapiDbContext db, Guid consumidorId, DateTimeOffset ahora, CancellationToken ct)
    {
        var bloqueo = ahora + Consumidor.DuracionBloqueo;
        return db.Set<Consumidor>().IgnoreQueryFilters().Where(c => c.Id == consumidorId).ExecuteUpdateAsync(s => s
            .SetProperty(c => c.BloqueadoHasta, c => c.IntentosFallidos + 1 >= Consumidor.IntentosAntesDeBloquear ? bloqueo : c.BloqueadoHasta)
            .SetProperty(c => c.IntentosFallidos, c => c.IntentosFallidos + 1 >= Consumidor.IntentosAntesDeBloquear ? 0 : c.IntentosFallidos + 1)
            .SetProperty(c => c.ActualizadoEn, ahora), ct);
    }

    private static CookieOptions OpcionesCookie() => new() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/" };
    private static object DatosPortal(PortalResuelto portal, string nombre, string token) => new
    {
        nombre,
        token,
        nombrePortal = portal.NombrePortal,
        hostPortal = portal.HostPortal,
        colorPortal = portal.ColorPrincipal,
        logoPortal = portal.Logo is null ? null : "true",
    };
    private static Dictionary<string, string[]> ValidarRegistro(PeticionRegistroConsumidor p)
    {
        var e = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(p.Nombre) || p.Nombre.Trim().Length > 120)
        {
            e["nombre"] = ["Escriba su nombre (máximo 120 caracteres)."];
        }

        if (string.IsNullOrWhiteSpace(p.NombreEmpresa) || p.NombreEmpresa.Trim().Length is < 2 or > 120)
        {
            e["nombreEmpresa"] = ["El nombre de la empresa debe tener entre 2 y 120 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(p.Correo) || p.Correo.Length > 254 || !System.Text.RegularExpressions.Regex.IsMatch(p.Correo.Trim(), ValidadorRegistroProveedor.PatronCorreo))
        {
            e["correo"] = ["Escriba un correo válido."];
        }

        var passwordError = ValidarContrasena(p.Contrasena, p.Correo);
        if (passwordError is not null)
        {
            e["contrasena"] = [passwordError];
        }

        return e;
    }
    private static string? ValidarContrasena(string? p, string? correo = null)
    {
        if (string.IsNullOrWhiteSpace(p))
        {
            return "Escriba una contraseña.";
        }

        if (p.Length is < ValidadorRegistroProveedor.LargoMinimoContrasena or > ValidadorRegistroProveedor.LargoMaximoContrasena)
        {
            return "La contraseña debe tener entre 10 y 128 caracteres.";
        }

        if (correo is not null && string.Equals(p.Trim(), correo.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "La contraseña no puede ser igual al correo.";
        }

        return null;
    }
    private static IResult DatosInvalidos(IDictionary<string, string[]> errores) => Problemas.Crear(400, CodigosError.DatosInvalidos, "Revise los datos del formulario.", errores);
    private static IResult TokenInvalido() => Problemas.Crear(422, CodigosError.TokenInvalido, "El enlace venció o ya se usó.");
    private static IResult CredencialesInvalidas() => Problemas.Crear(401, CodigosError.CredencialesInvalidas, "El correo o la contraseña no son correctos.");
    private static IResult PortalNoDisponible() => TypedResults.NotFound();
    private static readonly Consumidor ConsumidorFicticio = new(Guid.Empty, "ficticio", "ficticio", "ficticio@shapi.invalid", "x");
    private static readonly string HashFicticio = new PasswordHasher<Consumidor>().HashPassword(ConsumidorFicticio, SeguridadTokens.GenerarToken());

    public sealed record PeticionRegistroConsumidor(string? Nombre, string? NombreEmpresa, string? Correo, string? Contrasena);
    public sealed record PeticionToken(string? Token);
    public sealed record PeticionCorreo(string? Correo);
    public sealed record PeticionEntrar(string? Correo, string? Contrasena);
    public sealed record PeticionRestablecer(string? Token, string? Contrasena);
    public sealed record PeticionAceptarInvitacion(string? Nombre, string? NombreEmpresa, string? Contrasena);
}
