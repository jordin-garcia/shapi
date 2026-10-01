extern alias compuerta;

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Tests.Persistencia;
using Shapi.Trabajador.Resincronizacion;
using ProgramaCompuerta = compuerta::Program;

namespace Shapi.Api.Tests.Cache;

// JG-04 criterio 5: con Redis vacío y PostgreSQL con datos, después de resincronizar la compuerta responde igual (RNF-05).
[Collection(nameof(RedisCache))]
public sealed class ResincronizacionConCompuertaTests(PostgresPersistencia postgres, RedisCache redis)
    : BaseCache(postgres, redis)
{
    private const string Clave = "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e";
    private const string Secreto = "shps_secretoDeOrigenDePrueba0001";
    private const string HostApi = "envios.api.shapi.localhost";

    [Fact]
    public async Task RNF_05_Compuerta_RedisVacioYResincronizado_RespondeIgualQueAntes()
    {
        // RNF-05 y RNF-02: la compuerta solo lee Redis; la resincronización lo reconstruye desde PostgreSQL.
        await SembrarAsync();
        var origen = new OrigenEnMemoria();
        await using var compuerta = CrearCompuerta(origen);
        using var cliente = Cliente(compuerta);

        await ResincronizarAsync();
        var antes = await LlamarAsync(cliente);
        await RedisCache.VaciarAsync();
        var conRedisVacio = await LlamarAsync(cliente);
        await ResincronizarAsync();
        var despues = await LlamarAsync(cliente);

        antes.Should().Be((HttpStatusCode.OK, OrigenEnMemoria.Cuerpo));
        conRedisVacio.Codigo.Should().Be(HttpStatusCode.NotFound);
        despues.Should().Be(antes);
        origen.Llamadas.Should().Be(2, "con Redis vacío la petición no llega al origen");
    }

    [Fact]
    public async Task RF_14_Compuerta_ApiDespublicadaYPublicada_Responde404()
    {
        // RF-14: PublicarApi deja estado=despublicada y la compuerta responde 404. Desde JG-05 la compuerta también
        // exige org:{id} y susc:{id} (filtros 3 y 4 de 08 §3).
        var (api, organizacion, suscripcion) = await SembrarAsync();
        await using var compuerta = CrearCompuerta(new OrigenEnMemoria());
        using var cliente = Cliente(compuerta);
        using (var alcance = Servicios.CreateScope())
        {
            await Publicador(alcance).PublicarApi(api);
            await Publicador(alcance).PublicarClave(await Escalar<Guid>("SELECT id FROM clave"));
            await Publicador(alcance).PublicarOrganizacion(organizacion);
            await Publicador(alcance).PublicarSuscripcion(suscripcion);
        }

        (await LlamarAsync(cliente)).Codigo.Should().Be(HttpStatusCode.OK);

        await CambiarEstado("api", api, "despublicada");
        using (var alcance = Servicios.CreateScope())
        {
            await Publicador(alcance).PublicarApi(api);
        }

        (await LlamarAsync(cliente)).Codigo.Should().Be(HttpStatusCode.NotFound);
    }

    private WebApplicationFactory<ProgramaCompuerta> CrearCompuerta(OrigenEnMemoria origen) =>
        new WebApplicationFactory<ProgramaCompuerta>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
            web.ConfigureTestServices(servicios => servicios.AddSingleton(new HttpMessageInvoker(origen)));
        });

    private static HttpClient Cliente(WebApplicationFactory<ProgramaCompuerta> compuerta) =>
        compuerta.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{HostApi}") });

    private static async Task<(HttpStatusCode Codigo, string Cuerpo)> LlamarAsync(HttpClient cliente)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/guias/GT123");
        peticion.Headers.Add("X-Api-Key", Clave);
        using var respuesta = await cliente.SendAsync(peticion);
        return (respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync());
    }

    private async Task ResincronizarAsync()
    {
        await using var servicios = CrearServicios(ajustar: s => s.AgregarResincronizacion());
        using var alcance = servicios.CreateScope();
        await alcance.ServiceProvider.GetRequiredService<ResincronizarCache>().EjecutarAsync(CancellationToken.None);
    }

    private async Task<(Guid Api, Guid Organizacion, Guid Suscripcion)> SembrarAsync()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApiPublicada(organizacion, secreto: Secreto, urlOrigen: "http://origen.prueba");
        await NuevaRutaCompleta(api, "GET", "/guias/{numero}", expuesta: true);
        var plan = await NuevoPlanApiCon(api, "Comercio", 5000, 60);
        var suscripcion = await NuevaSuscripcionApiEn(await NuevoConsumidor(organizacion), api, plan, "activa",
            Reloj.Ahora.AddDays(-1), Reloj.Ahora.AddDays(29));
        await NuevaClaveCon(suscripcion, Clave);
        return (api, organizacion, suscripcion);
    }

    /// <summary>El origen del proveedor: responde 200 y cuenta las peticiones que le llegan.</summary>
    private sealed class OrigenEnMemoria : HttpMessageHandler
    {
        public const string Cuerpo = "{\"guia\":\"GT123\",\"estado\":\"en_ruta\"}";

        private int _llamadas;

        public int Llamadas => _llamadas;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _llamadas);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Cuerpo) });
        }
    }
}
