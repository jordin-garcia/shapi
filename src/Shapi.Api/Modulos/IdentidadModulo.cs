using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Extensiones;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Modulos;

public static class IdentidadModulo
{
    public static IServiceCollection AgregarModuloIdentidad(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<Shapi.Aplicacion.Comun.IContextoOrganizacion, Shapi.Api.Filtros.ContextoOrganizacionHttp>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AgregarPoliticasShapi();

        services.AddAuthentication(PersonalAutenticacionOpciones.Esquema)
            .AddScheme<PersonalAutenticacionOpciones, PersonalAutenticacionHandler>(PersonalAutenticacionOpciones.Esquema, null);

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("AuthLimiter", context =>
            {
                var isSesion = context.Request.Path.StartsWithSegments("/api/auth/sesion", StringComparison.OrdinalIgnoreCase);
                if (isSesion)
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });
        });

        return services;
    }

    public static WebApplication MapearModuloIdentidad(this WebApplication app)
    {
        app.UseMiddleware<Shapi.Api.Filtros.CsrfMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        var grupo = app.MapGroup("/api/auth")
                       .RequireRateLimiting("AuthLimiter");

        grupo.MapPost("/registro", RegistroProveedor);
        grupo.MapPost("/verificar-correo", VerificarCorreo);
        grupo.MapPost("/reenviar-verificacion", ReenviarVerificacion);
        grupo.MapPost("/entrar", IniciarSesion);
        grupo.MapPost("/salir", CerrarSesion).RequireAuthorization();
        grupo.MapGet("/sesion", ObtenerSesion).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegistroProveedor(
        [FromBody] PeticionRegistro peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IPasswordHasher<Usuario> hasher,
        [FromServices] IReloj reloj)
    {
        if (string.IsNullOrWhiteSpace(peticion.Contrasena) || peticion.Contrasena.Length < 10 ||
            string.IsNullOrWhiteSpace(peticion.Correo) || string.IsNullOrWhiteSpace(peticion.Nombre) ||
            peticion.Contrasena == peticion.Correo)
        {
            return TypedResults.BadRequest(new { error = "errores", detalle = "Datos inválidos" });
        }

        var correoNormalizado = peticion.Correo.Trim().ToLowerInvariant();

        var existe = await db.Set<Usuario>().IgnoreQueryFilters().AnyAsync(u => u.Correo == correoNormalizado);
        if (existe)
        {
            return TypedResults.Conflict(new { error = "correo_ya_registrado" });
        }

        var ahora = reloj.Ahora;
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = peticion.Nombre,
            Correo = correoNormalizado,
            HashContrasena = ""
        };
        usuario.HashContrasena = hasher.HashPassword(usuario, peticion.Contrasena);

        var org = new Organizacion
        {
            Id = Guid.NewGuid(),
            Nombre = peticion.Organizacion,
            Tipo = TipoOrganizacion.Proveedor
        };

        var membresia = new Membresia
        {
            UsuarioId = usuario.Id,
            OrganizacionId = org.Id,
            Rol = Rol.Propietario
        };

        var planPrueba = await db.Set<PlanPlataforma>().IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Nombre == "Prueba");
        if (planPrueba != null)
        {
            var suscripcion = new SuscripcionPlataforma
            {
                OrganizacionId = org.Id,
                PlanId = planPrueba.Id,
                Estado = EstadoSuscripcion.Activa,
                CreadoEn = ahora,
                Inicio = ahora,
                Fin = ahora.AddDays(30)
            };
            db.Add(suscripcion);
        }

        var tokenClaro = SeguridadTokens.GenerarToken();
        var tokenHash = SeguridadTokens.HashearToken(tokenClaro);

        var token = new Token
        {
            Id = Guid.NewGuid(),
            HashToken = tokenHash,
            UsuarioId = usuario.Id,
            Correo = usuario.Correo,
            Tipo = TipoToken.VerificacionCorreo,
            ExpiraEn = ahora.AddHours(24)
        };

        var correo = new CorreoSaliente
        {
            Id = Guid.NewGuid(),
            Destinatario = usuario.Correo,
            Asunto = "Verifica tu correo electrónico",
            Plantilla = "verificacion_correo",
            Datos = $"{{\"token\":\"{tokenClaro}\"}}",
            Estado = EstadoCorreo.Pendiente
        };

        db.Add(usuario);
        db.Add(org);
        db.Add(membresia);
        db.Add(token);
        db.Add(correo);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return TypedResults.Conflict(new { error = "correo_ya_registrado", detalle = ex.InnerException?.Message ?? ex.Message });
        }

        return TypedResults.Ok();
    }

    private static async Task<IResult> VerificarCorreo(
        [FromBody] PeticionVerificacion peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj,
        HttpContext context)
    {
        var hash = SeguridadTokens.HashearToken(peticion.Token);
        var ahora = reloj.Ahora;

        var token = await db.Set<Token>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.HashToken == hash && t.Tipo == TipoToken.VerificacionCorreo);

        if (token == null || token.UsadoEn != null || token.ExpiraEn < ahora)
        {
            return TypedResults.UnprocessableEntity(new { error = "token_invalido" });
        }

        var usuario = await db.Set<Usuario>().FirstOrDefaultAsync(u => u.Id == token.UsuarioId);

        token.UsadoEn = ahora;
        if (usuario != null)
        {
            usuario.CorreoVerificadoEn = ahora;
        }

        var tokenSesion = SeguridadTokens.GenerarToken();
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            HashIdentificador = SeguridadTokens.HashearToken(tokenSesion),
            UsuarioId = token.UsuarioId,
            Ambito = AmbitoSesion.Personal,
            ExpiraEn = ahora.AddDays(7),
            CreadaEn = ahora,
            UltimoUsoEn = ahora,
            Host = string.IsNullOrEmpty(context.Request.Host.Value) ? "unknown" : context.Request.Host.Value,
            Ip = context.Connection.RemoteIpAddress,
            AgenteUsuario = context.Request.Headers.UserAgent.ToString()
        };

        db.Add(sesion);
        await db.SaveChangesAsync();

        GenerarCookieSesion(context, tokenSesion, sesion.ExpiraEn);

        return TypedResults.Ok();
    }

    private static async Task<IResult> ReenviarVerificacion(
        [FromBody] PeticionReenviar peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj)
    {
        if (string.IsNullOrWhiteSpace(peticion.Correo))
        {
            return TypedResults.Ok();
        }

        var correoNormalizado = peticion.Correo.Trim().ToLowerInvariant();
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correoNormalizado);
        if (usuario == null)
        {
            return TypedResults.Ok();
        }

        var ahora = reloj.Ahora;
        var tokenClaro = SeguridadTokens.GenerarToken();
        var token = new Token
        {
            Id = Guid.NewGuid(),
            HashToken = SeguridadTokens.HashearToken(tokenClaro),
            UsuarioId = usuario.Id,
            Correo = usuario.Correo,
            Tipo = TipoToken.VerificacionCorreo,
            ExpiraEn = ahora.AddHours(24)
        };

        var correo = new CorreoSaliente
        {
            Id = Guid.NewGuid(),
            Destinatario = usuario.Correo,
            Asunto = "Verifica tu correo electrónico",
            Plantilla = "verificacion_correo",
            Datos = $"{{\"token\":\"{tokenClaro}\"}}",
            Estado = EstadoCorreo.Pendiente
        };

        db.Add(token);
        db.Add(correo);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }

    private static async Task<IResult> IniciarSesion(
        [FromBody] PeticionLogin peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IPasswordHasher<Usuario> hasher,
        [FromServices] IReloj reloj,
        HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(peticion.Correo) || string.IsNullOrWhiteSpace(peticion.Contrasena))
        {
            return TypedResults.Json(new { error = "credenciales_invalidas" }, statusCode: 401);
        }

        var correoNormalizado = peticion.Correo.Trim().ToLowerInvariant();
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correoNormalizado);
        var ahora = reloj.Ahora;

        if (usuario == null || usuario.Estado == EstadoCuenta.Desactivado || usuario.HashContrasena == null)
        {
            return TypedResults.Json(new { error = "credenciales_invalidas" }, statusCode: 401);
        }

        if (usuario.BloqueadoHasta > ahora)
        {
            return TypedResults.Json(new { error = "credenciales_invalidas" }, statusCode: 401);
        }

        var result = hasher.VerifyHashedPassword(usuario, usuario.HashContrasena, peticion.Contrasena);
        if (result == PasswordVerificationResult.Failed)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= 5)
            {
                usuario.BloqueadoHasta = ahora.AddMinutes(15);
                usuario.IntentosFallidos = 0;
            }
            await db.SaveChangesAsync();
            return TypedResults.Json(new { error = "credenciales_invalidas" }, statusCode: 401);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;

        var tokenSesion = SeguridadTokens.GenerarToken();
        var sesion = new Sesion
        {
            Id = Guid.NewGuid(),
            HashIdentificador = SeguridadTokens.HashearToken(tokenSesion),
            UsuarioId = usuario.Id,
            Ambito = AmbitoSesion.Personal,
            ExpiraEn = ahora.AddDays(7),
            CreadaEn = ahora,
            UltimoUsoEn = ahora,
            Host = string.IsNullOrEmpty(context.Request.Host.Value) ? "unknown" : context.Request.Host.Value,
            Ip = context.Connection.RemoteIpAddress,
            AgenteUsuario = context.Request.Headers.UserAgent.ToString()
        };

        db.Add(sesion);
        await db.SaveChangesAsync();

        GenerarCookieSesion(context, tokenSesion, sesion.ExpiraEn);

        return TypedResults.Ok();
    }

    private static async Task<IResult> CerrarSesion(
        HttpContext context,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj)
    {
        if (context.Request.Cookies.TryGetValue("shapi_sesion", out var tokenCookie))
        {
            var hash = SeguridadTokens.HashearToken(tokenCookie);
            var sesion = await db.Set<Sesion>().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.HashIdentificador == hash);
            if (sesion != null)
            {
                sesion.RevocadaEn = reloj.Ahora;
                await db.SaveChangesAsync();
            }
        }

        context.Response.Cookies.Delete("shapi_sesion");
        return TypedResults.Ok();
    }

    private static async Task<IResult> ObtenerSesion(
        HttpContext context,
        [FromServices] ShapiDbContext db)
    {
        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var orgIdStr = context.User.FindFirstValue("OrganizacionId");
        Guid.TryParse(orgIdStr, out var orgId);

        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        var org = await db.Set<Organizacion>().IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orgId);

        var rolStr = context.User.FindFirstValue(ClaimTypes.Role);
        var rol = rolStr?.ToLower() ?? "sin_rol";

        string destino = rol switch
        {
            "administrador" => "/admin/organizaciones",
            "soporte" => "/admin/casos",
            _ => "/panel/apis"
        };

        return TypedResults.Ok(new
        {
            usuario = new { nombre = usuario?.Nombre, correo = usuario?.Correo },
            organizacion = new { id = org?.Id, nombre = org?.Nombre },
            rol,
            correoVerificado = usuario?.CorreoVerificadoEn != null,
            destino
        });
    }

    private static void GenerarCookieSesion(HttpContext context, string token, DateTimeOffset expiraEn)
    {
        context.Response.Cookies.Append("shapi_sesion", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiraEn,
            Path = "/"
        });
    }
}

public record PeticionRegistro(string Nombre, string Correo, string Organizacion, string Contrasena);
public record PeticionVerificacion(string Token);
public record PeticionReenviar(string Correo);
public record PeticionLogin(string Correo, string Contrasena);
