using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
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
using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Red;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Consumo;
using Shapi.Infraestructura.Persistencia;
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
    public Task PublicarApi(Guid apiId, CancellationToken cancelacion = default) { ApisPublicadas.Add(apiId); return Task.CompletedTask; }
    public Task EliminarHost(string host, Guid apiId, CancellationToken cancelacion = default) => Task.CompletedTask;
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
                services.RemoveAll<IPublicadorCache>();
                services.AddSingleton<PublicadorCacheApisFalso>();
                services.AddSingleton<IPublicadorCache>(sp => sp.GetRequiredService<PublicadorCacheApisFalso>());
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

    private async Task<HttpResponseMessage> EnviarAutenticado(HttpMethod metodo, string ruta, object? cuerpo, string rol = "Propietario")
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Add("X-Prueba-Organizacion", _organizacionId.ToString());
        peticion.Headers.Add("X-Prueba-Rol", rol);
        peticion.Headers.Add("X-Prueba-Usuario", Guid.NewGuid().ToString());
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
