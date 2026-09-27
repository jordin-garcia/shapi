using System.Net;
using System.Net.Sockets;
using System.Text;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Correo;

namespace Shapi.Api.Tests.Correo;

public class EnviadorSmtpTests
{
    [Theory]
    [InlineData(false, 1025, SecureSocketOptions.None)]
    [InlineData(false, 465, SecureSocketOptions.None)]
    [InlineData(true, 465, SecureSocketOptions.SslOnConnect)]
    [InlineData(true, 587, SecureSocketOptions.StartTls)]
    [InlineData(true, 25, SecureSocketOptions.StartTls)]
    public void RF_46_Tls_UsaSmtpsImplicitoEnEl465YStartTlsObligatorioEnLosDemas(
        bool tls,
        int puerto,
        SecureSocketOptions esperado)
    {
        EnviadorSmtp.SeguridadPara(tls, puerto).Should().Be(esperado);
    }

    [Fact]
    public async Task RF_46_ErrorAlCerrarLaSesion_NoHaceFallarUnEnvioYaAceptado()
    {
        await using var servidor = new ServidorSmtpQueFallaAlSalir();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SHAPI_DOMINIO_BASE"] = "shapi.localhost",
                ["SHAPI_SMTP_HOST"] = "127.0.0.1",
                ["SHAPI_SMTP_PUERTO"] = servidor.Puerto.ToString(),
                ["SHAPI_SMTP_TLS"] = "false",
            })
            .Build();
        var enviador = new EnviadorSmtp(configuracion);
        var correo = new CorreoSaliente(
            "recuperacion",
            "ana@ejemplo.com",
            """{"nombre":"Ana","token":"token-1"}""",
            "Recupere su contraseña",
            DateTimeOffset.UtcNow);

        var accion = () => enviador.EnviarAsync(correo, new CorreoRenderizado("<p>Hola</p>", "Hola", null));

        await accion.Should().NotThrowAsync();
        servidor.MensajesAceptados.Should().Be(1);
    }

    /// <summary>Servidor SMTP mínimo que acepta el mensaje y cierra la conexión sin responder al <c>QUIT</c>.</summary>
    private sealed class ServidorSmtpQueFallaAlSalir : IAsyncDisposable
    {
        private readonly TcpListener _escucha = new(IPAddress.Loopback, 0);
        private readonly Task _atencion;

        public ServidorSmtpQueFallaAlSalir()
        {
            _escucha.Start();
            _atencion = Atender();
        }

        public int Puerto => ((IPEndPoint)_escucha.LocalEndpoint).Port;

        public int MensajesAceptados { get; private set; }

        private async Task Atender()
        {
            using var conexion = await _escucha.AcceptTcpClientAsync();
            var flujo = conexion.GetStream();
            using var lector = new StreamReader(flujo, Encoding.ASCII);
            await using var escritor = new StreamWriter(flujo, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            await escritor.WriteLineAsync("220 prueba");
            while (await lector.ReadLineAsync() is { } linea)
            {
                switch (linea.Split(' ')[0].ToUpperInvariant())
                {
                    case "DATA":
                        await escritor.WriteLineAsync("354 envie el mensaje");
                        while (await lector.ReadLineAsync() is { } dato && dato != ".")
                        {
                        }

                        MensajesAceptados++;
                        await escritor.WriteLineAsync("250 aceptado");
                        break;
                    case "QUIT":
                        return;
                    default:
                        await escritor.WriteLineAsync("250 prueba");
                        break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _escucha.Stop();
            try
            {
                await _atencion;
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
