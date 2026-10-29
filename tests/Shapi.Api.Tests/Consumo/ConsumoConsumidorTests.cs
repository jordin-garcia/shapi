extern alias compuerta;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Identidad;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Pagos;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Identidad;
using Shapi.Trabajador.Consolidacion;
using ProgramaCompuerta = compuerta::Program;

namespace Shapi.Api.Tests.Consumo;

[Collection(nameof(RedisCache))]
public sealed class ConsumoConsumidorTests(PostgresPersistencia postgres, RedisCache redis)
    : BaseCache(postgres, redis), IAsyncLifetime
{
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin = Inicio.AddDays(30);
    private WebApplicationFactory<Program>? _fabrica;
    private HttpClient? _cliente;
    private Guid _organizacion;
    private Guid _api;
    private Guid _consumidor;
    private Guid _suscripcion;
    private Guid _ruta;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _organizacion = await NuevaOrganizacion();
        _api = await NuevaApiPublicada(_organizacion);
        _consumidor = await NuevoConsumidor(_organizacion);
        var plan = await NuevoPlanApiCon(_api, "Comercio", 50_000, 120);
        _suscripcion = await NuevaSuscripcionApiEn(_consumidor, _api, plan, "activa", Inicio, Fin);
        _ruta = await NuevaRutaCompleta(_api, "POST", "/guias", true, peso: 5);
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            web.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
            web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
            web.UseSetting("SHAPI_DOMINIO_BASE", DominioBase);
            web.UseSetting("SHAPI_MODO_DEMO", "false");
            web.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton<IReloj>(Reloj);
                servicios.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = AuthHistorialPagosPrueba.Esquema;
                    o.DefaultChallengeScheme = AuthHistorialPagosPrueba.Esquema;
                    o.DefaultForbidScheme = AuthHistorialPagosPrueba.Esquema;
                }).AddScheme<AuthenticationSchemeOptions, AuthHistorialPagosPrueba>(AuthHistorialPagosPrueba.Esquema, _ => { });
            });
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _cliente.DefaultRequestHeaders.Host = "envios.shapi.localhost";
        Identidad($"consumidor:{_consumidor}:{_organizacion}");
    }

    public new async Task DisposeAsync()
    {
        _cliente?.Dispose();
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    [Fact]
    public async Task RF_37_CompuertaReal_TrasConsolidarElPortalCoincideConLaUltimaCabeceraDeCuota()
    {
        const string clave = "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e";
        // Redis aplica EXPIREAT con su reloj real: el ciclo debe seguir vigente también para ese servidor.
        Reloj.Ahora = DateTimeOffset.UtcNow;
        var inicio = Reloj.Ahora.AddDays(-1);
        var fin = inicio.AddDays(30);
        await Ejecutar($"UPDATE suscripcion_api SET inicio = '{inicio:O}', fin = '{fin:O}' WHERE id = '{_suscripcion}'");
        var peticionesOrigen = 0;
        var constructor = WebApplication.CreateSlimBuilder();
        constructor.WebHost.UseUrls("http://127.0.0.1:0");
        await using var origen = constructor.Build();
        origen.Run(http =>
        {
            Interlocked.Increment(ref peticionesOrigen);
            return http.Response.WriteAsync("{\"guia\":\"GT123\"}");
        });
        await origen.StartAsync();
        var puerto = new Uri(origen.Urls.Single()).Port;
        await Ejecutar($"UPDATE api SET url_origen = 'http://localhost:{puerto}' WHERE id = '{_api}'");
        var claveId = await NuevaClaveCon(_suscripcion, clave);
        var planPlataforma = await NuevoPlanPlataformaCon(100_000);
        await NuevaSuscripcionPlataformaEn(_organizacion, planPlataforma, "activa", inicio, fin);
        using (var alcance = Servicios.CreateScope())
        {
            var publicador = Publicador(alcance);
            await publicador.PublicarOrganizacion(_organizacion);
            await publicador.PublicarApi(_api);
            await publicador.PublicarSuscripcion(_suscripcion);
            await publicador.PublicarClave(claveId);
        }

        await using var fabrica = new WebApplicationFactory<ProgramaCompuerta>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
            web.UseSetting("SHAPI_MODO_DEMO", "true");
            web.UseSetting("SHAPI_ORIGENES_PERMITIDOS", $"localhost:{puerto}");
            web.ConfigureTestServices(servicios => servicios.AddSingleton<TimeProvider>(new HoraCompuerta(Reloj.Ahora)));
        });
        fabrica.UseKestrel(opciones => opciones.Listen(IPAddress.Loopback, 0));
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Host = "envios.api.shapi.localhost";
        cliente.DefaultRequestHeaders.Add("X-Api-Key", clave);
        long restante = 0;
        for (var i = 1; i <= 3; i++)
        {
            using var respuesta = await cliente.PostAsync("/guias", new StringContent("{}"));
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("{\"guia\":\"GT123\"}", await respuesta.Content.ReadAsStringAsync());
            restante = long.Parse(respuesta.Headers.GetValues("X-Cuota-Restante").Single(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(50_000 - i * 5, restante);
            Assert.Equal(restante, (await Obtener()).GetProperty("restante").GetInt64());
        }
        // Barrera en la misma conexión de la compuerta: sus métricas FireAndForget ya llegaron antes de consolidar.
        await fabrica.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>().GetDatabase().PingAsync();
        await using var servicios = CrearServicios(ajustar: s => s.AgregarConsolidacion());
        using (var alcance = servicios.CreateScope())
        {
            await alcance.ServiceProvider.GetRequiredService<ConsolidarConsumo>().EjecutarAsync(CancellationToken.None);
        }
        var consumo = await Obtener();
        Assert.Equal(3, peticionesOrigen);
        Assert.Equal(restante, consumo.GetProperty("restante").GetInt64());
        Assert.Equal(15, consumo.GetProperty("llamadas").GetInt64());
        Assert.Equal(15, consumo.GetProperty("llamadasConsolidadas").GetInt64());
        Assert.Equal(3, consumo.GetProperty("peticiones").GetInt64());
        Assert.Equal(_ruta, consumo.GetProperty("rutas")[0].GetProperty("rutaId").GetGuid());
        Assert.Equal(15, consumo.GetProperty("rutas")[0].GetProperty("llamadas").GetInt64());
    }

    private sealed class HoraCompuerta(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    [Fact]
    public async Task RF_37_Consumo_LeeCuotaEnTiempoRealYDetalleConPesoYLlamadasReales()
    {
        await Consumo(_suscripcion, _ruta, "2026-09-01", 8, 30);
        await Consumo(_suscripcion, _ruta, "2026-09-30", 2, 10);
        await Consumo(_suscripcion, null, "2026-09-26", 4, 10);
        await Redis.StringSetAsync(LlaveCuota(), 60);

        var json = await Obtener();

        Assert.Equal(_suscripcion, json.GetProperty("suscripcionId").GetGuid());
        Assert.Equal("2026-09-01", json.GetProperty("inicio").GetString());
        Assert.Equal("2026-09-30", json.GetProperty("fin").GetString());
        Assert.Equal(60, json.GetProperty("llamadas").GetInt64());
        Assert.Equal(50_000, json.GetProperty("cuota").GetInt64());
        Assert.Equal(49_940, json.GetProperty("restante").GetInt64());
        Assert.Equal(0.12m, json.GetProperty("porcentaje").GetDecimal());
        Assert.Equal(14, json.GetProperty("peticiones").GetInt64());
        Assert.Equal(50, json.GetProperty("llamadasConsolidadas").GetInt64());
        var rutas = json.GetProperty("rutas").EnumerateArray().ToArray();
        Assert.Equal(2, rutas.Length);
        Assert.Equal(_ruta, rutas[0].GetProperty("rutaId").GetGuid());
        Assert.Equal("POST", rutas[0].GetProperty("metodo").GetString());
        Assert.Equal("/guias", rutas[0].GetProperty("patron").GetString());
        Assert.Equal(5, rutas[0].GetProperty("pesoLlamadas").GetInt32());
        Assert.Equal(10, rutas[0].GetProperty("peticiones").GetInt64());
        Assert.Equal(40, rutas[0].GetProperty("llamadas").GetInt64());
        Assert.Equal(80m, rutas[0].GetProperty("porcentaje").GetDecimal());
        Assert.Equal("Sin ruta identificada", rutas[1].GetProperty("patron").GetString());
        Assert.Equal(JsonValueKind.Null, rutas[1].GetProperty("pesoLlamadas").ValueKind);
        Assert.Equal(20m, rutas[1].GetProperty("porcentaje").GetDecimal());

        await Redis.StringSetAsync(LlaveCuota(), 65);
        Assert.Equal(65, (await Obtener()).GetProperty("llamadas").GetInt64());
    }

    [Fact]
    public async Task RF_37_Consumo_AislaCicloProduccionSuscripcionYApiSinAceptarParametrosAjenos()
    {
        await Consumo(_suscripcion, _ruta, "2026-09-01", 1, 5);
        await Consumo(_suscripcion, _ruta, "2026-08-31", 100, 500);
        await Consumo(_suscripcion, _ruta, "2026-10-01", 100, 500);
        await Consumo(_suscripcion, _ruta, "2026-09-01", 100, 100, "pruebas");
        var otro = await NuevoConsumidor(_organizacion);
        var plan = await NuevoPlanApi(_api);
        var suscripcionAjena = await NuevaSuscripcionApi(otro, _api, plan);
        await Consumo(suscripcionAjena, _ruta, "2026-09-01", 100, 500);
        var otraApi = await NuevaApiPublicada(_organizacion, "agro");
        var otroPlan = await NuevoPlanApi(otraApi);
        var otraSuscripcion = await NuevaSuscripcionApi(_consumidor, otraApi, otroPlan);
        await Consumo(otraSuscripcion, null, "2026-09-01", 100, 500, api: otraApi);
        await Redis.StringSetAsync(LlavesRedis.CuotaSuscripcion(_suscripcion, Inicio.AddDays(-30).ToUnixTimeSeconds()), 999);

        var json = await Obtener($"?suscripcionId={suscripcionAjena}&apiId={otraApi}&desde=2026-01-01");

        Assert.Equal(1, json.GetProperty("peticiones").GetInt64());
        Assert.Equal(5, json.GetProperty("llamadasConsolidadas").GetInt64());
        Assert.Equal(0, json.GetProperty("llamadas").GetInt64());
    }

    [Fact]
    public async Task RF_37_Consumo_SinUsoDevuelveCuotaCompletaYDetalleVacio()
    {
        var json = await Obtener();
        Assert.Equal(0, json.GetProperty("llamadas").GetInt64());
        Assert.Equal(50_000, json.GetProperty("restante").GetInt64());
        Assert.Equal(0m, json.GetProperty("porcentaje").GetDecimal());
        Assert.Empty(json.GetProperty("rutas").EnumerateArray());
    }

    [Fact]
    public async Task RF_37_Consumo_PeticionesSinDescuentoNoDividenPorCero()
    {
        await Consumo(_suscripcion, _ruta, "2026-09-26", 3, 0);
        var json = await Obtener();
        Assert.Equal(3, json.GetProperty("peticiones").GetInt64());
        Assert.Equal(0m, json.GetProperty("rutas")[0].GetProperty("porcentaje").GetDecimal());
    }

    [Fact]
    public async Task RF_37_Consumo_CuotaExcedidaNoDevuelveRestanteNegativo()
    {
        await Redis.StringSetAsync(LlaveCuota(), 50_005);
        var json = await Obtener();
        Assert.Equal(0, json.GetProperty("restante").GetInt64());
        Assert.Equal(100.01m, json.GetProperty("porcentaje").GetDecimal());
    }

    [Theory]
    [InlineData("activa")]
    [InlineData("en_gracia")]
    [InlineData("suspendida")]
    public async Task RF_37_Consumo_ConservaElCicloHastaLaRenovacionIncluidaLaGracia(string estado)
    {
        await Ejecutar($"UPDATE suscripcion_api SET estado = '{estado}' WHERE id = '{_suscripcion}'");
        Reloj.Ahora = new DateTimeOffset(2026, 10, 3, 2, 0, 0, TimeSpan.Zero);
        await Consumo(_suscripcion, _ruta, "2026-10-02", 1, 5);
        await Consumo(_suscripcion, _ruta, "2026-10-03", 100, 500);
        await Redis.StringSetAsync(LlaveCuota(), 5);
        var json = await Obtener();
        Assert.Equal("2026-09-30", json.GetProperty("fin").GetString());
        Assert.Equal(5, json.GetProperty("llamadasConsolidadas").GetInt64());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RF_37_Consumo_SinSuscripcionOFinalizadaDevuelve404Identificable(bool finalizada)
    {
        if (finalizada)
        {
            await Ejecutar($"UPDATE suscripcion_api SET estado = 'finalizada' WHERE id = '{_suscripcion}'");
        }
        else
        {
            Identidad($"consumidor:{await NuevoConsumidor(_organizacion)}:{_organizacion}");
        }
        using var respuesta = await _cliente!.GetAsync("/api/portal/consumo");
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("suscripcion_no_encontrada", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task RNF_08_Consumo_HostDeOtraOrganizacionOInexistenteDevuelve404()
    {
        await NuevaApiPublicada(await NuevaOrganizacion(), "ajena");
        foreach (var host in new[] { "ajena.shapi.localhost", "inexistente.shapi.localhost", "shapi.localhost" })
        {
            _cliente!.DefaultRequestHeaders.Host = host;
            using var respuesta = await _cliente.GetAsync("/api/portal/consumo");
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Equal("api_no_encontrada", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
        }
    }

    [Fact]
    public async Task RF_37_Consumo_ApiDespublicadaDevuelve404()
    {
        await Ejecutar($"UPDATE api SET estado = 'borrador' WHERE id = '{_api}'");
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente!.GetAsync("/api/portal/consumo")).StatusCode);
    }

    [Fact]
    public async Task RF_37_Consumo_SinSesionDevuelve401()
    {
        Identidad(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente!.GetAsync("/api/portal/consumo")).StatusCode);
    }

    [Fact]
    public async Task RF_37_Consumo_CookieRealDelPortalAutorizaSoloSuHost()
    {
        var token = Guid.NewGuid().ToString("N");
        await using (var db = CrearDb())
        {
            db.Add(Sesion.IniciarConsumidor(SeguridadTokens.HashearToken(token), _consumidor,
                "envios.shapi.localhost", null, null, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        await using var fabricaReal = _fabrica!.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
            servicios.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = EsquemaAutenticacionPortal.Esquema;
                o.DefaultChallengeScheme = EsquemaAutenticacionPortal.Esquema;
                o.DefaultForbidScheme = EsquemaAutenticacionPortal.Esquema;
            })));
        using var clienteReal = fabricaReal.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        clienteReal.DefaultRequestHeaders.Host = "envios.shapi.localhost";
        clienteReal.DefaultRequestHeaders.Add("Cookie", $"portal_sesion={token}");
        using var respuesta = await clienteReal.GetAsync("/api/portal/consumo");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(_suscripcion, (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("suscripcionId").GetGuid());
        await NuevaApiPublicada(_organizacion, "agro");
        clienteReal.DefaultRequestHeaders.Host = "agro.shapi.localhost";
        Assert.Equal(HttpStatusCode.Unauthorized, (await clienteReal.GetAsync("/api/portal/consumo")).StatusCode);
    }

    [Theory]
    [InlineData("Propietario")]
    [InlineData("Editor")]
    [InlineData("Lector")]
    [InlineData("Administrador")]
    [InlineData("Soporte")]
    public async Task RF_37_Consumo_SesionPersonalDevuelve403(string rol)
    {
        Identidad($"personal:{rol}:{_organizacion}");
        Assert.Equal(HttpStatusCode.Forbidden, (await _cliente!.GetAsync("/api/portal/consumo")).StatusCode);
    }

    [Theory]
    [InlineData("texto")]
    [InlineData("-1")]
    [InlineData("tipo_incorrecto")]
    public async Task RF_37_Consumo_ContadorInvalidoNoSePresentaComoCero(string valor)
    {
        if (valor == "tipo_incorrecto")
        {
            await Redis.HashSetAsync(LlaveCuota(), "campo", "valor");
        }
        else
        {
            await Redis.StringSetAsync(LlaveCuota(), valor);
        }
        using var respuesta = await _cliente!.GetAsync("/api/portal/consumo");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("servicio_no_disponible", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
    }

    private void Identidad(string? identidad)
    {
        _cliente!.DefaultRequestHeaders.Remove("X-Prueba-Identidad");
        if (identidad is not null)
        {
            _cliente.DefaultRequestHeaders.Add("X-Prueba-Identidad", identidad);
        }
    }

    private string LlaveCuota() => LlavesRedis.CuotaSuscripcion(_suscripcion, Inicio.ToUnixTimeSeconds());

    private async Task<JsonElement> Obtener(string query = "")
    {
        using var respuesta = await _cliente!.GetAsync($"/api/portal/consumo{query}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl?.NoStore);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private Task Consumo(Guid suscripcion, Guid? ruta, string fecha, int peticiones, int llamadas,
        string entorno = "produccion", Guid? api = null) => Ejecutar($"""
        INSERT INTO consumo_diario (fecha, api_id, ruta_id, suscripcion_id, entorno, peticiones, llamadas,
            hist_latencia_total, hist_latencia_compuerta, latencia_total_suma_ms, latencia_compuerta_suma_ms)
        VALUES ('{fecha}', '{api ?? _api}', {ValorONulo(ruta)}, '{suscripcion}', '{entorno}', {peticiones}, {llamadas},
            array_fill(0, ARRAY[10]), array_fill(0, ARRAY[10]), 0, 0)
        """);
}
