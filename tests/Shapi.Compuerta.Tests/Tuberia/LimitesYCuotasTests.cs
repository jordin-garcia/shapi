using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Red;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Tests.Tuberia;

// JG-06: filtro 6 de 08 §3 (límites por minuto, cuotas y cabeceras), con Redis real y un reloj fijo.
public sealed class LimitesYCuotasTests : IClassFixture<EntornoCompuerta>, IDisposable
{
    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre",
        "diciembre",
    ];

    // Las fechas son relativas al día real: Redis aplica los EXPIREAT con su propia hora.
    private static readonly DateTimeOffset Hoy = new(DateTime.UtcNow.Date, TimeSpan.Zero);

    // 12:00:30 UTC (06:00:30 en Guatemala): faltan 30 segundos para el siguiente minuto.
    private static readonly DateTimeOffset Ahora = Hoy.AddHours(12).AddSeconds(30);

    // Un ciclo que empieza y termina a medianoche de Guatemala (09 §4).
    private static readonly DateTimeOffset InicioCiclo = Hoy.AddDays(-14).AddHours(6);
    private static readonly DateTimeOffset FinCiclo = Hoy.AddDays(16).AddHours(6);

    private static readonly DateOnly DiaGuatemala = DateOnly.FromDateTime(Hoy.UtcDateTime);

    private static readonly string SegundosHastaFinDelCiclo =
        ((long)(FinCiclo - Ahora).TotalSeconds).ToString(CultureInfo.InvariantCulture);

    private static readonly string[] CabecerasCuota =
    [
        "X-Shapi-Plan", "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset", "X-Cuota-Limite",
        "X-Cuota-Restante", "X-Cuota-Reinicio",
    ];

    private readonly EntornoCompuerta _entorno;
    private readonly RelojFijo _reloj = new(Ahora);
    private readonly WebApplicationFactory<Program> _fabrica;

    public LimitesYCuotasTests(EntornoCompuerta entorno)
    {
        _entorno = entorno;
        _fabrica = entorno.Fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
            servicios.AddSingleton<TimeProvider>(_reloj)));
    }

    public void Dispose()
    {
        _entorno.Origen.Olvidar();
        _fabrica.Dispose();
    }

    [Fact]
    public async Task RF_30_LimiteDelPlan_SeSuperaEnElMinuto_Responde429LimitePorMinutoConRetryAfter()
    {
        // Criterio 1
        var escenario = await SembrarAsync(limiteMinuto: 3);
        for (var i = 0; i < 3; i++)
        {
            (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var respuesta = await EnviarAsync(escenario);

        await VerificarErrorAsync(respuesta, "limite_por_minuto");
        Cabecera(respuesta, "Retry-After").Should().Be("30");
        _entorno.Origen.Ultima.Should().BeNull();
        VerificarCabecerasCuota(respuesta);
        Cabecera(respuesta, "X-RateLimit-Remaining").Should().Be("0");
    }

    [Fact]
    public async Task RF_30_LimiteDelPlan_EnElSiguienteMinuto_VuelveAPasar()
    {
        // Criterio 1: ventana fija de 60 segundos alineada al minuto (08 §3).
        var escenario = await SembrarAsync(limiteMinuto: 1);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        _reloj.Ahora = Ahora.AddSeconds(30);

        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RF_30_LimiteDeLaRuta_SeSupera_Responde429LimitePorMinutoSinAfectarOtrasRutas()
    {
        // Criterio 2
        var escenario = await SembrarAsync(rutas:
        [
            EntornoCompuerta.Ruta("GET", "/cotizaciones", limiteMinuto: 2),
            EntornoCompuerta.Ruta("GET", "/rastreo"),
        ]);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);

        var respuesta = await EnviarAsync(escenario);

        await VerificarErrorAsync(respuesta, "limite_por_minuto");
        Cabecera(respuesta, "Retry-After").Should().Be("30");
        VerificarCabecerasCuota(respuesta);
        Cabecera(respuesta, "X-RateLimit-Limit").Should().Be("2");
        Cabecera(respuesta, "X-RateLimit-Remaining").Should().Be("0");
        (await Contador(LlavesRedis.LimiteMinutoSuscripcion(escenario.Clave.SuscripcionId, MinutoEpoch(Ahora))))
            .Should().Be(2, "el rechazo revierte también el contador del plan");
        (await EnviarAsync(escenario, "/rastreo")).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RF_30_CuotaDelConsumidor_DescuentaElPesoYAlAgotarse_Responde429CuotaAgotada()
    {
        // Criterio 3
        var escenario = await SembrarAsync(cuotaLlamadas: 12, rutas:
        [
            EntornoCompuerta.Ruta("POST", "/cotizaciones", peso: 5),
            EntornoCompuerta.Ruta("GET", "/rastreo"),
        ]);
        (await EnviarAsync(escenario, metodo: HttpMethod.Post)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await EnviarAsync(escenario, metodo: HttpMethod.Post)).StatusCode.Should().Be(HttpStatusCode.Created);

        var respuesta = await EnviarAsync(escenario, metodo: HttpMethod.Post);

        var error = await VerificarErrorAsync(respuesta, "cuota_agotada");
        error.GetProperty("mensaje").GetString().Should().Be(
            "Agotó las 12 llamadas de su plan Comercio en este ciclo. "
            + $"La cuota se renueva el {FinCiclo.Day} de {Meses[FinCiclo.Month - 1]} de {FinCiclo.Year}.");
        Cabecera(respuesta, "Retry-After").Should().Be(SegundosHastaFinDelCiclo);
        _entorno.Origen.Ultima.Should().BeNull();
        VerificarCabecerasCuota(respuesta);
        Cabecera(respuesta, "X-Cuota-Restante").Should().Be("2");
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(10, "nunca se excede: el rechazo devuelve el peso");
        (await Contador(LlavesRedis.LimiteMinutoSuscripcion(escenario.Clave.SuscripcionId, MinutoEpoch(Ahora))))
            .Should().Be(2, "el rechazo revierte también el contador por minuto");

        (await EnviarAsync(escenario, "/rastreo")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(11);
    }

    [Fact]
    public async Task RF_30_CuotaDelConsumidor_TtlHastaOchoDiasDespuesDelFinDelCiclo()
    {
        // 07 §4: TTL = fin + 8 días.
        var escenario = await SembrarAsync();

        await EnviarAsync(escenario);

        var expira = await _entorno.Redis.GetDatabase().KeyExpireTimeAsync(CuotaSuscripcion(escenario));
        expira.Should().Be(FinCiclo.AddDays(8).UtcDateTime);
    }

    [Fact]
    public async Task RF_30_CuotaDelConsumidor_CicloVencidoHaceMasDeOchoDias_NoSeReiniciaEnCadaPeticion()
    {
        // Si el trabajador no ha cerrado el ciclo (RNF-04), fin + 8 días puede haber pasado: un EXPIREAT en el pasado
        // borraría el contador y la cuota se reiniciaría en cada petición.
        var escenario = await SembrarAsync();
        var fin = Hoy.AddDays(-10).AddHours(6);
        var inicio = fin.AddDays(-30);
        await _entorno.SembrarSuscripcionAsync(escenario.Clave.SuscripcionId, ContextoSuscripcion.EstadoEnGracia,
            inicio.ToUnixTimeSeconds(), fin.ToUnixTimeSeconds(), cuotaLlamadas: 2);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);

        var respuesta = await EnviarAsync(escenario);

        await VerificarErrorAsync(respuesta, "cuota_agotada");
        Cabecera(respuesta, "Retry-After").Should().Be("1", "el ciclo ya terminó");
        var llave = LlavesRedis.CuotaSuscripcion(escenario.Clave.SuscripcionId, inicio.ToUnixTimeSeconds());
        (await Contador(llave)).Should().Be(2);
        (await _entorno.Redis.GetDatabase().KeyExpireTimeAsync(llave)).Should().Be(Ahora.AddDays(8).UtcDateTime);
    }

    [Fact]
    public async Task RF_30_CuotaDelConsumidor_CincuentaPeticionesSimultaneasConCuotaTreinta_ExactamenteTreintaLleganAlOrigen()
    {
        // Criterio 3 (prueba de concurrencia): la reserva es atómica (ADR-21).
        var escenario = await SembrarAsync(cuotaLlamadas: 30, limiteMinuto: 100);
        var alOrigen = 0;
        _entorno.Origen.Responder = http =>
        {
            Interlocked.Increment(ref alOrigen);
            http.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        };
        using var cliente = EntornoCompuerta.Cliente(_fabrica, escenario.Host);

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 50).Select(async _ =>
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
            peticion.Headers.Add("X-Api-Key", escenario.ClaveTexto);
            return await cliente.SendAsync(peticion);
        }));

        respuestas.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(30);
        respuestas.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests).Should().Be(20);
        alOrigen.Should().Be(30);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(30);
        foreach (var rechazo in respuestas.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests))
        {
            await VerificarErrorAsync(rechazo, "cuota_agotada");
        }
    }

    [Fact]
    public async Task RF_30_CuotaDePlataforma_AlAgotarse_Responde429CuotaPlataformaAgotada()
    {
        // Criterio 4
        var escenario = await SembrarAsync(cuotaPlataforma: 2);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);

        var respuesta = await EnviarAsync(escenario);

        await VerificarErrorAsync(respuesta, "cuota_plataforma_agotada");
        Cabecera(respuesta, "Retry-After").Should().Be(SegundosHastaFinDelCiclo);
        _entorno.Origen.Ultima.Should().BeNull();
        VerificarCabecerasCuota(respuesta);
        (await Contador(CuotaOrganizacion(escenario))).Should().Be(2);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(2, "el rechazo devuelve también la cuota del consumidor");
    }

    [Fact]
    public async Task RF_30_CuotaDePlataforma_SinSuscripcionDePlataforma_NoSeAplica()
    {
        // 07 §4: la organización de la plataforma no tiene cuota ni ciclo.
        var escenario = await SembrarAsync(cuotaPlataforma: null);

        var respuesta = await EnviarAsync(escenario);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(1);
    }

    [Fact]
    public async Task RF_30_OrigenInaccesible_DevuelveLaReserva()
    {
        // Criterio 5: con el cliente real de YARP, 127.0.0.1 es una dirección prohibida y no se conecta (502).
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", _entorno.CadenaRedis);
            web.ConfigureTestServices(servicios => servicios.AddSingleton<TimeProvider>(_reloj));
        });
        var escenario = await SembrarAsync(urlOrigen: "http://127.0.0.1:1", rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", peso: 3)]);

        var respuesta = await EnviarAsync(escenario, fabrica: fabrica);

        await VerificarErrorAsync(respuesta, "origen_inaccesible", HttpStatusCode.BadGateway);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(0);
        (await Contador(CuotaOrganizacion(escenario))).Should().Be(0);
        VerificarCabecerasCuota(respuesta);
        Cabecera(respuesta, "X-Cuota-Restante").Should().Be("50000", "la cuota se devolvió");
    }

    [Fact]
    public async Task RF_31_ConexionQueNoTerminaEnElTiempoDeConexion_Responde502YDevuelveLaReserva()
    {
        // 08 §1 y criterio 6 de JG-05, criterio 5 de JG-06 (auditoría 2026-10-03, H-02): YARP reporta el vencimiento
        // de ConnectTimeout como RequestTimedOut (504). La petición no llegó al origen: 502 y la cuota se devuelve.
        // La resolución del host no termina nunca, así que vence el tiempo de conexión (300 ms en la prueba). Un connect
        // TCP lento sigue el mismo camino: ConnectTimeout cubre la resolución y la conexión.
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", _entorno.CadenaRedis);
            web.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton<TimeProvider>(_reloj);
                servicios.AddSingleton(new TiemposOrigen(TiemposOrigen.PorDefecto.Total, TimeSpan.FromMilliseconds(300)));
                servicios.AddSingleton(new ValidadorDireccionOrigen(async (_, cancelacion) =>
                {
                    await Task.Delay(Timeout.Infinite, cancelacion);
                    return [];
                }));
            });
        });
        var escenario = await SembrarAsync(urlOrigen: "http://origen-lento.prueba",
            rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", peso: 3)]);
        var reloj = System.Diagnostics.Stopwatch.StartNew();

        var respuesta = await EnviarAsync(escenario, fabrica: fabrica);

        reloj.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10), "vence el tiempo de conexión, no el total");
        await VerificarErrorAsync(respuesta, "origen_inaccesible", HttpStatusCode.BadGateway);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(0);
        (await Contador(CuotaOrganizacion(escenario))).Should().Be(0);
        Cabecera(respuesta, "X-Cuota-Restante").Should().Be("50000", "la cuota se devolvió");
    }

    [Fact]
    public async Task RF_31_OrigenHttpsSinTls_Responde502YDevuelveLaReserva()
    {
        // 08 §3 (auditoría 2026-10-03, revisión del paso 1): si falla la conexión segura, la petición no llegó al origen.
        await using var origen = await OrigenReal.IniciarAsync(http =>
        {
            http.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", _entorno.CadenaRedis);
            web.UseSetting("SHAPI_MODO_DEMO", "true");
            web.UseSetting("SHAPI_ORIGENES_PERMITIDOS", $"localhost:{origen.Puerto}");
            web.ConfigureTestServices(servicios => servicios.AddSingleton<TimeProvider>(_reloj));
        });
        var escenario = await SembrarAsync(urlOrigen: $"https://localhost:{origen.Puerto}",
            rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", peso: 3)]);

        var respuesta = await EnviarAsync(escenario, fabrica: fabrica);

        await VerificarErrorAsync(respuesta, "origen_inaccesible", HttpStatusCode.BadGateway);
        origen.Peticiones.Should().Be(0);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(0);
        (await Contador(CuotaOrganizacion(escenario))).Should().Be(0);
    }

    [Fact]
    public async Task RF_30_OrigenQueCortaLaConexionDespuesDeRecibirLaPeticion_Responde502YSiDescuenta()
    {
        // 08 §3 (auditoría 2026-10-03, decisión del paso 1): la petición llegó al origen, así que la cuota no se
        // devuelve aunque la respuesta sea 502.
        await using var origen = await OrigenReal.IniciarAsync(http =>
        {
            http.Abort();
            return Task.CompletedTask;
        });
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", _entorno.CadenaRedis);
            web.UseSetting("SHAPI_MODO_DEMO", "true");
            web.UseSetting("SHAPI_ORIGENES_PERMITIDOS", $"localhost:{origen.Puerto}");
            web.ConfigureTestServices(servicios => servicios.AddSingleton<TimeProvider>(_reloj));
        });
        var escenario = await SembrarAsync(urlOrigen: $"http://localhost:{origen.Puerto}",
            rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", peso: 3)]);

        var respuesta = await EnviarAsync(escenario, fabrica: fabrica);

        await VerificarErrorAsync(respuesta, "origen_inaccesible", HttpStatusCode.BadGateway);
        origen.Peticiones.Should().Be(1);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(3);
        (await Contador(CuotaOrganizacion(escenario))).Should().Be(1);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task RF_30_ErrorDelOrigen_SiDescuenta(int estado)
    {
        // Criterio 5: la petición llegó al origen.
        var escenario = await SembrarAsync(rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", peso: 3)]);
        _entorno.Origen.Responder = http =>
        {
            http.Response.StatusCode = estado;
            return Task.CompletedTask;
        };

        var respuesta = await EnviarAsync(escenario, olvidar: false);

        ((int)respuesta.StatusCode).Should().Be(estado);
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(3);
        (await Contador(CuotaOrganizacion(escenario))).Should().Be(1);
    }

    [Fact]
    public async Task RF_30_OrigenSinRespuesta504_SiDescuenta()
    {
        // Criterio 5
        using var fabrica = _fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
            servicios.AddSingleton(new TiemposOrigen(TimeSpan.FromMilliseconds(300), TiemposOrigen.PorDefecto.Conexion))));
        var escenario = await SembrarAsync(rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", peso: 3)]);
        _entorno.Origen.Responder = async http => await Task.Delay(TimeSpan.FromSeconds(5), http.RequestAborted);

        var respuesta = await EnviarAsync(escenario, fabrica: fabrica, olvidar: false);

        await VerificarErrorAsync(respuesta, "origen_sin_respuesta", HttpStatusCode.GatewayTimeout);
        VerificarCabecerasCuota(respuesta);
        Cabecera(respuesta, "X-Cuota-Restante").Should().Be("49997");
        (await Contador(CuotaSuscripcion(escenario))).Should().Be(3);
        (await Contador(CuotaOrganizacion(escenario))).Should().Be(1);
    }

    [Fact]
    public async Task RF_45_ClaveDePruebas_LimiteFijoDeDiezPorMinutoSinTocarLasCuotas()
    {
        // Criterio 6: ni el límite del plan (3) ni el de la ruta (2) aplican a la clave de pruebas.
        var escenario = await SembrarAsync(limiteMinuto: 3, tipo: ContextoClave.TipoPruebas,
            rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", limiteMinuto: 2)]);
        HttpResponseMessage ultima = null!;
        for (var i = 0; i < 10; i++)
        {
            ultima = await EnviarAsync(escenario);
            ultima.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var respuesta = await EnviarAsync(escenario);

        Cabecera(ultima, "X-Shapi-Plan").Should().Be("Pruebas");
        Cabecera(ultima, "X-RateLimit-Limit").Should().Be("10");
        Cabecera(ultima, "X-RateLimit-Remaining").Should().Be("0");
        Cabecera(ultima, "X-Cuota-Limite").Should().Be("1000");
        Cabecera(ultima, "X-Cuota-Restante").Should().Be("990");
        Cabecera(ultima, "X-Cuota-Reinicio").Should().Be(Iso(Hoy.AddDays(1).AddHours(6)), "la medianoche de Guatemala");
        await VerificarErrorAsync(respuesta, "limite_por_minuto");
        Cabecera(respuesta, "Retry-After").Should().Be("30");
        VerificarCabecerasCuota(respuesta);
        Cabecera(respuesta, "X-Shapi-Plan").Should().Be("Pruebas");
        var db = _entorno.Redis.GetDatabase();
        (await db.KeyExistsAsync(CuotaSuscripcion(escenario))).Should().BeFalse();
        (await db.KeyExistsAsync(CuotaOrganizacion(escenario))).Should().BeFalse();
        (await Contador(LlavesRedis.LimiteDiaPruebas(escenario.Clave.ClaveId, DiaGuatemala))).Should().Be(10);
    }

    [Fact]
    public async Task RF_45_ClaveDePruebas_MilPeticionesEnElDia_Responde429CuotaAgotadaHastaMedianoche()
    {
        // Criterio 6: 1,000 peticiones por día de Guatemala (07 §4).
        var escenario = await SembrarAsync(tipo: ContextoClave.TipoPruebas);
        var llaveDia = LlavesRedis.LimiteDiaPruebas(escenario.Clave.ClaveId, DiaGuatemala);
        await _entorno.Redis.GetDatabase().StringSetAsync(llaveDia, 1000);

        var respuesta = await EnviarAsync(escenario);

        await VerificarErrorAsync(respuesta, "cuota_agotada");
        Cabecera(respuesta, "Retry-After").Should().Be("64770", "faltan 17 h 59 min 30 s para la medianoche de Guatemala");
        Cabecera(respuesta, "X-Cuota-Restante").Should().Be("0");
        Cabecera(respuesta, "X-Shapi-Plan").Should().Be("Pruebas");
        (await Contador(llaveDia)).Should().Be(1000);
    }

    [Fact]
    public async Task RF_32_Cabeceras_RespuestaDelOrigen_LlevaElPlanElLimiteYLaCuota()
    {
        // Criterio 7 (08 §5). Las cabeceras del origen con el mismo nombre se reemplazan.
        var escenario = await SembrarAsync(limiteMinuto: 60,
            rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", limiteMinuto: 20, peso: 5)]);
        _entorno.Origen.Responder = http =>
        {
            http.Response.StatusCode = StatusCodes.Status200OK;
            http.Response.Headers["X-RateLimit-Limit"] = "999";
            return Task.CompletedTask;
        };

        var respuesta = await EnviarAsync(escenario, olvidar: false);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        Cabecera(respuesta, "X-Shapi-Plan").Should().Be("Comercio");
        Cabecera(respuesta, "X-RateLimit-Limit").Should().Be("20");
        Cabecera(respuesta, "X-RateLimit-Remaining").Should().Be("19");
        Cabecera(respuesta, "X-RateLimit-Reset").Should().Be("30");
        Cabecera(respuesta, "X-Cuota-Limite").Should().Be("50000");
        Cabecera(respuesta, "X-Cuota-Restante").Should().Be("49995");
        Cabecera(respuesta, "X-Cuota-Reinicio").Should().Be(Iso(FinCiclo));
    }

    [Fact]
    public async Task RF_32_Cabeceras_LimiteDelPlanMenorQueElDeLaRuta_InformaElMenor()
    {
        // 08 §5: X-RateLimit-Limit es el menor entre el del plan y el de la ruta.
        var escenario = await SembrarAsync(limiteMinuto: 5,
            rutas: [EntornoCompuerta.Ruta("GET", "/cotizaciones", limiteMinuto: 20)]);

        var respuesta = await EnviarAsync(escenario);

        Cabecera(respuesta, "X-RateLimit-Limit").Should().Be("5");
        Cabecera(respuesta, "X-RateLimit-Remaining").Should().Be("4");
    }

    [Fact]
    public async Task RF_32_Cabeceras_PlanConTilde_ViajaCodificadoYKestrelLoAcepta()
    {
        // Criterio 7 (auditoría 2026-10-03, H-01): Kestrel rechaza las cabeceras de respuesta que no son ASCII, y la
        // siembra de demostración tiene el plan "Básico". TestServer no valida las cabeceras: por eso esta prueba usa
        // Kestrel real. El cliente real de YARP no puede conectar con el origen de prueba (502), y el 502 también lleva las cabeceras de 08 §5.
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", _entorno.CadenaRedis);
            web.ConfigureTestServices(servicios => servicios.AddSingleton<TimeProvider>(_reloj));
        });
        // Un puerto libre: UseKestrel(0) usa el 5000 por defecto y choca con otras pruebas en paralelo.
        fabrica.UseKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0));
        fabrica.StartServer();
        var escenario = await SembrarAsync(planNombre: "Básico");
        using var cliente = fabrica.CreateClient();
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Host = escenario.Host;
        peticion.Headers.Add("X-Api-Key", escenario.ClaveTexto);

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, "origen_inaccesible", HttpStatusCode.BadGateway);
        Cabecera(respuesta, "X-Shapi-Plan").Should().Be("B%C3%A1sico");
        Uri.UnescapeDataString(Cabecera(respuesta, "X-Shapi-Plan")!).Should().Be("Básico");
        VerificarCabecerasCuota(respuesta);
    }

    [Theory]
    [InlineData("Comercio", "Comercio")]
    [InlineData("Pruebas", "Pruebas")]
    [InlineData("Básico", "B%C3%A1sico")]
    [InlineData("Plan Ñandú 2", "Plan%20%C3%91and%C3%BA%202")]
    public void RF_32_ValorCabeceraPlan_SoloAsciiVisibleYSeRecupera(string nombre, string esperado)
    {
        // 08 §5 (H-01): un nombre ASCII sin espacios queda igual.
        var valor = FiltroLimitesYCuotas.ValorCabeceraPlan(nombre);

        valor.Should().Be(esperado);
        valor.Should().MatchRegex("^[\x21-\x7E]+$");
        Uri.UnescapeDataString(valor).Should().Be(nombre);
    }

    [Fact]
    public async Task RF_30_LlavesPorMinutoYCuotaDePlataforma_VencenSegunElModelo()
    {
        // 07 §4 (auditoría 2026-10-03, H-03): rl:s y rl:r viven 120 s; cuota:org vence 8 días después del fin del
        // ciclo de plataforma. Si se perdiera un EXPIRE, las llaves por minuto no vencerían nunca.
        var ruta = EntornoCompuerta.Ruta("GET", "/cotizaciones", limiteMinuto: 20);
        var escenario = await SembrarAsync(rutas: [ruta]);

        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);

        var minuto = MinutoEpoch(Ahora);
        await VerificarTtlMinutoAsync(LlavesRedis.LimiteMinutoSuscripcion(escenario.Clave.SuscripcionId, minuto));
        await VerificarTtlMinutoAsync(LlavesRedis.LimiteMinutoRuta(escenario.Clave.SuscripcionId, ruta.RutaId, minuto));
        (await _entorno.Redis.GetDatabase().KeyExpireTimeAsync(CuotaOrganizacion(escenario)))
            .Should().Be(FinCiclo.AddDays(8).UtcDateTime);
    }

    [Fact]
    public async Task RF_45_ClaveDePruebas_LlavesDelMinutoYDelDia_VencenSegunElModelo()
    {
        // 07 §4 (auditoría 2026-10-03, H-03): rl:p vive 120 s y dia:p vence un día después de la medianoche de
        // Guatemala que termina el día.
        var escenario = await SembrarAsync(tipo: ContextoClave.TipoPruebas);

        (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);

        await VerificarTtlMinutoAsync(LlavesRedis.LimiteMinutoPruebas(escenario.Clave.ClaveId, MinutoEpoch(Ahora)));
        (await _entorno.Redis.GetDatabase().KeyExpireTimeAsync(LlavesRedis.LimiteDiaPruebas(escenario.Clave.ClaveId, DiaGuatemala)))
            .Should().Be(Hoy.AddDays(2).AddHours(6).UtcDateTime);
    }

    private async Task VerificarTtlMinutoAsync(RedisKey llave)
    {
        var ttl = await _entorno.Redis.GetDatabase().KeyTimeToLiveAsync(llave);
        ttl.Should().NotBeNull($"{llave} debe vencer");
        ttl!.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(100)).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public async Task RF_30_UnaSolaLlamadaEvalshaPorPeticion()
    {
        // Criterio 8: el filtro 6 es un solo viaje a Redis (08 §8, viaje 3), con EVALSHA y no EVAL.
        var contador = new ContadorRedis();
        using var fabrica = _fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
            servicios.AddSingleton(contador.Envolver(_entorno.Redis))));
        var escenario = await SembrarAsync();
        var antes = await EstadisticasScriptsAsync();

        for (var i = 0; i < 3; i++)
        {
            contador.Reiniciar();
            var respuesta = await EnviarAsync(escenario, fabrica: fabrica);
            respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
            contador.Viajes.Should().HaveCount(3);
            contador.Viajes[2].Should().Equal("ScriptEvaluateAsync");
        }

        var despues = await EstadisticasScriptsAsync();
        (despues.Evalsha - antes.Evalsha).Should().Be(3);
        despues.Eval.Should().Be(antes.Eval);
    }

    [Fact]
    public async Task RF_30_RedisSinElScript_LoEjecutaConEvalYDespuesVuelveAEvalsha()
    {
        // Criterio 8: si Redis se reinició o vació su caché de scripts, EVALSHA responde NOSCRIPT sin ejecutar nada.
        var escenario = await SembrarAsync(cuotaLlamadas: 10);
        await _entorno.Servidor.ScriptFlushAsync();

        try
        {
            var antes = await EstadisticasScriptsAsync();
            (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
            var primera = await EstadisticasScriptsAsync();
            (await EnviarAsync(escenario)).StatusCode.Should().Be(HttpStatusCode.Created);
            var segunda = await EstadisticasScriptsAsync();

            (primera.Eval - antes.Eval).Should().Be(1);
            (segunda.Evalsha - primera.Evalsha).Should().Be(1);
            segunda.Eval.Should().Be(primera.Eval);
            (await Contador(CuotaSuscripcion(escenario))).Should().Be(2, "el EVALSHA rechazado no reservó nada");
        }
        finally
        {
            await _entorno.Servidor.ScriptLoadAsync(FiltroLimitesYCuotas.Script);
        }
    }

    private async Task<(long Evalsha, long Eval)> EstadisticasScriptsAsync()
    {
        var texto = await _entorno.Servidor.InfoRawAsync("commandstats") ?? "";
        return (Llamadas(texto, "evalsha"), Llamadas(texto, "eval"));

        static long Llamadas(string texto, string comando)
        {
            var linea = texto.Split('\n').Select(l => l.Trim())
                .FirstOrDefault(l => l.StartsWith($"cmdstat_{comando}:", StringComparison.Ordinal));
            if (linea is null)
            {
                return 0;
            }

            var calls = linea.Split(':', 2)[1].Split(',').First(p => p.StartsWith("calls=", StringComparison.Ordinal));
            return long.Parse(calls["calls=".Length..], CultureInfo.InvariantCulture);
        }
    }

    private sealed record Escenario(string Host, ContextoApi Api, ContextoClave Clave, string ClaveTexto);

    private async Task<Escenario> SembrarAsync(long cuotaLlamadas = 50_000, int limiteMinuto = 60,
        IReadOnlyList<RutaCache>? rutas = null, long? cuotaPlataforma = 100_000, string tipo = ContextoClave.TipoProduccion,
        string urlOrigen = EntornoCompuerta.UrlOrigen, string planNombre = "Comercio")
    {
        var host = $"api{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await _entorno.SembrarApiAsync(host, urlOrigen: urlOrigen, rutas: rutas ?? EntornoCompuerta.RutasPorDefecto);
        await _entorno.SembrarOrganizacionAsync(api.OrganizacionId, ContextoOrganizacion.EstadoActiva, cuotaPlataforma,
            InicioCiclo.ToUnixTimeSeconds(), FinCiclo.ToUnixTimeSeconds());
        var texto = tipo == ContextoClave.TipoPruebas
            ? $"shp_prueba_{Guid.NewGuid():N}"[..37]
            : $"shp_prod_{Guid.NewGuid():N}"[..35];
        var clave = await _entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(texto), tipo);
        await _entorno.SembrarSuscripcionAsync(clave.SuscripcionId, ContextoSuscripcion.EstadoActiva,
            InicioCiclo.ToUnixTimeSeconds(), FinCiclo.ToUnixTimeSeconds(), cuotaLlamadas, limiteMinuto, planNombre);
        return new Escenario(host, api, clave, texto);
    }

    private async Task<HttpResponseMessage> EnviarAsync(Escenario escenario, string camino = "/cotizaciones",
        HttpMethod? metodo = null, WebApplicationFactory<Program>? fabrica = null, bool olvidar = true)
    {
        if (olvidar)
        {
            _entorno.Origen.Olvidar();
        }

        using var cliente = EntornoCompuerta.Cliente(fabrica ?? _fabrica, escenario.Host);
        using var peticion = new HttpRequestMessage(metodo ?? HttpMethod.Get, camino);
        peticion.Headers.Add("X-Api-Key", escenario.ClaveTexto);
        return await cliente.SendAsync(peticion);
    }

    private async Task<long> Contador(string llave) =>
        (long?)await _entorno.Redis.GetDatabase().StringGetAsync(llave) ?? 0;

    private static string CuotaSuscripcion(Escenario escenario) =>
        LlavesRedis.CuotaSuscripcion(escenario.Clave.SuscripcionId, InicioCiclo.ToUnixTimeSeconds());

    private static string CuotaOrganizacion(Escenario escenario) =>
        LlavesRedis.CuotaOrganizacion(escenario.Api.OrganizacionId, InicioCiclo.ToUnixTimeSeconds());

    private static long MinutoEpoch(DateTimeOffset momento) => momento.ToUnixTimeSeconds() / 60;

    private static string Iso(DateTimeOffset momento) =>
        momento.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string? Cabecera(HttpResponseMessage respuesta, string nombre) =>
        respuesta.Headers.TryGetValues(nombre, out var valores) || respuesta.Content.Headers.TryGetValues(nombre, out valores)
            ? string.Join(", ", valores)
            : null;

    /// <summary>Criterio 7: toda respuesta con una clave válida, incluidos los 429, lleva las cabeceras de 08 §5.</summary>
    private static void VerificarCabecerasCuota(HttpResponseMessage respuesta)
    {
        foreach (var nombre in CabecerasCuota)
        {
            Cabecera(respuesta, nombre).Should().NotBeNullOrEmpty(nombre);
        }
    }

    private static async Task<JsonElement> VerificarErrorAsync(HttpResponseMessage respuesta, string codigo,
        HttpStatusCode estado = HttpStatusCode.TooManyRequests)
    {
        respuesta.StatusCode.Should().Be(estado);
        respuesta.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var error = documento.RootElement.GetProperty("error").Clone();
        error.GetProperty("codigo").GetString().Should().Be(codigo);
        error.GetProperty("estado").GetInt32().Should().Be((int)estado);
        error.GetProperty("mensaje").GetString().Should().NotBeNullOrWhiteSpace();
        return error;
    }
}
