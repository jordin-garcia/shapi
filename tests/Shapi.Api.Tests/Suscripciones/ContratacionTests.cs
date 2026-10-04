using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Claves;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Claves;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Tests.Suscripciones;

[Collection(nameof(RedisCache))]
public sealed class ContratacionTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis), IAsyncLifetime
{
    private const string HostPortal = "envios.shapi.localhost";
    private WebApplicationFactory<Program>? _fabrica;
    private HttpClient? _cliente;

    private WebApplicationFactory<Program> Fabrica => _fabrica ??= new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
    {
        web.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        web.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
        web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
        web.UseSetting("SHAPI_DOMINIO_BASE", DominioBase);
        web.UseSetting("Pagos:DemoraMs", "0");
        web.ConfigureTestServices(servicios =>
        {
            servicios.AddSingleton<IReloj>(Reloj);
            SesionConsumidorDePrueba.Registrar(servicios);
        });
    });

    private HttpClient Cliente => _cliente ??= Fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    public new async Task DisposeAsync()
    {
        _cliente?.Dispose();
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    // RF-20, RF-26: paga, activa la suscripción y publica las claves en Redis.
    [Fact]
    public async Task Contratar_PagoAprobado_GuardaSuscripcionPagoMedioYPublicaClaves()
    {
        var e = await CrearEscenario(verificado: true);
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, Tarjeta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("claves").GetArrayLength().Should().Be(2);
        var id = json.GetProperty("suscripcion").GetProperty("id").GetGuid();
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_api WHERE id = '{id}' AND estado = 'activa'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_api_id = '{id}' AND estado = 'autorizado' AND concepto = 'contratacion'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM medio_pago WHERE consumidor_id = '{e.ConsumidorId}' AND token_pasarela LIKE 'tok_sim_%'")).Should().Be(1);
        foreach (var clave in json.GetProperty("claves").EnumerateArray())
        {
            var valor = clave.GetProperty("clave").GetString()!;
            (await Redis.HashGetAllAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(valor)))).Should().NotBeEmpty();
        }
    }

    // RF-20: el año de tarjeta con dos dígitos se normaliza antes de cobrar.
    [Fact]
    public async Task Contratar_AnioDeTarjetaConDosDigitos_SeGuardaNormalizado()
    {
        var e = await CrearEscenario(verificado: true);
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, Tarjeta with { AnioVencimiento = "28" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        (await Escalar<long>($"SELECT count(*) FROM medio_pago WHERE consumidor_id = '{e.ConsumidorId}' AND anio_vencimiento = 2028")).Should().Be(1);
    }

    // RF-20: un rechazo queda registrado sin crear una suscripción.
    [Fact]
    public async Task Contratar_TarjetaRechazada_RegistraPagoSinSuscripcion()
    {
        var e = await CrearEscenario(verificado: true);
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, Tarjeta with { Numero = "4000000000000002" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("codigo").GetString().Should().Be("pago_rechazado");
        problema.GetProperty("detalle").GetProperty("motivo").GetString().Should().Be("fondos_insuficientes");
        (await Escalar<long>("SELECT count(*) FROM suscripcion_api")).Should().Be(0);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE consumidor_id = '{e.ConsumidorId}' AND api_id = '{e.ApiId}' AND estado = 'rechazado'")).Should().Be(1);
    }

    // RF-20: un plan gratuito no requiere tarjeta ni crea pago.
    [Fact]
    public async Task Contratar_PlanGratuito_ActivaSinTarjetaNiPago()
    {
        var e = await CrearEscenario(verificado: true, gratuito: true);
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("suscripcion").GetProperty("estado").GetString().Should().Be("activa");
        json.GetProperty("claves").GetArrayLength().Should().Be(2);
        (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM medio_pago")).Should().Be(0);
    }

    // RF-20: el correo debe estar verificado.
    [Fact]
    public async Task Contratar_CorreoSinVerificar_Responde422()
    {
        var e = await CrearEscenario(verificado: false);
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId });
        await AfirmarProblema(respuesta, HttpStatusCode.UnprocessableEntity, "correo_no_verificado");
    }

    // RF-20: una suscripción existente se cambia de plan mediante el flujo correspondiente.
    [Fact]
    public async Task Contratar_ConSuscripcionVigente_Responde409()
    {
        var e = await CrearEscenario(verificado: true);
        await NuevaSuscripcionApiEn(e.ConsumidorId, e.ApiId, e.PlanId, "activa", Reloj.Ahora.AddDays(-1), Reloj.Ahora.AddDays(29));
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, Tarjeta);
        await AfirmarProblema(respuesta, HttpStatusCode.Conflict, "suscripcion_existente");
    }

    // RF-20: un plan inactivo no se puede contratar y no genera cobro.
    [Fact]
    public async Task Contratar_PlanInactivo_Responde422SinCobrar()
    {
        var e = await CrearEscenario(verificado: true);
        await Ejecutar($"UPDATE plan_api SET activo = false WHERE id = '{e.PlanId}'");
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, Tarjeta);
        await AfirmarProblema(respuesta, HttpStatusCode.UnprocessableEntity, "plan_no_encontrado");
        (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(0);
    }

    // RF-21: el consumidor consulta el periodo, renovación, límites y tarjeta enmascarada.
    [Fact]
    public async Task ObtenerSuscripcion_DevuelvePeriodoMostradoProximaRenovacionYLimites()
    {
        var e = await CrearEscenario(verificado: true);
        var medio = await Escalar<Guid>($"""
            INSERT INTO medio_pago (id, consumidor_id, token_pasarela, marca, ultimos4, titular, mes_vencimiento, anio_vencimiento)
            VALUES (gen_random_uuid(), '{e.ConsumidorId}', 'tok_sim_prueba', 'Visa', '4242', 'María Quiñónez', 12, 2030) RETURNING id
            """);
        await Ejecutar($"""
            INSERT INTO suscripcion_api (id, consumidor_id, api_id, plan_id, medio_pago_id, estado, inicio, fin)
            VALUES (gen_random_uuid(), '{e.ConsumidorId}', '{e.ApiId}', '{e.PlanId}', '{medio}', 'activa',
                    '{Reloj.Ahora.AddDays(-1):O}', '{Reloj.Ahora.AddDays(29):O}')
            """);
        using var respuesta = await Portal(HttpMethod.Get, "/api/portal/suscripcion", e);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("plan").GetProperty("nombre").GetString().Should().Be("Comercio");
        json.GetProperty("periodo").GetProperty("inicio").GetDateTimeOffset().Should().Be(Reloj.Ahora.AddDays(-1));
        json.GetProperty("periodo").GetProperty("fin").GetDateTimeOffset().Should().Be(Reloj.Ahora.AddDays(28));
        json.GetProperty("proximaRenovacion").GetDateTimeOffset().Should().Be(Reloj.Ahora.AddDays(29));
        json.GetProperty("tarjetaEnmascarada").GetString().Should().Be("Visa •••• 4242");
        json.GetProperty("cuotaLlamadas").GetInt64().Should().Be(5000);
        json.GetProperty("limiteMinuto").GetInt32().Should().Be(60);
    }

    // RNF-08: la sesión de otra organización no revela ni modifica datos del portal.
    [Fact]
    public async Task Contratar_SesionDeOtraOrganizacionEnElHost_Responde404()
    {
        var e = await CrearEscenario(verificado: true);
        var intruso = await CrearEscenario(verificado: true, subdominio: "agro");
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", intruso, Tarjeta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Escalar<long>("SELECT count(*) FROM suscripcion_api")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(0);
    }

    // RF-26: una falla antes del commit revierte suscripción y claves sin publicar en Redis.
    [Fact]
    public async Task PrepararClaves_DentroDeLaTransaccion_NoPublicaYSeRevierteConLaSuscripcion()
    {
        var e = await CrearEscenario(verificado: true, gratuito: true);
        using var alcance = Fabrica.Services.CreateScope();
        var http = new DefaultHttpContext { User = SesionConsumidorDePrueba.Principal(e.ConsumidorId, e.OrganizacionId) };
        alcance.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var inicio = SuscripcionApi.InicioDeCiclo(Reloj.Ahora);
        var suscripcion = SuscripcionApi.Crear(e.ConsumidorId, e.ApiId, e.PlanId, inicio, inicio.AddDays(30));

        await using var transaccion = await db.Database.BeginTransactionAsync();
        db.Add(suscripcion);
        await db.SaveChangesAsync();
        var claves = await alcance.ServiceProvider.GetRequiredService<IServicioClaves>()
            .PrepararClavesParaSuscripcion(suscripcion.Id);

        claves.Should().HaveCount(2);
        foreach (var clave in claves)
        {
            (await Redis.KeyExistsAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(clave.Clave)))).Should().BeFalse();
        }

        await transaccion.RollbackAsync();
        (await Escalar<long>("SELECT count(*) FROM suscripcion_api")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM clave")).Should().Be(0);
    }

    private sealed record Escenario(Guid OrganizacionId, Guid ApiId, Guid ConsumidorId, Guid PlanId);
    private sealed record DatosTarjetaPrueba(string Numero, string MesVencimiento, string AnioVencimiento, string Cvv, string Titular);
    private static readonly DatosTarjetaPrueba Tarjeta = new("4242424242424242", "12", "2030", "123", "María Quiñónez");

    private async Task<Escenario> CrearEscenario(bool verificado, bool gratuito = false, string subdominio = "envios")
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApiPublicada(organizacion, subdominio: subdominio);
        var consumidor = await Escalar<Guid>($"""
            INSERT INTO consumidor (id, organizacion_id, nombre, nombre_empresa, correo, hash_contrasena, estado, correo_verificado_en)
            VALUES (gen_random_uuid(), '{organizacion}', 'María', 'Mercadito', 'maria-{Guid.NewGuid():N}@ejemplo.com', 'hash', 'activo',
                    {(verificado ? $"'{Reloj.Ahora:O}'" : "NULL")}) RETURNING id
            """);
        var plan = gratuito
            ? await Escalar<Guid>($"""INSERT INTO plan_api (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias, cuota_llamadas, limite_minuto, activo) VALUES (gen_random_uuid(), '{api}', 'Comercio', 'Descripción', 0, true, 30, 5000, 60, true) RETURNING id""")
            : await NuevoPlanApiCon(api, "Comercio", 5000, 60);
        return new Escenario(organizacion, api, consumidor, plan);
    }

    private HttpRequestMessage Peticion(HttpMethod metodo, string ruta, Escenario e)
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Host = HostPortal;
        peticion.Headers.Add("X-Requested-With", "shapi");
        peticion.Headers.Add(SesionConsumidorDePrueba.Cabecera, $"{e.ConsumidorId}:{e.OrganizacionId}");
        return peticion;
    }

    private async Task<HttpResponseMessage> Portal(HttpMethod metodo, string ruta, Escenario e, object? cuerpo = null)
    {
        var peticion = Peticion(metodo, ruta, e);
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        return await Cliente.SendAsync(peticion);
    }

    private static async Task AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString().Should().Be(codigo);
    }
}
