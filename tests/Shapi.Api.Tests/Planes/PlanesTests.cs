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
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Planes;
using Shapi.Infraestructura.Persistencia;
using ApiDominio = Shapi.Dominio.Apis.Api;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Planes;

/// <summary>Cada prueba usa su propia base de datos en el PostgreSQL compartido (JG-18).</summary>
public sealed class ContenedorPostgresPlanes : PostgresDePrueba;

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
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    // RF-18
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
        // H-63: «en la API de Cotización de Envíos», sin repetir «API».
        Assert.Equal("Creó el plan Básico en la API de Cotización de Envíos.", entrada.Descripcion);
    }

    // RF-18 (H-65): la respuesta lleva la moneda y no las fechas de auditoría, como dice el contrato.
    [Fact]
    public async Task EM07_ListarPlanes_RespuestaComoElContrato()
    {
        var apiId = await InsertarApi(_organizacionId);
        await InsertarPlan(apiId, "Comercio");

        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/planes", null);

        var plan = Assert.Single((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Equal("GTQ", plan.GetProperty("moneda").GetString());
        Assert.False(plan.TryGetProperty("creadoEn", out _));
        Assert.False(plan.TryGetProperty("actualizadoEn", out _));
    }

    // RF-18 (H-58): A4.1 y A5.4 muestran los planes por precio, y una edición no cambia el orden.
    [Fact]
    public async Task EM07_ListarPlanes_OrdenaPorPrecioYNombre()
    {
        var apiId = await InsertarApi(_organizacionId);
        await InsertarPlan(apiId, "Volumen", precio: 1200);
        var comercio = await InsertarPlan(apiId, "Comercio", precio: 450);
        await InsertarPlan(apiId, "Básico", precio: 0, gratuito: true);
        await InsertarPlan(apiId, "Alfa", precio: 450);
        using (var editar = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/planes/{comercio}", new
        {
            nombre = "Comercio",
            descripcion = "Desc",
            precio = 450.0m,
            esGratuito = false,
            vigenciaDias = 30,
            cuotaLlamadas = 2000,
            limiteMinuto = 60,
        }))
        {
            Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        }

        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/planes", null);

        var nombres = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Select(p => p.GetProperty("nombre").GetString()!)
            .ToArray();
        Assert.Equal(["Básico", "Alfa", "Comercio", "Volumen"], nombres);
    }

    // RF-18 (H-60): un plan de pago con precio 0 no se puede crear; la base también lo impide (ck_plan_api_pago).
    [Fact]
    public async Task EM07_CrearPlan_DePagoConPrecioCero_Responde400YLaBaseLoRechaza()
    {
        var apiId = await InsertarApi(_organizacionId);

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", new
        {
            nombre = "Cero",
            descripcion = "Desc",
            precio = 0.0m,
            esGratuito = false,
            vigenciaDias = 30,
            cuotaLlamadas = 1000,
            limiteMinuto = 60,
        });

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("precio", out _));
        var error = await Assert.ThrowsAsync<PostgresException>(() => InsertarPlan(apiId, "Directo", precio: 0));
        Assert.Equal("ck_plan_api_pago", error.ConstraintName);
    }

    // RF-18 (H-59): un precio que no cabe en numeric(12,2) o con más de 2 decimales es 400, no 409 plan_duplicado.
    [Theory]
    [InlineData("100000000000")]
    [InlineData("10.005")]
    public async Task EM07_CrearPlan_PrecioFueraDeRango_Responde400(string precio)
    {
        var apiId = await InsertarApi(_organizacionId);

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", new
        {
            nombre = "Caro",
            descripcion = "Desc",
            precio = decimal.Parse(precio, System.Globalization.CultureInfo.InvariantCulture),
            esGratuito = false,
            vigenciaDias = 30,
            cuotaLlamadas = 1000,
            limiteMinuto = 60,
        });

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("precio", out _));
    }

    // Decidido (3 oct, EM-07): no se cambia entre gratuito y de pago mientras haya suscripciones vigentes (09 §5).
    [Fact]
    public async Task EM07_EditarPlan_CambiarAGratuitoConSuscripciones_Responde422SinCambios()
    {
        var apiId = await InsertarApi(_organizacionId);
        var planId = await InsertarPlan(apiId, "Comercio");
        await InsertarSuscripcion(apiId, planId);

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/planes/{planId}", new
        {
            nombre = "Comercio",
            descripcion = "Desc",
            precio = 0.0m,
            esGratuito = true,
            vigenciaDias = 30,
            cuotaLlamadas = 1000,
            limiteMinuto = 60,
        });

        await AfirmarProblema(respuesta, (HttpStatusCode)422, "plan_con_suscripciones");
        await using var db = Db(out var alcance);
        using var _ = alcance;
        Assert.False((await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(p => p.Id == planId)).EsGratuito);
    }

    [Fact]
    public async Task EM07_EditarPlan_CambiarAGratuitoSinSuscripciones_Responde200()
    {
        var apiId = await InsertarApi(_organizacionId);
        var planId = await InsertarPlan(apiId, "Comercio");

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/planes/{planId}", new
        {
            nombre = "Comercio",
            descripcion = "Desc",
            precio = 0.0m,
            esGratuito = true,
            vigenciaDias = 30,
            cuotaLlamadas = 1000,
            limiteMinuto = 60,
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // RF-18 (H-65): el portal de un host que no existe responde 404.
    [Fact]
    public async Task EM07_PlanesPortal_HostSinPortal_Responde404()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/portal/planes");
        peticion.Headers.Host = "no-existe.shapi.localhost";

        using var respuesta = await _cliente.SendAsync(peticion);

        await AfirmarProblema(respuesta, HttpStatusCode.NotFound, "api_no_encontrada");
    }

    // RF-18
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
            precio = 149.0m,
            esGratuito = false,
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
        Assert.False(plan.EsGratuito);
        Assert.Equal(149.0m, plan.Precio);
        Assert.Equal(15, plan.VigenciaDias);
        Assert.Equal(500, plan.CuotaLlamadas);
        Assert.Equal(30, plan.LimiteMinuto);

        var entradas = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().Where(e => e.ObjetivoId == planId).ToListAsync();
        Assert.Contains(entradas, e => e.Accion == "plan_api.editado");

        var publicador = _fabrica.Services.GetRequiredService<PublicadorCachePlanesFalso>();
        Assert.Contains(suscripcionId, publicador.SuscripcionesPublicadas);
    }

    // RF-18
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

    // RF-18
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

    // RF-18
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
    public async Task EM07_Planes_RolLector_PuedeLeer_Retorna200_NoPuedeEditar_Retorna403(string rol)
    {
        // RF-18
        var apiId = await InsertarApi(_organizacionId);
        var planId = await InsertarPlan(apiId, "Test Lector");

        using var respGet = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/planes", null, rol);
        Assert.Equal(HttpStatusCode.OK, respGet.StatusCode);

        var peticion = new { nombre = "Test", descripcion = "Desc", precio = 0.0m, esGratuito = true, vigenciaDias = 30, cuotaLlamadas = 1000, limiteMinuto = 60 };
        using var respPost = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", peticion, rol);
        Assert.Equal(HttpStatusCode.Forbidden, respPost.StatusCode);

        using var respPut = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/planes/{planId}", peticion, rol);
        Assert.Equal(HttpStatusCode.Forbidden, respPut.StatusCode);

        using var respDesactivar = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes/{planId}/desactivar", null, rol);
        Assert.Equal(HttpStatusCode.Forbidden, respDesactivar.StatusCode);
    }

    [Theory]
    [InlineData("", 0.0, true, 30, 1000, 60, "nombre")]
    [InlineData("Basico", 100.0, true, 30, 1000, 60, "precio")]
    [InlineData("Basico", 0.0, true, 0, 1000, 60, "vigenciaDias")]
    [InlineData("Basico", 0.0, true, 367, 1000, 60, "vigenciaDias")]
    [InlineData("Basico", 0.0, true, 30, 0, 60, "cuotaLlamadas")]
    [InlineData("Basico", 0.0, true, 30, 1000, 0, "limiteMinuto")]
    public async Task EM07_CrearPlan_DatosInvalidos_Retorna400(string nombre, decimal precio, bool esGratuito, int vigenciaDias, int cuotaLlamadas, int limiteMinuto, string campo)
    {
        // RF-18
        var apiId = await InsertarApi(_organizacionId);

        var peticion = new
        {
            nombre,
            descripcion = "Plan básico",
            precio,
            esGratuito,
            vigenciaDias,
            cuotaLlamadas,
            limiteMinuto
        };

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/planes", peticion);
        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        // H-64: el error va en el campo (convenciones §5).
        Assert.True(problema.GetProperty("errores").TryGetProperty(campo, out _));
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
    public async Task EM07_EditarPlan_NombreDuplicado_Retorna409()
    {
        // RF-19
        var apiId = await InsertarApi(_organizacionId);
        await InsertarPlan(apiId, "Plan 1");
        var plan2Id = await InsertarPlan(apiId, "Plan 2");

        var peticion = new
        {
            nombre = "Plan 1",
            descripcion = "Otro",
            precio = 0.0m,
            esGratuito = true,
            vigenciaDias = 30,
            cuotaLlamadas = 1000,
            limiteMinuto = 60
        };

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/planes/{plan2Id}", peticion);
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
            VALUES (gen_random_uuid(), @organizacion, 'API de Cotización de Envíos', @subdominio, 'https://8.8.8.8', 'publicada', 'cifrado')
            RETURNING id
            """;
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        comando.Parameters.AddWithValue("subdominio", subdominio);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task<Guid> InsertarPlan(Guid apiId, string nombre, bool activo = true, decimal precio = 10.0m, bool gratuito = false)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO plan_api (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias, cuota_llamadas, limite_minuto, activo)
            VALUES (gen_random_uuid(), @apiId, @nombre, 'Desc', @precio, @gratuito, 30, 1000, 60, @activo)
            RETURNING id
            """;
        comando.Parameters.AddWithValue("precio", precio);
        comando.Parameters.AddWithValue("gratuito", gratuito);
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

    private static async Task<System.Text.Json.JsonElement> AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(codigo, json.GetProperty("codigo").GetString());
        return json;
    }
}
