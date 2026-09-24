using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace Shapi.Compuerta.Reenvio;

/// <summary>
/// Reenvía la petición a <c>url_origen</c> con YARP, conservando el método, el camino, la query y el cuerpo,
/// y devuelve la respuesta del origen tal cual (RF-31).
/// </summary>
public sealed class ReenvioOrigen(IHttpForwarder reenviador, HttpMessageInvoker invocador) : IReenvioOrigen
{
    /// <summary>08 §1: el origen tiene 30 segundos para responder.</summary>
    private static readonly ForwarderRequestConfig Configuracion = new() { ActivityTimeout = TimeSpan.FromSeconds(30) };

    public async Task ReenviarAsync(ContextoPeticion contexto)
    {
        var api = contexto.Api ?? throw new InvalidOperationException("No hay API resuelta para reenviar.");
        var clave = contexto.Clave ?? throw new InvalidOperationException("No hay clave validada para reenviar.");

        // Si el origen falla, YARP responde 502 o 504 sin cuerpo; JG-05 los traduce al contrato de errores.
        _ = await reenviador.SendAsync(contexto.Http, api.UrlOrigen, invocador, Configuracion,
            new TransformadorOrigen(clave));
    }

    /// <summary>
    /// El cliente de YARP. <see cref="SocketsHttpHandler"/> mantiene un pool de conexiones por destino (08 §8).
    /// </summary>
    public static HttpMessageInvoker CrearInvocador() => new(new SocketsHttpHandler
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        EnableMultipleHttp2Connections = true,
        ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
        ConnectTimeout = TimeSpan.FromSeconds(15),
    });
}
