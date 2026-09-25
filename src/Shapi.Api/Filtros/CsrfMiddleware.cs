using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Shapi.Api.Filtros;

public class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo aplica para métodos diferentes a GET, HEAD, OPTIONS, TRACE
        var method = context.Request.Method;
        if (!HttpMethods.IsGet(method) &&
            !HttpMethods.IsHead(method) &&
            !HttpMethods.IsOptions(method) &&
            !HttpMethods.IsTrace(method))
        {
            // Verificamos cabecera X-Requested-With
            if (!context.Request.Headers.TryGetValue("X-Requested-With", out var xRequestedWith) ||
                xRequestedWith != "shapi")
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Type = "csrf",
                    Title = "Falta o es incorrecta la cabecera X-Requested-With."
                });
                return;
            }

            // Verificamos cabecera Origin (si está presente)
            if (context.Request.Headers.TryGetValue("Origin", out var originStr) && !string.IsNullOrEmpty(originStr))
            {
                if (Uri.TryCreate(originStr, UriKind.Absolute, out var originUri))
                {
                    if (!string.Equals(originUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Type = "csrf",
                            Title = "La cabecera Origin no coincide con el Host esperado."
                        });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
