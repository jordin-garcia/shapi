using Microsoft.Net.Http.Headers;
using Shapi.Contratos.Redis;
using Yarp.ReverseProxy.Forwarder;

namespace Shapi.Compuerta.Reenvio;

/// <summary>Las cabeceras hacia el origen (08 §5, RF-31).</summary>
internal sealed class TransformadorOrigen(ContextoApi api, ContextoClave clave, Action alRecibirRespuesta) : HttpTransformer
{
    public override ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse,
        CancellationToken cancellationToken)
    {
        if (proxyResponse is not null)
        {
            alRecibirRespuesta();
        }
        return base.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);
    }
    private const string ReenviadoPara = "X-Forwarded-For";
    private const string ReenviadoProtocolo = "X-Forwarded-Proto";
    private const string ReenviadoHost = "X-Forwarded-Host";

    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest,
        string destinationPrefix, CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
        var peticion = httpContext.Request;

        // El origen recibe el host de url_origen, no el de la API en Shapi.
        proxyRequest.Headers.Host = null;

        // El origen nunca recibe la clave del consumidor, ni una X-Shapi-* que no haya puesto la compuerta: así el
        // cliente no puede hacerse pasar por otro consumidor ni inventar un secreto.
        Quitar(proxyRequest, nombre => nombre.Equals(CabecerasCompuerta.ApiKey, StringComparison.OrdinalIgnoreCase)
            || nombre.StartsWith(CabecerasCompuerta.PrefijoShapi, StringComparison.OrdinalIgnoreCase));
        proxyRequest.Headers.TryAddWithoutValidation(CabecerasCompuerta.Consumidor, clave.ConsumidorId.ToString());
        proxyRequest.Headers.TryAddWithoutValidation(CabecerasCompuerta.Entorno, clave.Entorno);
        if (api.Secreto is not null)
        {
            proxyRequest.Headers.TryAddWithoutValidation(CabecerasCompuerta.Secreto, api.Secreto);
        }

        // X-Forwarded-For es una cadena: se agrega la IP de quien se conectó. El esquema original lo pone el borde
        // (Caddy), que le habla a la compuerta por http; si no viene, es el de esta conexión.
        Quitar(proxyRequest, nombre => nombre.Equals(ReenviadoPara, StringComparison.OrdinalIgnoreCase)
            || nombre.Equals(ReenviadoProtocolo, StringComparison.OrdinalIgnoreCase)
            || nombre.Equals(ReenviadoHost, StringComparison.OrdinalIgnoreCase));
        var cadena = string.Join(", ", peticion.Headers[ReenviadoPara].Where(v => !string.IsNullOrWhiteSpace(v)));
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var para = ip is null ? cadena : cadena.Length == 0 ? ip : $"{cadena}, {ip}";
        if (para.Length > 0)
        {
            proxyRequest.Headers.TryAddWithoutValidation(ReenviadoPara, para);
        }

        var protocolo = peticion.Headers[ReenviadoProtocolo].ToString();
        proxyRequest.Headers.TryAddWithoutValidation(ReenviadoProtocolo,
            protocolo is "http" or "https" ? protocolo : peticion.Scheme);
        proxyRequest.Headers.TryAddWithoutValidation(ReenviadoHost, peticion.Host.Value);

        // Nunca se envían las cookies de sesión de Shapi; las demás cookies del cliente pasan (08 §5).
        proxyRequest.Headers.Remove(HeaderNames.Cookie);
        var cookies = peticion.Headers.Cookie
            .SelectMany(valor => (valor ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(cookie => !EsCookieDeShapi(cookie))
            .ToList();
        if (cookies.Count > 0)
        {
            proxyRequest.Headers.TryAddWithoutValidation(HeaderNames.Cookie, string.Join("; ", cookies));
        }
    }

    private static bool EsCookieDeShapi(string cookie)
    {
        var nombre = cookie.Split('=', 2)[0].Trim();
        return CabecerasCompuerta.CookiesShapi.Contains(nombre, StringComparer.OrdinalIgnoreCase);
    }

    private static void Quitar(HttpRequestMessage peticion, Func<string, bool> condicion)
    {
        foreach (var nombre in peticion.Headers.Select(c => c.Key).Where(condicion).ToList())
        {
            peticion.Headers.Remove(nombre);
        }

        if (peticion.Content is not null)
        {
            foreach (var nombre in peticion.Content.Headers.Select(c => c.Key).Where(condicion).ToList())
            {
                peticion.Content.Headers.Remove(nombre);
            }
        }
    }
}
