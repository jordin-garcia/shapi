using System.Reflection;

namespace Shapi.Dominio.Tests;

public class HumoTests
{
    [Fact]
    public void Ensamblado_ProyectoDominio_SeCarga()
    {
        var ensamblado = Assembly.Load("Shapi.Dominio");

        ensamblado.GetName().Name.Should().Be("Shapi.Dominio");
    }
}
