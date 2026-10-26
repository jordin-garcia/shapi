using System.Security.Claims;
using FluentValidation;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Administracion;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Administracion;

public static class EndpointsCuentas
{
    public static IEndpointRouteBuilder MapearCuentasPlataforma(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/admin/cuentas").RequireAuthorization(Permisos.AdministrarCuentas);
        grupo.MapGet("", async (IRepositorioCuentasPlataforma repo, CancellationToken ct) => TypedResults.Ok(await repo.Listar(ct)));
        grupo.MapPost("", Crear);
        grupo.MapPost("/{id:guid}/desactivar", (Guid id, HttpContext http, CambiarEstadoCuentaPlataforma caso, CancellationToken ct) => Cambiar(id, false, http, caso, ct));
        grupo.MapPost("/{id:guid}/activar", (Guid id, HttpContext http, CambiarEstadoCuentaPlataforma caso, CancellationToken ct) => Cambiar(id, true, http, caso, ct));
        app.MapPost("/api/auth/definir-contrasena", Definir).AllowAnonymous()
            .RequireRateLimiting(Identidad.Endpoints.PoliticaLimiteAutenticacion);
        return app;
    }

    private static ActorCuenta Actor(HttpContext http) => new(
        Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        http.User.FindFirstValue(ClaimTypes.Name) ?? http.User.FindFirstValue(ClaimTypes.Email)!,
        http.Connection.RemoteIpAddress?.ToString());

    private static async Task<IResult> Crear(NuevaCuentaPlataforma solicitud, IValidator<NuevaCuentaPlataforma> validador,
        CrearCuentaPlataforma caso, HttpContext http, CancellationToken ct)
    {
        var validacion = await validador.ValidateAsync(solicitud, ct);
        if (!validacion.IsValid)
        {
            return Problemas.Crear(400, CodigosError.DatosInvalidos, "Revise los datos del formulario.", validacion.ToDictionary());
        }

        var resultado = await caso.Ejecutar(solicitud, Actor(http), ct);
        return resultado.EsExito ? TypedResults.Created($"/api/admin/cuentas/{resultado.Valor.Id}", resultado.Valor)
            : Problemas.Crear(409, resultado.Error.Codigo, resultado.Error.Mensaje);
    }

    private static async Task<IResult> Cambiar(Guid id, bool activar, HttpContext http, CambiarEstadoCuentaPlataforma caso, CancellationToken ct)
    {
        var actor = Actor(http);
        var resultado = await caso.Ejecutar(id, activar, actor, ct);
        return resultado.EsExito ? TypedResults.NoContent() : Problemas.Crear(!activar && actor.Id == id ? 422 : 404, resultado.Error.Codigo, resultado.Error.Mensaje);
    }

    private static async Task<IResult> Definir(DefinicionContrasena solicitud, DefinirContrasenaPlataforma caso,
        HttpContext http, TimeProvider tiempo, CancellationToken ct)
    {
        var resultado = await caso.Ejecutar(solicitud, http.Request.Host.Host, http.Connection.RemoteIpAddress?.ToString(), http.Request.Headers.UserAgent.ToString(), ct);
        if (!resultado.EsExito)
        {
            return Problemas.Crear(resultado.Error.Codigo == CodigosError.DatosInvalidos ? 400 : 422, resultado.Error.Codigo, resultado.Error.Mensaje,
                resultado.Error.Codigo == CodigosError.DatosInvalidos ? new Dictionary<string, string[]> { ["contrasena"] = [resultado.Error.Mensaje] } : null);
        }

        http.Response.Cookies.Append(PersonalAutenticacionOpciones.Cookie, resultado.Valor, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = tiempo.GetUtcNow() + Sesion.DuracionMaxima,
        });
        return TypedResults.Ok();
    }
}
