using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Medicion;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Tests.Medicion;

public sealed class MedicionTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>, IDisposable
{
    private const string Clave = "shp_prod_AAAAAAAAAAAAAAAAAAAAAAAAAA";

    public void Dispose() => entorno.Origen.Olvidar();

    [Fact]
    public async Task RF_33_PeticionesConcurrentes_ContadoresBytesYLlamadasCoinciden()
    {
        var ruta = EntornoCompuerta.Ruta("POST", "/cotizaciones", peso: 3);
        var host = $"{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await entorno.SembrarApiAsync(host, rutas: [ruta]);
        var clave = await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(Clave));
        using var cliente = entorno.Cliente(host);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, "/cotizaciones");
            peticion.Headers.Add("X-Api-Key", Clave);
            peticion.Content = new StringContent("ñ", Encoding.UTF8);
            using var respuesta = await cliente.SendAsync(peticion);
            respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
            await respuesta.Content.ReadAsByteArrayAsync();
        }));
        var llave = LlavesRedis.Metricas(FiltroLimitesYCuotas.DiaGuatemala(TimeProvider.System.GetUtcNow()),
            api.ApiId, ruta.RutaId, clave.SuscripcionId, "produccion");
        var campos = await Esperar(llave, 20);
        campos["llamadas"].Should().Be(60);
        campos["bytes_entrada"].Should().Be(40);
        campos["bytes_salida"].Should().Be(20 * Encoding.UTF8.GetByteCount(OrigenFalso.CuerpoRespuesta));
        campos["o2xx"].Should().Be(20);
        campos.Where(x => x.Key.StartsWith("h_t_", StringComparison.Ordinal)).Sum(x => x.Value).Should().Be(20);
        campos.Where(x => x.Key.StartsWith("h_c_", StringComparison.Ordinal)).Sum(x => x.Value).Should().Be(20);
        (await entorno.Redis.GetDatabase().SetContainsAsync(LlavesRedis.MetricasPendientes, llave)).Should().BeTrue();
    }

    [Fact]
    public async Task RF_33_ClaveDeOtraApi_ElRechazoNoSeAtribuyeASuSuscripcion()
    {
        var host = $"{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await entorno.SembrarApiAsync(host);
        var otra = await entorno.SembrarApiAsync($"{Guid.NewGuid():N}.api.shapi.localhost");
        await entorno.SembrarClaveAsync(otra, ContextoClave.CalcularHash(Clave));
        using var cliente = entorno.Cliente(host);
        cliente.DefaultRequestHeaders.Add("X-Api-Key", Clave);
        using var respuesta = await cliente.GetAsync("/cotizaciones");
        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var campos = await Esperar(LlavesRedis.Metricas(FiltroLimitesYCuotas.DiaGuatemala(TimeProvider.System.GetUtcNow()),
            api.ApiId, null, null, "produccion"), 1);
        campos["r401"].Should().Be(1);
        campos.GetValueOrDefault("llamadas").Should().Be(0);
    }

    [Fact]
    public async Task RF_33_ClaveDePruebas_NoSumaLlamadasDeProduccion()
    {
        var host = $"{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await entorno.SembrarApiAsync(host);
        var clave = await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(Clave), "pruebas");
        using var cliente = entorno.Cliente(host);
        cliente.DefaultRequestHeaders.Add("X-Api-Key", Clave);
        using var respuesta = await cliente.GetAsync("/cotizaciones");
        var ruta = EntornoCompuerta.RutasPorDefecto.Single(x => x.Metodo == "GET" && x.Patron == "/cotizaciones");
        var campos = await Esperar(LlavesRedis.Metricas(FiltroLimitesYCuotas.DiaGuatemala(TimeProvider.System.GetUtcNow()),
            api.ApiId, ruta.RutaId, clave.SuscripcionId, "pruebas"), 1);
        campos.GetValueOrDefault("llamadas").Should().Be(0);
    }

    [Fact]
    public async Task RF_34_Reenvio_MideLaEsperaHastaLaRespuestaDelOrigen()
    {
        var host = $"{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await entorno.SembrarApiAsync(host);
        await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(Clave));
        var captura = new MedicionCapturada(entorno.Redis);
        using var fabrica = entorno.Fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(s =>
            s.AddSingleton<IMedicionPeticion>(captura)));
        entorno.Origen.Responder = async http =>
        {
            await Task.Delay(150);
            await http.Response.WriteAsync("respuesta");
        };
        using var cliente = EntornoCompuerta.Cliente(fabrica, host);
        cliente.DefaultRequestHeaders.Add("X-Api-Key", Clave);
        using var respuesta = await cliente.GetAsync("/cotizaciones");
        captura.Contexto.Should().NotBeNull();
        captura.Contexto!.RespondioOrigen.Should().BeTrue();
        captura.Contexto.TiempoEsperaOrigen.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
    }

    private sealed class MedicionCapturada(IConnectionMultiplexer redis) : IMedicionPeticion
    {
        public ContextoPeticion? Contexto { get; private set; }
        public Task MedirAsync(ContextoPeticion contexto, Func<Task> siguiente)
        {
            Contexto = contexto;
            return new MedicionMiddleware(redis, TimeProvider.System, NullLogger<MedicionMiddleware>.Instance)
                .MedirAsync(contexto, siguiente);
        }
    }

    [Fact]
    public async Task RNF_05_LatidoCompuerta_TieneMarcaYVenceEnTreintaSegundos()
    {
        using var cliente = entorno.Cliente("localhost");
        using var respuesta = await cliente.GetAsync("/salud");
        var latido = entorno.Fabrica.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<LatidoCompuerta>().Single();
        await latido.EscribirAsync(CancellationToken.None);
        var llave = LlavesRedis.SaludCompuerta(latido.Instancia);
        DateTimeOffset.TryParse((await entorno.Redis.GetDatabase().StringGetAsync(llave)).ToString(), out _).Should().BeTrue();
        (await entorno.Redis.GetDatabase().KeyTimeToLiveAsync(llave))!.Value.Should()
            .BeGreaterThan(TimeSpan.FromSeconds(25)).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
        var primera = await entorno.Redis.GetDatabase().StringGetAsync(llave);
        var inicio = TimeProvider.System.GetTimestamp();
        while (TimeProvider.System.GetElapsedTime(inicio) < TimeSpan.FromSeconds(15))
        {
            await Task.Delay(100);
            if (await entorno.Redis.GetDatabase().StringGetAsync(llave) != primera)
            {
                return; // El BackgroundService renueva el latido por sí mismo al pasar su intervalo.
            }
        }
        throw new Xunit.Sdk.XunitException("La compuerta no renovó su latido en el intervalo de diez segundos.");
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(5.01, 1)]
    [InlineData(10, 1)]
    [InlineData(25, 2)]
    [InlineData(50, 3)]
    [InlineData(100, 4)]
    [InlineData(250, 5)]
    [InlineData(500, 6)]
    [InlineData(1000, 7)]
    [InlineData(2500, 8)]
    [InlineData(2500.01, 9)]
    public void RF_34_Histograma_RespetaLosLimitesInclusivos(double milisegundos, int esperado) =>
        HistogramaMetricas.Rango(TimeSpan.FromMilliseconds(milisegundos)).Should().Be(esperado);

    private async Task<Dictionary<string, long>> Esperar(string llave, long peticiones)
    {
        for (var intento = 0; intento < 100; intento++)
        {
            var campos = (await entorno.Redis.GetDatabase().HashGetAllAsync(llave))
                .ToDictionary(x => x.Name.ToString(), x => (long)x.Value);
            if (campos.GetValueOrDefault("peticiones") == peticiones)
            {
                return campos;
            }
            await Task.Delay(20);
        }
        throw new Xunit.Sdk.XunitException($"No llegaron {peticiones} peticiones a {llave}.");
    }
}
