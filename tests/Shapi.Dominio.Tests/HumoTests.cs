using System.Reflection;

namespace Shapi.Dominio.Tests;

// RNF-15: humo para que el proyecto corra en la CI.
public class HumoTests
{
    [Fact]
    public void Ensamblado_ProyectoDominio_SeCarga()
    {
        var ensamblado = Assembly.Load("Shapi.Dominio");

        ensamblado.GetName().Name.Should().Be("Shapi.Dominio");
    }
}
