using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
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
using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Red;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Consumo;
using Shapi.Infraestructura.Persistencia;
using Shapi.Trabajador.VerificacionDominios;
using ApiDominio = Shapi.Dominio.Apis.Api;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Apis;

/// <summary>Cada prueba usa su propia base de datos en el PostgreSQL compartido (JG-18).</summary>
public sealed class ContenedorPostgresApis : PostgresDePrueba;

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

public sealed class PublicadorCacheApisFalso : IPublicadorCache
{
    public List<Guid> ApisPublicadas { get; } = [];
    public List<(string Host, Guid ApiId)> HostsEliminados { get; } = [];
    public Task PublicarApi(Guid apiId, CancellationToken cancelacion = default) { ApisPublicadas.Add(apiId); return Task.CompletedTask; }
    public Task EliminarHost(string host, Guid apiId, CancellationToken cancelacion = default)
    {
        HostsEliminados.Add((host, apiId));
        return Task.CompletedTask;
    }
    public Task PublicarClave(Guid claveId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task EliminarClave(string hashClave, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default) => Task.CompletedTask;
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

public sealed class DireccionLoopbackPrueba : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => app =>
    {
        app.Use(async (contexto, proximo) =>
        {
            contexto.Connection.RemoteIpAddress = IPAddress.Parse(
                contexto.Request.Headers["X-Prueba-Ip"].FirstOrDefault() ?? "127.0.0.1");
            await proximo();
        });
        siguiente(app);
    };
}

public sealed class RelojApisFalso : IReloj
{
    public DateTimeOffset Ahora { get; set; } = DateTimeOffset.Parse("2026-10-14T12:00:00Z");
}

public class ApisTests(ContenedorPostgresApis postgres) : IClassFixture<ContenedorPostgresApis>, IAsyncLifetime
{
    private string _cadena = null!;
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private Guid _organizacionId;
    private Guid _usuarioId;
    private readonly RelojApisFalso _reloj = new();

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
                services.RemoveAll<IReloj>();
                services.AddSingleton<IReloj>(_reloj);
                services.AddScoped<VerificarDominiosPendientes>();
                services.AddTransient<IStartupFilter, DireccionLoopbackPrueba>();
                services.RemoveAll<IPublicadorCache>();
                services.AddSingleton<PublicadorCacheApisFalso>();
                services.AddSingleton<IPublicadorCache>(sp => sp.GetRequiredService<PublicadorCacheApisFalso>());
            });
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        await _cliente.GetAsync("/salud");
        _organizacionId = await CrearOrganizacion("Envíos Xelajú, S.A.");
        _usuarioId = await CrearUsuario(_organizacionId, "ana@envios.test", verificado: true, "propietario");
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    // RNF-15: la API responde /salud (antes en SaludTests, que arrancaba su propio contenedor; JG-18).
    [Fact]
    public async Task Salud_ApiEnEjecucion_Responde200()
    {
        var respuesta = await _cliente.GetAsync("/salud");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
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

    // RF-43: todas las APIs de la organización cuentan para el límite y el rechazo no guarda otra API.
    [Fact]
    public async Task RF_43_RegistrarApi_AlcanzarMaximoResponde422ConLimiteApis()
    {
        await using (var conexion = new NpgsqlConnection(_cadena))
        {
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand("UPDATE plan_plataforma SET max_apis = 1 WHERE es_prueba", conexion);
            await comando.ExecuteNonQueryAsync();
        }

        using var existente = await Registrar($"existente-{Guid.NewGuid():N}"[..20], "https://8.8.8.8");
        Assert.Equal(HttpStatusCode.Created, existente.StatusCode);
        using var respuesta = await Registrar($"nueva-{Guid.NewGuid():N}"[..20], "https://8.8.8.8");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("limite_del_plan", problema.GetProperty("codigo").GetString());
        Assert.Equal("apis", problema.GetProperty("detalle").GetProperty("limite").GetString());
        Assert.Equal(1, await ContarApis());
    }

    [Fact]
    public async Task RF_08_Registrar_SubdominioReservado_Responde400SinGuardar()
    {
        using var respuesta = await Registrar("admin", "https://8.8.8.8");

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("subdominio", out _));
        Assert.Equal(0, await ContarApis());
    }

    // H-56: "admin\n" pasaba el patrón (`$` acepta un salto de línea final) y la entidad respondía 500.
    [Theory]
    [InlineData("admin\n", false)]
    [InlineData("a\n", false)]
    [InlineData("envios\n", true)]
    public async Task RF_08_Registrar_SubdominioConSaltoDeLineaSeRecortaAntesDeValidar(string subdominio, bool valido)
    {
        using var respuesta = await Registrar(subdominio, "https://8.8.8.8");

        if (valido)
        {
            Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
            Assert.Equal("envios", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("subdominio").GetString());
            return;
        }

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

    [Theory]
    [InlineData("origenes-demo/envios-xelaju/cotizacion-envios.yaml", 5, "API de Cotización de Envíos")]
    [InlineData("origenes-demo/agro-precios/openapi.yaml", 4, "API de Precios de Mercado")]
    public async Task RF_09_CargarEspecificacion_DemosExtraeInfoRutasYDefinicion(
        string archivoRelativo,
        int totalEsperado,
        string tituloEsperado)
    {
        var apiId = await RegistrarYObtenerId($"demo-{Guid.NewGuid():N}"[..20]);

        using var respuesta = await CargarEspecificacion(apiId, archivoRelativo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(totalEsperado, json.GetProperty("totalRutas").GetInt32());
        Assert.Equal(tituloEsperado, json.GetProperty("titulo").GetString());
        Assert.Equal("3.0.3", json.GetProperty("versionOpenApi").GetString());
        Assert.All(json.GetProperty("rutas").EnumerateArray(), ruta => Assert.False(ruta.GetProperty("expuesta").GetBoolean()));

        await using var db = Db(out var alcance);
        using var _ = alcance;
        var api = await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Id == apiId);
        Assert.Equal(tituloEsperado, api.EspecificacionTitulo);
        Assert.Equal("1.0.0", api.EspecificacionVersion);
        var rutas = await db.Set<Ruta>().IgnoreQueryFilters().Where(r => r.ApiId == apiId).ToListAsync();
        Assert.Equal(totalEsperado, rutas.Count);
        Assert.Contains(rutas, r => r.Definicion.Contains("parametros", StringComparison.Ordinal)
            && r.Definicion.Contains("respuestas", StringComparison.Ordinal));
        if (archivoRelativo.Contains("agro-precios", StringComparison.Ordinal))
        {
            var precios = Assert.Single(rutas, r => r.Patron == "/precios");
            Assert.Contains("producto", precios.Definicion, StringComparison.Ordinal);
            Assert.Contains("frijol_negro", precios.Definicion, StringComparison.Ordinal);
        }
        else
        {
            var cotizaciones = Assert.Single(rutas, r => r.Patron == "/cotizaciones");
            Assert.Contains("cuerpo", cotizaciones.Definicion, StringComparison.Ordinal);
            Assert.Contains("peso_kg", cotizaciones.Definicion, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RF_09_CargarEspecificacion_InvalidaResponde422ConUbicacion()
    {
        var apiId = await RegistrarYObtenerId("invalida");
        const string contenido = "openapi: 3.0.3\ninfo:\n  title: Sin versión\npaths: [";

        using var respuesta = await CargarEspecificacion(apiId, "invalida.yaml", contenido);

        var problema = await AfirmarProblema(respuesta, (HttpStatusCode)422, "especificacion_invalida");
        var detalle = problema.GetProperty("detalle");
        Assert.False(string.IsNullOrWhiteSpace(detalle.GetProperty("ubicacion").GetString()));
        // RNF-12 (decidido el 3 oct, DC-05): el mensaje va en español y el de SharpYaml, aparte.
        Assert.Equal("El documento tiene un error de formato o de estructura.", detalle.GetProperty("mensaje").GetString());
        Assert.False(string.IsNullOrWhiteSpace(detalle.GetProperty("detalleTecnico").GetString()));
        Assert.Equal(0, await ContarRutas(apiId));
    }

    [Fact]
    public async Task RF_09_CargarEspecificacion_Json31EsAceptada()
    {
        var apiId = await RegistrarYObtenerId("json-31");
        const string contenido = """
            {"openapi":"3.1.0","info":{"title":"API JSON","version":"1.2.3"},"paths":{"/saludo":{"parameters":[{"name":"idioma","in":"query","schema":{"type":"string"}}],"get":{"responses":{"200":{"description":"Correcto"}}}}}}
            """;

        using var respuesta = await CargarEspecificacion(apiId, "openapi.json", contenido);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("3.1.0", json.GetProperty("versionOpenApi").GetString());
        Assert.Equal("json", json.GetProperty("formato").GetString());
        Assert.Equal("/saludo", Assert.Single(json.GetProperty("rutas").EnumerateArray()).GetProperty("patron").GetString());
        await using var db = Db(out var alcance);
        using var _ = alcance;
        Assert.Contains("idioma", (await db.Set<Ruta>().IgnoreQueryFilters().SingleAsync(r => r.ApiId == apiId)).Definicion,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RF_09_CargarEspecificacion_VersionYamlSeLeeDeLaRaiz()
    {
        var apiId = await RegistrarYObtenerId("version-yaml");
        const string contenido = """
            x-datos:
              openapi: 3.0.3
            openapi: 3.1.0
            info:
              title: API YAML
              version: 1.0.0
            paths: {}
            """;

        using var respuesta = await CargarEspecificacion(apiId, "openapi.yaml", contenido);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("3.1.0", json.GetProperty("versionOpenApi").GetString());
    }

    [Fact]
    public async Task RF_09_CargarEspecificacion_MayorDe2MbResponde422SinGuardar()
    {
        var apiId = await RegistrarYObtenerId("grande");
        var contenido = new string('x', Shapi.Aplicacion.Apis.CargarEspecificacion.MaximoBytes + 1);

        using var respuesta = await CargarEspecificacion(apiId, "grande.yaml", contenido);

        var problema = await AfirmarProblema(respuesta, (HttpStatusCode)422, "especificacion_invalida");
        Assert.Equal("archivo", problema.GetProperty("detalle").GetProperty("ubicacion").GetString());
        Assert.Equal(0, await ContarRutas(apiId));
    }

    [Fact]
    public async Task RF_09_ListarRutas_ApiDeOtraOrganizacionResponde404()
    {
        var otra = await CrearOrganizacion("Otra organización");
        var apiAjena = await InsertarApi(otra, "api-ajena");

        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiAjena}/rutas", null);

        await AfirmarProblema(respuesta, HttpStatusCode.NotFound, "api_no_encontrada");
    }

    [Fact]
    public async Task RF_09_Recargar_ConservaConfiguracionAgregaOcultasYEliminaAusentes()
    {
        var apiId = await RegistrarYObtenerId("recarga");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);

        Guid tarifasId;
        await using (var db = Db(out var alcance))
        using (alcance)
        {
            var tarifas = await db.Set<Ruta>().IgnoreQueryFilters()
                .SingleAsync(r => r.ApiId == apiId && r.Patron == "/tarifas");
            tarifasId = tarifas.Id;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE ruta SET expuesta = true, limite_minuto = 25, cache_segundos = 90, peso_llamadas = 3
                WHERE id = {tarifasId}
                """);
            var retiradas = await db.Set<Ruta>().IgnoreQueryFilters()
                .Where(r => r.ApiId == apiId && (r.Patron == "/guias" || r.Patron == "/cobertura"))
                .Select(r => r.Id)
                .ToArrayAsync();
            var suscripcionId = Guid.NewGuid();
            var consumidorId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO consumidor
                    (id, organizacion_id, nombre, nombre_empresa, correo, hash_contrasena, estado)
                VALUES
                    ({consumidorId}, {_organizacionId}, 'Consumidor', 'Empresa', {suscripcionId + "@prueba.test"}, 'hash', 'activo');
                INSERT INTO plan_api
                    (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias,
                     cuota_llamadas, limite_minuto, activo)
                VALUES
                    ({planId}, {apiId}, 'Plan', 'Plan de prueba', 10, false, 30, 1000, 60, true);
                INSERT INTO suscripcion_api (id, consumidor_id, api_id, plan_id, estado, inicio, fin)
                VALUES ({suscripcionId}, {consumidorId}, {apiId}, {planId}, 'activa', now(), now() + interval '30 days');
                INSERT INTO consumo_diario
                    (fecha, api_id, ruta_id, suscripcion_id, entorno,
                     peticiones, llamadas, bytes_entrada, bytes_salida,
                     rechazos_401, rechazos_403, rechazos_404, rechazos_429,
                     origen_2xx, origen_3xx, origen_4xx, origen_5xx, origen_fallo,
                     hist_latencia_total, hist_latencia_compuerta,
                     latencia_total_suma_ms, latencia_compuerta_suma_ms, creado_en, actualizado_en)
                VALUES
                    ({new DateOnly(2026, 10, 1)}, {apiId}, NULL, {suscripcionId}, 'produccion',
                     1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                     array_fill(1, ARRAY[10]), array_fill(1, ARRAY[10]), 10, 20,
                     {new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)}, {new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)}),
                    ({new DateOnly(2026, 10, 1)}, {apiId}, {retiradas[0]}, {suscripcionId}, 'produccion',
                     2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                     array_fill(2, ARRAY[10]), array_fill(2, ARRAY[10]), 20, 40,
                     {new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero)}, {new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero)}),
                    ({new DateOnly(2026, 10, 1)}, {apiId}, {retiradas[1]}, {suscripcionId}, 'produccion',
                     3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                     array_fill(3, ARRAY[10]), array_fill(3, ARRAY[10]), 30, 60,
                     {new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero)}, {new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero)}),
                    ({new DateOnly(2026, 10, 2)}, {apiId}, {retiradas[0]}, {suscripcionId}, 'produccion',
                     4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                     array_fill(4, ARRAY[10]), array_fill(4, ARRAY[10]), 40, 80,
                     {new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero)}, {new DateTimeOffset(2026, 9, 4, 0, 0, 0, TimeSpan.Zero)}),
                    ({new DateOnly(2026, 10, 2)}, {apiId}, {retiradas[1]}, {suscripcionId}, 'produccion',
                     5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                     array_fill(5, ARRAY[10]), array_fill(5, ARRAY[10]), 50, 100,
                     {new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero)}, {new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero)})
                """);
        }

        const string recarga = """
            openapi: 3.1.0
            info:
              title: API recargada
              version: 2.0.0
            paths:
              /tarifas:
                get:
                  summary: Tarifas actualizadas
                  responses:
                    '200': { description: Correcto }
              /nueva:
                get:
                  summary: Ruta nueva
                  responses:
                    '200': { description: Correcto }
            """;

        using var respuesta = await CargarEspecificacion(apiId, "recarga.json", recarga);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var respuestaJson = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        await using var dbFinal = Db(out var alcanceFinal);
        using var __ = alcanceFinal;
        var rutas = await dbFinal.Set<Ruta>().IgnoreQueryFilters().Where(r => r.ApiId == apiId).ToListAsync();
        Assert.Equal(2, rutas.Count);
        var conservada = Assert.Single(rutas, r => r.Patron == "/tarifas");
        Assert.Equal(tarifasId, conservada.Id);
        Assert.True(conservada.Expuesta);
        Assert.Equal(25, conservada.LimiteMinuto);
        Assert.Equal(90, conservada.CacheSegundos);
        Assert.Equal(3, conservada.PesoLlamadas);
        Assert.Equal("Tarifas actualizadas", conservada.Resumen);
        Assert.False(Assert.Single(rutas, r => r.Patron == "/nueva").Expuesta);
        var cargadaEn = respuestaJson.GetProperty("cargadaEn").GetDateTimeOffset();
        var consumos = await dbFinal.Set<ConsumoDiario>().IgnoreQueryFilters()
            .Where(c => c.ApiId == apiId)
            .OrderBy(c => c.Fecha)
            .ToArrayAsync();
        Assert.Collection(
            consumos,
            consumo => AfirmarConsumoConsolidado(
                consumo,
                6,
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
                cargadaEn),
            consumo => AfirmarConsumoConsolidado(consumo, 9, cargadaEn, cargadaEn));
    }

    [Fact]
    public async Task RF_09_RecargarApiPublicada_PublicaCacheDespuesDePersistir()
    {
        var apiId = await RegistrarYObtenerId("cache-especificacion");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        await MarcarPublicada(apiId);

        const string recarga = """
            openapi: 3.0.3
            info:
              title: API publicada recargada
              version: 2.0.0
            paths:
              /cotizaciones:
                post:
                  responses:
                    '200': { description: Correcto }
            """;
        using var respuesta = await CargarEspecificacion(apiId, "recarga.yaml", recarga);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var db = Db(out var alcance);
        using var _ = alcance;
        Assert.Equal("2.0.0", (await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Id == apiId)).EspecificacionVersion);
        var publicador = _fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>();
        Assert.Contains(apiId, publicador.ApisPublicadas);
    }

    [Fact]
    public async Task RF_10_Exposicion_ActualizaRutasYRegistraBitacora()
    {
        var apiId = await RegistrarYObtenerId("exposicion");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var listadoInicial = await ObtenerRutas(apiId);
        var rutas = listadoInicial.GetProperty("elementos").EnumerateArray().ToArray();
        var primera = rutas[0].GetProperty("id").GetGuid();
        var segunda = rutas[1].GetProperty("id").GetGuid();

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/rutas/exposicion", new[]
        {
            new { rutaId = primera, expuesta = true },
            new { rutaId = segunda, expuesta = true },
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("totalExpuestas").GetInt32());
        await using var db = Db(out var alcance);
        using var _ = alcance;
        var entradas = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters()
            .Where(e => e.ObjetivoTipo == "ruta" && (e.ObjetivoId == primera || e.ObjetivoId == segunda))
            .ToListAsync();
        Assert.Equal(2, entradas.Count);
        Assert.All(entradas, entrada => Assert.Equal("ruta.expuesta", entrada.Accion));

        using var ocultar = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/rutas/exposicion", new[]
        {
            new { rutaId = primera, expuesta = false },
        });
        Assert.Equal(HttpStatusCode.OK, ocultar.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Contains(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync(),
            entrada => entrada.ObjetivoId == primera && entrada.Accion == "ruta.ocultada");
    }

    [Fact]
    public async Task RF_10_Exposicion_ApiPublicadaPublicaCacheDespuesDeGuardar()
    {
        var apiId = await RegistrarYObtenerId("cache-rutas");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var rutaId = (await ObtenerRutas(apiId)).GetProperty("elementos")[0].GetProperty("id").GetGuid();
        await MarcarPublicada(apiId);

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/rutas/exposicion", new[]
        {
            new { rutaId, expuesta = true },
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var publicador = _fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>();
        Assert.Contains(apiId, publicador.ApisPublicadas);
    }

    // 04 §3.1: el lector ve las APIs y sus rutas (H-48 de la auditoría del 3 oct).
    [Theory]
    [InlineData("Propietario")]
    [InlineData("Editor")]
    [InlineData("Lector")]
    public async Task RF_09_ListarRutas_PermitidoATodosLosRolesConVerApis(string rol)
    {
        var apiId = await RegistrarYObtenerId($"rol-{rol.ToLowerInvariant()}");

        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/rutas", null, rol);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task RF_09_RF_10_Escrituras_LectorResponde403()
    {
        var apiId = await RegistrarYObtenerId("rol-lector-escribe");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var rutaId = (await ObtenerRutas(apiId)).GetProperty("elementos")[0].GetProperty("id").GetGuid();

        using var carga = await CargarEspecificacion(apiId, "otra.yaml", "openapi: 3.0.3", "Lector");
        using var exposicion = await EnviarAutenticado(
            HttpMethod.Put,
            $"/api/apis/{apiId}/rutas/exposicion",
            new[] { new { rutaId, expuesta = true } },
            "Lector");

        Assert.Equal(HttpStatusCode.Forbidden, carga.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, exposicion.StatusCode);
        Assert.False((await ObtenerRutas(apiId)).GetProperty("elementos")[0].GetProperty("expuesta").GetBoolean());
    }

    // H-49: al volver a A3.3, la tarjeta del archivo muestra la especificación que ya está cargada.
    [Fact]
    public async Task RF_09_ListarRutas_DevuelveElResumenDeLaEspecificacionCargada()
    {
        var apiId = await RegistrarYObtenerId("resumen");
        Assert.Equal(JsonValueKind.Null, (await ObtenerRutas(apiId)).GetProperty("especificacion").ValueKind);
        var archivo = RaizRepositorio.Ruta("origenes-demo", "envios-xelaju", "cotizacion-envios.yaml");
        using var carga = await CargarEspecificacion(apiId, "origenes-demo/envios-xelaju/cotizacion-envios.yaml");
        var cargadaEn = (await carga.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cargadaEn").GetDateTimeOffset();

        var especificacion = (await ObtenerRutas(apiId)).GetProperty("especificacion");

        Assert.Equal("API de Cotización de Envíos", especificacion.GetProperty("titulo").GetString());
        Assert.Equal("1.0.0", especificacion.GetProperty("version").GetString());
        Assert.Equal("3.0.3", especificacion.GetProperty("versionOpenApi").GetString());
        Assert.Equal("yaml", especificacion.GetProperty("formato").GetString());
        Assert.Equal(
            Encoding.UTF8.GetByteCount(await File.ReadAllTextAsync(archivo)),
            especificacion.GetProperty("tamanoBytes").GetInt32());
        Assert.InRange(
            especificacion.GetProperty("cargadaEn").GetDateTimeOffset(),
            cargadaEn.AddMilliseconds(-1),
            cargadaEn.AddMilliseconds(1));
    }

    // H-49 con un documento JSON: la versión de OpenAPI se lee del campo de la raíz.
    [Fact]
    public async Task RF_09_ListarRutas_ResumenDeUnaEspecificacionJson()
    {
        var apiId = await RegistrarYObtenerId("resumen-json");
        const string contenido = """
            {"openapi":"3.1.0","info":{"title":"API JSON","version":"1.2.3","x-datos":{"openapi":"3.0.0"}},"paths":{}}
            """;
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(apiId, "openapi.json", contenido)).StatusCode);

        var especificacion = (await ObtenerRutas(apiId)).GetProperty("especificacion");

        Assert.Equal("API JSON", especificacion.GetProperty("titulo").GetString());
        Assert.Equal("3.1.0", especificacion.GetProperty("versionOpenApi").GetString());
        Assert.Equal("json", especificacion.GetProperty("formato").GetString());
    }

    // H-50: dos cargas a la vez no chocan con el UNIQUE (api_id, metodo, patron) ni responden 500.
    [Fact]
    public async Task RF_09_CargarEspecificacion_CargasSimultaneasNoChocan()
    {
        var apiId = await RegistrarYObtenerId("simultaneas");

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            CargarEspecificacion(apiId, "origenes-demo/envios-xelaju/cotizacion-envios.yaml")));

        try
        {
            Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode));
            Assert.Equal(5, await ContarRutas(apiId));
        }
        finally
        {
            foreach (var respuesta in respuestas)
            {
                respuesta.Dispose();
            }
        }
    }

    // Revisión del paso 7: la carga que espera el bloqueo vuelve a leer la API, así que la especificación guardada
    // (la del resumen y la de la caché) siempre corresponde a las rutas que quedaron.
    [Fact]
    public async Task RF_09_CargarEspecificacion_CargasSimultaneasDejanEspecificacionYRutasCoherentes()
    {
        var apiId = await RegistrarYObtenerId("coherentes");
        const string otra = """
            openapi: 3.0.3
            info:
              title: API B
              version: 2.0.0
            paths:
              /b:
                get:
                  responses:
                    '200': { description: Correcto }
            """;
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);

        for (var intento = 0; intento < 3; intento++)
        {
            var respuestas = await Task.WhenAll(
                CargarEspecificacion(apiId, "b.yaml", otra),
                CargarEspecificacion(apiId, "origenes-demo/envios-xelaju/cotizacion-envios.yaml"));
            Assert.All(respuestas, respuesta => Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode));
            foreach (var respuesta in respuestas)
            {
                respuesta.Dispose();
            }

            var lista = await ObtenerRutas(apiId);
            var patrones = lista.GetProperty("elementos").EnumerateArray()
                .Select(ruta => ruta.GetProperty("patron").GetString())
                .ToArray();
            if (lista.GetProperty("especificacion").GetProperty("titulo").GetString() == "API B")
            {
                Assert.Equal("/b", Assert.Single(patrones));
            }
            else
            {
                Assert.Equal(5, patrones.Length);
                Assert.DoesNotContain("/b", patrones);
            }
        }
    }

    // H-52: sin la parte `archivo`, 400 datos_invalidos con el error en el campo.
    [Fact]
    public async Task RF_09_CargarEspecificacion_SinArchivoResponde400()
    {
        var apiId = await RegistrarYObtenerId("sin-archivo");
        var multipart = new MultipartFormDataContent { { new StringContent("x"), "otro" } };

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/especificacion", multipart);

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("archivo", out _));
    }

    // Decidido (3 oct, DC-05): documentos semánticamente inválidos y una bomba de alias YAML también responden 422.
    [Theory]
    [InlineData("swagger: '2.0'\ninfo:\n  title: Swagger\n  version: 1.0.0\npaths: {}\n")]
    [InlineData("openapi: 3.0.3\ninfo:\n  title: Sin versión\npaths: {}\n")]
    [InlineData("openapi: 3.0.3\ninfo:\n  title: Bomba\n  version: 1.0.0\npaths: {}\n"
        + "x-a: &a [x, x, x, x, x, x, x, x, x, x]\n"
        + "x-b: &b [*a, *a, *a, *a, *a, *a, *a, *a, *a, *a]\n"
        + "x-c: &c [*b, *b, *b, *b, *b, *b, *b, *b, *b, *b]\n"
        + "x-d: &d [*c, *c, *c, *c, *c, *c, *c, *c, *c, *c]\n"
        + "x-e: &e [*d, *d, *d, *d, *d, *d, *d, *d, *d, *d]\n"
        + "x-f: &f [*e, *e, *e, *e, *e, *e, *e, *e, *e, *e]\n"
        + "x-g: &g [*f, *f, *f, *f, *f, *f, *f, *f, *f, *f]\n"
        + "x-h: &h [*g, *g, *g, *g, *g, *g, *g, *g, *g, *g]\n"
        + "x-i: [*h, *h, *h, *h, *h, *h, *h, *h, *h, *h]\n")]
    public async Task RF_09_CargarEspecificacion_DocumentoSemanticamenteInvalidoResponde422(string contenido)
    {
        var apiId = await RegistrarYObtenerId($"semantica-{Guid.NewGuid():N}"[..20]);

        using var respuesta = await CargarEspecificacion(apiId, "invalida.yaml", contenido);

        var problema = await AfirmarProblema(respuesta, (HttpStatusCode)422, "especificacion_invalida");
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("detalle").GetProperty("ubicacion").GetString()));
        Assert.Equal(0, await ContarRutas(apiId));
    }

    // H-51: un elemento sin `expuesta` o nulo no oculta rutas sin aviso ni responde 500.
    [Theory]
    [InlineData("[{\"rutaId\":\"{ruta}\"}]", "[0].expuesta")]
    [InlineData("[{\"expuesta\":true}]", "[0].rutaId")]
    [InlineData("[null]", "[0]")]
    [InlineData("[{\"rutaId\":\"{ruta}\",\"expuesta\":true},{\"rutaId\":\"{ruta}\",\"expuesta\":false}]", "[1].rutaId")]
    public async Task RF_10_Exposicion_LoteInvalidoResponde400SinCambios(string plantilla, string campo)
    {
        var apiId = await RegistrarYObtenerId($"lote-{Guid.NewGuid():N}"[..20]);
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var rutaId = (await ObtenerRutas(apiId)).GetProperty("elementos")[0].GetProperty("id").GetGuid();
        using (var exponer = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/rutas/exposicion", new[]
        {
            new { rutaId, expuesta = true },
        }))
        {
            Assert.Equal(HttpStatusCode.OK, exponer.StatusCode);
        }

        using var respuesta = await EnviarAutenticado(
            HttpMethod.Put,
            $"/api/apis/{apiId}/rutas/exposicion",
            new StringContent(plantilla.Replace("{ruta}", rutaId.ToString()), Encoding.UTF8, "application/json"));

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty(campo, out _));
        Assert.True((await ObtenerRutas(apiId)).GetProperty("elementos")[0].GetProperty("expuesta").GetBoolean());
    }

    // H-52: una ruta que no es de la API responde 404 y no cambia nada.
    [Fact]
    public async Task RF_10_Exposicion_RutaDeOtraApiResponde404()
    {
        var apiId = await RegistrarYObtenerId("ruta-propia");
        var otraApiId = await InsertarApi(_organizacionId, "ruta-ajena");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            otraApiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var rutaAjena = (await ObtenerRutas(otraApiId)).GetProperty("elementos")[0].GetProperty("id").GetGuid();

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/rutas/exposicion", new[]
        {
            new { rutaId = rutaAjena, expuesta = true },
        });

        await AfirmarProblema(respuesta, HttpStatusCode.NotFound, "api_no_encontrada");
        Assert.False((await ObtenerRutas(otraApiId)).GetProperty("elementos")[0].GetProperty("expuesta").GetBoolean());
    }

    // RF-13
    [Fact]
    public async Task RF_13_ConfiguracionRutas_ValidaGuardaYRepublicaApiPublicada()
    {
        var apiId = await RegistrarYObtenerId("configuracion");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var rutas = (await ObtenerRutas(apiId)).GetProperty("elementos").EnumerateArray().ToArray();
        var post = rutas.First(r => r.GetProperty("metodo").GetString() == "POST").GetProperty("id").GetGuid();
        var get = rutas.First(r => r.GetProperty("metodo").GetString() == "GET").GetProperty("id").GetGuid();
        await MarcarPublicada(apiId);

        using var respuesta = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/configuracion-rutas", new[]
        {
            new { rutaId = post, limiteMinuto = (int?)120, cacheSegundos = 0, pesoLlamadas = 5 },
            new { rutaId = get, limiteMinuto = (int?)null, cacheSegundos = 3600, pesoLlamadas = 1 },
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var listado = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var configuradas = listado.GetProperty("elementos").EnumerateArray().ToArray();
        Assert.Equal(5, configuradas.Single(r => r.GetProperty("id").GetGuid() == post).GetProperty("pesoLlamadas").GetInt32());
        Assert.Equal(3600, configuradas.Single(r => r.GetProperty("id").GetGuid() == get).GetProperty("cacheSegundos").GetInt32());
        Assert.Contains(apiId, _fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>().ApisPublicadas);

        object[] invalidas =
        [
            new { rutaId = post, limiteMinuto = (int?)0, cacheSegundos = 0, pesoLlamadas = 1 },
            new { rutaId = post, limiteMinuto = (int?)1, cacheSegundos = 30, pesoLlamadas = 1 },
            new { rutaId = get, limiteMinuto = (int?)1, cacheSegundos = 86401, pesoLlamadas = 1 },
            new { rutaId = get, limiteMinuto = (int?)1, cacheSegundos = 0, pesoLlamadas = 1001 },
        ];
        foreach (var invalida in invalidas)
        {
            using var fallo = await EnviarAutenticado(
                HttpMethod.Put,
                $"/api/apis/{apiId}/configuracion-rutas",
                new[] { invalida });
            await AfirmarProblema(fallo, HttpStatusCode.BadRequest, "datos_invalidos");
        }
    }

    // RF-14
    [Fact]
    public async Task RF_14_PublicarYDespublicar_ValidaRequisitosGuardaBitacoraYRepublica()
    {
        var apiId = await RegistrarYObtenerId("publicacion");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiId,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var rutaId = (await ObtenerRutas(apiId)).GetProperty("elementos")[0].GetProperty("id").GetGuid();
        var noVerificado = await CrearUsuario(_organizacionId, "sin-verificar@envios.test", verificado: false, "editor");

        using (var respuestaCorreo = await EnviarAutenticado(
            HttpMethod.Post, $"/api/apis/{apiId}/publicar", null, usuarioId: noVerificado))
        {
            await AfirmarProblema(respuestaCorreo, (HttpStatusCode)422, "correo_no_verificado");
        }

        using (var incompleta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/publicar", null))
        {
            var problema = await AfirmarProblema(incompleta, (HttpStatusCode)422, "publicacion_incompleta");
            Assert.Equal(2, problema.GetProperty("detalle").GetProperty("faltan").GetArrayLength());
        }

        using (var exponer = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/rutas/exposicion", new[]
        {
            new { rutaId, expuesta = true },
        }))
        {
            Assert.Equal(HttpStatusCode.OK, exponer.StatusCode);
        }
        await CrearPlanActivo(apiId);

        using var publicar = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/publicar", null);
        Assert.Equal(HttpStatusCode.OK, publicar.StatusCode);
        await using var db = Db(out var alcance);
        using var _ = alcance;
        var api = await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Id == apiId);
        Assert.Equal(EstadoApi.Publicada, api.Estado);
        Assert.NotNull(api.PublicadaEn);
        Assert.Contains(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync(),
            entrada => entrada.ObjetivoId == apiId && entrada.Accion == "api.publicada");

        using var despublicar = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/despublicar", null);
        Assert.Equal(HttpStatusCode.OK, despublicar.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(EstadoApi.Despublicada,
            (await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Id == apiId)).Estado);
        Assert.Contains(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync(),
            entrada => entrada.ObjetivoId == apiId && entrada.Accion == "api.despublicada");
        Assert.True(_fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>().ApisPublicadas.Count(id => id == apiId) >= 2);
    }

    // RF-14 / 07 §5: solo una API publicada puede pasar a despublicada.
    [Fact]
    public async Task RF_14_DespublicarApiEnBorrador_NoCambiaEstadoNiRegistraBitacora()
    {
        var apiId = await RegistrarYObtenerId("borrador-no-despublicar");

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/despublicar", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var db = Db(out var alcance);
        using var _ = alcance;
        Assert.Equal(EstadoApi.Borrador,
            (await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(api => api.Id == apiId)).Estado);
        Assert.DoesNotContain(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync(),
            entrada => entrada.ObjetivoId == apiId && entrada.Accion == "api.despublicada");
    }

    // RNF-08 y 04 §3.1: todos los endpoints nuevos aíslan organizaciones y solo permiten configurar al propietario/editor.
    [Fact]
    public async Task RF_13_RF_14_EndpointsNuevos_ApiAjenaResponde404YLector403()
    {
        var organizacionAjena = await CrearOrganizacion("Organización ajena");
        var apiAjena = await InsertarApi(organizacionAjena, "ajena-dc06");
        var rutaAjena = await InsertarRuta(apiAjena);
        var configuracionAjena = new[] { new { rutaId = rutaAjena, limiteMinuto = (int?)10, cacheSegundos = 0, pesoLlamadas = 1 } };
        using var configurarAjena = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiAjena}/configuracion-rutas", configuracionAjena);
        using var publicarAjena = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiAjena}/publicar", null);
        using var despublicarAjena = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiAjena}/despublicar", null);
        await AfirmarProblema(configurarAjena, HttpStatusCode.NotFound, "api_no_encontrada");
        await AfirmarProblema(publicarAjena, HttpStatusCode.NotFound, "api_no_encontrada");
        await AfirmarProblema(despublicarAjena, HttpStatusCode.NotFound, "api_no_encontrada");

        var apiPropia = await RegistrarYObtenerId("lector-dc06");
        Assert.Equal(HttpStatusCode.OK, (await CargarEspecificacion(
            apiPropia,
            "origenes-demo/envios-xelaju/cotizacion-envios.yaml")).StatusCode);
        var rutaPropia = (await ObtenerRutas(apiPropia)).GetProperty("elementos")[0].GetProperty("id").GetGuid();
        var configuracionPropia = new[] { new { rutaId = rutaPropia, limiteMinuto = (int?)10, cacheSegundos = 0, pesoLlamadas = 1 } };
        using var configurarLector = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiPropia}/configuracion-rutas", configuracionPropia, "Lector");
        using var publicarLector = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiPropia}/publicar", null, "Lector");
        using var despublicarLector = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiPropia}/despublicar", null, "Lector");
        Assert.Equal(HttpStatusCode.Forbidden, configurarLector.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, publicarLector.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, despublicarLector.StatusCode);
    }

    // RF-11, RF-12: el flujo simulado conecta, crea el CNAME, verifica y autoriza TLS.
    [Fact]
    public async Task RF_12_DominioPropio_FlujoSimuladoCompleto()
    {
        var apiId = await RegistrarYObtenerId("dominio-dc14");
        await HabilitarDominioPropio(_organizacionId);

        using var conectar = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio",
            new { dominio = "API.EnviosXelaju.Localhost" });
        Assert.Equal(HttpStatusCode.OK, conectar.StatusCode);
        var conectado = await conectar.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("api.enviosxelaju.localhost", conectado.GetProperty("dominio").GetString());
        Assert.Equal("dominio-dc14.api.shapi.localhost", conectado.GetProperty("destinoCname").GetString());
        Assert.Equal("pendiente", conectado.GetProperty("estado").GetString());

        using var sinCoincidencia = await EnviarAutenticado(HttpMethod.Post,
            $"/api/apis/{apiId}/dominio/verificar", null);
        var pendiente = await sinCoincidencia.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pendiente", pendiente.GetProperty("estado").GetString());
        Assert.False(string.IsNullOrWhiteSpace(pendiente.GetProperty("motivo").GetString()));

        using var pendienteTls = await _cliente.GetAsync("/interno/tls/autorizar?domain=api.enviosxelaju.localhost");
        Assert.Equal(HttpStatusCode.NotFound, pendienteTls.StatusCode);

        using var simular = await EnviarAutenticado(HttpMethod.Post,
            $"/api/apis/{apiId}/dominio/simular-registro", null);
        Assert.Equal(HttpStatusCode.NoContent, simular.StatusCode);

        using var verificar = await EnviarAutenticado(HttpMethod.Post,
            $"/api/apis/{apiId}/dominio/verificar", null);
        Assert.Equal(HttpStatusCode.OK, verificar.StatusCode);
        Assert.Equal("verificado", (await verificar.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("estado").GetString());

        using var autorizar = await _cliente.GetAsync("/interno/tls/autorizar?domain=api.enviosxelaju.localhost");
        Assert.Equal(HttpStatusCode.OK, autorizar.StatusCode);
        Assert.Contains(apiId, _fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>().ApisPublicadas);
        await using var db = Db(out var alcance);
        using var _ = alcance;
        Assert.Contains(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync(),
            e => e.ObjetivoId == apiId && e.Accion == "dominio.verificado");

        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync("/interno/tls/autorizar?domain=desconocido.localhost")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync("/interno/tls/autorizar")).StatusCode);
        using var publica = new HttpRequestMessage(HttpMethod.Get, "/interno/tls/autorizar?domain=api.enviosxelaju.localhost");
        publica.Headers.Add("X-Prueba-Ip", "8.8.8.8");
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.SendAsync(publica)).StatusCode);
    }

    // RF-47: secretos heredados cortos nunca producen 500 ni se revelan completos.
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("abcd")]
    public async Task RF_47_ConsultarDominio_SecretoCorto_RespondeEnmascarado(string secreto)
    {
        var apiId = await RegistrarYObtenerId("secreto-corto-jg24");
        await using (var db = Db(out var alcance))
        {
            using var _ = alcance;
            var api = await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(x => x.Id == apiId);
            api.RegenerarSecretoOrigen(alcance.ServiceProvider.GetRequiredService<IProtectorSecretoOrigen>().Cifrar(secreto), _reloj.Ahora);
            await db.SaveChangesAsync();
        }
        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/dominio", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("shps_••••••••••••", datos.GetProperty("secretoEnmascarado").GetString());
    }

    // RF-12: el estado informa los datos del plan de la organización.
    [Fact]
    public async Task RF_12_ConsultarDominio_InformaPlanYPermisoReal()
    {
        var apiId = await RegistrarYObtenerId("plan-dominio-jg24");
        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/dominio", null);
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(datos.GetProperty("dominioPropioPermitido").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(datos.GetProperty("planNombre").GetString()));
    }

    // RF-12: DNS simulado rechaza dominios fuera de .localhost y conserva el motivo.
    [Fact]
    public async Task RF_12_ConectarDominio_FueraDeLocalhost_Responde400ConMotivo()
    {
        var apiId = await RegistrarYObtenerId("dominio-real-jg24");
        await HabilitarDominioPropio(_organizacionId);
        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio", new { dominio = "api.ejemplo.com" });
        var datos = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.Contains(".localhost", datos.GetProperty("errores").GetProperty("dominio")[0].GetString());
    }

    // RF-12: un CNAME a otro destino no autoriza el dominio ni su TLS.
    [Fact]
    public async Task RF_12_VerificarDominio_CnameDistinto_ConservaPendiente()
    {
        var apiId = await RegistrarYObtenerId("cname-distinto-jg24");
        await HabilitarDominioPropio(_organizacionId);
        await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio", new { dominio = "api.distinto.localhost" });
        await using (var db = Db(out var alcance))
        {
            using var _ = alcance;
            db.Add(new RegistroDnsSimulado("api.distinto.localhost", "otra.api.shapi.localhost", _reloj.Ahora));
            await db.SaveChangesAsync();
        }
        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio/verificar", null);
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pendiente", datos.GetProperty("estado").GetString());
        Assert.Contains("otra.api.shapi.localhost", datos.GetProperty("motivo").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync("/interno/tls/autorizar?domain=api.distinto.localhost")).StatusCode);
    }

    // RF-12: el dominio propio depende del plan de plataforma vigente.
    [Fact]
    public async Task RF_12_ConectarDominio_PlanSinDominioPropio_Responde422()
    {
        var apiId = await RegistrarYObtenerId("sin-dominio-dc14");

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio",
            new { dominio = "api.sinplan.localhost" });

        await AfirmarProblema(respuesta, (HttpStatusCode)422, "plan_sin_dominio_propio");
    }

    // RF-12: los hosts de Shapi no pueden reclamarse y un dominio solo pertenece a una API.
    [Fact]
    public async Task RF_12_ConectarDominio_ProtegeHostsPlataformaYDominioOcupado()
    {
        await AumentarLimiteApis(_organizacionId, 50);
        var apiUno = await RegistrarYObtenerId("dominio-uno-dc14");
        var apiDos = await RegistrarYObtenerId("dominio-dos-dc14");
        await HabilitarDominioPropio(_organizacionId);

        using var plataforma = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiUno}/dominio",
            new { dominio = "victima.api.shapi.localhost" });
        await AfirmarProblema(plataforma, HttpStatusCode.BadRequest, "datos_invalidos");

        using var primero = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiUno}/dominio",
            new { dominio = "api.ocupado.localhost" });
        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);
        using var ocupado = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiDos}/dominio",
            new { dominio = "api.ocupado.localhost" });
        await AfirmarProblema(ocupado, HttpStatusCode.BadRequest, "datos_invalidos");
    }

    // RF-12: reemplazar o quitar un host verificado lo elimina de Redis después del commit.
    [Fact]
    public async Task RF_12_DominioVerificado_ReemplazarYEliminarQuitaHostAnterior()
    {
        var apiId = await RegistrarYObtenerId("reemplazo-dc14");
        await HabilitarDominioPropio(_organizacionId);
        await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio", new { dominio = "api.uno.localhost" });
        await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio/simular-registro", null);
        await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio/verificar", null);

        using var reemplazar = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/dominio",
            new { dominio = "api.dos.localhost" });
        Assert.Equal(HttpStatusCode.OK, reemplazar.StatusCode);
        var publicador = _fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>();
        Assert.Contains(("api.uno.localhost", apiId), publicador.HostsEliminados);

        using var eliminar = await EnviarAutenticado(HttpMethod.Delete, $"/api/apis/{apiId}/dominio", null);
        Assert.Equal(HttpStatusCode.NoContent, eliminar.StatusCode);
        Assert.Contains(("api.dos.localhost", apiId), publicador.HostsEliminados);
    }

    // RF-47: el secreto regenerado se devuelve una sola vez, se cifra y se audita.
    [Fact]
    public async Task RF_47_RegenerarSecreto_DevuelveNuevoUnaVezPublicaYRegistraBitacora()
    {
        var apiId = await RegistrarYObtenerId("secreto-dc14");

        using var respuesta = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/secreto/regenerar", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var secreto = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("secretoOrigen").GetString()!;
        Assert.Matches("^shps_[A-Za-z0-9]{32}$", secreto);
        await using var db = Db(out var alcance);
        using var _ = alcance;
        var api = await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Id == apiId);
        Assert.Equal(secreto, alcance.ServiceProvider.GetRequiredService<IProtectorSecretoOrigen>()
            .Descifrar(api.SecretoOrigenCifrado));
        Assert.Contains(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync(),
            e => e.ObjetivoId == apiId && e.Accion == "secreto_origen.regenerado");
        Assert.Contains(apiId, _fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>().ApisPublicadas);
    }

    // RF-12: el trabajo verifica pendientes y aplica exactamente el umbral de 72 horas.
    [Fact]
    public async Task RF_12_Trabajador_VerificaCnameYMarcaFallidoALas72Horas()
    {
        await AumentarLimiteApis(_organizacionId, 50);
        var apiVerificada = await RegistrarYObtenerId("trabajo-verifica-dc14");
        var apiFallida = await RegistrarYObtenerId("trabajo-falla-dc14");
        var apiPendiente = await RegistrarYObtenerId("trabajo-pendiente-dc14");
        await using var db = Db(out var alcance);
        using var _ = alcance;
        var ahora = _reloj.Ahora;
        var correcta = new DominioPropio(apiVerificada, "correcta.localhost", "trabajo-verifica-dc14.api.shapi.localhost", ahora.AddHours(-1));
        var vencida = new DominioPropio(apiFallida, "vencida.localhost", "trabajo-falla-dc14.api.shapi.localhost", ahora.AddHours(-72));
        var reciente = new DominioPropio(apiPendiente, "reciente.localhost", "trabajo-pendiente-dc14.api.shapi.localhost", ahora.AddHours(-71));
        db.AddRange(correcta, vencida, reciente,
            new RegistroDnsSimulado(correcta.Dominio, correcta.DestinoCname, ahora));
        await db.SaveChangesAsync();

        await alcance.ServiceProvider.GetRequiredService<VerificarDominiosPendientes>().EjecutarAsync();

        db.ChangeTracker.Clear();
        var dominios = await db.Set<DominioPropio>().IgnoreQueryFilters().ToDictionaryAsync(d => d.ApiId);
        Assert.Equal(EstadoDominio.Verificado, dominios[apiVerificada].Estado);
        Assert.Equal(EstadoDominio.Fallido, dominios[apiFallida].Estado);
        Assert.Equal(EstadoDominio.Pendiente, dominios[apiPendiente].Estado);
        Assert.Contains(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync(),
            e => e.ObjetivoId == apiVerificada && e.Accion == "dominio.verificado");
        Assert.Contains(apiVerificada, _fabrica.Services.GetRequiredService<PublicadorCacheApisFalso>().ApisPublicadas);
    }

    // RF-15: la personalización del portal se guarda, devuelve rutas expuestas y planes activos para la vista previa,
    // y al guardarse se refleja en el portal publicado.
    [Fact]
    public async Task RF_15_PersonalizarPortal_GuardaMarcaYLaReflejaEnElPortalPublicado()
    {
        var apiId = await RegistrarYObtenerId("portal-dc12");
        using var carga = await CargarEspecificacion(apiId, "origenes-demo/envios-xelaju/cotizacion-envios.yaml");
        Assert.Equal(HttpStatusCode.OK, carga.StatusCode);
        var rutas = await ObtenerRutas(apiId);
        var cambiosExposicion = rutas.GetProperty("elementos").EnumerateArray()
            .Select((r, indice) => new { rutaId = r.GetProperty("id").GetGuid(), expuesta = indice < 2 })
            .ToArray();
        using var exponer = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/rutas/exposicion", cambiosExposicion);
        Assert.Equal(HttpStatusCode.OK, exponer.StatusCode);
        await CrearPlanActivo(apiId);
        using var publicar = await EnviarAutenticado(HttpMethod.Post, $"/api/apis/{apiId}/publicar", null);
        Assert.Equal(HttpStatusCode.OK, publicar.StatusCode);

        using var inicial = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/portal", null, "Lector");
        Assert.Equal(HttpStatusCode.OK, inicial.StatusCode);
        var jsonInicial = await inicial.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("API de Cotización de Envíos", jsonInicial.GetProperty("apiNombre").GetString());
        Assert.Equal("Envíos Xelajú, S.A.", jsonInicial.GetProperty("nombreOrganizacion").GetString());
        Assert.Equal("https://portal-dc12.shapi.localhost", jsonInicial.GetProperty("urlPortal").GetString());
        Assert.Equal(JsonValueKind.Null, jsonInicial.GetProperty("nombre").ValueKind);
        Assert.Equal("#3B6FF0", jsonInicial.GetProperty("color").GetString());
        Assert.Equal(JsonValueKind.Null, jsonInicial.GetProperty("bienvenida").ValueKind);
        Assert.False(jsonInicial.GetProperty("tieneLogo").GetBoolean());
        Assert.Equal(JsonValueKind.Null, jsonInicial.GetProperty("urlLogo").ValueKind);
        Assert.Equal(2, jsonInicial.GetProperty("rutas").GetArrayLength());
        Assert.Equal(1, jsonInicial.GetProperty("planes").GetArrayLength());

        using var actualizar = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/portal", new
        {
            nombre = "Envíos Xelajú",
            color = "#b8322a",
            bienvenida = "Cotice y genere guías de envío a todo Guatemala desde su tienda en línea.",
        }, "Editor");
        Assert.Equal(HttpStatusCode.OK, actualizar.StatusCode);
        var jsonActualizado = await actualizar.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Envíos Xelajú", jsonActualizado.GetProperty("nombre").GetString());
        Assert.Equal("#B8322A", jsonActualizado.GetProperty("color").GetString());
        Assert.Equal("Cotice y genere guías de envío a todo Guatemala desde su tienda en línea.", jsonActualizado.GetProperty("bienvenida").GetString());

        var svg = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"></svg>");
        using var subirLogo = await SubirLogoPortal(apiId, "logo.svg", svg, "image/svg+xml");
        Assert.Equal(HttpStatusCode.OK, subirLogo.StatusCode);
        var jsonConLogo = await subirLogo.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(jsonConLogo.GetProperty("tieneLogo").GetBoolean());
        Assert.Equal($"/api/apis/{apiId}/portal/logo", jsonConLogo.GetProperty("urlLogo").GetString());

        using var logoPanel = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/portal/logo", null, "Lector");
        Assert.Equal(HttpStatusCode.OK, logoPanel.StatusCode);
        Assert.Equal("image/svg+xml", logoPanel.Content.Headers.ContentType?.MediaType);
        Assert.Equal("sandbox", logoPanel.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", logoPanel.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(svg, await logoPanel.Content.ReadAsByteArrayAsync());

        // El portal publicado refleja de inmediato la marca y el logotipo guardados.
        using var peticionPortal = new HttpRequestMessage(HttpMethod.Get, "/api/portal/configuracion");
        peticionPortal.Headers.Host = "portal-dc12.shapi.localhost";
        using var respuestaPortal = await _cliente.SendAsync(peticionPortal);
        Assert.Equal(HttpStatusCode.OK, respuestaPortal.StatusCode);
        var jsonPortal = await respuestaPortal.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Envíos Xelajú", jsonPortal.GetProperty("nombrePortal").GetString());
        Assert.Equal("#B8322A", jsonPortal.GetProperty("colorPrincipal").GetString());
        Assert.Equal("/api/portal/logo", jsonPortal.GetProperty("urlLogo").GetString());
        Assert.Equal("Cotice y genere guías de envío a todo Guatemala desde su tienda en línea.", jsonPortal.GetProperty("bienvenida").GetString());

        using var peticionLogoPortal = new HttpRequestMessage(HttpMethod.Get, "/api/portal/logo");
        peticionLogoPortal.Headers.Host = "portal-dc12.shapi.localhost";
        using var respuestaLogoPortal = await _cliente.SendAsync(peticionLogoPortal);
        Assert.Equal(HttpStatusCode.OK, respuestaLogoPortal.StatusCode);
        Assert.Equal(svg, await respuestaLogoPortal.Content.ReadAsByteArrayAsync());
    }

    // RF-15: valida color #RRGGBB y texto de bienvenida de hasta 280 caracteres.
    [Fact]
    public async Task RF_15_PersonalizarPortal_ValidaColorYBienvenida()
    {
        var apiId = await RegistrarYObtenerId("portal-valida-dc12");

        using var invalido = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/portal", new
        {
            nombre = "Portal",
            color = "rojo",
            bienvenida = new string('a', 281),
        });
        var problema = await AfirmarProblema(invalido, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("color", out _));
        Assert.True(problema.GetProperty("errores").TryGetProperty("bienvenida", out _));

        // 280 caracteres exactos y nombre vacío (vuelve al nombre de la API como respaldo) son válidos.
        using var limite = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/portal", new
        {
            nombre = "   ",
            color = "#1F8A5B",
            bienvenida = new string('a', 280),
        });
        Assert.Equal(HttpStatusCode.OK, limite.StatusCode);
        var json = await limite.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nombre").ValueKind);
        Assert.Equal("#1F8A5B", json.GetProperty("color").GetString());
        Assert.Equal(280, json.GetProperty("bienvenida").GetString()!.Length);
    }

    // RF-15: el logotipo acepta PNG o SVG de hasta 512 KB y rechaza otros formatos o tamaños con 422 logo_invalido.
    [Fact]
    public async Task RF_15_LogoPortal_AceptaPngYSvgHasta512KbYRechazaInvalidosCon422()
    {
        var apiId = await RegistrarYObtenerId("portal-logo-dc12");

        byte[] pngValido = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
        using var respuestaPng = await SubirLogoPortal(apiId, "marca.png", pngValido, "image/png");
        Assert.Equal(HttpStatusCode.OK, respuestaPng.StatusCode);
        using var descargaPng = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/portal/logo", null);
        Assert.Equal(HttpStatusCode.OK, descargaPng.StatusCode);
        Assert.Equal("image/png", descargaPng.Content.Headers.ContentType?.MediaType);
        Assert.False(descargaPng.Headers.Contains("Content-Security-Policy"));
        Assert.Equal(pngValido, await descargaPng.Content.ReadAsByteArrayAsync());

        // Mayor de 512 KB (524,288 bytes) -> 422 logo_invalido.
        var gigante = new byte[(512 * 1024) + 1];
        Array.Copy(pngValido, gigante, pngValido.Length);
        using var respuestaGigante = await SubirLogoPortal(apiId, "gigante.png", gigante, "image/png");
        await AfirmarProblema(respuestaGigante, HttpStatusCode.UnprocessableEntity, "logo_invalido");

        // Formato no permitido (JPEG / texto arbitrario / XML que no es <svg>) -> 422 logo_invalido.
        using var respuestaJpg = await SubirLogoPortal(apiId, "foto.jpg", [0xFF, 0xD8, 0xFF, 0xE0], "image/jpeg");
        await AfirmarProblema(respuestaJpg, HttpStatusCode.UnprocessableEntity, "logo_invalido");

        using var respuestaHtml = await SubirLogoPortal(
            apiId,
            "falso.svg",
            Encoding.UTF8.GetBytes("<html><body>no es svg</body></html>"),
            "image/svg+xml");
        await AfirmarProblema(respuestaHtml, HttpStatusCode.UnprocessableEntity, "logo_invalido");

        // El logotipo anterior se conserva tras los rechazos.
        using var conserva = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/portal/logo", null);
        Assert.Equal(pngValido, await conserva.Content.ReadAsByteArrayAsync());
    }

    // RF-07 y RNF-08: permisos de PersonalizarPortal y aislamiento entre organizaciones en el portal del panel.
    [Fact]
    public async Task RF_07_RNF_08_PersonalizarPortal_RespetaPermisosYAislamiento()
    {
        var apiPropia = await RegistrarYObtenerId("portal-propia-dc12");
        var otraOrg = await CrearOrganizacion("Otra Organización, S.A.");
        var apiAjena = await InsertarApi(otraOrg, "portal-ajena-dc12");

        using var lectorPut = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiPropia}/portal", new
        {
            nombre = "Intento",
            color = "#3B6FF0",
            bienvenida = "Texto",
        }, "Lector");
        Assert.Equal(HttpStatusCode.Forbidden, lectorPut.StatusCode);

        using var lectorLogo = await SubirLogoPortal(apiPropia, "logo.svg", Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"), "image/svg+xml", "Lector");
        Assert.Equal(HttpStatusCode.Forbidden, lectorLogo.StatusCode);

        using var ajenaGet = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiAjena}/portal", null);
        await AfirmarProblema(ajenaGet, HttpStatusCode.NotFound, "api_no_encontrada");

        using var ajenaPut = await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiAjena}/portal", new
        {
            nombre = "Intento",
            color = "#3B6FF0",
            bienvenida = "Texto",
        });
        await AfirmarProblema(ajenaPut, HttpStatusCode.NotFound, "api_no_encontrada");

        using var ajenaLogo = await SubirLogoPortal(apiAjena, "logo.svg", Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"), "image/svg+xml");
        await AfirmarProblema(ajenaLogo, HttpStatusCode.NotFound, "api_no_encontrada");
    }

    private static void AfirmarConsumoConsolidado(
        ConsumoDiario consumo,
        long valor,
        DateTimeOffset creadoEn,
        DateTimeOffset actualizadoEn)
    {
        Assert.Null(consumo.RutaId);
        Assert.Equal(valor, consumo.Peticiones);
        Assert.Equal(valor, consumo.Llamadas);
        Assert.Equal(valor, consumo.BytesEntrada);
        Assert.Equal(valor, consumo.BytesSalida);
        Assert.Equal(valor, consumo.Rechazos401);
        Assert.Equal(valor, consumo.Rechazos403);
        Assert.Equal(valor, consumo.Rechazos404);
        Assert.Equal(valor, consumo.Rechazos429);
        Assert.Equal(valor, consumo.Origen2xx);
        Assert.Equal(valor, consumo.Origen3xx);
        Assert.Equal(valor, consumo.Origen4xx);
        Assert.Equal(valor, consumo.Origen5xx);
        Assert.Equal(valor, consumo.OrigenFallo);
        Assert.All(consumo.HistLatenciaTotal, rango => Assert.Equal((int)valor, rango));
        Assert.All(consumo.HistLatenciaCompuerta, rango => Assert.Equal((int)valor, rango));
        Assert.Equal(valor * 10, consumo.LatenciaTotalSumaMs);
        Assert.Equal(valor * 20, consumo.LatenciaCompuertaSumaMs);
        Assert.InRange(consumo.CreadoEn, creadoEn.AddMilliseconds(-1), creadoEn.AddMilliseconds(1));
        Assert.InRange(consumo.ActualizadoEn, actualizadoEn.AddMilliseconds(-1), actualizadoEn.AddMilliseconds(1));
    }

    private async Task<HttpResponseMessage> Registrar(string subdominio, string urlOrigen, string rol = "Propietario") =>
        await EnviarAutenticado(HttpMethod.Post, "/api/apis", new
        {
            nombre = "API de Cotización de Envíos",
            urlOrigen,
            subdominio,
        }, rol);

    private async Task<Guid> RegistrarYObtenerId(string subdominio)
    {
        using var respuesta = await Registrar(subdominio, "https://8.8.8.8");
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> CargarEspecificacion(Guid apiId, string archivoRelativo)
    {
        var ruta = RaizRepositorio.Ruta(archivoRelativo.Split('/'));
        return await CargarEspecificacion(apiId, Path.GetFileName(ruta), await File.ReadAllTextAsync(ruta));
    }

    private async Task<HttpResponseMessage> CargarEspecificacion(
        Guid apiId,
        string nombre,
        string contenido,
        string rol = "Propietario")
    {
        var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(contenido, Encoding.UTF8), "archivo", nombre);
        return await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/especificacion", multipart, rol);
    }

    private async Task<HttpResponseMessage> SubirLogoPortal(
        Guid apiId,
        string nombre,
        byte[] contenido,
        string contentType,
        string rol = "Propietario")
    {
        var multipart = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        multipart.Add(archivo, "archivo", nombre);
        return await EnviarAutenticado(HttpMethod.Put, $"/api/apis/{apiId}/portal/logo", multipart, rol);
    }

    private async Task<JsonElement> ObtenerRutas(Guid apiId)
    {
        using var respuesta = await EnviarAutenticado(HttpMethod.Get, $"/api/apis/{apiId}/rutas", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> ContarRutas(Guid apiId)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT count(*) FROM ruta WHERE api_id = @api";
        comando.Parameters.AddWithValue("api", apiId);
        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    private async Task<HttpResponseMessage> EnviarAutenticado(
        HttpMethod metodo,
        string ruta,
        object? cuerpo,
        string rol = "Propietario",
        Guid? usuarioId = null)
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Add("X-Prueba-Organizacion", _organizacionId.ToString());
        peticion.Headers.Add("X-Prueba-Rol", rol);
        peticion.Headers.Add("X-Prueba-Usuario", (usuarioId ?? _usuarioId).ToString());
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cuerpo is HttpContent contenido)
        {
            peticion.Content = contenido;
        }
        else if (cuerpo is not null)
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

    private async Task<Guid> CrearUsuario(Guid organizacionId, string correo, bool verificado, string rol)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            WITH usuario_nuevo AS (
                INSERT INTO usuario (id, nombre, correo, hash_contrasena, correo_verificado_en, estado)
                VALUES (gen_random_uuid(), 'Ana Lucía Morales', @correo, 'hash',
                        CASE WHEN @verificado THEN now() ELSE NULL END, 'activo')
                RETURNING id
            )
            INSERT INTO membresia (id, usuario_id, organizacion_id, rol)
            SELECT gen_random_uuid(), id, @organizacion, @rol FROM usuario_nuevo
            RETURNING usuario_id
            """;
        comando.Parameters.AddWithValue("correo", correo);
        comando.Parameters.AddWithValue("verificado", verificado);
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        comando.Parameters.AddWithValue("rol", rol);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task CrearPlanActivo(Guid apiId)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO plan_api
                (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias, cuota_llamadas, limite_minuto, activo)
            VALUES (gen_random_uuid(), @api, 'Básico', 'Plan activo', 0, true, 30, 5000, 60, true)
            """;
        comando.Parameters.AddWithValue("api", apiId);
        await comando.ExecuteNonQueryAsync();
    }

    private async Task HabilitarDominioPropio(Guid organizacionId)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            UPDATE plan_plataforma SET dominio_propio = true
            WHERE id = (SELECT plan_id FROM suscripcion_plataforma
                        WHERE organizacion_id = @organizacion AND estado <> 'finalizada')
            """;
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        await comando.ExecuteNonQueryAsync();
    }

    private async Task AumentarLimiteApis(Guid organizacionId, int maxApis)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            UPDATE plan_plataforma SET max_apis = @max_apis
            WHERE id = (SELECT plan_id FROM suscripcion_plataforma
                        WHERE organizacion_id = @organizacion AND estado <> 'finalizada')
            """;
        comando.Parameters.AddWithValue("max_apis", maxApis);
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        await comando.ExecuteNonQueryAsync();
    }

    private async Task<Guid> InsertarApi(Guid organizacionId, string subdominio)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, secreto_origen_cifrado)
            VALUES (gen_random_uuid(), @organizacion, 'API ajena', @subdominio, 'https://8.8.8.8', 'borrador', 'cifrado')
            RETURNING id
            """;
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        comando.Parameters.AddWithValue("subdominio", subdominio);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task<Guid> InsertarRuta(Guid apiId)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO ruta (id, api_id, metodo, patron, definicion, expuesta, cache_segundos, peso_llamadas)
            VALUES (gen_random_uuid(), @api, 'GET', '/ajena', '{}'::jsonb, true, 0, 1)
            RETURNING id
            """;
        comando.Parameters.AddWithValue("api", apiId);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task MarcarPublicada(Guid apiId)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "UPDATE api SET estado = 'publicada', publicada_en = now() WHERE id = @api";
        comando.Parameters.AddWithValue("api", apiId);
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
