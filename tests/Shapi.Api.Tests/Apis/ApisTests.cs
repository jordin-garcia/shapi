using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Red;
using Shapi.Dominio.Apis;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;
using ApiDominio = Shapi.Dominio.Apis.Api;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Apis;

public sealed class ContenedorPostgresApis : IAsyncLifetime
{
    public PostgreSqlContainer Contenedor { get; } = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => Contenedor.StartAsync();

    public Task DisposeAsync() => Contenedor.DisposeAsync().AsTask();
}

public sealed class ProbadorOrigenFalso : IProbadorOrigen
{
    public Task<ResultadoPruebaOrigen> Probar(DireccionOrigenValidada origen, CancellationToken cancelacion = default)
    {
        if (origen.Direccion.AbsoluteUri.Contains("caido", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ResultadoPruebaOrigen.Fallo("El servidor no aceptó la conexión."));
        }

        return Task.FromResult(ResultadoPruebaOrigen.Exito(142));
    }
}

public sealed class AutenticacionApisPrueba(
    IOptionsMonitor<AuthenticationSchemeOptions> opciones,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, encoder)
{
    public const string Esquema = "ApisPrueba";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Prueba-Organizacion", out var organizacion))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var rol = Request.Headers["X-Prueba-Rol"].FirstOrDefault() ?? "Propietario";
        var usuario = Request.Headers["X-Prueba-Usuario"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, usuario),
            new(ClaimTypes.Name, "Ana Lucía Morales"),
            new(ClaimTypes.Email, "ana@envios.test"),
            new(ClaimTypes.Role, rol),
            new("OrganizacionId", organizacion.ToString()),
            new("Ambito", "Personal"),
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Esquema)));
    }
}

public class ApisTests(ContenedorPostgresApis postgres) : IClassFixture<ContenedorPostgresApis>, IAsyncLifetime
{
    private string _cadena = null!;
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private Guid _organizacionId;

    public async Task InitializeAsync()
    {
        _cadena = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
        {
            Database = $"prueba_{Guid.NewGuid():N}",
        }.ConnectionString;

        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _cadena);
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Admin");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");
            builder.UseSetting("SHAPI_MODO_DEMO", "false");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(opciones =>
                    {
                        opciones.DefaultAuthenticateScheme = AutenticacionApisPrueba.Esquema;
                        opciones.DefaultChallengeScheme = AutenticacionApisPrueba.Esquema;
                        opciones.DefaultForbidScheme = AutenticacionApisPrueba.Esquema;
                    })
                    .AddScheme<AuthenticationSchemeOptions, AutenticacionApisPrueba>(AutenticacionApisPrueba.Esquema, _ => { });
                services.AddSingleton<IProbadorOrigen, ProbadorOrigenFalso>();
            });
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        await _cliente.GetAsync("/salud");
        _organizacionId = await CrearOrganizacion("Envíos Xelajú, S.A.");
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task RF_08_Registrar_DatosValidos_GuardaBorradorSecretoCifradoYBitacora()
    {
        using var respuesta = await Registrar("envios", "https://8.8.8.8/v1");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var apiId = json.GetProperty("id").GetGuid();
        var secreto = json.GetProperty("secretoOrigen").GetString()!;
        Assert.Matches("^shps_[A-Za-z0-9]{32}$", secreto);
        Assert.Equal(142, json.GetProperty("conexionMilisegundos").GetInt64());
        Assert.Equal("borrador", json.GetProperty("estado").GetString());

        await using var db = Db(out var alcance);
        using var _ = alcance;
        var api = await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Id == apiId);
        Assert.Equal(EstadoApi.Borrador, api.Estado);
        Assert.NotEqual(secreto, api.SecretoOrigenCifrado);
        var protector = alcance.ServiceProvider.GetRequiredService<IProtectorSecretoOrigen>();
        Assert.Equal(secreto, protector.Descifrar(api.SecretoOrigenCifrado));
        var entrada = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().SingleAsync(e => e.ObjetivoId == apiId);
        Assert.Equal("api.registrada", entrada.Accion);
    }

    [Fact]
    public async Task RF_08_Registrar_SubdominioReservado_Responde400SinGuardar()
    {
        using var respuesta = await Registrar("admin", "https://8.8.8.8");

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("subdominio", out _));
        Assert.Equal(0, await ContarApis());
    }

    [Fact]
    public async Task RF_08_Registrar_SubdominioOcupado_Responde409()
    {
        Assert.Equal(HttpStatusCode.Created, (await Registrar("envios", "https://8.8.8.8")).StatusCode);

        using var respuesta = await Registrar("envios", "https://1.1.1.1");

        await AfirmarProblema(respuesta, HttpStatusCode.Conflict, "subdominio_ocupado");
        Assert.Equal(1, await ContarApis());
    }

    [Fact]
    public async Task RNF_10_Registrar_OrigenPrivado_Responde422SinGuardar()
    {
        using var respuesta = await Registrar("envios", "http://127.0.0.1:5101");

        await AfirmarProblema(respuesta, (HttpStatusCode)422, "origen_no_permitido");
        Assert.Equal(0, await ContarApis());
    }

    [Fact]
    public async Task RF_08_Registrar_OrigenCaido_Responde422SinGuardar()
    {
        using var respuesta = await Registrar("envios", "https://8.8.8.8/caido");

        var problema = await AfirmarProblema(respuesta, (HttpStatusCode)422, "origen_inaccesible");
        Assert.Equal("El servidor no aceptó la conexión.", problema.GetProperty("detalle").GetProperty("motivo").GetString());
        Assert.Equal(0, await ContarApis());
    }

    [Theory]
    [InlineData("Propietario", HttpStatusCode.Created)]
    [InlineData("Editor", HttpStatusCode.Created)]
    [InlineData("Lector", HttpStatusCode.Forbidden)]
    public async Task RF_08_Registrar_RespetaPermisoConfigurarApis(string rol, HttpStatusCode esperado)
    {
        using var respuesta = await Registrar($"api-{rol.ToLowerInvariant()}", "https://8.8.8.8", rol);

        Assert.Equal(esperado, respuesta.StatusCode);
    }

    [Fact]
    public async Task RF_08_Registrar_SinSesion_Responde401()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/apis")
        {
            Content = JsonContent.Create(new
            {
                nombre = "API de Envíos",
                urlOrigen = "https://8.8.8.8",
                subdominio = "envios",
            }),
        };
        peticion.Headers.Add("X-Requested-With", "shapi");

        using var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task RF_14_Listar_DevuelveApisDeLaOrganizacionYDatosDelPlan()
    {
        Assert.Equal(HttpStatusCode.Created, (await Registrar("envios", "https://8.8.8.8")).StatusCode);
        var otra = await CrearOrganizacion("Otra organización");
        await InsertarApi(otra, "ajena");

        using var respuesta = await EnviarAutenticado(HttpMethod.Get, "/api/apis", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("total").GetInt32());
        Assert.Equal("Prueba", json.GetProperty("planNombre").GetString());
        Assert.Equal(1, json.GetProperty("maxApis").GetInt32());
        var elemento = Assert.Single(json.GetProperty("elementos").EnumerateArray());
        Assert.Equal("envios", elemento.GetProperty("subdominio").GetString());
        Assert.False(json.ToString().Contains("shps_", StringComparison.Ordinal));
        Assert.False(json.ToString().Contains("ajena", StringComparison.Ordinal));
    }

    private async Task<HttpResponseMessage> Registrar(string subdominio, string urlOrigen, string rol = "Propietario") =>
        await EnviarAutenticado(HttpMethod.Post, "/api/apis", new
        {
            nombre = "API de Cotización de Envíos",
            urlOrigen,
            subdominio,
        }, rol);

    private async Task<HttpResponseMessage> EnviarAutenticado(HttpMethod metodo, string ruta, object? cuerpo, string rol = "Propietario")
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Add("X-Prueba-Organizacion", _organizacionId.ToString());
        peticion.Headers.Add("X-Prueba-Rol", rol);
        peticion.Headers.Add("X-Prueba-Usuario", Guid.NewGuid().ToString());
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }
        return await _cliente.SendAsync(peticion);
    }

    private async Task<Guid> CrearOrganizacion(string nombre)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            WITH organizacion_nueva AS (
                INSERT INTO organizacion (id, nombre, tipo, estado_admin)
                VALUES (gen_random_uuid(), @nombre, 'proveedor', 'activa')
                RETURNING id
            )
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin)
            SELECT gen_random_uuid(), o.id, p.id, 'activa', now(), now() + interval '30 days'
            FROM organizacion_nueva o
            CROSS JOIN plan_plataforma p
            WHERE p.es_prueba
            RETURNING organizacion_id
            """;
        comando.Parameters.AddWithValue("nombre", nombre);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task InsertarApi(Guid organizacionId, string subdominio)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, secreto_origen_cifrado)
            VALUES (gen_random_uuid(), @organizacion, 'API ajena', @subdominio, 'https://8.8.8.8', 'borrador', 'cifrado')
            """;
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        comando.Parameters.AddWithValue("subdominio", subdominio);
        await comando.ExecuteNonQueryAsync();
    }

    private async Task<int> ContarApis()
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT count(*) FROM api";
        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    private ShapiDbContext Db(out IServiceScope alcance)
    {
        alcance = _fabrica.Services.CreateScope();
        return alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
    }

    private static async Task<JsonElement> AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(codigo, problema.GetProperty("codigo").GetString());
        return problema;
    }
}
