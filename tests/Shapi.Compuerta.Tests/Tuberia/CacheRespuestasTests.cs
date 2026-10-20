using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.Http;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Tuberia;

// RF-13: caché compartida por ruta con Redis real y reenvío YARP.
public sealed class CacheRespuestasTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>, IDisposable
{
    private const string Secreto = "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e";
    private int _peticiones;

    public void Dispose() => entorno.Origen.Olvidar();

    [Fact]
    public async Task RF_13_SegundaPeticion_NoLlegaAlOrigenYDescuentaCuotas()
    {
        var (cliente, api, clave, ruta) = await PrepararAsync();
        using var primera = await cliente.GetAsync("/cotizaciones?q=uno");
        using var segunda = await cliente.GetAsync("/cotizaciones?q=uno");

        primera.Headers.GetValues("X-Shapi-Cache").Should().Equal("MISS");
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal("HIT");
        segunda.StatusCode.Should().Be(HttpStatusCode.OK);
        (await segunda.Content.ReadAsStringAsync()).Should().Be("respuesta-1");
        segunda.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        segunda.Headers.GetValues("X-Origen").Should().Equal("propio");
        segunda.Headers.GetValues("X-Cuota-Restante").Should().Equal("49996");
        _peticiones.Should().Be(1);
        var db = entorno.Redis.GetDatabase();
        ((long)await db.StringGetAsync(LlavesRedis.CuotaSuscripcion(clave.SuscripcionId, 1_790_000_000))).Should().Be(4);
        ((long)await db.StringGetAsync(LlavesRedis.CuotaOrganizacion(api.OrganizacionId, 1_790_000_000))).Should().Be(2);
        var llave = LlavesRedis.Cache(api.ApiId, ruta.RutaId, "GET", "/cotizaciones", "?q=uno");
        (await db.KeyTimeToLiveAsync(llave)).Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(60));
        ((string?)await db.HashGetAsync(llave, "status")).Should().Be("200");
        ((byte[]?)await db.HashGetAsync(llave, "body")).Should().Equal("respuesta-1"u8.ToArray());
    }

    [Fact]
    public async Task RF_13_PublicacionNueva_InvalidaRespuestaAnterior()
    {
        var (cliente, api, _, _) = await PrepararAsync();
        using var primera = await cliente.GetAsync("/cotizaciones");
        await entorno.CambiarVersionAsync(api, 2);
        using var segunda = await cliente.GetAsync("/cotizaciones");
        using var tercera = await cliente.GetAsync("/cotizaciones");
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal("MISS");
        (await segunda.Content.ReadAsStringAsync()).Should().Be("respuesta-2");
        tercera.Headers.GetValues("X-Shapi-Cache").Should().Equal("HIT");
        _peticiones.Should().Be(2);
    }

    [Theory]
    [InlineData("GET", 0)]
    [InlineData("POST", 60)]
    [InlineData("HEAD", 60)]
    public async Task RF_13_MetodoOConfiguracionSinCache_NoGuardaNiAgregaCabecera(string metodo, int segundos)
    {
        var (cliente, api, _, ruta) = await PrepararAsync(metodo, segundos);
        using var primera = await cliente.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), "/cotizaciones"));
        using var segunda = await cliente.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), "/cotizaciones"));
        segunda.Headers.Contains("X-Shapi-Cache").Should().BeFalse();
        _peticiones.Should().Be(2);
        (await entorno.Redis.GetDatabase().KeyExistsAsync(LlavesRedis.Cache(api.ApiId, ruta.RutaId, metodo, "/cotizaciones", ""))).Should().BeFalse();
    }

    [Theory]
    [InlineData(201, 10, false)]
    [InlineData(404, 10, false)]
    [InlineData(500, 10, false)]
    [InlineData(200, 1048576, true)]
    [InlineData(200, 1048577, false)]
    public async Task RF_13_Solo200HastaUnMegabyte_SeGuardaSinAlterarCuerpo(int estado, int bytes, bool guarda)
    {
        var (cliente, _, _, _) = await PrepararAsync();
        var cuerpo = Enumerable.Range(0, bytes).Select(i => (byte)(i % 251)).ToArray();
        entorno.Origen.Responder = async http =>
        {
            _peticiones++;
            http.Response.StatusCode = estado;
            // Sin Content-Length: también limita las respuestas transmitidas por partes.
            await http.Response.Body.WriteAsync(cuerpo);
        };
        using var primera = await cliente.GetAsync("/cotizaciones");
        using var segunda = await cliente.GetAsync("/cotizaciones");
        segunda.StatusCode.Should().Be((HttpStatusCode)estado);
        (await segunda.Content.ReadAsByteArrayAsync()).Should().Equal(cuerpo);
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal(guarda ? "HIT" : "MISS");
        _peticiones.Should().Be(guarda ? 1 : 2);
    }

    [Fact]
    public async Task RF_13_QueryYCaminoDistintos_NoCompartenRespuesta()
    {
        var (cliente, _, _, _) = await PrepararAsync(patron: "/cotizaciones/{id}");
        foreach (var url in new[] { "/cotizaciones/1?a=1", "/cotizaciones/1?a=2", "/cotizaciones/2?a=1" })
        {
            using var respuesta = await cliente.GetAsync(url);
            respuesta.Headers.GetValues("X-Shapi-Cache").Should().Equal("MISS");
        }
        _peticiones.Should().Be(3);
    }

    [Fact]
    public async Task RF_13_CacheCompartida_ConservaCuotaYCorsDeCadaPeticion()
    {
        var (cliente, api, _, _) = await PrepararAsync();
        cliente.DefaultRequestHeaders.Add("Origin", "https://portal.prueba");
        using var primera = await cliente.GetAsync("/cotizaciones");
        var otra = "shp_prod_00000000000000000000000000";
        var clave = await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(otra));
        await entorno.SembrarSuscripcionAsync(clave.SuscripcionId, ContextoSuscripcion.EstadoActiva, cuotaLlamadas: 100, planNombre: "Otro");
        cliente.DefaultRequestHeaders.Remove("X-Api-Key");
        cliente.DefaultRequestHeaders.Add("X-Api-Key", otra);
        cliente.DefaultRequestHeaders.Remove("Origin");
        cliente.DefaultRequestHeaders.Add("Origin", "https://ajeno.prueba");
        using var segunda = await cliente.GetAsync("/cotizaciones");
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal("HIT");
        segunda.Headers.GetValues("X-Shapi-Plan").Should().Equal("Otro");
        segunda.Headers.GetValues("X-Cuota-Restante").Should().Equal("98");
        segunda.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        _peticiones.Should().Be(1);
    }

    [Fact]
    public async Task RF_13_CuotaAgotadaOClaveRevocada_NoEntregaCache()
    {
        var (cliente, _, clave, _) = await PrepararAsync();
        await entorno.SembrarSuscripcionAsync(clave.SuscripcionId, ContextoSuscripcion.EstadoActiva, cuotaLlamadas: 2);
        using var primera = await cliente.GetAsync("/cotizaciones");
        using var segunda = await cliente.GetAsync("/cotizaciones");
        segunda.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        await entorno.Redis.GetDatabase().KeyDeleteAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(Secreto)));
        using var tercera = await cliente.GetAsync("/cotizaciones");
        tercera.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _peticiones.Should().Be(1);
    }

    [Fact]
    public async Task RF_13_VariaCompresion_NoEntregaGzipAClienteSinSoporte()
    {
        var (cliente, _, _, _) = await PrepararAsync();
        entorno.Origen.Responder = async http =>
        {
            _peticiones++;
            http.Response.Headers.Vary = "Accept-Encoding";
            var cuerpo = "respuesta"u8.ToArray();
            if (http.Request.Headers.AcceptEncoding.ToString().Contains("gzip", StringComparison.Ordinal))
            {
                http.Response.Headers.ContentEncoding = "gzip";
                using var comprimido = new MemoryStream();
                await using (var gzip = new GZipStream(comprimido, CompressionMode.Compress, leaveOpen: true))
                {
                    await gzip.WriteAsync(cuerpo);
                }
                cuerpo = comprimido.ToArray();
            }
            await http.Response.Body.WriteAsync(cuerpo);
        };
        cliente.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
        using var primera = await cliente.GetAsync("/cotizaciones");
        primera.Content.Headers.ContentEncoding.Should().Contain("gzip");
        cliente.DefaultRequestHeaders.Remove("Accept-Encoding");
        using var segunda = await cliente.GetAsync("/cotizaciones");
        segunda.Content.Headers.ContentEncoding.Should().BeEmpty();
        (await segunda.Content.ReadAsStringAsync()).Should().Be("respuesta");
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal("MISS");
        _peticiones.Should().Be(2);
    }

    [Theory]
    [InlineData("Vary", "*")]
    [InlineData("Cache-Control", "private")]
    [InlineData("Cache-Control", "no-store")]
    [InlineData("Cache-Control", "no-cache")]
    [InlineData("Cache-Control", "public, private=\"X-Privado\"")]
    public async Task RF_13_RespuestaNoCompartible_NoSeGuarda(string cabecera, string valor)
    {
        var (cliente, _, _, _) = await PrepararAsync();
        entorno.Origen.Responder = async http =>
        {
            _peticiones++;
            http.Response.Headers[cabecera] = valor;
            await http.Response.WriteAsync($"respuesta-{_peticiones}");
        };
        using var primera = await cliente.GetAsync("/cotizaciones");
        using var segunda = await cliente.GetAsync("/cotizaciones");
        (await segunda.Content.ReadAsStringAsync()).Should().Be("respuesta-2");
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal("MISS");
        _peticiones.Should().Be(2);
    }

    [Fact]
    public async Task RF_13_RespuestaConCookie_NoLaComparteEntreConsumidores()
    {
        var (cliente, _, _, _) = await PrepararAsync();
        entorno.Origen.Responder = async http =>
        {
            _peticiones++;
            http.Response.Headers.SetCookie = "sesion=privada; HttpOnly";
            await http.Response.WriteAsync("con-cookie");
        };
        using var primera = await cliente.GetAsync("/cotizaciones");
        using var segunda = await cliente.GetAsync("/cotizaciones");
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal("MISS");
        _peticiones.Should().Be(2);
    }

    [Fact]
    public async Task RF_13_PublicacionDuranteReenvio_RespuestaViejaNoEsAciertoNuevo()
    {
        var (cliente, api, _, _) = await PrepararAsync();
        var recibida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continuar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        entorno.Origen.Responder = async http =>
        {
            var numero = Interlocked.Increment(ref _peticiones);
            if (numero == 1)
            {
                recibida.SetResult();
                await continuar.Task.WaitAsync(TimeSpan.FromSeconds(15));
            }
            await http.Response.WriteAsync($"respuesta-{numero}");
        };
        var primera = cliente.GetAsync("/cotizaciones");
        try
        {
            await recibida.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await entorno.CambiarVersionAsync(api, 2);
            using var nueva = await cliente.GetAsync("/cotizaciones");
            (await nueva.Content.ReadAsStringAsync()).Should().Be("respuesta-2");
        }
        finally
        {
            continuar.TrySetResult();
        }
        using var vieja = await primera;
        using var ultima = await cliente.GetAsync("/cotizaciones");
        (await ultima.Content.ReadAsStringAsync()).Should().NotBe("respuesta-1");
    }

    [Fact]
    public async Task RF_13_EntradaVencida_VuelveAlOrigen()
    {
        var (cliente, api, _, ruta) = await PrepararAsync();
        using var primera = await cliente.GetAsync("/cotizaciones");
        var llave = LlavesRedis.Cache(api.ApiId, ruta.RutaId, "GET", "/cotizaciones", "");
        await entorno.Redis.GetDatabase().KeyExpireAsync(llave, TimeSpan.FromMilliseconds(1));
        await Task.Delay(30);
        using var segunda = await cliente.GetAsync("/cotizaciones");
        segunda.Headers.GetValues("X-Shapi-Cache").Should().Equal("MISS");
        _peticiones.Should().Be(2);
    }

    private async Task<(HttpClient Cliente, ContextoApi Api, ContextoClave Clave, RutaCache Ruta)> PrepararAsync(
        string metodo = "GET", int segundos = 60, string patron = "/cotizaciones")
    {
        var host = $"{Guid.NewGuid():N}.api.prueba";
        var ruta = EntornoCompuerta.Ruta(metodo, patron, peso: 2) with { CacheSegundos = segundos };
        var api = await entorno.SembrarApiAsync(host, portalHost: "portal.prueba", rutas: [ruta]);
        var clave = await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(Secreto));
        entorno.Origen.Responder = async http =>
        {
            _peticiones++;
            http.Response.Headers["X-Origen"] = "propio";
            if (metodo == "GET" && segundos > 0)
            {
                http.Response.Headers["X-Shapi-Cache"] = "inventado";
            }
            http.Response.ContentType = "text/plain";
            await http.Response.WriteAsync($"respuesta-{_peticiones}");
        };
        var cliente = entorno.Cliente(host);
        cliente.DefaultRequestHeaders.Add("X-Api-Key", Secreto);
        return (cliente, api, clave, ruta);
    }
}
