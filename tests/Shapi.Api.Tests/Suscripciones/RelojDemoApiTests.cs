using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shapi.Api.Suscripciones;
using Shapi.Api.Tests.Persistencia;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Cache;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Suscripciones;

public sealed class RelojDemoApiTests
{
    // RF-44: el administrador adelanta el reloj compartido y solicita de inmediato el cierre del ciclo.
    [Fact]
    public async Task Adelantar_ModoDemo_ActualizaDesplazamientoYPublicaSolicitud()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "true",
        }).Build();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        var subscriber = Substitute.For<ISubscriber>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        redis.GetSubscriber(Arg.Any<object?>()).Returns(subscriber);
        var api = new RelojDemoApi(configuracion, redis);

        var respuesta = await api.Adelantar(new PeticionRelojDemo(30), CancellationToken.None);

        respuesta.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        await database.Received(1).StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, 30L * 24 * 60 * 60, CommandFlags.None);
        await subscriber.Received(1).PublishAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), "30", CommandFlags.None);
    }

    // RF-44: el endpoint no existe cuando se desactiva el modo demo.
    [Fact]
    public async Task Adelantar_ModoDemoDesactivado_Devuelve404SinCambiarRedis()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_MODO_DEMO"] = "false",
        }).Build();
        var redis = Substitute.For<IConnectionMultiplexer>();
        var api = new RelojDemoApi(configuracion, redis);

        var respuesta = await api.Adelantar(new PeticionRelojDemo(30), CancellationToken.None);

        respuesta.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
        redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
    }
}

[Collection(nameof(PostgresPersistencia))]
public sealed class RelojDemoEndpointTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    // RF-44: la ruta permite al administrador solo en modo demo, devuelve 404 al desactivarlo y rechaza otros roles.
    [Fact]
    public async Task EndpointAdelantar_AutorizaSoloAdministradorEnModoDemo()
    {
        var redis = CrearRedisFalso();
        using (var fabrica = CrearFabrica("true", redis))
        using (var cliente = fabrica.CreateClient())
        {
            using var noAdministrador = await Enviar(cliente, "Propietario");
            noAdministrador.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        using (var fabrica = CrearFabrica("false", redis))
        using (var cliente = fabrica.CreateClient())
        {
            using var modoDesactivado = await Enviar(cliente, Rol.Administrador.ToString());
            modoDesactivado.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        using (var fabrica = CrearFabrica("true", redis))
        using (var cliente = fabrica.CreateClient())
        {
            using var administrador = await Enviar(cliente, Rol.Administrador.ToString());
            administrador.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        await redis.GetDatabase().Received(1).StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, 86400L, CommandFlags.None);
        await redis.GetSubscriber().Received(1).PublishAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), "1", CommandFlags.None);
    }

    private WebApplicationFactory<Program> CrearFabrica(string modoDemo, IConnectionMultiplexer redis) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
            web.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            web.UseSetting("SHAPI_MODO_DEMO", modoDemo);
            web.ConfigureTestServices(servicios =>
            {
                servicios.AddAuthentication(opciones =>
                {
                    opciones.DefaultAuthenticateScheme = AuthPlataformaPrueba.Esquema;
                    opciones.DefaultChallengeScheme = AuthPlataformaPrueba.Esquema;
                    opciones.DefaultForbidScheme = AuthPlataformaPrueba.Esquema;
                }).AddScheme<AuthenticationSchemeOptions, AuthPlataformaPrueba>(AuthPlataformaPrueba.Esquema, _ => { });
                servicios.RemoveAll<IConnectionMultiplexer>();
                servicios.AddSingleton(redis);
            });
        });

    private static async Task<HttpResponseMessage> Enviar(HttpClient cliente, string rol)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/admin/demo/reloj")
        {
            Content = JsonContent.Create(new { adelantarDias = 1 }),
        };
        peticion.Headers.Add("X-Requested-With", "shapi");
        peticion.Headers.Add("X-Prueba-Organizacion", Guid.NewGuid().ToString());
        peticion.Headers.Add("X-Prueba-Rol", rol);
        return await cliente.SendAsync(peticion);
    }

    private static IConnectionMultiplexer CrearRedisFalso()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(Substitute.For<IDatabase>());
        redis.GetSubscriber(Arg.Any<object?>()).Returns(Substitute.For<ISubscriber>());
        return redis;
    }
}
