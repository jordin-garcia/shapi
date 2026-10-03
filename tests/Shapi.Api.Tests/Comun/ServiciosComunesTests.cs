using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Cache;
using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Tests.Comun;

// Criterio 7 de JG-01: implementaciones por defecto, reemplazables por el módulo dueño.
// EM-01 reemplazó la cola de correo y la bitácora nulas por las que escriben en la base de datos, y JG-04 el publicador
// nulo por el que escribe en Redis.
public class ServiciosComunesTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _fabrica;
    private string _cadena = null!;

    public ServiciosComunesTests(WebApplicationFactory<Program> fabrica)
    {
        _fabrica = fabrica;
    }

    // Cada prueba usa su propia base en el PostgreSQL compartido (JG-18).
    public async Task InitializeAsync()
    {
        await PostgresCompartido.IniciarAsync();
        _cadena = PostgresCompartido.NuevaCadena();
    }

    public Task DisposeAsync() => PostgresCompartido.EliminarBaseAsync(_cadena);

    private WebApplicationFactory<Program> CrearFabrica()
    {
        return _fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _cadena);

            // Sin Redis: el publicador no debe lanzar aunque Redis no responda (criterio 3 de JG-04).
            builder.UseSetting("SHAPI_REDIS", "127.0.0.1:1,connectTimeout=100");
        });
    }

    [Fact]
    public void ServiciosComunes_SinReemplazoDelModulo_ResuelvenLasImplementacionesPorDefecto()
    {
        var fabricaConfigurada = CrearFabrica();
        using var alcance = fabricaConfigurada.Services.CreateScope();
        var servicios = alcance.ServiceProvider;

        servicios.GetRequiredService<IReloj>().Should().BeOfType<RelojSistema>();
        servicios.GetRequiredService<IColaCorreo>().Should().BeOfType<Shapi.Infraestructura.Correo.ColaCorreoBaseDatos>();
        servicios.GetRequiredService<IBitacora>().Should().BeOfType<Shapi.Infraestructura.Bitacora.BitacoraBaseDatos>();
        servicios.GetRequiredService<IPublicadorCache>().Should().BeOfType<PublicadorCacheRedis>();
    }

    [Fact]
    public async Task ServiciosComunes_AlUsarlos_TerminanSinError()
    {
        var fabricaConfigurada = CrearFabrica();
        using var alcance = fabricaConfigurada.Services.CreateScope();
        var servicios = alcance.ServiceProvider;
        var db = servicios.GetRequiredService<ShapiDbContext>();
        await db.Database.MigrateAsync();

        var entrada = new EntradaBitacora(TipoActor.Sistema, null, "Sistema", null,
            AccionesBitacora.SuscripcionSuspendida, "Suspendió por falta de pago la suscripción");

        await servicios.GetRequiredService<IColaCorreo>().Encolar("verificacion_correo", "ana@ejemplo.com", new { enlace = "x" });
        await servicios.GetRequiredService<IBitacora>().Registrar(entrada);
        var publicador = servicios.GetRequiredService<IPublicadorCache>();
        await publicador.PublicarApi(Guid.NewGuid());
        await publicador.PublicarClave(Guid.NewGuid());
        await publicador.ExpirarClave(new string('a', 64), DateTimeOffset.UnixEpoch);
        await publicador.EliminarClave(new string('a', 64));
        await publicador.PublicarSuscripcion(Guid.NewGuid());
        await publicador.PublicarOrganizacion(Guid.NewGuid());
    }

    [Fact]
    public void ServiciosComunes_ModuloRegistraSuImplementacionDespues_SeUsaLaDelModulo()
    {
        var bitacora = Substitute.For<IBitacora>();
        var colaCorreo = Substitute.For<IColaCorreo>();
        var publicador = Substitute.For<IPublicadorCache>();

        using var fabricaConModulos = CrearFabrica().WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureTestServices(servicios =>
            {
                servicios.AddScoped(_ => bitacora);
                servicios.AddScoped(_ => colaCorreo);
                servicios.AddSingleton(publicador);
            });
        });
        using var alcance = fabricaConModulos.Services.CreateScope();

        alcance.ServiceProvider.GetRequiredService<IBitacora>().Should().BeSameAs(bitacora);
        alcance.ServiceProvider.GetRequiredService<IColaCorreo>().Should().BeSameAs(colaCorreo);
        alcance.ServiceProvider.GetRequiredService<IPublicadorCache>().Should().BeSameAs(publicador);
    }
}
