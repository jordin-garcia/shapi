using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shapi.Contratos.Red;
using Shapi.Infraestructura.Apis;

namespace Shapi.Api.Tests.Apis;

public class ProbadorOrigenHttpTests
{
    [Theory]
    [InlineData("503 Service Unavailable", null)]
    [InlineData("302 Found", "Location: http://127.0.0.1:1/no-seguir\r\n")]
    public async Task RF_08_Probar_CualquierRespuestaHttpEsExitosaYSinRedirecciones(string estado, string? encabezado)
    {
        using var servidor = new TcpListener(IPAddress.Loopback, 0);
        servidor.Start();
        var puerto = ((IPEndPoint)servidor.LocalEndpoint).Port;
        var origen = new DireccionOrigenValidada(
            new Uri($"http://origen.ejemplo:{puerto}/salud"),
            [IPAddress.Loopback]);
        var probador = new ProbadorOrigenHttp();

        var pruebaPendiente = probador.Probar(origen);
        using var conexion = await servidor.AcceptTcpClientAsync();
        await using var flujo = conexion.GetStream();
        var peticion = new byte[1024];
        var leidos = await flujo.ReadAsync(peticion);
        var respuesta = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {estado}\r\n{encabezado}Content-Length: 0\r\nConnection: close\r\n\r\n");
        await flujo.WriteAsync(respuesta);

        var resultado = await pruebaPendiente;

        Encoding.ASCII.GetString(peticion, 0, leidos).Should().StartWith("GET /salud HTTP/1.1\r\n");
        resultado.EsExitosa.Should().BeTrue();
    }

    [Fact]
    public async Task RF_08_Probar_OrigenQueNoResponde_CancelaALosCincoSegundos()
    {
        using var servidor = new TcpListener(IPAddress.Loopback, 0);
        servidor.Start();
        var puerto = ((IPEndPoint)servidor.LocalEndpoint).Port;
        var origen = new DireccionOrigenValidada(
            new Uri($"http://origen.ejemplo:{puerto}/salud"),
            [IPAddress.Loopback]);
        var probador = new ProbadorOrigenHttp();
        var cronometro = Stopwatch.StartNew();

        var pruebaPendiente = probador.Probar(origen);
        using var conexion = await servidor.AcceptTcpClientAsync();
        var resultado = await pruebaPendiente;
        cronometro.Stop();

        resultado.EsExitosa.Should().BeFalse();
        resultado.Detalle.Should().Contain("5 segundos");
        cronometro.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(4.5));
        cronometro.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(7));
    }
}
