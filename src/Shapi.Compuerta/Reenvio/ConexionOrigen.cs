using System.Net.Sockets;
using Shapi.Contratos.Red;

namespace Shapi.Compuerta.Reenvio;

/// <summary>El <c>ConnectCallback</c> del cliente de la compuerta.</summary>
public sealed class ConexionOrigen(ValidadorDireccionOrigen validador, ProteccionOrigen proteccion)
{
    public async ValueTask<Stream> ConectarAsync(SocketsHttpConnectionContext contexto, CancellationToken cancelacion)
    {
        _ = validador;
        _ = proteccion;
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(contexto.DnsEndPoint, cancelacion);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
