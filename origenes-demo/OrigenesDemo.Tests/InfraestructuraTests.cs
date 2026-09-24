namespace OrigenesDemo.Tests;

public sealed class InfraestructuraTests
{
    [Fact]
    public async Task Compose_OrigenesDeDemostracion_ExponePuertosAsignados()
    {
        // JZ-02 CA5
        var ruta = Path.Combine(RaizRepositorio.Ruta, "infra", "compose.yml");
        var compose = await File.ReadAllTextAsync(ruta);

        compose.Should().Contain("origen-envios:");
        compose.Should().Contain("\"5101:8080\"");
        compose.Should().Contain("origen-agro:");
        compose.Should().Contain("\"5102:8080\"");
    }
}
