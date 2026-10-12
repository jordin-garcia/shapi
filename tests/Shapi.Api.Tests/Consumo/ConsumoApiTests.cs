using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Shapi.Api.Tests.Apis;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;

namespace Shapi.Api.Tests.Consumo;

public sealed class ConsumoApiTests(ContenedorPostgresApis postgres) : IClassFixture<ContenedorPostgresApis>, IAsyncLifetime
{
    private string _cadena = null!;
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private Guid _organizacion;
    private Guid _api;
    private Guid _ruta;

    public async Task InitializeAsync()
    {
        _cadena = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
        { Database = $"consumo_{Guid.NewGuid():N}" }.ConnectionString;
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
                services.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = AutenticacionApisPrueba.Esquema;
                    o.DefaultChallengeScheme = AutenticacionApisPrueba.Esquema;
                    o.DefaultForbidScheme = AutenticacionApisPrueba.Esquema;
                }).AddScheme<AuthenticationSchemeOptions, AutenticacionApisPrueba>(AutenticacionApisPrueba.Esquema, _ => { });
                services.RemoveAll<IReloj>();
                services.AddSingleton<IReloj>(new RelojConsumo());
            });
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        await _cliente.GetAsync("/salud");
        _organizacion = Guid.NewGuid();
        _api = Guid.NewGuid();
        _ruta = Guid.NewGuid();
        await Sql("""
            INSERT INTO organizacion (id, nombre, tipo, estado_admin) VALUES (@org, 'Envíos Xelajú', 'proveedor', 'activa');
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, secreto_origen_cifrado)
            VALUES (@api, @org, 'API de Cotización de Envíos', 'envios', 'https://8.8.8.8', 'borrador', 'cifrado');
            INSERT INTO ruta (id, api_id, metodo, patron, definicion, expuesta, cache_segundos, peso_llamadas)
            VALUES (@ruta, @api, 'POST', '/cotizaciones', '{}'::jsonb, true, 0, 2);
            """);
        _cliente.DefaultRequestHeaders.Add("X-Prueba-Organizacion", _organizacion.ToString());
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    [Fact]
    public async Task RF_35_Consumo_AgregaDiasRutasEntornosHistogramasBytesYErrores()
    {
        await Insertar("2026-10-01", true, "produccion", 100, 0);
        await Insertar("2026-10-03", true, "pruebas", 100, 1);
        await Insertar("2026-10-03", false, "produccion", 50, 0);
        await Insertar("2026-09-30", true, "produccion", 1000, 9);
        await Insertar("2026-10-04", true, "produccion", 1000, 9);
        var json = await Obtener("?desde=2026-10-01&hasta=2026-10-03");
        Assert.Equal(250, json.GetProperty("peticiones").GetInt64());
        Assert.Equal(500, json.GetProperty("llamadas").GetInt64());
        Assert.Equal(750, json.GetProperty("bytesEntrada").GetInt64());
        Assert.Equal(1000, json.GetProperty("bytesSalida").GetInt64());
        Assert.Equal(9.375m, json.GetProperty("latenciaTotalP95").GetProperty("milisegundos").GetDecimal());
        Assert.Equal(4.75m, json.GetProperty("latenciaCompuertaP95").GetProperty("milisegundos").GetDecimal());
        var dias = json.GetProperty("dias").EnumerateArray().ToArray();
        Assert.Equal(new long[] { 100, 0, 150 }, dias.Select(x => x.GetProperty("peticiones").GetInt64()));
        var rutas = json.GetProperty("rutas").EnumerateArray().ToArray();
        Assert.Equal(_ruta, rutas[0].GetProperty("rutaId").GetGuid());
        Assert.Equal("/cotizaciones", rutas[0].GetProperty("patron").GetString());
        Assert.Equal("POST", rutas[0].GetProperty("metodo").GetString());
        Assert.Equal(80m, rutas[0].GetProperty("porcentaje").GetDecimal());
        Assert.Equal(9.5m, rutas[0].GetProperty("latenciaTotalP95").GetProperty("milisegundos").GetDecimal());
        Assert.Equal(JsonValueKind.Null, rutas[1].GetProperty("rutaId").ValueKind);
        Assert.Equal(20m, rutas[1].GetProperty("porcentaje").GetDecimal());
        Assert.Equal(new[] { "401", "403", "429" }, json.GetProperty("rechazosCompuerta").EnumerateArray().Select(x => x.GetProperty("codigo").GetString()));
        Assert.Equal(new[] { "4xx", "5xx", "fallas" }, json.GetProperty("erroresOrigen").EnumerateArray().Select(x => x.GetProperty("codigo").GetString()));
        foreach (var error in json.GetProperty("rechazosCompuerta").EnumerateArray().Concat(json.GetProperty("erroresOrigen").EnumerateArray()))
        {
            Assert.Equal(3, error.GetProperty("peticiones").GetInt64());
            Assert.Equal(1.2m, error.GetProperty("porcentaje").GetDecimal());
        }
    }

    [Fact]
    public async Task RF_35_Consumo_SinDatosDevuelveCerosSinPercentilNiRutas()
    {
        var json = await Obtener();
        Assert.Equal(0, json.GetProperty("peticiones").GetInt64());
        Assert.Equal(0, json.GetProperty("llamadas").GetInt64());
        Assert.Equal(0, json.GetProperty("bytesEntrada").GetInt64());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("latenciaTotalP95").GetProperty("milisegundos").ValueKind);
        Assert.False(json.GetProperty("latenciaTotalP95").GetProperty("supera2500").GetBoolean());
        Assert.Empty(json.GetProperty("rutas").EnumerateArray());
        Assert.All(json.GetProperty("rechazosCompuerta").EnumerateArray(), x => Assert.Equal(0m, x.GetProperty("porcentaje").GetDecimal()));
    }

    [Fact]
    public async Task RF_35_Consumo_PorDefectoDiezDiasUsaElDiaDeGuatemala()
    {
        // El reloj es 12 oct 02:00 UTC: en Guatemala todavía es 11 oct.
        var json = await Obtener();
        Assert.Equal("2026-10-02", json.GetProperty("desde").GetString());
        Assert.Equal("2026-10-11", json.GetProperty("hasta").GetString());
        Assert.Equal(10, json.GetProperty("dias").GetArrayLength());
        var fin = await Obtener("?hasta=2026-09-10");
        Assert.Equal("2026-09-01", fin.GetProperty("desde").GetString());
    }

    [Fact]
    public async Task RF_35_Consumo_RangoAbiertoSeReportaMayorQue2500()
    {
        await Insertar("2026-10-11", true, "produccion", 100, 9);
        var json = await Obtener();
        Assert.True(json.GetProperty("latenciaTotalP95").GetProperty("supera2500").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("latenciaTotalP95").GetProperty("milisegundos").ValueKind);
    }

    [Theory]
    [InlineData("Propietario")]
    [InlineData("Editor")]
    [InlineData("Lector")]
    public async Task RF_35_Consumo_PersonalDeLaOrganizacionPuedeConsultar(string rol)
    {
        _cliente.DefaultRequestHeaders.Add("X-Prueba-Rol", rol);
        await Obtener();
    }

    [Theory]
    [InlineData("Consumidor")]
    [InlineData("Administrador")]
    [InlineData("Soporte")]
    public async Task RF_35_Consumo_OtrosRolesReciben403(string rol)
    {
        _cliente.DefaultRequestHeaders.Add("X-Prueba-Rol", rol);
        Assert.Equal(HttpStatusCode.Forbidden, (await _cliente.GetAsync($"/api/apis/{_api}/consumo")).StatusCode);
    }

    [Fact]
    public async Task RF_35_Consumo_SinSesionRecibe401()
    {
        _cliente.DefaultRequestHeaders.Clear();
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync($"/api/apis/{_api}/consumo")).StatusCode);
    }

    [Fact]
    public async Task RNF_08_Consumo_OtraOrganizacionOApiInexistenteRecibe404()
    {
        _cliente.DefaultRequestHeaders.Remove("X-Prueba-Organizacion");
        _cliente.DefaultRequestHeaders.Add("X-Prueba-Organizacion", Guid.NewGuid().ToString());
        foreach (var id in new[] { _api, Guid.NewGuid() })
        {
            var respuesta = await _cliente.GetAsync($"/api/apis/{id}/consumo");
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("api_no_encontrada", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
        }
    }

    [Theory]
    [InlineData("?desde=no-es-fecha", "desde")]
    [InlineData("?hasta=2026-02-30", "hasta")]
    [InlineData("?desde=2026-10-10&hasta=2026-10-01", "desde")]
    [InlineData("?desde=2020-01-01&hasta=2026-10-11", "desde")]
    [InlineData("?hasta=0001-01-01", "hasta")]
    [InlineData("?desde=2026-10-01&desde=2026-10-02", "desde")]
    public async Task RF_35_Consumo_PeriodoInvalidoDevuelve400PorCampo(string query, string campo)
    {
        var respuesta = await _cliente.GetAsync($"/api/apis/{_api}/consumo{query}");
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("datos_invalidos", json.GetProperty("codigo").GetString());
        Assert.True(json.GetProperty("errores").TryGetProperty(campo, out _));
    }

    private async Task<JsonElement> Obtener(string query = "")
    {
        var respuesta = await _cliente.GetAsync($"/api/apis/{_api}/consumo{query}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private Task Insertar(string fecha, bool ruta, string entorno, int peticiones, int rango) => Sql($"""
        INSERT INTO consumo_diario (fecha, api_id, ruta_id, entorno, peticiones, llamadas, bytes_entrada, bytes_salida,
            rechazos_401, rechazos_403, rechazos_429, origen_4xx, origen_5xx, origen_fallo, hist_latencia_total, hist_latencia_compuerta,
            latencia_total_suma_ms, latencia_compuerta_suma_ms, actualizado_en)
        VALUES ('{fecha}', @api, {(ruta ? "@ruta" : "NULL")}, '{entorno}', {peticiones}, {peticiones * 2}, {peticiones * 3}, {peticiones * 4},
            1, 1, 1, 1, 1, 1, ARRAY[{string.Join(',', Enumerable.Range(0, 10).Select(i => i == rango ? peticiones : 0))}],
            ARRAY[{peticiones},0,0,0,0,0,0,0,0,0], 0, 0, now());
        """);

    private async Task Sql(string sql)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("org", _organizacion);
        comando.Parameters.AddWithValue("api", _api);
        comando.Parameters.AddWithValue("ruta", _ruta);
        await comando.ExecuteNonQueryAsync();
    }

    private sealed class RelojConsumo : IReloj
    {
        public DateTimeOffset Ahora => new(2026, 10, 12, 2, 0, 0, TimeSpan.Zero);
    }
}
