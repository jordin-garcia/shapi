using Microsoft.Net.Http.Headers;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 0 (08 §6): CORS. Solo el portal de la API (<c>portal_host</c>, con <c>http</c> o <c>https</c> y cualquier
/// puerto) recibe <c>Access-Control-Allow-Origin</c>. El preflight (<c>OPTIONS</c> con <c>Origin</c> y
/// <c>Access-Control-Request-Method</c>) se contesta aquí con 204, sin pedir la clave. Las cabeceras se ponen justo
/// antes de responder, en todas las respuestas, y reemplazan las <c>Access-Control-*</c> que haya mandado el origen.
/// </summary>
public sealed class FiltroCors : IFiltroCompuerta
{
    private const string PrefijoCors = "Access-Control-";

    private static readonly string CabecerasPermitidas = string.Join(", ", CabecerasCompuerta.PermitidasCors);

    private static readonly string CabecerasExpuestas = string.Join(", ", CabecerasCompuerta.ExpuestasCors);

    private static readonly ResultadoFiltro Preflight = ResultadoFiltro.Responder(StatusCodes.Status204NoContent);

    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        var http = contexto.Http;
        var peticion = http.Request;
        var origen = peticion.Headers.Origin.ToString();
        var api = contexto.Api is { EstaPublicada: true } publicada ? publicada : null;
        var esPreflight = HttpMethods.IsOptions(peticion.Method) && origen.Length > 0
            && !string.IsNullOrEmpty(peticion.Headers.AccessControlRequestMethod);

        var cabeceras = new Dictionary<string, string>();
        if (api is not null && EsDelPortal(origen, api.PortalHost))
        {
            cabeceras[HeaderNames.AccessControlAllowOrigin] = origen;
            if (esPreflight)
            {
                if (contexto.Rutas.MetodosExpuestos.Count > 0)
                {
                    cabeceras[HeaderNames.AccessControlAllowMethods] = string.Join(", ", contexto.Rutas.MetodosExpuestos);
                }

                cabeceras[HeaderNames.AccessControlAllowHeaders] = CabecerasPermitidas;
                cabeceras[HeaderNames.AccessControlMaxAge] = "600";
            }
            else
            {
                cabeceras[HeaderNames.AccessControlExposeHeaders] = CabecerasExpuestas;
            }
        }

        http.Response.OnStarting(() =>
        {
            Aplicar(http.Response, cabeceras, varia: origen.Length > 0);
            return Task.CompletedTask;
        });

        // El preflight de un host sin API publicada sigue hasta FiltroApi, que responde 404.
        return ValueTask.FromResult(esPreflight && api is not null ? Preflight : ResultadoFiltro.Continuar);
    }

    private static bool EsDelPortal(string origen, string? portalHost) =>
        portalHost is not null
        && Uri.TryCreate(origen, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && uri.AbsolutePath == "/"
        && string.Equals(uri.Host, portalHost, StringComparison.OrdinalIgnoreCase);

    private static void Aplicar(HttpResponse respuesta, Dictionary<string, string> cabeceras, bool varia)
    {
        foreach (var nombre in respuesta.Headers.Keys.Where(n => n.StartsWith(PrefijoCors, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            respuesta.Headers.Remove(nombre);
        }

        foreach (var (nombre, valor) in cabeceras)
        {
            respuesta.Headers[nombre] = valor;
        }

        if (varia)
        {
            respuesta.Headers.Append(HeaderNames.Vary, HeaderNames.Origin);
        }
    }
}
