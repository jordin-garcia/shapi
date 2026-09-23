using System.Net;
using Microsoft.Extensions.Configuration;
using Shapi.Compuerta.Demo;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Demo;

// JG-02 criterio 7: comando temporal sembrar-demo (lo elimina JG-04).
public class SembradoDemoTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>
{
    private const string HostEnvios = "envios.api.shapi.localhost";
    private const string ClaveDemo = "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e";

    [Fact]
    public async Task SembrarDemo_SinModoDemo_NoEscribeNadaYTerminaConError()
    {
        await entorno.Redis.GetDatabase().KeyDeleteAsync(LlavesRedis.ApiPorHost(HostEnvios));
        using var salida = new StringWriter();

        var codigo = await SembradoDemo.EjecutarAsync(Configuracion(modoDemo: null), salida);

        codigo.Should().NotBe(0);
        salida.ToString().Should().Contain("SHAPI_MODO_DEMO");
        (await entorno.Redis.GetDatabase().KeyExistsAsync(LlavesRedis.ApiPorHost(HostEnvios))).Should().BeFalse();
    }

    [Fact]
    public async Task SembrarDemo_ConModoDemo_LaCompuertaReenviaLaApiEnviosConLaClaveDeDemostracion()
    {
        using var salida = new StringWriter();

        var codigo = await SembradoDemo.EjecutarAsync(Configuracion(modoDemo: "true"), salida);

        codigo.Should().Be(0);
        var db = entorno.Redis.GetDatabase();
        var apiId = Guid.Parse((await db.StringGetAsync(LlavesRedis.ApiPorHost(HostEnvios))).ToString());
        (await db.HashGetAsync(LlavesRedis.Api(apiId), "url_origen")).ToString().Should().Be("http://localhost:5101");
        (await db.HashGetAsync(LlavesRedis.Api(apiId), "estado")).ToString().Should().Be("publicada");

        using var cliente = entorno.Cliente(HostEnvios);
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/cotizaciones")
        {
            Content = new StringContent("{\"origen\":\"0901\",\"destino\":\"0301\",\"peso_kg\":2.5}"),
        };
        peticion.Headers.Add("X-Api-Key", ClaveDemo);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        entorno.Origen.Ultima!.Cabeceras["X-Shapi-Entorno"].Should().Be("produccion");
    }

    private IConfiguration Configuracion(string? modoDemo) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SHAPI_REDIS"] = entorno.CadenaRedis,
                ["SHAPI_MODO_DEMO"] = modoDemo,
            })
            .Build();
}
