#pragma warning disable CS0618
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Modulos;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;
using Xunit;

namespace Shapi.Api.Tests.Integracion;

public class AutenticacionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Environment.SetEnvironmentVariable("SHAPI_POSTGRES_CADENA", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("SHAPI_APLICAR_MIGRACIONES", "true");
        Environment.SetEnvironmentVariable("SHAPI_ADMIN_CORREO", "admin@shapi.test");
        Environment.SetEnvironmentVariable("SHAPI_ADMIN_NOMBRE", "Admin");
        Environment.SetEnvironmentVariable("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");

        _factory = new WebApplicationFactory<Program>();

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_factory != null) await _factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task RF_01_RegistroProveedor_CreaUsuarioYOrganizacion()
    {
        var uid = Guid.NewGuid().ToString("N");
        var peticion = new PeticionRegistro("Juan", $"juan_{uid}@shapi.test", $"Org_{uid}", "ContraLarga123");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/registro")
        {
            Content = JsonContent.Create(peticion)
        };
        request.Headers.Add("X-Requested-With", "shapi");

        var response = await _client!.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}. Body: {content}");

        // Verificar DB
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == $"juan_{uid}@shapi.test");
        Assert.NotNull(usuario);
        Assert.NotEqual(Guid.Empty, usuario.Id);
    }

    [Fact]
    public async Task RF_04_IniciarSesion_BloqueaTras5Intentos()
    {
        var uid = Guid.NewGuid().ToString("N");
        var correo = $"maria_{uid}@shapi.test";

        // Registrar
        var peticionReg = new PeticionRegistro("Maria", correo, $"Org_{uid}", "ContraLarga123");
        var reqReg = new HttpRequestMessage(HttpMethod.Post, "/api/auth/registro") { Content = JsonContent.Create(peticionReg) };
        reqReg.Headers.Add("X-Requested-With", "shapi");
        var resReg = await _client!.SendAsync(reqReg);
        var contentReg = await resReg.Content.ReadAsStringAsync();
        Assert.True(resReg.StatusCode == HttpStatusCode.OK, $"Registro falló: {resReg.StatusCode}. Body: {contentReg}");

        var peticionLoginErronea = new PeticionLogin(correo, "Mal12345678");

        // 5 intentos malos
        for (int i = 0; i < 5; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/entrar") { Content = JsonContent.Create(peticionLoginErronea) };
            req.Headers.Add("X-Requested-With", "shapi");
            var res = await _client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        // Intento 6 (Incluso con contraseña correcta, debe fallar)
        var peticionLoginCorrecta = new PeticionLogin(correo, "ContraLarga123");
        var req6 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/entrar") { Content = JsonContent.Create(peticionLoginCorrecta) };
        req6.Headers.Add("X-Requested-With", "shapi");
        var res6 = await _client.SendAsync(req6);
        Assert.Equal(HttpStatusCode.Unauthorized, res6.StatusCode);

        // Verificar BloqueadoHasta
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correo);
        Assert.NotNull(usuario);
        Assert.NotNull(usuario.BloqueadoHasta);
    }

    [Fact]
    public async Task RNF_08_Csrf_RechazaPeticionSinCabecera()
    {
        var peticion = new PeticionRegistro("Pedro", "pedro@shapi.test", "Org", "ContraLarga123");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/registro")
        {
            Content = JsonContent.Create(peticion)
        };
        // Omitimos la cabecera X-Requested-With intencionalmente

        var response = await _client!.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
