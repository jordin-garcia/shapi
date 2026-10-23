using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Persistencia;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Tests.Identidad;

[Collection(nameof(RedisCache))]
public sealed class RegistroCacheTests(PostgresPersistencia postgres, RedisCache redis) : BaseDePrueba(postgres)
{
    // RF-01, RF-11 y RNF-04: un proveedor en Prueba no espera la resincronización para recibir tráfico.
    [Fact]
    public async Task RF_01_RegistrarProveedor_PublicaEnRedisLaOrganizacionActivaConLaCuotaYElCicloConfirmados()
    {
        await using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
            builder.UseSetting("SHAPI_REDIS", redis.Cadena);
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@registro.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Administrador");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "Administrador2026!");
        });
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Requested-With", "shapi");

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/registro", new
        {
            nombre = "Proveedor nuevo",
            correo = "proveedor@registro.test",
            organizacion = "Organización recién registrada",
            contrasena = "Contrasena2026!"
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        using var alcance = fabrica.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var organizacion = await db.Set<Organizacion>().SingleAsync(o => o.Nombre == "Organización recién registrada");
        var suscripcion = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters()
            .SingleAsync(s => s.OrganizacionId == organizacion.Id);
        var plan = await db.Set<PlanPlataforma>().SingleAsync(p => p.Id == suscripcion.PlanId);
        plan.EsPrueba.Should().BeTrue();
        var campos = (await redis.Conexion.GetDatabase().HashGetAllAsync(LlavesRedis.Organizacion(organizacion.Id)))
            .ToDictionary(c => c.Name.ToString(), c => c.Value.ToString());
        var publicada = ContextoOrganizacion.DesdeCampos(organizacion.Id, campos);
        publicada.Should().Be(new ContextoOrganizacion(organizacion.Id, ContextoOrganizacion.EstadoActiva,
            plan.CuotaPeticiones, suscripcion.Inicio.ToUnixTimeSeconds(), suscripcion.Fin.ToUnixTimeSeconds()));
    }
}
