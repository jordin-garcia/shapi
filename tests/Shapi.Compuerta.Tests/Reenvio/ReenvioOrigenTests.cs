using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Compuerta.Reenvio;
using Shapi.Compuerta.Tests.Soporte;
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
        using var manejador = ReenvioOrigen.CrearManejador(TiemposOrigen.PorDefecto);

        manejador.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(10));
        manejador.UseProxy.Should().BeFalse();
        manejador.AllowAutoRedirect.Should().BeFalse();
        manejador.UseCookies.Should().BeFalse();
        manejador.AutomaticDecompression.Should().Be(DecompressionMethods.None);
        manejador.EnableMultipleHttp2Connections.Should().BeTrue();
    }

    [Fact]
    public async Task RF_31_InvocadorReal_OrigenQueNoAceptaConexiones_Responde502()
    {
        // Con el cliente real de YARP (sin el origen en memoria): un puerto cerrado responde 502. JG-05 lo traduce
        // a origen_inaccesible (08 §4).
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
            web.UseSetting("SHAPI_REDIS", entorno.CadenaRedis));
        var (host, clave) = await SembrarAsync("http://127.0.0.1:1");
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{host}") });
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task RF_31_TiempoTotal_OrigenQueNoResponde_Responde504()
    {
        // 08 §1 y §3: si el origen no responde a tiempo, 504. JG-05 lo traduce a origen_sin_respuesta.
        using var fabrica = FabricaConTiempoTotal(TimeSpan.FromMilliseconds(500));
        var (host, clave) = await SembrarAsync(EntornoCompuerta.UrlOrigen);
        entorno.Origen.Responder = async http => await Task.Delay(TimeSpan.FromSeconds(5), http.RequestAborted);

        try
        {
            using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{host}") });
            using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
            peticion.Headers.Add("X-Api-Key", clave);

            var respuesta = await cliente.SendAsync(peticion);

            respuesta.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
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
}
