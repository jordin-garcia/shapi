using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Comun;

namespace Shapi.Api.Tests.Comun;

// Criterio 5 de JG-17: en desarrollo, la API y el trabajador usan las mismas llaves de Data Protection (10 §3).
// Una ruta relativa en SHAPI_DPKEYS_DIR se resuelve desde la raíz del repositorio (la carpeta de Shapi.slnx), no desde
// la carpeta de cada proyecto.
public sealed class DirectorioLlavesTests : IDisposable
{
    private readonly string _repositorio = Path.Combine(Path.GetTempPath(), "shapi-dpkeys-" + Guid.NewGuid().ToString("N"));

    public DirectorioLlavesTests()
    {
        Directory.CreateDirectory(Path.Combine(_repositorio, "src", "Shapi.Api", "bin"));
        Directory.CreateDirectory(Path.Combine(_repositorio, "src", "Shapi.Trabajador"));
        File.WriteAllText(Path.Combine(_repositorio, "Shapi.slnx"), "<Solution />");
    }

    public void Dispose()
    {
        if (Directory.Exists(_repositorio))
        {
            Directory.Delete(_repositorio, recursive: true);
        }
    }

    private string Ruta(params string[] partes) => Path.Combine([_repositorio, .. partes]);

    [Fact]
    public void JG17_RutaRelativa_DesdeLaApiYElTrabajador_SeResuelveDesdeLaRaizDelRepositorio()
    {
        var desdeApi = DirectorioLlaves.Resolver(".shapi/dpkeys", Ruta("src", "Shapi.Api"));
        var desdeTrabajador = DirectorioLlaves.Resolver(".shapi/dpkeys", Ruta("src", "Shapi.Trabajador"));
        var desdeBin = DirectorioLlaves.Resolver(".shapi/dpkeys", Ruta("src", "Shapi.Api", "bin"));

        var esperado = Path.GetFullPath(Ruta(".shapi", "dpkeys"));
        desdeApi.Should().Be(esperado);
        desdeTrabajador.Should().Be(esperado);
        desdeBin.Should().Be(esperado);
    }

    [Fact]
    public void JG17_RutaAbsoluta_SeConservaSinCambios()
    {
        var absoluta = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "llaves-absolutas"));

        DirectorioLlaves.Resolver(absoluta, Ruta("src", "Shapi.Api")).Should().Be(absoluta);
    }

    [Fact]
    public void JG17_RutaRelativa_FueraDelRepositorio_SeResuelveDesdeLaCarpetaDeInicio()
    {
        // En un contenedor no hay Shapi.slnx; el ambiente productivo usa de todos modos una ruta absoluta.
        var fuera = Path.Combine(Path.GetTempPath(), "shapi-sin-repo-" + Guid.NewGuid().ToString("N"));

        DirectorioLlaves.Resolver("llaves", fuera).Should().Be(Path.GetFullPath(Path.Combine(fuera, "llaves")));
    }

    [Fact]
    public void JG17_SecretoCifradoPorLaApi_ElTrabajadorLoDescifra_ConLaMismaRutaRelativa()
    {
        // Antes de JG-17, cada proyecto creaba su propio anillo en src/<proyecto>/.shapi/dpkeys y el trabajador no
        // podía descifrar el secreto de origen que cifró la API (hallazgo de JZ-15).
        using var api = CrearServicios(Ruta("src", "Shapi.Api"));
        var cifrado = api.GetRequiredService<IProtectorSecretoOrigen>().Cifrar("shps_secretoDeOrigenDePrueba0001");

        using var trabajador = CrearServicios(Ruta("src", "Shapi.Trabajador"));
        trabajador.GetRequiredService<IProtectorSecretoOrigen>().Descifrar(cifrado)
            .Should().Be("shps_secretoDeOrigenDePrueba0001");

        Directory.EnumerateFiles(Ruta(".shapi", "dpkeys"), "key-*.xml").Should().ContainSingle();
        Directory.Exists(Ruta("src", "Shapi.Api", ".shapi")).Should().BeFalse();
        Directory.Exists(Ruta("src", "Shapi.Trabajador", ".shapi")).Should().BeFalse();
    }

    // La API y el trabajador registran Data Protection con AgregarServiciosComunes; la raíz de contenido de cada uno
    // es la carpeta de su proyecto, como con dotnet run.
    private static ServiceProvider CrearServicios(string raizContenido)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [HostDefaults.ContentRootKey] = raizContenido,
            [ServiciosComunes.VariableDirectorioLlaves] = ".shapi/dpkeys",
        }).Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AgregarServiciosComunes(configuracion);
        return servicios.BuildServiceProvider();
    }
}
