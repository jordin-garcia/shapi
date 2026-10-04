using System.Diagnostics;
using System.Net;
using Shapi.Compuerta.Filtros;
using Shapi.Contratos;
using Yarp.ReverseProxy.Forwarder;

namespace Shapi.Compuerta.Reenvio;

/// <summary>
/// Reenvía la petición a <c>url_origen</c> con YARP, conservando el método, el camino, la query y el cuerpo,
/// y devuelve la respuesta del origen tal cual (RF-31). Si el reenvío falla antes de que empiece la respuesta,
/// responde con el contrato de errores (08 §4).
/// </summary>
public sealed class ReenvioOrigen(IHttpForwarder reenviador, HttpMessageInvoker invocador, TiemposOrigen tiempos)
    : IReenvioOrigen
{
    private static readonly ResultadoFiltro Inaccesible = ResultadoFiltro.Rechazar(
        StatusCodes.Status502BadGateway, CodigosError.OrigenInaccesible,
        "No se pudo conectar con el servidor del proveedor de esta API. Intente de nuevo más tarde.");

    private static readonly ResultadoFiltro SinRespuesta = ResultadoFiltro.Rechazar(
        StatusCodes.Status504GatewayTimeout, CodigosError.OrigenSinRespuesta,
        "El servidor del proveedor de esta API no respondió a tiempo.");

    private readonly ForwarderRequestConfig _configuracion = new() { ActivityTimeout = tiempos.Total };

    public async Task ReenviarAsync(ContextoPeticion contexto)
    {
        var api = contexto.Api ?? throw new InvalidOperationException("No hay API resuelta para reenviar.");
        var clave = contexto.Clave ?? throw new InvalidOperationException("No hay clave validada para reenviar.");
        var http = contexto.Http;

        // 08 §1: el tiempo es total, desde el reenvío hasta el final de la respuesta, no solo de inactividad
        // (ActivityTimeout se reinicia con cada byte).
        using var tiempoTotal = new CancellationTokenSource(tiempos.Total);

        var error = await reenviador.SendAsync(http, api.UrlOrigen, invocador, _configuracion,
            new TransformadorOrigen(api, clave), tiempoTotal.Token);
        if (error == ForwarderError.None || http.Response.HasStarted || http.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        // YARP deja 502 (falló el envío o la respuesta), 504 o 400 (el cuerpo del cliente falló, por ejemplo al pasar
        // de 10 MB) sin cuerpo; se traducen al contrato de errores. YARP reporta el vencimiento de ConnectTimeout como
        // RequestTimedOut (504), así que la falta de conexión se decide por la excepción y no por el código.
        var sinConexion = !tiempoTotal.IsCancellationRequested && NoSeConecto(http);
        var rechazo = CuerpoDemasiadoGrande(http) ? TuberiaCompuerta.CuerpoDemasiadoGrande
            : sinConexion ? Inaccesible
            : tiempoTotal.IsCancellationRequested || http.Response.StatusCode == StatusCodes.Status504GatewayTimeout ? SinRespuesta
            : http.Response.StatusCode == StatusCodes.Status502BadGateway ? Inaccesible
            : null;
        if (rechazo is not null)
        {
            // 08 §3: si no se pudo conectar, la petición no llegó al origen y la cuota se devuelve. Si el origen cortó
            // la conexión después de recibirla (502), o no respondió a tiempo (504), la cuota se descuenta.
            if (sinConexion && contexto.DevolverReserva is { } devolver)
            {
                await devolver();
            }

            await RespuestaError.EscribirAsync(http, rechazo);
        }
    }

    /// <summary>
    /// Si la petición no llegó al origen porque no se pudo abrir la conexión: la dirección se rechazó o no resolvió
    /// (<see cref="ConexionOrigen"/>), el origen no la aceptó o venció <see cref="TiemposOrigen.Conexion"/>
    /// (<see cref="SocketsHttpHandler.ConnectTimeout"/> lanza una cancelación con un <see cref="TimeoutException"/>
    /// adentro).
    /// </summary>
    private static bool NoSeConecto(HttpContext http)
    {
        for (var excepcion = http.Features.Get<IForwarderErrorFeature>()?.Exception; excepcion is not null;
             excepcion = excepcion.InnerException)
        {
            // Un SocketException también aparece si el origen corta después de recibir la petición; por eso solo cuenta
            // el error de conexión que arma SocketsHttpHandler (también envuelve lo que lanza ConnectCallback).
            if (excepcion is TimeoutException
                or HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError or HttpRequestError.NameResolutionError })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Si el reenvío falló porque el cuerpo pasó del límite de <see cref="TuberiaCompuerta.LimiteCuerpo"/>.</summary>
    private static bool CuerpoDemasiadoGrande(HttpContext http)
    {
        for (var excepcion = http.Features.Get<IForwarderErrorFeature>()?.Exception; excepcion is not null;
             excepcion = excepcion.InnerException)
        {
            if (excepcion is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge })
            {
                return true;
            }
        }

        return false;
    }

    public static HttpMessageInvoker CrearInvocador(TiemposOrigen tiempos, ConexionOrigen conexion) =>
        new(CrearManejador(tiempos, conexion));

    /// <summary>
    /// El cliente de YARP. <see cref="SocketsHttpHandler"/> mantiene un pool de conexiones por destino (08 §8).
    /// Si no conecta en <see cref="TiemposOrigen.Conexion"/>, la compuerta responde 502 (08 §1; ver
    /// <see cref="NoSeConecto"/>). Cada conexión pasa por
    /// <see cref="ConexionOrigen"/>, que rechaza las direcciones internas (RNF-10).
    /// </summary>
    public static SocketsHttpHandler CrearManejador(TiemposOrigen tiempos, ConexionOrigen conexion) => new()
    {
        ConnectCallback = conexion.ConectarAsync,
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        EnableMultipleHttp2Connections = true,
        ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
        ConnectTimeout = tiempos.Conexion,
    };
}
