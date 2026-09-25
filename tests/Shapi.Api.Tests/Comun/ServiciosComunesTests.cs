using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Comun;

namespace Shapi.Api.Tests.Comun;

// Criterio 7 de JG-01: implementaciones nulas por defecto, reemplazables por el módulo dueño.
public class ServiciosComunesTests(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void ServiciosComunes_SinImplementacionDelModuloDueno_ResuelvenLasNulas()
    {
        var fabricaConfigurada = fabrica.WithWebHostBuilder(builder => 
            builder.UseSetting("SHAPI_POSTGRES_CADENA", "Host=localhost;Database=dummy"));
        using var alcance = fabricaConfigurada.Services.CreateScope();
        var servicios = alcance.ServiceProvider;

        servicios.GetRequiredService<IReloj>().Should().BeOfType<RelojSistema>();
        servicios.GetRequiredService<IColaCorreo>().Should().BeOfType<ColaCorreoNula>();
        servicios.GetRequiredService<IBitacora>().Should().BeOfType<BitacoraNula>();
        servicios.GetRequiredService<IPublicadorCache>().Should().BeOfType<PublicadorCacheNulo>();
    }

    [Fact]
    public async Task ServiciosNulos_AlUsarlos_TerminanSinError()
    {
        var fabricaConfigurada = fabrica.WithWebHostBuilder(builder => 
            builder.UseSetting("SHAPI_POSTGRES_CADENA", "Host=localhost;Database=dummy"));
        using var alcance = fabricaConfigurada.Services.CreateScope();
        var servicios = alcance.ServiceProvider;
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
        using var fabricaConModulos = fabrica.WithWebHostBuilder(constructor => 
        {
            constructor.UseSetting("SHAPI_POSTGRES_CADENA", "Host=localhost;Database=dummy");
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
