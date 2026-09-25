#pragma warning disable CS0618
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.PostgreSql;

namespace Shapi.Api.Tests;

// RNF-15: humo para que el proyecto corra en la CI.
public class SaludTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _fabrica;
    private readonly PostgreSqlContainer _dbContainer;

    public SaludTests(WebApplicationFactory<Program> fabrica)
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

    [Fact]
    public async Task Salud_ApiEnEjecucion_Responde200()
    {
        var fabricaConfigurada = _fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _dbContainer.GetConnectionString());
        });
        using var cliente = fabricaConfigurada.CreateClient();
        var respuesta = await cliente.GetAsync("/salud");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
