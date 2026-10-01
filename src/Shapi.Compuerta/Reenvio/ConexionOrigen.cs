using System.Net;
using System.Net.Sockets;
using Shapi.Contratos.Red;

namespace Shapi.Compuerta.Reenvio;

/// <summary>
/// El <c>ConnectCallback</c> del cliente de la compuerta (RNF-10, 10 §4): antes de cada conexión resuelve el host del
/// origen con <see cref="ValidadorDireccionOrigen"/>, rechaza las direcciones internas y se conecta a una de las
/// direcciones ya validadas, para que un DNS que cambie entre la validación y la conexión (<i>DNS rebinding</i>) no
/// sirva de nada. Un rechazo termina en 502 <c>origen_inaccesible</c>.
/// </summary>
public sealed class ConexionOrigen(ValidadorDireccionOrigen validador, ProteccionOrigen proteccion)
{
    public async ValueTask<Stream> ConectarAsync(SocketsHttpConnectionContext contexto, CancellationToken cancelacion)
    {
        var destino = contexto.DnsEndPoint;
        var host = destino.Host.Trim('[', ']');
        var autoridad = host.Contains(':') ? $"[{host}]:{destino.Port}" : $"{host}:{destino.Port}";

        // El esquema solo importa para la forma de la URL: la lista del modo demostración compara host y puerto.
        var validacion = await validador.Validar($"http://{autoridad}/", proteccion.ModoDemo, proteccion.OrigenesPermitidos,
            cancelacion);
        if (!validacion.EsValida)
        {
            throw new HttpRequestException(HttpRequestError.ConnectionError,
                $"La compuerta no se conecta a {autoridad}: {validacion.Detalle}");
        }

        SocketException? ultimoError = null;
        foreach (var direccion in validacion.Direcciones)
        {
            var socket = new Socket(direccion.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(direccion, destino.Port), cancelacion);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException excepcion)
            {
                socket.Dispose();
                ultimoError = excepcion;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw ultimoError ?? new SocketException((int)SocketError.HostNotFound);
    }
}
