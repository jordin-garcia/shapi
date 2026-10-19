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

public sealed class ConsumoConsumidoresTests(ContenedorPostgresApis postgres) : IClassFixture<ContenedorPostgresApis>, IAsyncLifetime
{
    private string _cadena = null!;
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private readonly Guid _organizacion = Guid.NewGuid();
    private readonly Guid _api = Guid.NewGuid();
    private readonly Guid _plan = Guid.NewGuid();
    private readonly Guid _consumidor = Guid.NewGuid();
    private readonly Guid _suscripcion = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _cadena = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
        { Database = $"consumidores_{Guid.NewGuid():N}" }.ConnectionString;
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
                services.AddSingleton<IReloj>(new RelojConsumidores());
            });
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        await _cliente.GetAsync("/salud");
        await Sql("""
            INSERT INTO organizacion (id, nombre, tipo, estado_admin) VALUES (@org, 'Envíos', 'proveedor', 'activa');
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, secreto_origen_cifrado)
            VALUES (@api, @org, 'API de Cotización de Envíos', 'envios', 'https://8.8.8.8', 'borrador', 'cifrado');
            INSERT INTO plan_api (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias, cuota_llamadas, limite_minuto)
            VALUES (@plan, @api, 'Comercio', 'Plan', 999, false, 30, 50000, 100);
            INSERT INTO consumidor (id, organizacion_id, nombre, nombre_empresa, correo, hash_contrasena, estado)
            VALUES (@consumidor, @org, 'María', 'Mercadito Antigua', 'maria@mercado.test', 'hash', 'activo');
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
    public async Task RF_36_Consumidores_SumaSoloProduccionDelCicloYPagosAutorizadosDelPeriodo()
    {
        await Suscripcion();
        await Sql("""
            INSERT INTO consumo_diario (fecha, api_id, suscripcion_id, entorno, llamadas, peticiones, hist_latencia_total, hist_latencia_compuerta, actualizado_en)
            VALUES ('2026-10-01', @api, @susc, 'produccion', 31000, 100, array_fill(0::bigint, ARRAY[10]), array_fill(0::bigint, ARRAY[10]), now()),
            ('2026-10-02', @api, @susc, 'produccion', 480, 10, array_fill(0::bigint, ARRAY[10]), array_fill(0::bigint, ARRAY[10]), now()),
            ('2026-10-01', @api, @susc, 'pruebas', 80000, 1, array_fill(0::bigint, ARRAY[10]), array_fill(0::bigint, ARRAY[10]), now()),
            ('2026-09-30', @api, @susc, 'produccion', 90000, 1, array_fill(0::bigint, ARRAY[10]), array_fill(0::bigint, ARRAY[10]), now()),
            ('2026-10-31', @api, @susc, 'produccion', 70000, 1, array_fill(0::bigint, ARRAY[10]), array_fill(0::bigint, ARRAY[10]), now());
            """);
        await Pago(400, "autorizado", "2026-10-01T06:00:00Z", "2026-10-31T06:00:00Z", "2026-10-01T06:00:00Z");
        await Pago(50, "autorizado", "2026-10-01T06:00:00Z", "2026-10-31T06:00:00Z", "2026-10-02T06:00:00Z");
        await Pago(600, "autorizado", "2026-09-01T06:00:00Z", "2026-10-01T06:00:00Z", "2026-09-01T06:00:00Z");
        await Pago(999, "rechazado", null, null, "2026-10-03T06:00:00Z");
        var json = await Obtener();
        var fila = Assert.Single(json.GetProperty("elementos").EnumerateArray());
        Assert.Equal("Mercadito Antigua", fila.GetProperty("consumidor").GetString());
        Assert.Equal(_consumidor, fila.GetProperty("consumidorId").GetGuid());
        Assert.Equal(_suscripcion, fila.GetProperty("suscripcionId").GetGuid());
        Assert.Equal("Comercio", fila.GetProperty("plan").GetString());
        Assert.Equal("2026-10-01", fila.GetProperty("inicio").GetString());
        Assert.Equal("2026-10-30", fila.GetProperty("fin").GetString());
        Assert.Equal(31480, fila.GetProperty("llamadas").GetInt64());
        Assert.Equal(50000, fila.GetProperty("cuota").GetInt64());
        Assert.Equal(62.96m, fila.GetProperty("porcentaje").GetDecimal());
        Assert.Equal(450m, fila.GetProperty("facturacion").GetDecimal());
        Assert.Equal("rechazado", fila.GetProperty("ultimoCobro").GetString());
        Assert.Equal("activa", fila.GetProperty("estadoSuscripcion").GetString());
        Assert.Equal("GTQ", json.GetProperty("moneda").GetString());
        Assert.Equal("API de Cotización de Envíos", json.GetProperty("apiNombre").GetString());
        Assert.Equal(1, json.GetProperty("total").GetInt32());
        Assert.Equal(1, json.GetProperty("activas").GetInt32());
        Assert.Equal(31480, json.GetProperty("llamadas").GetInt64());
        Assert.Equal(50000, json.GetProperty("cuota").GetInt64());
        Assert.Equal(450m, json.GetProperty("facturacion").GetDecimal());
    }

    [Theory]
    [InlineData("activa", 1)]
    [InlineData("en_gracia", 0)]
    [InlineData("suspendida", 0)]
    public async Task RF_36_Consumidores_IncluyeTodaSuscripcionNoFinalizadaSinInventarCobros(string estado, int activas)
    {
        await Suscripcion(estado);
        var json = await Obtener();
        var fila = Assert.Single(json.GetProperty("elementos").EnumerateArray());
        Assert.Equal(estado, fila.GetProperty("estadoSuscripcion").GetString());
        Assert.Equal(JsonValueKind.Null, fila.GetProperty("ultimoCobro").ValueKind);
        Assert.Equal(0, fila.GetProperty("llamadas").GetInt64());
        Assert.Equal(0, fila.GetProperty("porcentaje").GetDecimal());
        Assert.Equal(0, fila.GetProperty("facturacion").GetDecimal());
        Assert.Equal(activas, json.GetProperty("activas").GetInt32());
    }

    [Fact]
    public async Task RF_36_Consumidores_SinSuscripcionesOFinalizadasDevuelveListaYTotalesVacios()
    {
        await Suscripcion("finalizada");
        var json = await Obtener();
        Assert.Empty(json.GetProperty("elementos").EnumerateArray());
        foreach (var campo in new[] { "total", "activas", "llamadas", "cuota", "facturacion" })
        {
            Assert.Equal(0, json.GetProperty(campo).GetDecimal());
        }
    }

    [Fact]
    public async Task RF_36_Consumidores_GraciaConservaConsumoTrasFinHastaHoyDeGuatemala()
    {
        await Suscripcion("en_gracia");
        await Sql("""
            UPDATE suscripcion_api SET inicio = '2026-09-11T06:00:00Z', fin = '2026-10-11T06:00:00Z' WHERE id = @susc;
            INSERT INTO consumo_diario (fecha, api_id, suscripcion_id, entorno, llamadas, hist_latencia_total, hist_latencia_compuerta, actualizado_en)
            VALUES ('2026-10-11', @api, @susc, 'produccion', 100, array_fill(0::bigint, ARRAY[10]), array_fill(0::bigint, ARRAY[10]), now()),
            ('2026-10-12', @api, @susc, 'produccion', 200, array_fill(0::bigint, ARRAY[10]), array_fill(0::bigint, ARRAY[10]), now());
            """);
        var fila = Assert.Single((await Obtener()).GetProperty("elementos").EnumerateArray());
        Assert.Equal(100, fila.GetProperty("llamadas").GetInt64());
        Assert.Equal("2026-10-10", fila.GetProperty("fin").GetString());
    }

    [Theory]
    [InlineData("Propietario")]
    [InlineData("Editor")]
    [InlineData("Lector")]
    public async Task RF_36_Consumidores_PersonalAutorizadoPuedeConsultar(string rol)
    {
        _cliente.DefaultRequestHeaders.Add("X-Prueba-Rol", rol);
        await Obtener();
    }

    [Theory]
    [InlineData("Consumidor")]
    [InlineData("Administrador")]
    [InlineData("Soporte")]
    public async Task RF_36_Consumidores_OtrosRolesReciben403(string rol)
    {
        _cliente.DefaultRequestHeaders.Add("X-Prueba-Rol", rol);
        Assert.Equal(HttpStatusCode.Forbidden, (await _cliente.GetAsync($"/api/apis/{_api}/consumidores")).StatusCode);
    }

    [Fact]
    public async Task RF_36_Consumidores_SinSesionRecibe401()
    {
        _cliente.DefaultRequestHeaders.Clear();
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync($"/api/apis/{_api}/consumidores")).StatusCode);
    }

    [Fact]
    public async Task RNF_08_Consumidores_OtraOrganizacionOApiInexistenteRecibe404()
    {
        await Suscripcion();
        _cliente.DefaultRequestHeaders.Remove("X-Prueba-Organizacion");
        _cliente.DefaultRequestHeaders.Add("X-Prueba-Organizacion", Guid.NewGuid().ToString());
        foreach (var id in new[] { _api, Guid.NewGuid() })
        {
            var respuesta = await _cliente.GetAsync($"/api/apis/{id}/consumidores");
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("api_no_encontrada", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
        }
    }

    private Task Suscripcion(string estado = "activa") => Sql($"""
        INSERT INTO suscripcion_api (id, consumidor_id, api_id, plan_id, estado, inicio, fin)
        VALUES (@susc, @consumidor, @api, @plan, '{estado}', '2026-10-01T06:00:00Z', '2026-10-31T06:00:00Z');
        """);

    private Task Pago(decimal monto, string estado, string? inicio, string? fin, string creado) => Sql($"""
        INSERT INTO pago (id, suscripcion_api_id, concepto, descripcion, monto, estado, periodo_inicio, periodo_fin, creado_en, actualizado_en)
        VALUES ('{Guid.NewGuid()}', @susc, 'cambio_plan', 'Cobro', {monto}, '{estado}',
        {(inicio is null ? "NULL" : $"'{inicio}'")}, {(fin is null ? "NULL" : $"'{fin}'")}, '{creado}', '{creado}');
        """);

    private async Task<JsonElement> Obtener()
    {
        var respuesta = await _cliente.GetAsync($"/api/apis/{_api}/consumidores");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("no-store", respuesta.Headers.CacheControl?.ToString());
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task Sql(string sql)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("org", _organizacion);
        comando.Parameters.AddWithValue("api", _api);
        comando.Parameters.AddWithValue("plan", _plan);
        comando.Parameters.AddWithValue("consumidor", _consumidor);
        comando.Parameters.AddWithValue("susc", _suscripcion);
        await comando.ExecuteNonQueryAsync();
    }

    private sealed class RelojConsumidores : IReloj
    {
        public DateTimeOffset Ahora => new(2026, 10, 12, 2, 0, 0, TimeSpan.Zero);
    }
}
