#pragma warning disable CS0618
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;

namespace Shapi.Api.Tests.Comun;

// Criterio 7 de JG-01: implementaciones nulas por defecto, reemplazables por el módulo dueño.
// (Modificado en EM-01 para usar BD directamente)
public class ServiciosComunesTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _fabrica;
    private readonly PostgreSqlContainer _dbContainer;

    public ServiciosComunesTests(WebApplicationFactory<Program> fabrica)
    {
        _dbContainer = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        _fabrica = fabrica;
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    private WebApplicationFactory<Program> CrearFabrica()
    {
        return _fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _dbContainer.GetConnectionString());
        });
    }

    [Fact]
    public void ServiciosComunes_SinImplementacionDelModuloDueno_ResuelvenLasNulas()
    {
        var fabricaConfigurada = CrearFabrica();
        using var alcance = fabricaConfigurada.Services.CreateScope();
        var servicios = alcance.ServiceProvider;

        servicios.GetRequiredService<IReloj>().Should().BeOfType<RelojSistema>();
        servicios.GetRequiredService<IColaCorreo>().Should().BeOfType<Shapi.Infraestructura.Correo.ColaCorreoBaseDatos>();
        servicios.GetRequiredService<IBitacora>().Should().BeOfType<Shapi.Infraestructura.Bitacora.BitacoraBaseDatos>();
        servicios.GetRequiredService<IPublicadorCache>().Should().BeOfType<PublicadorCacheNulo>();
    }

    [Fact]
    public async Task ServiciosNulos_AlUsarlos_TerminanSinError()
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
