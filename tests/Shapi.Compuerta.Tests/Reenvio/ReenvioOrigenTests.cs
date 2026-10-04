using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Compuerta.Reenvio;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Red;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Reenvio;

// Filtro 8 de 08 §3: tiempos de espera y configuración real del cliente de YARP (auditoría H-80 y H-81).
public class ReenvioOrigenTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>
{
    [Fact]
    public void RF_31_TiemposPorDefecto_TreintaSegundosEnTotalYDiezParaConectar()
    {
        // 08 §1
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
            web.UseSetting("SHAPI_REDIS", entorno.CadenaRedis));

        var tiempos = fabrica.Services.GetRequiredService<TiemposOrigen>();

        tiempos.Total.Should().Be(TimeSpan.FromSeconds(30));
        tiempos.Conexion.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void RF_31_Invocador_UsaLaConfiguracionDelCliente()
    {
        // 08 §1 y §8: sin proxy ni redirecciones ni cookies, con el tiempo de conexión y un pool por destino.
        // Criterio 5 de JG-05: cada conexión pasa por el ConnectCallback que valida la dirección (RNF-10).
        var conexion = new ConexionOrigen(new ValidadorDireccionOrigen(), new ProteccionOrigen(false, []));
        using var manejador = ReenvioOrigen.CrearManejador(TiemposOrigen.PorDefecto, conexion);

        manejador.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(10));
        manejador.UseProxy.Should().BeFalse();
        manejador.AllowAutoRedirect.Should().BeFalse();
        manejador.UseCookies.Should().BeFalse();
        manejador.AutomaticDecompression.Should().Be(DecompressionMethods.None);
        manejador.EnableMultipleHttp2Connections.Should().BeTrue();
        manejador.ConnectCallback.Should().NotBeNull();
    }

    [Fact]
    public async Task RF_31_InvocadorReal_OrigenQueNoAceptaConexiones_Responde502()
    {
        // Con el cliente real de YARP (sin el origen en memoria): 502 origen_inaccesible (08 §4). Desde JG-05,
        // 127.0.0.1 además es una dirección prohibida (RNF-10).
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
            web.UseSetting("SHAPI_REDIS", entorno.CadenaRedis));
        var (host, clave) = await SembrarAsync("http://127.0.0.1:1");
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{host}") });
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadGateway, "origen_inaccesible");
    }

    [Fact]
    public async Task RF_31_TiempoTotal_OrigenQueNoResponde_Responde504()
    {
        // 08 §1 y §3, criterio 6 de JG-05: si el origen no responde a tiempo, 504 origen_sin_respuesta.
        using var fabrica = FabricaConTiempoTotal(TimeSpan.FromMilliseconds(500));
        var (host, clave) = await SembrarAsync(EntornoCompuerta.UrlOrigen);
        entorno.Origen.Responder = async http => await Task.Delay(TimeSpan.FromSeconds(5), http.RequestAborted);

        try
        {
            using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{host}") });
            using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
            peticion.Headers.Add("X-Api-Key", clave);

            var respuesta = await cliente.SendAsync(peticion);

            await VerificarErrorAsync(respuesta, HttpStatusCode.GatewayTimeout, "origen_sin_respuesta");
        }
        finally
        {
            entorno.Origen.Olvidar();
        }
    }

    [Fact]
    public async Task RF_31_TiempoTotal_OrigenQueEnviaPocoAPoco_SeCortaAlVencer()
    {
        // 08 §1 (auditoría H-80): el límite es total, no de inactividad. Un origen que manda un byte cada 100 ms
        // nunca está inactivo, pero no puede pasarse del tiempo total.
        using var fabrica = FabricaConTiempoTotal(TimeSpan.FromMilliseconds(700));
        var (host, clave) = await SembrarAsync(EntornoCompuerta.UrlOrigen);
        entorno.Origen.Responder = async http =>
        {
            http.Response.StatusCode = StatusCodes.Status200OK;
            await http.Response.StartAsync(http.RequestAborted);
            for (var i = 0; i < 40; i++)
            {
                await http.Response.WriteAsync("x", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
                await Task.Delay(TimeSpan.FromMilliseconds(100), http.RequestAborted);
            }
        };

        try
        {
            using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{host}") });
            using var peticion = new HttpRequestMessage(HttpMethod.Get, "/descarga");
            peticion.Headers.Add("X-Api-Key", clave);
            var cronometro = Stopwatch.StartNew();
            var recibido = "";

            try
            {
                using var respuesta = await cliente.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead);
                recibido = await respuesta.Content.ReadAsStringAsync();
            }
            catch (Exception excepcion) when (excepcion is HttpRequestException or IOException or OperationCanceledException)
            {
                // La compuerta corta la respuesta al vencer el tiempo total.
            }

            cronometro.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3), "el origen tardaría 4 s en terminar");
            recibido.Length.Should().BeLessThan(40);
        }
        finally
        {
            entorno.Origen.Olvidar();
        }
    }

    [Fact]
    public async Task RF_31_Cuerpo_ContentLengthDeMasDeDiezMegabytes_Responde413SinReenviar()
    {
        // Criterio 6 y 08 §1
        var (host, clave) = await SembrarAsync(EntornoCompuerta.UrlOrigen);
        entorno.Origen.Olvidar();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/cotizaciones")
        {
            Content = new ByteArrayContent(new byte[(10 * 1024 * 1024) + 1]),
        };
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.RequestEntityTooLarge, "cuerpo_demasiado_grande");
        entorno.Origen.Ultima.Should().BeNull();
    }

    [Fact]
    public async Task RF_31_Cuerpo_SinContentLengthYDeMasDeDiezMegabytes_Responde413()
    {
        // Criterio 6: un cuerpo por partes (chunked) no anuncia su tamaño; Kestrel lo corta al pasar el límite.
        // TestServer no aplica MaxRequestBodySize, así que la compuerta y el origen corren en Kestrel de verdad.
        await using var origen = await OrigenReal.IniciarAsync(async http =>
        {
            await http.Request.Body.CopyToAsync(Stream.Null, http.RequestAborted);
            http.Response.StatusCode = StatusCodes.Status200OK;
        });
        var (host, clave) = await SembrarAsync($"http://localhost:{origen.Puerto}");
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", entorno.CadenaRedis);
            web.UseSetting("SHAPI_MODO_DEMO", "true");
            web.UseSetting("SHAPI_ORIGENES_PERMITIDOS", $"localhost:{origen.Puerto}");
        });
        // Un puerto libre: UseKestrel(0) usa el 5000 por defecto y choca con otras pruebas en paralelo.
        fabrica.UseKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0));
        fabrica.StartServer();
        using var cliente = fabrica.CreateClient();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/cotizaciones")
        {
            Content = new StreamContent(new FlujoSinLongitud((10 * 1024 * 1024) + 1)),
        };
        peticion.Headers.Host = host;
        peticion.Headers.Add("X-Api-Key", clave);
        peticion.Headers.TransferEncodingChunked = true;

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.RequestEntityTooLarge, "cuerpo_demasiado_grande");
    }

    [Fact]
    public async Task RF_31_Cuerpo_DeExactamenteDiezMegabytes_SeReenvia()
    {
        // Criterio 6: el límite es "más de 10 MB".
        var (host, clave) = await SembrarAsync(EntornoCompuerta.UrlOrigen);
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/cotizaciones")
        {
            Content = new ByteArrayContent(new byte[10 * 1024 * 1024]),
        };
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado);
        respuesta.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var error = documento.RootElement.GetProperty("error");
        error.GetProperty("codigo").GetString().Should().Be(codigo);
        error.GetProperty("estado").GetInt32().Should().Be((int)estado);
        error.GetProperty("mensaje").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private WebApplicationFactory<Program> FabricaConTiempoTotal(TimeSpan total) =>
        entorno.Fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
            servicios.AddSingleton(new TiemposOrigen(total, TiemposOrigen.PorDefecto.Conexion))));

    private async Task<(string Host, string Clave)> SembrarAsync(string urlOrigen)
    {
        var host = $"api{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await entorno.SembrarApiAsync(host, urlOrigen: urlOrigen);
        var clave = $"shp_prod_{Guid.NewGuid():N}"[..35];
        await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(clave));
        return (host, clave);
    }

    /// <summary>Un flujo que no se puede medir, para que el cliente lo mande por partes.</summary>
    private sealed class FlujoSinLongitud(long longitud) : Stream
    {
        private long _restante = longitud;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var leidos = (int)Math.Min(count, _restante);
            Array.Clear(buffer, offset, leidos);
            _restante -= leidos;
            return leidos;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
