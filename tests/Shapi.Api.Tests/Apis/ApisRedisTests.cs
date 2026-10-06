using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Api.Identidad;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Tests.Apis;

[Collection(nameof(RedisCache))]
public sealed class ApisRedisTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis), IAsyncLifetime
{
    private WebApplicationFactory<Program>? _fabrica;

    private WebApplicationFactory<Program> Fabrica => _fabrica ??= new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
    {
        web.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        web.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
        web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
        web.UseSetting("SHAPI_DOMINIO_BASE", DominioBase);
        web.ConfigureTestServices(servicios => servicios.AddSingleton<IReloj>(Reloj));
    });

    public new async Task DisposeAsync()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    // RF-14: el endpoint publica exactamente las llaves que consume la compuerta.
    [Fact]
    public async Task RF_14_PublicarApi_EscribeEstadoHostYRutasEnRedis()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApiPublicada(organizacion, estado: "borrador");
        var secretoCifrado = Fabrica.Services.GetRequiredService<IProtectorSecretoOrigen>()
            .Cifrar("shps_secretoDeOrigenDePrueba0001");
        await using (var conexion = new NpgsqlConnection(Cadena))
        {
            await conexion.OpenAsync();
            await using var comando = conexion.CreateCommand();
            comando.CommandText = "UPDATE api SET secreto_origen_cifrado = @secreto WHERE id = @id";
            comando.Parameters.AddWithValue("secreto", secretoCifrado);
            comando.Parameters.AddWithValue("id", api);
            await comando.ExecuteNonQueryAsync();
        }
        var ruta = await NuevaRutaCompleta(api, "GET", "/rastreo", true, 120, 30, 2);
        await NuevoPlanApiCon(api, "Básico", 5000, 60);
        var usuario = await Escalar<Guid>($"""
            WITH usuario_nuevo AS (
                INSERT INTO usuario (id, nombre, correo, hash_contrasena, correo_verificado_en, estado)
                VALUES (gen_random_uuid(), 'Ana', 'ana-{Guid.NewGuid():N}@ejemplo.com', 'hash', now(), 'activo')
                RETURNING id
            )
            INSERT INTO membresia (id, usuario_id, organizacion_id, rol)
            SELECT gen_random_uuid(), id, '{organizacion}', 'propietario' FROM usuario_nuevo RETURNING usuario_id
            """);

        using var alcance = Fabrica.Services.CreateScope();
        alcance.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuario.ToString()),
                new Claim(PoliticasAutorizacion.ClaimOrganizacion, organizacion.ToString()),
                new Claim(PoliticasAutorizacion.ClaimAmbito, nameof(AmbitoSesion.Personal)),
            ], "Prueba")),
        };

        var resultado = await alcance.ServiceProvider.GetRequiredService<CambiarPublicacionApi>().Publicar(
            api,
            organizacion,
            new ActorRegistroApi(usuario, "Ana", null));

        resultado.EsExito.Should().BeTrue();
        var contexto = ContextoApi.DesdeCampos(api, await Hash(LlavesRedis.Api(api)));
        contexto.Should().NotBeNull();
        contexto!.Estado.Should().Be(ContextoApi.EstadoPublicada);
        (await Redis.StringGetAsync(LlavesRedis.ApiPorHost($"envios.api.{DominioBase}"))).ToString().Should().Be(api.ToString());
        var rutas = RutaCache.Deserializar((await Redis.StringGetAsync(LlavesRedis.RutasApi(api))).ToString());
        rutas.Should().ContainSingle().Which.Should().Be(new RutaCache(ruta, "GET", "/rastreo", true, 120, 30, 2));
    }
}
