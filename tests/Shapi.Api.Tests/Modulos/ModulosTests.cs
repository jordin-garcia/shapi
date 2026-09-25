using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Shapi.Api.Tests.Modulos;

// docs/plan/convenciones.md §1 y §3: un Modulos/<Modulo>Modulo.cs por módulo, llamado desde Program.cs.
public class ModulosTests
{
    private static readonly string[] NombresDeModulos =
    [
        "Identidad", "Organizaciones", "Planes", "Suscripciones", "Pagos",
        "Apis", "Portal", "Claves", "Consumo", "Cache",
        "Administracion", "Soporte", "Bitacora", "Correo", "Estado",
    ];

    public static TheoryData<string> Modulos => new(NombresDeModulos);

    private static readonly Type[] ClasesDeModulo = typeof(Program).Assembly.GetTypes()
        .Where(tipo => tipo.Namespace == "Shapi.Api.Modulos" && tipo.IsAbstract && tipo.IsSealed && !tipo.Name.Contains('<'))
        .ToArray();

    [Fact]
    public void Modulos_EnsambladoDeLaApi_TieneUnaClasePorCadaUnoDeLos15Modulos()
    {
        ClasesDeModulo.Select(tipo => tipo.Name)
            .Should().BeEquivalentTo(NombresDeModulos.Select(modulo => $"{modulo}Modulo"));
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Modulo_ClaseDelModulo_TieneAgregarYMapear(string modulo)
    {
        var clase = ClasesDeModulo.Single(tipo => tipo.Name == $"{modulo}Modulo");

        var agregar = clase.GetMethod($"AgregarModulo{modulo}", BindingFlags.Public | BindingFlags.Static,
            [typeof(IServiceCollection)]);
        var mapear = clase.GetMethod($"MapearModulo{modulo}", BindingFlags.Public | BindingFlags.Static,
            [typeof(WebApplication)]);

        agregar.Should().NotBeNull();
        agregar!.ReturnType.Should().Be<IServiceCollection>();
        mapear.Should().NotBeNull();
        mapear!.ReturnType.Should().Be<WebApplication>();
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Program_CadaModulo_SeAgregaYSeMapea(string modulo)
    {
        var program = File.ReadAllText(RaizRepositorio.Ruta("src", "Shapi.Api", "Program.cs"));

        File.Exists(RaizRepositorio.Ruta("src", "Shapi.Api", "Modulos", $"{modulo}Modulo.cs")).Should().BeTrue();
        program.Should().Contain($".AgregarModulo{modulo}()");
        program.Should().Contain($".MapearModulo{modulo}()");
    }
}
