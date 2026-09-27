using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace Shapi.Compuerta.Reenvio;

/// <summary>
/// Reenvía la petición a <c>url_origen</c> con YARP, conservando el método, el camino, la query y el cuerpo,
/// y devuelve la respuesta del origen tal cual (RF-31).
/// </summary>
public sealed class ReenvioOrigen(IHttpForwarder reenviador, HttpMessageInvoker invocador, TiemposOrigen tiempos)
    : IReenvioOrigen
{
    private readonly ForwarderRequestConfig _configuracion = new() { ActivityTimeout = tiempos.Total };

    public async Task ReenviarAsync(ContextoPeticion contexto)
    {
        var api = contexto.Api ?? throw new InvalidOperationException("No hay API resuelta para reenviar.");
        var clave = contexto.Clave ?? throw new InvalidOperationException("No hay clave validada para reenviar.");
        var http = contexto.Http;

        // 08 §1: el tiempo es total, desde el reenvío hasta el final de la respuesta, no solo de inactividad
        // (ActivityTimeout se reinicia con cada byte).
        using var tiempoTotal = new CancellationTokenSource(tiempos.Total);

        // Si el origen falla, YARP responde 502 o 504 sin cuerpo; JG-05 los traduce al contrato de errores.
        var error = await reenviador.SendAsync(http, api.UrlOrigen, invocador, _configuracion,
            new TransformadorOrigen(clave), tiempoTotal.Token);

        if (error != ForwarderError.None && tiempoTotal.IsCancellationRequested
            && !http.RequestAborted.IsCancellationRequested && !http.Response.HasStarted)
        {
            http.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
        }
    }

    public static HttpMessageInvoker CrearInvocador(TiemposOrigen tiempos) => new(CrearManejador(tiempos));

    /// <summary>
    /// El cliente de YARP. <see cref="SocketsHttpHandler"/> mantiene un pool de conexiones por destino (08 §8).
    /// Si no conecta en <see cref="TiemposOrigen.Conexion"/>, YARP responde 502 (08 §1).
    /// </summary>
    public static SocketsHttpHandler CrearManejador(TiemposOrigen tiempos) => new()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        EnableMultipleHttp2Connections = true,
        ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
        ConnectTimeout = tiempos.Conexion,
    };
}
