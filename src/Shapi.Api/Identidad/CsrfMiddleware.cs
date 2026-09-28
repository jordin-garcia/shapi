using Microsoft.AspNetCore.Http;
using Shapi.Contratos;

namespace Shapi.Api.Identidad;

/// <summary>
/// Protección CSRF de la API de control (10 §1): todo método que no sea GET exige <c>X-Requested-With: shapi</c>
/// y, si llega <c>Origin</c>, que su esquema, su host y su puerto sean los de la petición. Si no, 403 <c>csrf</c>.
/// </summary>
public class CsrfMiddleware(RequestDelegate next)
{
    public const string Cabecera = "X-Requested-With";
    public const string ValorCabecera = "shapi";

    public async Task InvokeAsync(HttpContext context)
    {
        var metodo = context.Request.Method;
        var esSeguro = HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo) || HttpMethods.IsTrace(metodo);
        if (!esSeguro)
        {
            if (context.Request.Headers[Cabecera] != ValorCabecera)
            {
                await Problemas.Escribir(context, StatusCodes.Status403Forbidden, CodigosError.Csrf, "Falta la cabecera X-Requested-With o su valor no es válido.");
                return;
            }

            var origen = context.Request.Headers.Origin.ToString();
            if (!string.IsNullOrEmpty(origen) && !MismoOrigen(origen, context.Request))
            {
                await Problemas.Escribir(context, StatusCodes.Status403Forbidden, CodigosError.Csrf, "El origen de la petición no coincide con el host.");
                return;
            }
        }

        await next(context);
    }

    /// <summary>Compara el esquema, el host y el puerto. Detrás del borde, el esquema llega en X-Forwarded-Proto.</summary>
    private static bool MismoOrigen(string origen, HttpRequest peticion)
    {
        if (!Uri.TryCreate(origen, UriKind.Absolute, out var uriOrigen))
        {
            return false;
        }

        var puertoPeticion = peticion.Host.Port ?? (peticion.IsHttps ? 443 : 80);
        return string.Equals(uriOrigen.Scheme, peticion.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(uriOrigen.Host, peticion.Host.Host, StringComparison.OrdinalIgnoreCase)
            && uriOrigen.Port == puertoPeticion;
    }
}
