using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Shapi.Aplicacion.Apis;
using Shapi.Contratos.Red;

namespace Shapi.Infraestructura.Apis;

/// <summary>Prueba el origen sin seguir redirecciones y contra las direcciones que ya pasaron la validación SSRF.</summary>
public sealed class ProbadorOrigenHttp : IProbadorOrigen
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(5);

    public async Task<ResultadoPruebaOrigen> Probar(
        DireccionOrigenValidada origen,
        CancellationToken cancelacion = default)
    {
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = (contexto, token) => Conectar(origen.Direcciones, contexto.DnsEndPoint.Port, token),
        };
        using var cliente = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        limite.CancelAfter(EsperaMaxima);
        var cronometro = Stopwatch.StartNew();
        try
        {
            using var respuesta = await cliente.GetAsync(
                origen.Direccion,
                HttpCompletionOption.ResponseHeadersRead,
                limite.Token);
            cronometro.Stop();
            return ResultadoPruebaOrigen.Exito(cronometro.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            return ResultadoPruebaOrigen.Fallo("El servidor no respondió en menos de 5 segundos.");
        }
        catch (HttpRequestException)
        {
            return ResultadoPruebaOrigen.Fallo("No se pudo establecer la conexión con el servidor de origen.");
        }
    }

    private static async ValueTask<Stream> Conectar(
        IReadOnlyList<IPAddress> direcciones,
        int puerto,
        CancellationToken cancelacion)
    {
        Exception? ultimoError = null;
        foreach (var direccion in direcciones)
        {
            var socket = new Socket(direccion.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(direccion, puerto), cancelacion);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                socket.Dispose();
                ultimoError = ex;
                if (cancelacion.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        throw ultimoError ?? new SocketException((int)SocketError.HostUnreachable);
    }
}
