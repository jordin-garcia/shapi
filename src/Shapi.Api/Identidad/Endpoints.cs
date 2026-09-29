using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Identidad;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Identidad;

/// <summary>Endpoints de <c>/api/auth</c> del ámbito personal (EM-02).</summary>
public static class Endpoints
{
    public const string PoliticaLimiteAutenticacion = "LimiteAutenticacion";
    public const string PlantillaVerificacionCorreo = "verificacion_correo";

    /// <summary>10 §1: como máximo 3 reenvíos de la verificación por cuenta en una hora.</summary>
    public const int ReenviosPorHora = 3;

    public static IEndpointRouteBuilder MapearEndpointsIdentidad(this IEndpointRouteBuilder app)
    {
        // Todo lo demás exige una sesión (política por defecto, 04). Estos son los únicos endpoints públicos.
        var grupo = app.MapGroup("/api/auth");
        grupo.MapPost("/registro", RegistrarProveedor).AllowAnonymous().RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/verificar-correo", VerificarCorreo).AllowAnonymous().RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/reenviar-verificacion", ReenviarVerificacion).AllowAnonymous().RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/entrar", IniciarSesion).AllowAnonymous().RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/recuperar", SolicitarRecuperacion).AllowAnonymous().RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/restablecer", RestablecerContrasena).AllowAnonymous();
        // salir y sesion no reciben credenciales y el panel consulta la sesión en cada carga, así que no llevan el límite.
        // salir es público: con la sesión vencida también tiene que borrar la cookie.
        grupo.MapPost("/salir", CerrarSesion).AllowAnonymous();
        grupo.MapGet("/sesion", ObtenerSesion).RequireAuthorization();

        var grupoPerfil = app.MapGroup("/api/perfil").RequireAuthorization();
        grupoPerfil.MapGet("/", ObtenerPerfil);
        grupoPerfil.MapPut("/", EditarPerfil);
        grupoPerfil.MapPost("/contrasena", CambiarContrasena);

        return app;
    }

    // CU-01, RF-01: crea el usuario, la organización, la membresía de propietario y la Prueba, y encola la verificación.
    private static async Task<IResult> RegistrarProveedor(
        [FromBody] RegistroProveedor peticion,
        [FromServices] IValidator<RegistroProveedor> validador,
        [FromServices] ShapiDbContext db,
        [FromServices] IPasswordHasher<Usuario> hasher,
        [FromServices] IColaCorreo colaCorreo,
        [FromServices] IReloj reloj,
        CancellationToken cancelacion)
    {
        var validacion = await validador.ValidateAsync(peticion, cancelacion);
        if (!validacion.IsValid)
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.",
                validacion.ToDictionary());
        }

        var correo = Usuario.NormalizarCorreo(peticion.Correo!);
        if (await db.Set<Usuario>().IgnoreQueryFilters().AnyAsync(u => u.Correo == correo, cancelacion))
        {
            return CorreoYaRegistrado();
        }

        // El plan Prueba lo crean los datos base (07 §6). Si falta, es un error de instalación: 500 y no se guarda nada.
        var planPrueba = await db.Set<PlanPlataforma>().IgnoreQueryFilters().SingleOrDefaultAsync(p => p.EsPrueba, cancelacion)
            ?? throw new InvalidOperationException("No existe el plan Prueba en los datos base.");

        var ahora = reloj.Ahora;
        var usuario = new Usuario(peticion.Nombre!, correo);
        usuario.DefinirHashContrasena(hasher.HashPassword(usuario, peticion.Contrasena!));
        var organizacion = new Organizacion(peticion.Organizacion!, TipoOrganizacion.Proveedor);
        var valorToken = SeguridadTokens.GenerarToken();

        db.AddRange(
            usuario,
            organizacion,
            new Membresia(usuario.Id, organizacion.Id, Rol.Propietario),
            SuscripcionPlataforma.IniciarPrueba(organizacion.Id, planPrueba, ahora),
            Token.VerificacionCorreo(SeguridadTokens.HashearToken(valorToken), usuario, ahora));

        try
        {
            // Todo se guarda en una sola transacción, junto con el correo. Si la cola ya guardó, SaveChanges no hace nada.
            await colaCorreo.Encolar(PlantillaVerificacionCorreo, usuario.Correo, new { nombre = usuario.Nombre, token = valorToken }, cancelacion);
            await db.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, TableName: "usuario" })
        {
            // Dos registros simultáneos con el mismo correo: el índice único decide.
            return CorreoYaRegistrado();
        }

        return TypedResults.Ok();
    }

    // CU-01 paso 4, RF-02: verifica el correo con el enlace de un solo uso e inicia la sesión.
    private static async Task<IResult> VerificarCorreo(
        [FromBody] PeticionVerificacion peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj,
        HttpContext contexto,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(peticion.Token))
        {
            return TokenInvalido();
        }

        var hash = SeguridadTokens.HashearToken(peticion.Token);
        var ahora = reloj.Ahora;
        var token = await db.Set<Token>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.HashToken == hash && t.Tipo == TipoToken.VerificacionCorreo && t.UsuarioId != null, cancelacion);
        if (token is null || !token.EsValido(ahora))
        {
            return TokenInvalido();
        }

        // Marcar el token, verificar el correo e iniciar la sesión van juntos: si algo falla, el enlace sigue sin usarse.
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);

        // Se marca usado con una actualización condicional para que dos peticiones simultáneas no lo usen dos veces.
        var marcados = await db.Set<Token>().IgnoreQueryFilters()
            .Where(t => t.Id == token.Id && t.UsadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), cancelacion);
        if (marcados == 0)
        {
            return TokenInvalido();
        }

        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == token.UsuarioId, cancelacion);
        usuario.VerificarCorreo(ahora);
        if (usuario.Estado == EstadoCuenta.Desactivado)
        {
            await db.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);
            return CuentaDesactivada();
        }

        await IniciarSesionPersonal(db, contexto, usuario, ahora, cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return TypedResults.Ok();
    }

    // RF-02: genera un enlace nuevo. Responde igual exista o no la cuenta, para no revelar qué correos están registrados.
    private static async Task<IResult> ReenviarVerificacion(
        [FromBody] PeticionReenviar peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IColaCorreo colaCorreo,
        [FromServices] IReloj reloj,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(peticion.Correo))
        {
            return TypedResults.Ok();
        }

        var correo = Usuario.NormalizarCorreo(peticion.Correo);
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correo, cancelacion);
        if (usuario is null || usuario.CorreoVerificadoEn is not null || usuario.Estado == EstadoCuenta.Desactivado)
        {
            return TypedResults.Ok();
        }

        // Los reenvíos de una misma cuenta se serializan bloqueando su fila: si no, varias peticiones simultáneas
        // leerían el mismo conteo y encolarían más de 3 enlaces.
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM usuario WHERE id = {usuario.Id} FOR UPDATE", cancelacion);

        // 10 §1: como máximo 3 reenvíos por hora. El enlace del registro no cuenta como reenvío.
        var ahora = reloj.Ahora;
        var haceUnaHora = ahora.AddHours(-1);
        var enlacesRecientes = await db.Set<Token>().IgnoreQueryFilters()
            .CountAsync(t => t.UsuarioId == usuario.Id && t.Tipo == TipoToken.VerificacionCorreo && t.CreadoEn > haceUnaHora, cancelacion);
        var reenviosRecientes = enlacesRecientes - (usuario.CreadoEn > haceUnaHora ? 1 : 0);
        if (reenviosRecientes >= ReenviosPorHora)
        {
            return TypedResults.Ok();
        }

        var valorToken = SeguridadTokens.GenerarToken();
        db.Add(Token.VerificacionCorreo(SeguridadTokens.HashearToken(valorToken), usuario, ahora));
        await colaCorreo.Encolar(PlantillaVerificacionCorreo, usuario.Correo, new { nombre = usuario.Nombre, token = valorToken }, cancelacion);
        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return TypedResults.Ok();
    }

    // CU-02, RF-04: inicia la sesión. Tras 5 intentos fallidos seguidos, la cuenta se bloquea 15 minutos.
    private static async Task<IResult> IniciarSesion(
        [FromBody] PeticionEntrar peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IPasswordHasher<Usuario> hasher,
        [FromServices] IReloj reloj,
        HttpContext contexto,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(peticion.Correo) || string.IsNullOrEmpty(peticion.Contrasena))
        {
            return CredencialesInvalidas();
        }

        var ahora = reloj.Ahora;
        var correo = Usuario.NormalizarCorreo(peticion.Correo);
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correo, cancelacion);
        if (usuario?.HashContrasena is null)
        {
            // 10 §1: el mismo tiempo de respuesta exista o no la cuenta. Se calcula un hash y se hace la misma
            // actualización que con una contraseña incorrecta, sobre una cuenta que no existe.
            hasher.VerifyHashedPassword(UsuarioFicticio, HashFicticio, peticion.Contrasena);
            await RegistrarIntentoFallido(db, Guid.Empty, ahora, cancelacion);
            return CredencialesInvalidas();
        }

        if (usuario.EstaBloqueado(ahora))
        {
            return Problemas.Crear(StatusCodes.Status423Locked, CodigosError.CuentaBloqueada,
                "La cuenta está bloqueada por intentos fallidos. Intente de nuevo en 15 minutos.");
        }

        var resultado = hasher.VerifyHashedPassword(usuario, usuario.HashContrasena, peticion.Contrasena);
        if (resultado == PasswordVerificationResult.Failed)
        {
            await RegistrarIntentoFallido(db, usuario.Id, ahora, cancelacion);
            return CredencialesInvalidas();
        }

        // CU-02 2b: solo se dice que la cuenta está desactivada a quien conoce la contraseña.
        if (usuario.Estado == EstadoCuenta.Desactivado)
        {
            return CuentaDesactivada();
        }

        // RNF-07: si el hash se hizo con parámetros más débiles que los actuales, se recalcula.
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.DefinirHashContrasena(hasher.HashPassword(usuario, peticion.Contrasena));
        }

        usuario.RegistrarInicioExitoso();
        await IniciarSesionPersonal(db, contexto, usuario, ahora, cancelacion);
        return TypedResults.Ok();
    }

    /// <summary>
    /// Cuenta un intento fallido en una sola sentencia, para que los intentos simultáneos no se pierdan.
    /// Al quinto seguido bloquea la cuenta 15 minutos y reinicia el contador, igual que <see cref="Usuario.RegistrarIntentoFallido"/>.
    /// </summary>
    private static Task<int> RegistrarIntentoFallido(ShapiDbContext db, Guid usuarioId, DateTimeOffset ahora, CancellationToken cancelacion)
    {
        var bloqueo = ahora + Usuario.DuracionBloqueo;
        return db.Set<Usuario>().IgnoreQueryFilters()
            .Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.BloqueadoHasta, u => u.IntentosFallidos + 1 >= Usuario.IntentosAntesDeBloquear ? bloqueo : u.BloqueadoHasta)
                .SetProperty(u => u.IntentosFallidos, u => u.IntentosFallidos + 1 >= Usuario.IntentosAntesDeBloquear ? 0 : u.IntentosFallidos + 1)
                .SetProperty(u => u.ActualizadoEn, ahora), cancelacion);
    }

    // RF-04: revoca la sesión en el servidor, si existe, y borra la cookie siempre, aunque la sesión ya haya vencido.
    private static async Task<IResult> CerrarSesion(
        HttpContext contexto,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj,
        CancellationToken cancelacion)
    {
        if (contexto.Request.Cookies.TryGetValue(PersonalAutenticacionOpciones.Cookie, out var valorCookie) && !string.IsNullOrEmpty(valorCookie))
        {
            var hash = SeguridadTokens.HashearToken(valorCookie);
            var sesion = await db.Set<Sesion>().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.HashIdentificador == hash, cancelacion);
            if (sesion is not null)
            {
                sesion.Revocar(reloj.Ahora);
                await db.SaveChangesAsync(cancelacion);
            }
        }

        contexto.Response.Cookies.Delete(PersonalAutenticacionOpciones.Cookie, OpcionesCookie());
        return TypedResults.Ok();
    }

    // RF-04, 10 §1: datos de la sesión actual y destino según el rol.
    private static async Task<IResult> ObtenerSesion(
        ClaimsPrincipal usuarioActual,
        [FromServices] ShapiDbContext db,
        CancellationToken cancelacion)
    {
        var usuarioId = Guid.Parse(usuarioActual.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var organizacionId = Guid.Parse(usuarioActual.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion)!);
        var rol = Enum.Parse<Rol>(usuarioActual.FindFirstValue(ClaimTypes.Role)!);

        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == usuarioId, cancelacion);
        var organizacion = await db.Set<Organizacion>().SingleAsync(o => o.Id == organizacionId, cancelacion);

        return TypedResults.Ok(new RespuestaSesion(
            new UsuarioSesion(usuario.Nombre, usuario.Correo),
            new OrganizacionSesion(organizacion.Id, organizacion.Nombre),
            rol.ToString().ToLowerInvariant(),
            usuario.CorreoVerificadoEn is not null,
            DestinoSegunRol(rol)));
    }

    public static string DestinoSegunRol(Rol rol) => rol switch
    {
        Rol.Administrador => "/admin/organizaciones",
        Rol.Soporte => "/admin/casos",
        _ => "/panel/apis",
    };

    private static async Task IniciarSesionPersonal(ShapiDbContext db, HttpContext contexto, Usuario usuario, DateTimeOffset ahora, CancellationToken cancelacion)
    {
        var valorCookie = SeguridadTokens.GenerarToken();
        var sesion = Sesion.IniciarPersonal(
            SeguridadTokens.HashearToken(valorCookie),
            usuario.Id,
            contexto.Request.Host.Host is { Length: > 0 } host ? host : "desconocido",
            contexto.Connection.RemoteIpAddress,
            contexto.Request.Headers.UserAgent.ToString() is { Length: > 0 } agente ? agente : null,
            ahora);
        db.Add(sesion);
        await db.SaveChangesAsync(cancelacion);

        var opciones = OpcionesCookie();
        opciones.Expires = sesion.ExpiraEn;
        contexto.Response.Cookies.Append(PersonalAutenticacionOpciones.Cookie, valorCookie, opciones);
    }

    // 10 §1: HttpOnly, Secure, SameSite=Lax y Path=/.
    private static CookieOptions OpcionesCookie() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
    };

    private static readonly Usuario UsuarioFicticio = new("ficticio", "ficticio@shapi.invalid");
    private static readonly string HashFicticio = new PasswordHasher<Usuario>().HashPassword(UsuarioFicticio, SeguridadTokens.GenerarToken());

    private static IResult CorreoYaRegistrado() =>
        Problemas.Crear(StatusCodes.Status409Conflict, CodigosError.CorreoYaRegistrado, "Ya existe una cuenta con ese correo.");

    private static IResult CredencialesInvalidas() =>
        Problemas.Crear(StatusCodes.Status401Unauthorized, CodigosError.CredencialesInvalidas, "El correo o la contraseña no son correctos.");

    private static IResult CuentaDesactivada() =>
        Problemas.Crear(StatusCodes.Status403Forbidden, CodigosError.CuentaDesactivada, "Cuenta desactivada.");

    private static IResult TokenInvalido() =>
        Problemas.Crear(StatusCodes.Status422UnprocessableEntity, CodigosError.TokenInvalido, "El enlace venció o ya se usó.");

    // EM-04
    private static async Task<IResult> SolicitarRecuperacion(
        [FromBody] PeticionRecuperar peticion,
        [FromServices] IServicioRecuperacion servicio,
        CancellationToken cancelacion)
    {
        if (!string.IsNullOrWhiteSpace(peticion.Correo))
        {
            await servicio.Solicitar(peticion.Correo, AmbitoSesion.Personal, cancelacion: cancelacion);
        }
        return TypedResults.Ok();
    }

    private static async Task<IResult> RestablecerContrasena(
        [FromBody] PeticionRestablecer peticion,
        [FromServices] IServicioRecuperacion servicio,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj,
        HttpContext contexto,
        CancellationToken cancelacion)
    {
        // Obtener el correo asociado al token para validar la política completa (10 §1) antes de consumirlo.
        string? correoDelToken = null;
        if (!string.IsNullOrWhiteSpace(peticion.Token))
        {
            var hash = SeguridadTokens.HashearToken(peticion.Token);
            correoDelToken = await db.Set<Token>().IgnoreQueryFilters()
                .Where(t => t.HashToken == hash && t.Tipo == TipoToken.Recuperacion)
                .Select(t => t.Correo)
                .FirstOrDefaultAsync(cancelacion);
        }

        // Validar la política de contraseña (10 §1) antes de consumir el token.
        var erroresContrasena = ValidarContrasena(peticion.Contrasena, correoDelToken);
        if (erroresContrasena is not null)
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.",
                new Dictionary<string, string[]> { ["contrasena"] = [erroresContrasena] });
        }

        var resultado = await servicio.Restablecer(peticion.Token ?? "", peticion.Contrasena!, cancelacion);
        if (resultado is null)
        {
            return TokenInvalido();
        }

        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == resultado.UsuarioId, cancelacion);
        await IniciarSesionPersonal(db, contexto, usuario, reloj.Ahora, cancelacion);
        return TypedResults.Ok();
    }

    private static async Task<IResult> ObtenerPerfil(
        ClaimsPrincipal usuarioActual,
        [FromServices] ShapiDbContext db,
        CancellationToken cancelacion)
    {
        var usuarioId = Guid.Parse(usuarioActual.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == usuarioId, cancelacion);
        return TypedResults.Ok(new { nombre = usuario.Nombre });
    }

    private static async Task<IResult> EditarPerfil(
        [FromBody] PeticionPerfil peticion,
        ClaimsPrincipal usuarioActual,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj,
        CancellationToken cancelacion)
    {
        var nombreTrim = peticion.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombreTrim))
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "El nombre es obligatorio.",
                new Dictionary<string, string[]> { ["nombre"] = ["Escriba su nombre."] });
        }

        if (nombreTrim.Length > 120)
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.",
                new Dictionary<string, string[]> { ["nombre"] = ["El nombre no puede tener más de 120 caracteres."] });
        }

        var usuarioId = Guid.Parse(usuarioActual.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ahora = reloj.Ahora;
        await db.Set<Usuario>().IgnoreQueryFilters()
            .Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Nombre, nombreTrim).SetProperty(u => u.ActualizadoEn, ahora), cancelacion);

        return TypedResults.Ok();
    }

    private static async Task<IResult> CambiarContrasena(
        [FromBody] PeticionCambiarContrasena peticion,
        ClaimsPrincipal usuarioActual,
        HttpContext contexto,
        [FromServices] ShapiDbContext db,
        [FromServices] IPasswordHasher<Usuario> hasher,
        [FromServices] IReloj reloj,
        CancellationToken cancelacion)
    {
        var usuarioId = Guid.Parse(usuarioActual.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == usuarioId, cancelacion);

        if (string.IsNullOrWhiteSpace(peticion.ContrasenaActual))
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.",
                new Dictionary<string, string[]> { ["contrasenaActual"] = ["La contraseña actual es obligatoria."] });
        }

        var erroresNueva = ValidarContrasena(peticion.ContrasenaNueva, usuario.Correo);
        if (erroresNueva is not null)
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.",
                new Dictionary<string, string[]> { ["contrasenaNueva"] = [erroresNueva] });
        }

        var resultado = hasher.VerifyHashedPassword(usuario, usuario.HashContrasena!, peticion.ContrasenaActual);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return CredencialesInvalidas();
        }

        var hashNuevo = hasher.HashPassword(usuario, peticion.ContrasenaNueva!);
        usuario.DefinirHashContrasena(hashNuevo);

        var ahora = reloj.Ahora;

        // Revocar sesiones excepto la actual
        var hashActual = string.Empty;
        if (contexto.Request.Cookies.TryGetValue(PersonalAutenticacionOpciones.Cookie, out var valorCookie) && !string.IsNullOrEmpty(valorCookie))
        {
            hashActual = SeguridadTokens.HashearToken(valorCookie);
        }

        await db.Set<Sesion>().IgnoreQueryFilters()
            .Where(s => s.UsuarioId == usuario.Id && s.RevocadaEn == null && s.HashIdentificador != hashActual)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, ahora).SetProperty(x => x.ActualizadoEn, ahora), cancelacion);

        await db.SaveChangesAsync(cancelacion);
        return TypedResults.Ok();
    }

    /// <summary>Valida la política de contraseña de 10 §1 (10-128 caracteres, distinta del correo). Retorna el mensaje de error o null si es válida.</summary>
    private static string? ValidarContrasena(string? contrasena, string? correo = null)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            return "Escriba una contraseña.";
        }

        if (contrasena.Length < ValidadorRegistroProveedor.LargoMinimoContrasena)
        {
            return $"La contraseña debe tener entre {ValidadorRegistroProveedor.LargoMinimoContrasena} y {ValidadorRegistroProveedor.LargoMaximoContrasena} caracteres.";
        }

        if (contrasena.Length > ValidadorRegistroProveedor.LargoMaximoContrasena)
        {
            return $"La contraseña debe tener entre {ValidadorRegistroProveedor.LargoMinimoContrasena} y {ValidadorRegistroProveedor.LargoMaximoContrasena} caracteres.";
        }

        if (correo is not null && string.Equals(contrasena.Trim(), correo.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "La contraseña no puede ser igual al correo.";
        }

        return null;
    }
}

public record PeticionVerificacion(string? Token);
public record PeticionReenviar(string? Correo);
public record PeticionEntrar(string? Correo, string? Contrasena);
public record UsuarioSesion(string Nombre, string Correo);
public record OrganizacionSesion(Guid Id, string Nombre);
public record RespuestaSesion(UsuarioSesion Usuario, OrganizacionSesion Organizacion, string Rol, bool CorreoVerificado, string Destino);
public record PeticionRecuperar(string? Correo);
public record PeticionRestablecer(string? Token, string? Contrasena);
public record PeticionPerfil(string? Nombre);
public record PeticionCambiarContrasena(string? ContrasenaActual, string? ContrasenaNueva);
