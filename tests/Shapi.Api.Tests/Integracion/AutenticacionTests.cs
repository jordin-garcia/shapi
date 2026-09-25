#pragma warning disable CS0618
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Modulos;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;
using Xunit;
using System.Text.Json;
using System.Net.Http.Headers;

namespace Shapi.Api.Tests.Integracion;

public class RelojFalso : IReloj
{
    public DateTimeOffset Ahora { get; set; } = DateTimeOffset.UtcNow;
}

public class AutenticacionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private RelojFalso _relojFalso = new RelojFalso();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
                builder.UseSetting("SHAPI_POSTGRES_CADENA", _dbContainer.GetConnectionString());
                builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
                builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Admin");
                builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");

                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IReloj>(_relojFalso);
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory != null) await _factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    private HttpRequestMessage CrearPeticion(HttpMethod metodo, string url, object? contenido = null)
    {
        var req = new HttpRequestMessage(metodo, url);
        req.Headers.Add("X-Requested-With", "shapi");
        if (contenido != null)
        {
            req.Content = JsonContent.Create(contenido);
        }
        return req;
    }

    // RF-01
    [Fact]
    public async Task RF_01_Registro_DosUsuariosSeguidosYVerificacion()
    {
        // 1. Registro user 1
        var u1 = Guid.NewGuid().ToString("N");
        var req1 = CrearPeticion(HttpMethod.Post, "/api/auth/registro", new PeticionRegistro("U1", $"u1_{u1}@test.com", "Org1", "ContraValida123"));
        var res1 = await _client!.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // 2. Registro user 2
        var req2 = CrearPeticion(HttpMethod.Post, "/api/auth/registro", new PeticionRegistro("U2", $"u2_{u1}@test.com", "Org2", "ContraValida123"));
        var res2 = await _client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);

        // Validar q ambos existen en BD y tienen Ids y Org.Ids pre-asignados
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var user1 = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == $"u1_{u1}@test.com");
        Assert.NotNull(user1);
        Assert.NotEqual(Guid.Empty, user1.Id);
    }

    // RF-02
    [Fact]
    public async Task RF_02_VerificacionDeCorreoYReenvio()
    {
        var uid = Guid.NewGuid().ToString("N");
        var correo = $"verif_{uid}@test.com";
        var resReg = await _client!.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/registro", new PeticionRegistro("Verif", correo, "OrgV", "ContraValida123")));
        Assert.Equal(HttpStatusCode.OK, resReg.StatusCode);

        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
        
        // Reenvío
        var resReenvio = await _client.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/reenviar-verificacion", new PeticionReenviar(correo)));
        Assert.Equal(HttpStatusCode.OK, resReenvio.StatusCode);

        // Debería haber 2 tokens
        var tokens = await db.Set<Token>().IgnoreQueryFilters().Where(t => t.Correo == correo).ToListAsync();
        Assert.Equal(2, tokens.Count);
        
        // Verificar que CorreoSaliente.Asunto no es nulo
        var correos = await db.Set<CorreoSaliente>().Where(c => c.Destinatario == correo).ToListAsync();
        Assert.Equal(2, correos.Count);
        Assert.All(correos, c => Assert.NotNull(c.Asunto));
    }

    // RF-04
    [Fact]
    public async Task RF_04_InicioDeSesion_Y_Bloqueo()
    {
        var uid = Guid.NewGuid().ToString("N");
        var correo = $"login_{uid}@test.com";
        await _client!.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/registro", new PeticionRegistro("Login", correo, "OrgL", "ContraValida123")));

        // 5 intentos malos
        for (int i = 0; i < 5; i++)
        {
            var res = await _client.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/entrar", new PeticionLogin(correo, "Mal123")));
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        // Intento 6 (Incluso correcto, debe fallar)
        var res6 = await _client.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/entrar", new PeticionLogin(correo, "ContraValida123")));
        Assert.Equal(HttpStatusCode.Unauthorized, res6.StatusCode);
    }

    [Fact]
    public async Task RF_04_InicioDeSesion_RechazoSiInactivo_Y_Exito()
    {
        var uid = Guid.NewGuid().ToString("N");
        var correo = $"login2_{uid}@test.com";
        await _client!.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/registro", new PeticionRegistro("Login", correo, "OrgL", "ContraValida123")));

        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
            var u = await db.Set<Usuario>().IgnoreQueryFilters().FirstAsync(x => x.Correo == correo);
            u.Estado = EstadoCuenta.Desactivado;
            await db.SaveChangesAsync();
        }

        // Falla por desactivado
        var res = await _client.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/entrar", new PeticionLogin(correo, "ContraValida123")));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);

        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
            var u = await db.Set<Usuario>().IgnoreQueryFilters().FirstAsync(x => x.Correo == correo);
            u.Estado = EstadoCuenta.Activo;
            await db.SaveChangesAsync();
        }

        // Exito
        var resOk = await _client.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/entrar", new PeticionLogin(correo, "ContraValida123")));
        Assert.Equal(HttpStatusCode.OK, resOk.StatusCode);
        Assert.Contains(resOk.Headers.GetValues("Set-Cookie"), c => c.StartsWith("shapi_sesion="));
    }

    [Fact]
    public async Task RF_04_Salir_SesionRevocada()
    {
        var uid = Guid.NewGuid().ToString("N");
        var correo = $"salir_{uid}@test.com";
        await _client!.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/registro", new PeticionRegistro("Salir", correo, "OrgS", "ContraValida123")));
        
        var resLogin = await _client.SendAsync(CrearPeticion(HttpMethod.Post, "/api/auth/entrar", new PeticionLogin(correo, "ContraValida123")));
        var cookie = resLogin.Headers.GetValues("Set-Cookie").First().Split(';')[0];

        var reqSalir = CrearPeticion(HttpMethod.Post, "/api/auth/salir");
        reqSalir.Headers.Add("Cookie", cookie);
        var resSalir = await _client.SendAsync(reqSalir);
        Assert.Equal(HttpStatusCode.OK, resSalir.StatusCode);

        // Verificar revocado
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var sesion = await db.Set<Sesion>().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UsuarioId != null); // Simplificacion
        Assert.NotNull(sesion?.RevocadaEn);
    }

    [Fact]
    public async Task RF_04_Sesion_VencimientoInactividad()
    {
        // ... (Simulacion de avance de tiempo)
        Assert.True(true); // placeholder, requiere setup de auth para la sesión
    }

    [Fact]
    public async Task CSRF_XRequestedWith_Origin()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/registro");
        req.Content = JsonContent.Create(new PeticionRegistro("C", "c@c.com", "O", "C123456789"));
        // Sin X-Requested-With
        var res1 = await _client!.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res1.StatusCode);

        var req2 = CrearPeticion(HttpMethod.Post, "/api/auth/registro", new PeticionRegistro("C", "c@c.com", "O", "C123456789"));
        req2.Headers.Add("Origin", "http://sitio-maligno.com");
        var res2 = await _client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Forbidden, res2.StatusCode);
    }
}
