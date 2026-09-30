using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Claves;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Portal;
using Shapi.Contratos;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Claves;

/// <summary>Claves de los consumidores: panel (A4.3) y portal (B2.3 a B2.6). RF-26 a RF-28.</summary>
public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsClaves(this IEndpointRouteBuilder app)
    {
        var panel = app.MapGroup("/api/apis/{apiId:guid}/claves");
        panel.MapGet("/", ListarDeApi).RequireAuthorization(Permisos.VerClaves);
        panel.MapPost("/{claveId:guid}/revocar", RevocarDeConsumidor).RequireAuthorization(Permisos.RevocarClaves);

        var portal = app.MapGroup("/api/portal/claves");
        portal.MapGet("/", ListarDelConsumidor).RequireAuthorization(Permisos.ConsumidorVerCuenta);
        portal.MapPost("/emitir", Emitir).RequireAuthorization(Permisos.ConsumidorAdministrarClaves);
        portal.MapPost("/{claveId:guid}/rotar", Rotar).RequireAuthorization(Permisos.ConsumidorAdministrarClaves);
        portal.MapPost("/{claveId:guid}/revocar", RevocarPropia).RequireAuthorization(Permisos.ConsumidorAdministrarClaves);
        return app;
    }

    // ---------- Panel ----------

    private static async Task<IResult> ListarDeApi(
        Guid apiId, HttpRequest peticion, IServicioClaves claves, CancellationToken cancelacion)
    {
        if (!LeerEntero(peticion.Query["pagina"], 1, int.MaxValue / 100, 1, out var pagina)
            || !LeerEntero(peticion.Query["tamano"], 1, 100, 20, out var tamano))
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos,
                "Use pagina mayor que cero y tamano entre 1 y 100.");
        }

        return Responder(await claves.ClavesDeApi(apiId, pagina, tamano, cancelacion), TypedResults.Ok);
    }

    private static async Task<IResult> RevocarDeConsumidor(
        Guid apiId, Guid claveId, HttpContext http, IServicioClaves claves, CancellationToken cancelacion)
    {
        var usuarioId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var miembro = new MiembroDelPanel(usuarioId, Ip(http));
        return Responder(await claves.RevocarDeConsumidor(miembro, apiId, claveId, cancelacion), TypedResults.Ok);
    }

    // ---------- Portal ----------

    private static async Task<IResult> ListarDelConsumidor(
        HttpContext http, IResolutorPortal resolutor, IServicioClaves claves, CancellationToken cancelacion)
    {
        var consumidor = await ConsumidorDelHost(http, resolutor, cancelacion);
        if (consumidor is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new { elementos = await claves.ClavesDelConsumidor(consumidor, cancelacion) });
    }

    private static async Task<IResult> Emitir(
        [FromBody] PeticionEmitirClave cuerpo,
        HttpContext http,
        IValidator<PeticionEmitirClave> validador,
        IResolutorPortal resolutor,
        IServicioClaves claves,
        CancellationToken cancelacion)
    {
        var validacion = await validador.ValidateAsync(cuerpo, cancelacion);
        if (!validacion.IsValid)
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos,
                "Revise los datos enviados.", validacion.ToDictionary());
        }

        var consumidor = await ConsumidorDelHost(http, resolutor, cancelacion);
        if (consumidor is null)
        {
            return TypedResults.NotFound();
        }

        var tipo = cuerpo.Tipo == ContextoClave.TipoProduccion ? TipoClave.Produccion : TipoClave.Pruebas;
        return Responder(await claves.Emitir(consumidor, tipo, cancelacion),
            emitida => TypedResults.Created((string?)null, emitida));
    }

    private static async Task<IResult> Rotar(
        Guid claveId, HttpContext http, IResolutorPortal resolutor, IServicioClaves claves, CancellationToken cancelacion)
    {
        var consumidor = await ConsumidorDelHost(http, resolutor, cancelacion);
        if (consumidor is null)
        {
            return TypedResults.NotFound();
        }

        return Responder(await claves.Rotar(consumidor, claveId, cancelacion), TypedResults.Ok);
    }

    private static async Task<IResult> RevocarPropia(
        Guid claveId, HttpContext http, IResolutorPortal resolutor, IServicioClaves claves, CancellationToken cancelacion)
    {
        var consumidor = await ConsumidorDelHost(http, resolutor, cancelacion);
        if (consumidor is null)
        {
            return TypedResults.NotFound();
        }

        return Responder(await claves.RevocarPropia(consumidor, claveId, cancelacion), TypedResults.Ok);
    }

    /// <summary>
    /// El consumidor de la sesión y la API del host (10 §2). La sesión del portal pone el consumidor en
    /// <see cref="ClaimTypes.NameIdentifier"/> y su organización en <see cref="PoliticasAutorizacion.ClaimOrganizacion"/>;
    /// si la organización no es la de la API del host, es como si la clave no existiera (404).
    /// </summary>
    private static async Task<ConsumidorDelPortal?> ConsumidorDelHost(
        HttpContext http, IResolutorPortal resolutor, CancellationToken cancelacion)
    {
        var portal = await resolutor.Resolver(http.Request.Host.Host, cancelacion);
        if (portal is null
            || !Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var consumidorId)
            || !Guid.TryParse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion), out var organizacionId)
            || organizacionId != portal.OrganizacionId)
        {
            return null;
        }

        return new ConsumidorDelPortal(consumidorId, portal.ApiId, Ip(http));
    }

    // ---------- Traducción ----------

    private static IResult Responder<T>(Resultado<T> resultado, Func<T, IResult> exito)
    {
        if (resultado.EsExito)
        {
            return exito(resultado.Valor);
        }

        var error = resultado.Error;
        return error.Codigo switch
        {
            CodigosError.ClaveNoRotable => Problemas.Crear(StatusCodes.Status422UnprocessableEntity, error.Codigo, error.Mensaje),
            CodigosError.ClaveActivaExistente => Problemas.Crear(StatusCodes.Status409Conflict, error.Codigo, error.Mensaje),
            _ => TypedResults.NotFound(),
        };
    }

    private static string? Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();

    private static bool LeerEntero(string? texto, int minimo, int maximo, int predeterminado, out int valor)
    {
        if (string.IsNullOrEmpty(texto))
        {
            valor = predeterminado;
            return true;
        }

        return int.TryParse(texto, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out valor)
            && valor >= minimo && valor <= maximo;
    }
}
