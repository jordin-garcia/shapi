using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Reenvio;

// Criterios 5 y 6 de JG-05: el cliente real de la compuerta (ConnectCallback) contra SSRF (RNF-10, 10 §4) y los
// errores del reenvío (08 §4), con un origen Kestrel en 127.0.0.1.
public class ProteccionOrigenTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>, IAsyncLifetime
{
    private OrigenReal _origen = null!;

    public async Task InitializeAsync() =>
        _origen = await OrigenReal.IniciarAsync(async http =>
        {
            if (http.Request.Path == "/redirige")
            {
                http.Response.StatusCode = StatusCodes.Status302Found;
                http.Response.Headers.Location = "http://169.254.169.254/latest/meta-data";
                return;
            }

            http.Response.StatusCode = StatusCodes.Status200OK;
            await http.Response.WriteAsync("origen real");
        });

    public async Task DisposeAsync() => await _origen.DisposeAsync();

    [Theory]
    [InlineData("http://127.0.0.1:{0}")]
    [InlineData("http://localhost:{0}")]
    [InlineData("http://10.0.0.5:8080")]
    [InlineData("http://[::1]:{0}")]
    [InlineData("http://169.254.169.254")]
    public async Task RNF_10_OrigenConDireccionInterna_Responde502OrigenInaccesibleSinConectar(string formato)
    {
        // Criterio 5: fuera del modo demostración, una dirección interna no se conecta.
        using var fabrica = Fabrica(modoDemo: false, permitidos: null);
        var host = await SembrarAsync(string.Format(System.Globalization.CultureInfo.InvariantCulture, formato, _origen.Puerto));

        var respuesta = await EnviarAsync(fabrica, host);

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadGateway, "origen_inaccesible");
        _origen.Peticiones.Should().Be(0);
    }

    [Fact]
    public async Task RNF_10_ModoDemoConElOrigenPermitido_SeConectaYResponde200()
    {
        // Criterio 5: SHAPI_ORIGENES_PERMITIDOS, con entradas host:puerto.
        using var fabrica = Fabrica(modoDemo: true, permitidos: $"origen-envios:8080, localhost:{_origen.Puerto}");
        var host = await SembrarAsync($"http://localhost:{_origen.Puerto}");

        var respuesta = await EnviarAsync(fabrica, host);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadAsStringAsync()).Should().Be("origen real");
    }

    [Theory]
    [InlineData(false, "localhost:{0}")]
    [InlineData(true, "localhost:1")]
    [InlineData(true, "127.0.0.1:{0}")]
    public async Task RNF_10_PermitidoSoloEnModoDemoYConElMismoHostYPuerto(bool modoDemo, string permitido)
    {
        // Criterio 5: la lista solo vale con SHAPI_MODO_DEMO=true, y compara el host y el puerto de url_origen.
        using var fabrica = Fabrica(modoDemo, string.Format(System.Globalization.CultureInfo.InvariantCulture, permitido, _origen.Puerto));
        var host = await SembrarAsync($"http://localhost:{_origen.Puerto}");

        var respuesta = await EnviarAsync(fabrica, host);

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadGateway, "origen_inaccesible");
        _origen.Peticiones.Should().Be(0);
    }

    [Fact]
    public async Task RNF_10_RedireccionDelOrigen_NoSeSigueYSeDevuelveTalCual()
    {
        // Criterio 5 y 10 §4, punto 5
        using var fabrica = Fabrica(modoDemo: true, permitidos: $"localhost:{_origen.Puerto}");
        var host = await SembrarAsync($"http://localhost:{_origen.Puerto}");

        var respuesta = await EnviarAsync(fabrica, host, "/redirige");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Found);
        respuesta.Headers.Location.Should().Be(new Uri("http://169.254.169.254/latest/meta-data"));
    }

    [Fact]
    public async Task RF_31_OrigenPermitidoQueNoAceptaConexiones_Responde502OrigenInaccesible()
    {
        // Criterio 6: no se pudo conectar.
        int puertoCerrado;
        await using (var temporal = await OrigenReal.IniciarAsync(_ => Task.CompletedTask))
        {
            puertoCerrado = temporal.Puerto;
        }

        using var fabrica = Fabrica(modoDemo: true, permitidos: $"localhost:{puertoCerrado}");
        var host = await SembrarAsync($"http://localhost:{puertoCerrado}");

        var respuesta = await EnviarAsync(fabrica, host);

        await VerificarErrorAsync(respuesta, HttpStatusCode.BadGateway, "origen_inaccesible");
    }

    private WebApplicationFactory<Program> Fabrica(bool modoDemo, string? permitidos) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", entorno.CadenaRedis);
            web.UseSetting("SHAPI_MODO_DEMO", modoDemo ? "true" : "false");
            if (permitidos is not null)
            {
                web.UseSetting("SHAPI_ORIGENES_PERMITIDOS", permitidos);
            }
        });

    private async Task<string> SembrarAsync(string urlOrigen)
    {
        var host = $"api{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await entorno.SembrarApiAsync(host, urlOrigen: urlOrigen,
            rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones"), EntornoCompuerta.Ruta("GET", "/redirige")]);
        await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(ClaveDe(host)));
        return host;
    }

    private static async Task<HttpResponseMessage> EnviarAsync(WebApplicationFactory<Program> fabrica, string host,
        string camino = "/cotizaciones")
    {
        using var cliente = EntornoCompuerta.Cliente(fabrica, host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, camino);
        peticion.Headers.Add("X-Api-Key", ClaveDe(host));
        return await cliente.SendAsync(peticion);
    }

    /// <summary>Una clave distinta por API, derivada de su host.</summary>
    private static string ClaveDe(string host) => $"shp_prod_{host[3..29]}";

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado);
        respuesta.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var error = documento.RootElement.GetProperty("error");
        error.GetProperty("codigo").GetString().Should().Be(codigo);
        error.GetProperty("estado").GetInt32().Should().Be((int)estado);
    }
}
