using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Claves;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Claves;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Claves;
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
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta });

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

    // RF-20 · criterio 3 de EM-08 (H-68): el ciclo empieza a la medianoche de America/Guatemala (09 §4), aunque en UTC
    // ya sea el día siguiente, y se publican la suscripción, el pago con su periodo y el medio de pago.
    [Fact]
    public async Task Contratar_PagoAprobado_GuardaCicloPagoYMedioYPublicaLaSuscripcion()
    {
        Reloj.Ahora = new DateTimeOffset(2026, 9, 27, 3, 30, 0, TimeSpan.Zero); // 26 sep, 21:30 en Guatemala
        var inicioEsperado = new DateTimeOffset(2026, 9, 26, 6, 0, 0, TimeSpan.Zero);
        var finEsperado = inicioEsperado.AddDays(30);
        var e = await CrearEscenario(verificado: true);

        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        var suscripcion = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("suscripcion");
        var id = suscripcion.GetProperty("id").GetGuid();
        suscripcion.GetProperty("inicio").GetDateTimeOffset().Should().Be(inicioEsperado);
        suscripcion.GetProperty("fin").GetDateTimeOffset().Should().Be(finEsperado);

        var publicada = ContextoSuscripcion.DesdeCampos(id, await Hash(LlavesRedis.Suscripcion(id)));
        publicada.Should().NotBeNull();
        publicada!.PlanId.Should().Be(e.PlanId);
        publicada.Estado.Should().Be("activa");
        publicada.Inicio.Should().Be(inicioEsperado.ToUnixTimeSeconds());
        publicada.Fin.Should().Be(finEsperado.ToUnixTimeSeconds());
        publicada.CuotaLlamadas.Should().Be(5000);
        publicada.LimiteMinuto.Should().Be(60);

        (await Fila(
            $"SELECT monto::text, (periodo_inicio = '{inicioEsperado:O}'::timestamptz)::text, " +
            $"(periodo_fin = '{finEsperado:O}'::timestamptz)::text, (referencia_pasarela LIKE 'ch_sim_%')::text " +
            $"FROM pago WHERE suscripcion_api_id = '{id}'")).Should().Equal("450.00", "true", "true", "true");
        (await Fila(
            "SELECT marca, ultimos4, titular, mes_vencimiento::text, anio_vencimiento::text " +
            $"FROM medio_pago WHERE consumidor_id = '{e.ConsumidorId}'")).Should().Equal("Visa", "4242", "María Quiñónez", "12", "2030");
    }

    // RF-20 (H-67): si algo falla después de autorizar el cobro, se reembolsa y no queda nada guardado.
    [Fact]
    public async Task Contratar_FallaDespuesDelCobro_ReembolsaYNoGuardaNada()
    {
        var e = await CrearEscenario(verificado: true);
        var pasarela = new PasarelaEspia();
        using var fabrica = Fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
        {
            servicios.AddScoped<IPasarelaPagos>(sp =>
            {
                pasarela.Interna ??= ActivatorUtilities.CreateInstance<Shapi.Infraestructura.Pagos.PasarelaSimulada>(sp);
                return pasarela;
            });
            servicios.RemoveAll<IServicioClaves>();
            servicios.AddScoped<IServicioClaves, ServicioClavesQueFalla>();
        }));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var peticion = Peticion(HttpMethod.Post, "/api/portal/suscripciones", e);
        peticion.Content = JsonContent.Create(new { planId = e.PlanId, tarjeta = Tarjeta });

        using var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        pasarela.Cobros.Should().ContainSingle();
        pasarela.Reembolsos.Should().Equal(pasarela.Cobros);
        (await Escalar<long>("SELECT count(*) FROM suscripcion_api")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM medio_pago")).Should().Be(0);
    }

    // RF-20 (H-71): si la pasarela no responde al cobrar, 503 como en la tokenización, sin registrar un rechazo.
    [Fact]
    public async Task Contratar_PasarelaNoDisponibleAlCobrar_Responde503SinRegistrarPago()
    {
        var e = await CrearEscenario(verificado: true);
        var pasarela = new PasarelaEspia { CobroNoDisponible = true };
        using var fabrica = Fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
            servicios.AddScoped<IPasarelaPagos>(sp =>
            {
                pasarela.Interna ??= ActivatorUtilities.CreateInstance<Shapi.Infraestructura.Pagos.PasarelaSimulada>(sp);
                return pasarela;
            })));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var peticion = Peticion(HttpMethod.Post, "/api/portal/suscripciones", e);
        peticion.Content = JsonContent.Create(new { planId = e.PlanId, tarjeta = Tarjeta });

        using var respuesta = await cliente.SendAsync(peticion);

        await AfirmarProblema(respuesta, HttpStatusCode.ServiceUnavailable, "pasarela_no_disponible");
        (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM suscripcion_api")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM medio_pago")).Should().Be(0);
    }

    // RF-20 (H-70 y H-64): un titular inválido es datos_invalidos con 400 y errores por campo (convenciones §5).
    [Fact]
    public async Task Contratar_TitularEnBlanco_Responde400ConErroresPorCampo()
    {
        var e = await CrearEscenario(verificado: true);

        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta with { Titular = "   " } });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("codigo").GetString().Should().Be("datos_invalidos");
        problema.GetProperty("errores").TryGetProperty("tarjeta.titular", out _).Should().BeTrue();
        problema.GetProperty("errores").TryGetProperty("tarjeta.vencimiento", out _).Should().BeFalse();
        (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(0);
    }

    // RF-20 (H-64): un plan de pago sin tarjeta responde 400 con el error en `tarjeta`.
    [Fact]
    public async Task Contratar_PlanDePagoSinTarjeta_Responde400ConErrorEnTarjeta()
    {
        var e = await CrearEscenario(verificado: true);

        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errores").TryGetProperty("tarjeta", out _).Should().BeTrue();
    }

    // RF-20 (H-69): dos contrataciones simultáneas del mismo consumidor dejan una sola suscripción y un solo cobro.
    [Fact]
    public async Task Contratar_DosContratacionesSimultaneas_DejaUnaSolaSuscripcion()
    {
        var e = await CrearEscenario(verificado: true);

        var respuestas = await Task.WhenAll(
            Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta }),
            Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta }));

        try
        {
            respuestas.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
            (await Escalar<long>("SELECT count(*) FROM suscripcion_api")).Should().Be(1);
            (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(1);
            (await Escalar<long>("SELECT count(*) FROM clave")).Should().Be(2);
        }
        finally
        {
            foreach (var respuesta in respuestas)
            {
                respuesta.Dispose();
            }
        }
    }

    // RF-20: el año de tarjeta con dos dígitos se normaliza antes de cobrar.
    [Fact]
    public async Task Contratar_AnioDeTarjetaConDosDigitos_SeGuardaNormalizado()
    {
        var e = await CrearEscenario(verificado: true);
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta with { AnioVencimiento = "28" } });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        (await Escalar<long>($"SELECT count(*) FROM medio_pago WHERE consumidor_id = '{e.ConsumidorId}' AND anio_vencimiento = 2028")).Should().Be(1);
    }

    // RF-20: un rechazo queda registrado sin crear una suscripción.
    [Fact]
    public async Task Contratar_TarjetaRechazada_RegistraPagoSinSuscripcion()
    {
        var e = await CrearEscenario(verificado: true);
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta with { Numero = "4000000000000002" } });

        respuesta.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("codigo").GetString().Should().Be("pago_rechazado");
        problema.GetProperty("detalle").GetProperty("motivo").GetString().Should().Be("fondos_insuficientes");
        (await Escalar<long>("SELECT count(*) FROM suscripcion_api")).Should().Be(0);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE consumidor_id = '{e.ConsumidorId}' AND api_id = '{e.ApiId}' AND estado = 'rechazado'")).Should().Be(1);
        // 09 §2: un rechazo no guarda el medio de pago ni activa claves (H-69).
        (await Escalar<long>("SELECT count(*) FROM medio_pago")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM clave")).Should().Be(0);
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
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta });
        await AfirmarProblema(respuesta, HttpStatusCode.Conflict, "suscripcion_existente");
    }

    // RF-20: un plan inactivo no se puede contratar y no genera cobro.
    [Fact]
    public async Task Contratar_PlanInactivo_Responde422SinCobrar()
    {
        var e = await CrearEscenario(verificado: true);
        await Ejecutar($"UPDATE plan_api SET activo = false WHERE id = '{e.PlanId}'");
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", e, new { planId = e.PlanId, tarjeta = Tarjeta });
        await AfirmarProblema(respuesta, HttpStatusCode.UnprocessableEntity, "plan_no_encontrado");
        (await Escalar<long>("SELECT count(*) FROM pago")).Should().Be(0);
    }

    // RF-20: el consumidor consulta el periodo, renovación, límites y tarjeta enmascarada.
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
        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/suscripciones", intruso, new { planId = intruso.PlanId, tarjeta = Tarjeta });

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

    private async Task<string[]> Fila(string sql)
    {
        await using var conexion = new Npgsql.NpgsqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new Npgsql.NpgsqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        (await lector.ReadAsync()).Should().BeTrue("la consulta debe devolver una fila: {0}", sql);
        return Enumerable.Range(0, lector.FieldCount).Select(i => lector.GetValue(i).ToString()!).ToArray();
    }

    private static async Task AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString().Should().Be(codigo);
    }
}

/// <summary>La pasarela simulada, que anota cobros y reembolsos y puede simular que no responde al cobrar.</summary>
internal sealed class PasarelaEspia : IPasarelaPagos
{
    public IPasarelaPagos? Interna { get; set; }

    public bool CobroNoDisponible { get; init; }

    public List<string> Cobros { get; } = [];

    public List<string> Reembolsos { get; } = [];

    public Task<ResultadoTokenizacion> TokenizarAsync(DatosTarjeta tarjeta) => Interna!.TokenizarAsync(tarjeta);

    public async Task<ResultadoCobro> CobrarAsync(string token, decimal monto, string referencia, bool esRenovacion)
    {
        if (CobroNoDisponible)
        {
            return new ResultadoCobro { Exitoso = false, Error = Shapi.Contratos.CodigosError.PasarelaNoDisponible };
        }

        var cobro = await Interna!.CobrarAsync(token, monto, referencia, esRenovacion);
        if (cobro.Exitoso)
        {
            Cobros.Add(cobro.Referencia!);
        }

        return cobro;
    }

    public Task<ResultadoReembolso> ReembolsarAsync(string referenciaCobro)
    {
        Reembolsos.Add(referenciaCobro);
        return Interna!.ReembolsarAsync(referenciaCobro);
    }
}

/// <summary>Falla al preparar las claves, después de que se autorizó el cobro.</summary>
internal sealed class ServicioClavesQueFalla : IServicioClaves
{
    public Task<IReadOnlyList<ClaveEmitida>> PrepararClavesParaSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default) =>
        throw new InvalidOperationException("Falla simulada después del cobro.");

    public Task<IReadOnlyList<ClaveEmitida>> EmitirClavesParaSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<VistaClave>> ClavesDelConsumidor(ConsumidorDelPortal consumidor, CancellationToken cancelacion = default) =>
        throw new NotSupportedException();

    public Task<Resultado<ClaveEmitida>> Emitir(ConsumidorDelPortal consumidor, TipoClave tipo, CancellationToken cancelacion = default) =>
        throw new NotSupportedException();

    public Task<Resultado<ClaveRotada>> Rotar(ConsumidorDelPortal consumidor, Guid claveId, CancellationToken cancelacion = default) =>
        throw new NotSupportedException();

    public Task<Resultado<VistaClave>> RevocarPropia(ConsumidorDelPortal consumidor, Guid claveId, CancellationToken cancelacion = default) =>
        throw new NotSupportedException();

    public Task<Resultado<PaginaClavesDeApi>> ClavesDeApi(Guid apiId, int pagina, int tamano, CancellationToken cancelacion = default) =>
        throw new NotSupportedException();

    public Task<Resultado<VistaClave>> RevocarDeConsumidor(MiembroDelPanel miembro, Guid apiId, Guid claveId, CancellationToken cancelacion = default) =>
        throw new NotSupportedException();
}
