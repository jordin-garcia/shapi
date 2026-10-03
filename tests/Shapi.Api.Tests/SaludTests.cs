using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Shapi.Api.Tests.Persistencia;

namespace Shapi.Api.Tests;

// RNF-15: humo para que el proyecto corra en la CI.
public class SaludTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _fabrica;
    private string _cadena = null!;

    public SaludTests(WebApplicationFactory<Program> fabrica)
    {
        _fabrica = fabrica;
    }

    // Usa su propia base en el PostgreSQL compartido (JG-18).
    public async Task InitializeAsync()
    {
        await PostgresCompartido.IniciarAsync();
        _cadena = PostgresCompartido.NuevaCadena();
    }

    public Task DisposeAsync() => PostgresCompartido.EliminarBaseAsync(_cadena);

    [Fact]
    public async Task Salud_ApiEnEjecucion_Responde200()
    {
        var fabricaConfigurada = _fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _cadena);
        });
        using var cliente = fabricaConfigurada.CreateClient();
        var respuesta = await cliente.GetAsync("/salud");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
