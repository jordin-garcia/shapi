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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Planes;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;
using ApiDominio = Shapi.Dominio.Apis.Api;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Planes;

public sealed class ContenedorPostgresPlanes : IAsyncLifetime
{
    public PostgreSqlContainer Contenedor { get; } = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => Contenedor.StartAsync();

    public Task DisposeAsync() => Contenedor.DisposeAsync().AsTask();
}

public sealed class AutenticacionPlanesPrueba(
    IOptionsMonitor<AuthenticationSchemeOptions> opciones,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, encoder)
{
    public const string Esquema = "PlanesPrueba";

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

public sealed class PublicadorCachePlanesFalso : IPublicadorCache
{
    public List<Guid> SuscripcionesPublicadas { get; } = [];
    public Task PublicarApi(Guid apiId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarClave(Guid claveId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task EliminarClave(string hashClave, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default) { SuscripcionesPublicadas.Add(suscripcionId); return Task.CompletedTask; }
    public Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default) => Task.CompletedTask;
}

public class PlanesTests(ContenedorPostgresPlanes postgres) : IClassFixture<ContenedorPostgresPlanes>, IAsyncLifetime
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
            builder.UseSetting("SHAPI_MODO_DEMO", "false");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(opciones =>
                    {
                        opciones.DefaultAuthenticateScheme = AutenticacionPlanesPrueba.Esquema;
                        opciones.DefaultChallengeScheme = AutenticacionPlanesPrueba.Esquema;
                        opciones.DefaultForbidScheme = AutenticacionPlanesPrueba.Esquema;
                    })
                    .AddScheme<AuthenticationSchemeOptions, AutenticacionPlanesPrueba>(AutenticacionPlanesPrueba.Esquema, _ => { });

                services.RemoveAll<IPublicadorCache>();
                services.AddSingleton<PublicadorCachePlanesFalso>();
                services.AddSingleton<IPublicadorCache>(sp => sp.GetRequiredService<PublicadorCachePlanesFalso>());
            });
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        _organizacionId = await CrearOrganizacion("Envíos Xelajú, S.A.");
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task EM07_CrearPlan_DatosValidos_Retorna200YGuardaEnBD()
    {
        var apiId = await InsertarApi(_organizacionId);

        var peticion = new
        {
            nombre = "Básico",
            descripcion = "Plan básico",
            precio = 100.0m,
            esGratuito = false,
            vigenciaDias = 30,
            cuotaLlamadas = 1000,
            limiteMinuto = 60
        };

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var planId = json.GetProperty("id").GetGuid();
        Assert.Equal("Básico", json.GetProperty("nombre").GetString());

        await using var db = Db(out var alcance);
        using var _ = alcance;
        var plan = await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(p => p.Id == planId);
        Assert.Equal("Básico", plan.Nombre);
        Assert.Equal(100.0m, plan.Precio);

        var entrada = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().SingleAsync(e => e.ObjetivoId == planId);
        Assert.Equal("plan_api.creado", entrada.Accion);
    }

    [Fact]
    public async Task EM07_EditarPlan_DatosValidos_ActualizaPlanYPublicaSuscripciones()
    {
        var apiId = await InsertarApi(_organizacionId);
        var planId = await InsertarPlan(apiId, "Original");
        var suscripcionId = await InsertarSuscripcion(apiId, planId);

        var peticion = new
        {
            nombre = "Editado",
            descripcion = "Plan editado",
            precio = 0.0m,
            esGratuito = true,
            vigenciaDias = 15,
            cuotaLlamadas = 500,
            limiteMinuto = 30
        };

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/planes/{planId}", peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        await using var db = Db(out var alcance);
        using var _ = alcance;
        var plan = await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(p => p.Id == planId);
        Assert.Equal("Editado", plan.Nombre);
        Assert.True(plan.EsGratuito);
        Assert.Equal(0.0m, plan.Precio); // Validado por el backend

        var entradas = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().Where(e => e.ObjetivoId == planId).ToListAsync();
        Assert.Contains(entradas, e => e.Accion == "plan_api.editado");

        var publicador = _fabrica.Services.GetRequiredService<PublicadorCachePlanesFalso>();
        Assert.Contains(suscripcionId, publicador.SuscripcionesPublicadas);
    }

    [Fact]
    public async Task EM07_DesactivarPlan_DesactivaPlanYRegistraEnBitacora()
    {
        var apiId = await InsertarApi(_organizacionId);
        var planId = await InsertarPlan(apiId, "A Desactivar");

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes/{planId}/desactivar", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        await using var db = Db(out var alcance);
        using var _ = alcance;
        var plan = await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(p => p.Id == planId);
        Assert.False(plan.Activo);

        var entradas = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().Where(e => e.ObjetivoId == planId).ToListAsync();
        Assert.Contains(entradas, e => e.Accion == "plan_api.desactivado");
    }

    [Fact]
    public async Task EM07_ListarPlanes_RetornaSoloPlanesDeLaApi()
    {
        var api1 = await InsertarApi(_organizacionId);
        var api2 = await InsertarApi(_organizacionId);
        await InsertarPlan(api1, "Plan API 1");
        await InsertarPlan(api2, "Plan API 2");

        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{api1}/planes", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var elementos = json.EnumerateArray().ToArray();
        Assert.Single(elementos);
        Assert.Equal("Plan API 1", elementos[0].GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task EM07_PlanesPortal_RetornaPlanesActivosResolviendoPortalPorHost()
    {
        var apiId = await InsertarApi(_organizacionId, "portal-test");
        await InsertarPlan(apiId, "Plan Activo", activo: true);
        await InsertarPlan(apiId, "Plan Inactivo", activo: false);

        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/portal/planes");
        peticion.Headers.Host = "portal-test.shapi.localhost";

        using var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var elementos = json.EnumerateArray().ToArray();
        Assert.Single(elementos);
        Assert.Equal("Plan Activo", elementos[0].GetProperty("nombre").GetString());
    }



    [Theory]
    [InlineData("Lector")]
    public async Task EM07_Planes_RolLector_Recibe403(string rol)
    {
        // RF-18
        var apiId = await InsertarApi(_organizacionId);
        var peticion = new { nombre = "Test", descripcion = "Desc", precio = 0.0m, esGratuito = true, vigenciaDias = 30, cuotaLlamadas = 1000, limiteMinuto = 60 };
        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", peticion, rol);
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task EM07_CrearPlan_DatosInvalidos_Retorna400()
    {
        // RF-18
        var apiId = await InsertarApi(_organizacionId);

        var peticion = new
        {
            nombre = "",
            descripcion = "Plan básico",
            precio = 100.0m,
            esGratuito = true,
            vigenciaDias = 0,
            cuotaLlamadas = 0,
            limiteMinuto = 0
        };

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", peticion);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task EM07_CrearPlan_NombreRepetido_Retorna409()
    {
        // RF-18
        var apiId = await InsertarApi(_organizacionId);
        await InsertarPlan(apiId, "Básico");

        var peticion = new
        {
            nombre = "Básico",
            descripcion = "Otro",
            precio = 0.0m,
            esGratuito = true,
            vigenciaDias = 30,
            cuotaLlamadas = 1000,
            limiteMinuto = 60
        };

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", peticion);
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task EM07_Planes_OtraOrganizacion_Recibe404()
    {
        // RNF-08
        var apiId = await InsertarApi(_organizacionId);
        var otraOrgId = await CrearOrganizacion("Otra Org");

        var peticion = new HttpRequestMessage(HttpMethod.Get, $"/api/apis/{apiId}/planes");
        peticion.Headers.Add("X-Prueba-Organizacion", otraOrgId.ToString());
        peticion.Headers.Add("X-Prueba-Rol", "Propietario");
        peticion.Headers.Add("X-Prueba-Usuario", Guid.NewGuid().ToString());
        peticion.Headers.Add("X-Requested-With", "shapi");

        using var respuesta = await _cliente.SendAsync(peticion);
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
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

    private async Task<Guid> InsertarApi(Guid organizacionId, string? subdominio = null)
    {
        subdominio ??= "prueba" + Guid.NewGuid().ToString("N").Substring(0, 8);
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, secreto_origen_cifrado)
            VALUES (gen_random_uuid(), @organizacion, 'API', @subdominio, 'https://8.8.8.8', 'publicada', 'cifrado')
            RETURNING id
            """;
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        comando.Parameters.AddWithValue("subdominio", subdominio);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task<Guid> InsertarPlan(Guid apiId, string nombre, bool activo = true)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO plan_api (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias, cuota_llamadas, limite_minuto, activo)
            VALUES (gen_random_uuid(), @apiId, @nombre, 'Desc', 10.0, false, 30, 1000, 60, @activo)
            RETURNING id
            """;
        comando.Parameters.AddWithValue("apiId", apiId);
        comando.Parameters.AddWithValue("nombre", nombre);
        comando.Parameters.AddWithValue("activo", activo);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task<Guid> InsertarSuscripcion(Guid apiId, Guid planId)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO consumidor (id, organizacion_id, nombre, correo, hash_contrasena, estado, nombre_empresa)
            VALUES (gen_random_uuid(), @org, 'Consumidor', 'c@test.com', 'hash', 'activo', 'Empresa Prueba')
            RETURNING id
            """;
        comando.Parameters.AddWithValue("org", _organizacionId);
        var consumidorId = (Guid)(await comando.ExecuteScalarAsync())!;

        comando.CommandText = """
            INSERT INTO suscripcion_api (id, consumidor_id, api_id, plan_id, estado, inicio, fin)
            VALUES (gen_random_uuid(), @consumidor, @apiId, @planId, 'activa', now(), now() + interval '30 days')
            RETURNING id
            """;
        comando.Parameters.AddWithValue("consumidor", consumidorId);
        comando.Parameters.AddWithValue("apiId", apiId);
        comando.Parameters.AddWithValue("planId", planId);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private ShapiDbContext Db(out IServiceScope alcance)
    {
        alcance = _fabrica.Services.CreateScope();
        return alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
    }
}
