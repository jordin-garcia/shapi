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
using Shapi.Infraestructura.Comun;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Suscripciones;

public sealed class RelojDemoApiTests
{
    // RF-44: validar antes de modificar el reloj compartido, sin desbordar la fecha.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(int.MaxValue)]
    public async Task RF_44_Adelantar_DiasInvalidos_NoModificaRedis(int dias)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["SHAPI_MODO_DEMO"] = "true" }).Build();
        var redis = Substitute.For<IConnectionMultiplexer>();
        using var reloj = new RelojSistema(TimeProvider.System, configuracion, redis);
        var api = new RelojDemoApi(configuracion, redis, reloj);
        var respuesta = await api.Adelantar(new PeticionRelojDemo(dias), CancellationToken.None);
        respuesta.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>()
            .Which.StatusCode.Should().Be(400);
        redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
    }

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
        database.StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, 30L * 24 * 60 * 60, CommandFlags.None)
            .Returns(60L * 24 * 60 * 60);
        using var reloj = new RelojSistema(TimeProvider.System, configuracion, redis);
        var api = new RelojDemoApi(configuracion, redis, reloj);

        var respuesta = await api.Adelantar(new PeticionRelojDemo(30), CancellationToken.None);

        respuesta.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        await database.Received(1).StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, 30L * 24 * 60 * 60, CommandFlags.None);
        await subscriber.Received(1).PublishAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), 60L * 24 * 60 * 60, CommandFlags.None);
        reloj.Ahora.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(60), TimeSpan.FromSeconds(1));
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
        using var reloj = new RelojSistema(TimeProvider.System, configuracion, redis);
        var api = new RelojDemoApi(configuracion, redis, reloj);

        var respuesta = await api.Adelantar(new PeticionRelojDemo(30), CancellationToken.None);

        respuesta.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
        redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
    }
}

[Collection(nameof(PostgresPersistencia))]
public sealed class RelojDemoEndpointTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    // RF-44: solo el administrador puede consultar el control de demostración.
    [Theory]
    [InlineData("true", "Administrador", HttpStatusCode.OK)]
    [InlineData("true", "Soporte", HttpStatusCode.Forbidden)]
    [InlineData("true", "Propietario", HttpStatusCode.Forbidden)]
    [InlineData("false", "Administrador", HttpStatusCode.NotFound)]
    public async Task RF_44_ConsultarReloj_RespetaModoDemoYPermiso(string modo, string rol, HttpStatusCode esperado)
    {
        using var fabrica = CrearFabrica(modo, CrearRedisFalso());
        using var cliente = fabrica.CreateClient();
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/admin/demo/reloj");
        peticion.Headers.Add("X-Prueba-Organizacion", Guid.NewGuid().ToString());
        peticion.Headers.Add("X-Prueba-Rol", rol);
        using var respuesta = await cliente.SendAsync(peticion);
        respuesta.StatusCode.Should().Be(esperado);
        if (esperado == HttpStatusCode.OK)
        {
            var datos = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            datos.GetProperty("ahora").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    // RNF-06: este endpoint lee IReloj antes de consultar las credenciales; Redis no produce un 500.
    [Fact]
    public async Task Entrar_TimeoutRedisAlSincronizarReloj_ConservaRespuestaHttp()
    {
        var redis = CrearRedisFalso();
        var database = redis.GetDatabase();
        var fallo = new RedisTimeoutException(CommandFlags.None, "Timeout simulado", CommandStatus.Sent);
        database.StringGet(Arg.Any<RedisKey>()).ReturnsForAnyArgs(_ => throw fallo);
        database.StringGetAsync(Arg.Any<RedisKey>()).ReturnsForAnyArgs(Task.FromException<RedisValue>(fallo));
        using var fabrica = CrearFabrica("true", redis);
        using var cliente = fabrica.CreateClient();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/auth/entrar")
        {
            Content = JsonContent.Create(new { correo = "inexistente@ejemplo.com", contrasena = "Invalida123!" }),
        };
        peticion.Headers.Add("X-Requested-With", "shapi");

        using var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        database.DidNotReceive().StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

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
        await redis.GetSubscriber().Received(1).PublishAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), 86400L, CommandFlags.None);
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
        redis.GetDatabase().StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, 86400L, CommandFlags.None).Returns(86400L);
        return redis;
    }
}
