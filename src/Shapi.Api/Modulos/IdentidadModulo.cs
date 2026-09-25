using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Identidad;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Modulos;

public static class IdentidadModulo
{
    public const string PoliticaLimiteAutenticacion = "LimiteAutenticacion";
    public const string PlantillaVerificacionCorreo = "verificacion_correo";

    public static IServiceCollection AgregarModuloIdentidad(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IContextoOrganizacion, ContextoOrganizacionHttp>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<IValidator<RegistroProveedor>, ValidadorRegistroProveedor>();
        services.AgregarPoliticasShapi();

        services.AddAuthentication(PersonalAutenticacionOpciones.Esquema)
            .AddScheme<PersonalAutenticacionOpciones, PersonalAutenticacionHandler>(PersonalAutenticacionOpciones.Esquema, null);

        // Detrás del borde (Caddy), la IP del cliente llega en X-Forwarded-For. Solo se confía en ella si la conexión
        // viene de la máquina o de una red privada (la red de Docker), que es donde está el borde (06 §4).
        // Caddy reemplaza la X-Forwarded-For que mande el cliente, así que no se puede falsificar a través del borde.
        services.Configure<ForwardedHeadersOptions>(opciones =>
        {
            opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opciones.ForwardLimit = 1;
            opciones.KnownIPNetworks.Clear();
            opciones.KnownProxies.Clear();
            foreach (var red in new[] { "127.0.0.0/8", "::1/128", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" })
            {
                opciones.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(red));
            }
        });

        // 10 §1: los endpoints que reciben credenciales o tokens aceptan 10 peticiones por minuto por IP.
        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = async (contexto, _) =>
            {
                var espera = contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var restante) ? restante : TimeSpan.FromMinutes(1);
                contexto.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                await Problemas.Escribir(contexto.HttpContext, StatusCodes.Status429TooManyRequests, CodigosError.DemasiadasPeticiones,
                    "Demasiadas peticiones. Espere un minuto e intente de nuevo.");
            };
            opciones.AddPolicy(PoliticaLimiteAutenticacion, contexto =>
            {
                var limite = contexto.RequestServices.GetRequiredService<IConfiguration>().GetValue<int?>("Autenticacion:LimitePorMinuto") ?? 10;
                return RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });

        return services;
    }

    public static WebApplication MapearModuloIdentidad(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CsrfMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        var grupo = app.MapGroup("/api/auth");
        grupo.MapPost("/registro", RegistrarProveedor).RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/verificar-correo", VerificarCorreo).RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/reenviar-verificacion", ReenviarVerificacion).RequireRateLimiting(PoliticaLimiteAutenticacion);
        grupo.MapPost("/entrar", IniciarSesion).RequireRateLimiting(PoliticaLimiteAutenticacion);
        // salir y sesion no reciben credenciales y el panel consulta la sesión en cada carga, así que no llevan el límite.
        grupo.MapPost("/salir", CerrarSesion).RequireAuthorization();
        grupo.MapGet("/sesion", ObtenerSesion).RequireAuthorization();

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
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora), cancelacion);
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

        var valorToken = SeguridadTokens.GenerarToken();
        db.Add(Token.VerificacionCorreo(SeguridadTokens.HashearToken(valorToken), usuario, reloj.Ahora));
        await colaCorreo.Encolar(PlantillaVerificacionCorreo, usuario.Correo, new { nombre = usuario.Nombre, token = valorToken }, cancelacion);
        await db.SaveChangesAsync(cancelacion);
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
            // Se calcula el hash igual para que el tiempo de respuesta no revele si la cuenta existe (10 §8).
            hasher.VerifyHashedPassword(UsuarioFicticio, HashFicticio, peticion.Contrasena);
            return CredencialesInvalidas();
        }

        if (usuario.EstaBloqueado(ahora))
        {
            return Problemas.Crear(StatusCodes.Status423Locked, CodigosError.CuentaBloqueada,
                "La cuenta está bloqueada por intentos fallidos. Intente de nuevo en 15 minutos.");
        }

        if (hasher.VerifyHashedPassword(usuario, usuario.HashContrasena, peticion.Contrasena) == PasswordVerificationResult.Failed)
        {
            usuario.RegistrarIntentoFallido(ahora);
            await db.SaveChangesAsync(cancelacion);
            return CredencialesInvalidas();
        }

        // CU-02 2b: solo se dice que la cuenta está desactivada a quien conoce la contraseña.
        if (usuario.Estado == EstadoCuenta.Desactivado)
        {
            return CuentaDesactivada();
        }

        usuario.RegistrarInicioExitoso();
        await IniciarSesionPersonal(db, contexto, usuario, ahora, cancelacion);
        return TypedResults.Ok();
    }

    // RF-04: revoca la sesión en el servidor y borra la cookie.
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
}

public record PeticionVerificacion(string? Token);
public record PeticionReenviar(string? Correo);
public record PeticionEntrar(string? Correo, string? Contrasena);
public record UsuarioSesion(string Nombre, string Correo);
public record OrganizacionSesion(Guid Id, string Nombre);
public record RespuestaSesion(UsuarioSesion Usuario, OrganizacionSesion Organizacion, string Rol, bool CorreoVerificado, string Destino);
