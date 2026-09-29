using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shapi.Api.Tests.Persistencia;
using Shapi.Contratos.Redis;
using Shapi.Trabajador.Resincronizacion;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Cache;

// JG-04 criterio 4: si Redis pierde sus datos, la caché se reconstruye desde PostgreSQL (RNF-05).
[Collection(nameof(RedisCache))]
public sealed class ResincronizarCacheTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis)
{
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin = new(2026, 10, 1, 6, 0, 0, TimeSpan.Zero);

    private const string ClaveActiva = "shp_prod_AAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string ClaveRotada = "shp_prod_BBBBBBBBBBBBBBBBBBBBBBBBBB";
    private const string ClaveRotadaVencida = "shp_prod_CCCCCCCCCCCCCCCCCCCCCCCCCC";
    private const string ClaveRevocada = "shp_prod_DDDDDDDDDDDDDDDDDDDDDDDDDD";

    [Fact]
    public async Task RNF_05_Resincronizar_RedisVacio_EscribeTodasLasLlavesDeConfiguracion()
    {
        // RNF-05
        var datos = await SembrarAsync();

        await ResincronizarAsync();

        (await Redis.HashGetAsync(LlavesRedis.Api(datos.Api), ContextoApi.CampoEstado)).ToString().Should().Be("publicada");
        (await Redis.HashGetAsync(LlavesRedis.Api(datos.Api), ContextoApi.CampoSecreto)).ToString()
            .Should().Be("shps_secretoDeOrigenDePrueba0001");
        RutaCache.Deserializar((await Redis.StringGetAsync(LlavesRedis.RutasApi(datos.Api))).ToString())
            .Should().ContainSingle(r => r.RutaId == datos.Ruta && r.Expuesta);
        (await Redis.StringGetAsync(LlavesRedis.ApiPorHost("envios.api.shapi.localhost"))).ToString().Should().Be(datos.Api.ToString());
        (await Redis.StringGetAsync(LlavesRedis.ApiPorHost("api.envios-xelaju.localhost"))).ToString().Should().Be(datos.Api.ToString());
        (await Redis.KeyExistsAsync(LlavesRedis.Api(datos.ApiBorrador))).Should().BeFalse();

        (await Redis.KeyExistsAsync(Llave(ClaveActiva))).Should().BeTrue();
        (await Redis.KeyTimeToLiveAsync(Llave(ClaveActiva))).Should().BeNull();
        (await Redis.KeyExpireTimeAsync(Llave(ClaveRotada))).Should().BeCloseTo(DateTime.UtcNow.AddHours(12), TimeSpan.FromSeconds(5));
        (await Redis.KeyExistsAsync(Llave(ClaveRotadaVencida))).Should().BeFalse();
        (await Redis.KeyExistsAsync(Llave(ClaveRevocada))).Should().BeFalse();

        (await Redis.HashGetAsync(LlavesRedis.Suscripcion(datos.Suscripcion), ContextoSuscripcion.CampoEstado)).ToString()
            .Should().Be("activa");
        (await Redis.KeyExistsAsync(LlavesRedis.Suscripcion(datos.SuscripcionFinalizada))).Should().BeFalse();
        (await Redis.HashGetAsync(LlavesRedis.Organizacion(datos.Organizacion), ContextoOrganizacion.CampoCuotaPeticiones))
            .ToString().Should().Be("100000");
    }

    [Fact]
    public async Task RNF_05_Resincronizar_ConContadores_NoLosToca()
    {
        // RNF-05: los contadores de cuotas, límites, métricas y la caché de respuestas no se reescriben ni se borran.
        var datos = await SembrarAsync();
        string[] contadores =
        [
            LlavesRedis.CuotaSuscripcion(datos.Suscripcion, Inicio.ToUnixTimeSeconds()),
            LlavesRedis.CuotaOrganizacion(datos.Organizacion, Inicio.ToUnixTimeSeconds()),
            LlavesRedis.LimiteMinutoSuscripcion(datos.Suscripcion, 29_000_000),
            LlavesRedis.LimiteDiaPruebas(Guid.NewGuid(), new DateOnly(2026, 9, 26)),
            LlavesRedis.Metricas(new DateOnly(2026, 9, 26), datos.Api, datos.Ruta, datos.Suscripcion, "produccion"),
            LlavesRedis.Cache(datos.Api, datos.Ruta, "GET", "/guias/GT1", null),
        ];
        foreach (var contador in contadores)
        {
            await Redis.StringSetAsync(contador, "41", TimeSpan.FromHours(1));
        }

        await ResincronizarAsync();

        foreach (var contador in contadores)
        {
            (await Redis.StringGetAsync(contador)).ToString().Should().Be("41", contador);
            (await Redis.KeyTimeToLiveAsync(contador)).Should().BeGreaterThan(TimeSpan.FromMinutes(59), contador);
        }
    }

    [Fact]
    public async Task RF_28_Resincronizar_LlavesQueYaNoCorresponden_LasBorra()
    {
        // RF-28 y RNF-05: si Redis falló al revocar una clave o al quitar un dominio, la resincronización lo corrige.
        var datos = await SembrarAsync();
        await Redis.HashSetAsync(Llave(ClaveRevocada), ContextoClave.CampoTipo, ContextoClave.TipoProduccion);
        await Redis.StringSetAsync(LlavesRedis.ApiPorHost("dominio-viejo.localhost"), datos.Api.ToString());
        await Redis.HashSetAsync(LlavesRedis.Suscripcion(datos.SuscripcionFinalizada), ContextoSuscripcion.CampoEstado, "activa");
        await Redis.HashSetAsync(LlavesRedis.Api(datos.ApiBorrador), ContextoApi.CampoEstado, "publicada");

        await ResincronizarAsync();

        (await Redis.KeyExistsAsync(Llave(ClaveRevocada))).Should().BeFalse();
        (await Redis.KeyExistsAsync(LlavesRedis.ApiPorHost("dominio-viejo.localhost"))).Should().BeFalse();
        (await Redis.KeyExistsAsync(LlavesRedis.Suscripcion(datos.SuscripcionFinalizada))).Should().BeFalse();
        (await Redis.KeyExistsAsync(LlavesRedis.Api(datos.ApiBorrador))).Should().BeFalse();
        (await Redis.KeyExistsAsync(Llave(ClaveActiva))).Should().BeTrue();
    }

    [Fact]
    public async Task RNF_04_Resincronizar_SecretoQueNoSePuedeDescifrar_ConservaLoQuePublicoLaApi()
    {
        // RNF-04: si el trabajador no comparte el anillo de llaves, no debe dejar la API en 404 cada 5 minutos.
        var datos = await SembrarAsync();
        using (var alcance = Servicios.CreateScope())
        {
            await Publicador(alcance).PublicarApi(datos.Api);
        }

        var otroAnillo = new EphemeralDataProtectionProvider().CreateProtector("Shapi.SecretoOrigen");
        await Ejecutar($"UPDATE api SET secreto_origen_cifrado = '{otroAnillo.Protect("shps_otro")}' WHERE id = '{datos.Api}'");

        await ResincronizarAsync();

        (await Redis.HashGetAsync(LlavesRedis.Api(datos.Api), ContextoApi.CampoSecreto)).ToString()
            .Should().Be("shps_secretoDeOrigenDePrueba0001");
        (await Redis.KeyExistsAsync(LlavesRedis.RutasApi(datos.Api))).Should().BeTrue();
        (await Redis.KeyExistsAsync(LlavesRedis.ApiPorHost("envios.api.shapi.localhost"))).Should().BeTrue();
        (await Redis.KeyExistsAsync(LlavesRedis.ApiPorHost("api.envios-xelaju.localhost"))).Should().BeTrue();
        Registros.Entradas.Should().Contain(e => e.Nivel == LogLevel.Error && e.Texto.Contains(datos.Api.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task RNF_05_TrabajoResincronizacion_AlArrancarYCadaIntervalo_ReescribeLaCache()
    {
        // RNF-05: al arrancar el trabajador y luego cada intervalo (5 minutos en producción).
        var datos = await SembrarAsync();
        await using var servicios = CrearServicios(ajustar: s =>
        {
            s.AgregarResincronizacion();
            s.AddSingleton(new OpcionesResincronizacion { Intervalo = TimeSpan.FromMilliseconds(200) });
        });
        var trabajo = servicios.GetServices<IHostedService>().OfType<TrabajoResincronizacion>().Single();

        await trabajo.StartAsync(CancellationToken.None);
        try
        {
            (await EsperarLlaveAsync(LlavesRedis.Api(datos.Api))).Should().BeTrue("resincroniza al arrancar");
            await RedisCache.VaciarAsync();
            (await EsperarLlaveAsync(LlavesRedis.Api(datos.Api))).Should().BeTrue("resincroniza otra vez al pasar el intervalo");
        }
        finally
        {
            await trabajo.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task RNF_05_TrabajoResincronizacion_RedisNoDisponible_RegistraElErrorYSigue()
    {
        // RNF-05: una falla no detiene el trabajo; se vuelve a intentar en el siguiente intervalo.
        await SembrarAsync();
        await using var servicios = CrearServicios("127.0.0.1:1,connectTimeout=200", s =>
        {
            s.AgregarResincronizacion();
            s.AddSingleton(new OpcionesResincronizacion { Intervalo = TimeSpan.FromMilliseconds(100) });
        });
        var trabajo = servicios.GetServices<IHostedService>().OfType<TrabajoResincronizacion>().Single();

        await trabajo.StartAsync(CancellationToken.None);
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (Registros.Entradas.Count(e => e.Nivel == LogLevel.Error) < 2 && DateTime.UtcNow < limite)
        {
            await Task.Delay(50);
        }

        await trabajo.StopAsync(CancellationToken.None);
        Registros.Entradas.Count(e => e.Nivel == LogLevel.Error).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void RNF_05_OpcionesResincronizacion_PorDefecto_CadaCincoMinutos()
    {
        // RNF-05 (07 §4)
        new OpcionesResincronizacion().Intervalo.Should().Be(TimeSpan.FromMinutes(5));
    }

    private static RedisKey Llave(string claveEnClaro) => LlavesRedis.Clave(ContextoClave.CalcularHash(claveEnClaro));

    private async Task ResincronizarAsync()
    {
        await using var servicios = CrearServicios(ajustar: s => s.AgregarResincronizacion());
        using var alcance = servicios.CreateScope();
        await alcance.ServiceProvider.GetRequiredService<ResincronizarCache>().EjecutarAsync(CancellationToken.None);
    }

    private async Task<bool> EsperarLlaveAsync(string llave)
    {
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < limite)
        {
            if (await Redis.KeyExistsAsync(llave))
            {
                return true;
            }

            await Task.Delay(50);
        }

        return false;
    }

    /// <summary>
    /// Una organización con su suscripción de plataforma, una API publicada con una ruta y dominio propio verificado,
    /// una API en borrador y un consumidor con una suscripción vigente (con claves en todos los estados) y otra finalizada.
    /// </summary>
    private async Task<(Guid Organizacion, Guid Api, Guid ApiBorrador, Guid Ruta, Guid Suscripcion, Guid SuscripcionFinalizada)>
        SembrarAsync()
    {
        var organizacion = await NuevaOrganizacion();
        await NuevaSuscripcionPlataformaEn(organizacion, await NuevoPlanPlataformaCon(100_000), "activa", Inicio, Fin);
        var api = await NuevaApiPublicada(organizacion);
        var ruta = await NuevaRutaCompleta(api, "GET", "/guias/{numero}", expuesta: true);
        await NuevoDominioPropio(api, "api.envios-xelaju.localhost", "verificado");
        var apiBorrador = await NuevaApiPublicada(organizacion, subdominio: "agro", estado: "borrador");

        var plan = await NuevoPlanApiCon(api, "Comercio", 5000, 60);
        var suscripcion = await NuevaSuscripcionApiEn(await NuevoConsumidor(organizacion), api, plan, "activa", Inicio, Fin);
        await NuevaClaveCon(suscripcion, ClaveActiva);
        await NuevaClaveCon(suscripcion, ClaveRotada, estado: "rotada", expiraEn: Reloj.Ahora.AddHours(12));
        await NuevaClaveCon(suscripcion, ClaveRotadaVencida, estado: "rotada", expiraEn: Reloj.Ahora.AddHours(-1));
        await NuevaClaveCon(suscripcion, ClaveRevocada, estado: "revocada");
        var finalizada = await NuevaSuscripcionApiEn(await NuevoConsumidor(organizacion), api, plan, "finalizada", Inicio, Fin);

        return (organizacion, api, apiBorrador, ruta, suscripcion, finalizada);
    }
}
