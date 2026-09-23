using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Shapi.Api.Tests;

public class SaludTests(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Salud_ApiEnEjecucion_Responde200()
    {
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
