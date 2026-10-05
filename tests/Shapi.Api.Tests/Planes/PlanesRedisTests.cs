using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Identidad;
using Shapi.Api.Planes;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Tests.Planes;

/// <summary>
/// Criterio 2 de EM-07 con Redis real (auditoría del 3 oct, H-61): al editar un plan, la cuota y el límite nuevos quedan
/// en <c>susc:{id}</c> de cada suscripción vigente, que es lo que lee la compuerta.
/// </summary>
[Collection(nameof(RedisCache))]
public sealed class PlanesRedisTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis), IAsyncLifetime
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

    // RF-18
    [Fact]
    public async Task EM07_EditarPlan_PublicaCuotaYLimiteNuevosEnRedis()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApiPublicada(organizacion);
        var plan = await NuevoPlanApiCon(api, "Comercio", 5000, 60);
        var consumidor = await Escalar<Guid>($"""
            INSERT INTO consumidor (id, organizacion_id, nombre, nombre_empresa, correo, hash_contrasena, estado)
            VALUES (gen_random_uuid(), '{organizacion}', 'María', 'Mercadito', 'maria@ejemplo.com', 'hash', 'activo') RETURNING id
            """);
        var vigente = await NuevaSuscripcionApiEn(consumidor, api, plan, "activa", Reloj.Ahora.AddDays(-1), Reloj.Ahora.AddDays(29));
        using var alcance = Fabrica.Services.CreateScope();
        // El filtro global por organización (10 §2) toma la organización de la sesión, como en una petición del panel.
        alcance.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(PoliticasAutorizacion.ClaimOrganizacion, organizacion.ToString()),
                new Claim(PoliticasAutorizacion.ClaimAmbito, nameof(AmbitoSesion.Personal)),
            ], "Prueba")),
        };

        var resultado = await alcance.ServiceProvider.GetRequiredService<EditarPlan>().Ejecutar(
            api,
            plan,
            organizacion,
            new SolicitudPlanApi("Comercio", "Descripción", 450, false, 30, 8000, 90),
            Guid.NewGuid(),
            "Ana Lucía Morales",
            null);

        resultado.EsExito.Should().BeTrue();
        var publicada = ContextoSuscripcion.DesdeCampos(vigente, await Hash(LlavesRedis.Suscripcion(vigente)));
        publicada.Should().NotBeNull();
        publicada!.CuotaLlamadas.Should().Be(8000);
        publicada.LimiteMinuto.Should().Be(90);
    }
}
