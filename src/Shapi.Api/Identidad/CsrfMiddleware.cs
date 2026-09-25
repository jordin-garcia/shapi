using Microsoft.AspNetCore.Http;
using Shapi.Contratos;

namespace Shapi.Api.Identidad;

/// <summary>
/// Protección CSRF de la API de control (10 §1): todo método que no sea GET exige <c>X-Requested-With: shapi</c>
/// y, si llega <c>Origin</c>, que su host sea el de la petición. Si no, 403 <c>csrf</c>.
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
            if (!string.IsNullOrEmpty(origen) &&
                (!Uri.TryCreate(origen, UriKind.Absolute, out var uriOrigen) ||
                 !string.Equals(uriOrigen.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase)))
            {
                await Problemas.Escribir(context, StatusCodes.Status403Forbidden, CodigosError.Csrf, "El origen de la petición no coincide con el host.");
                return;
            }
        }

        await next(context);
    }
}
