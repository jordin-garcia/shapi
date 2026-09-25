using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Shapi.Api.Tests;

// RNF-15: humo para que el proyecto corra en la CI.
public class SaludTests(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Salud_ApiEnEjecucion_Responde200()
    {
        var fabricaConfigurada = fabrica.WithWebHostBuilder(builder =>
            builder.UseSetting("SHAPI_POSTGRES_CADENA", "Host=localhost;Database=dummy"));
        using var cliente = fabricaConfigurada.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
