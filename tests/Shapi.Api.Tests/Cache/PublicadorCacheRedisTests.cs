using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shapi.Api.Tests.Persistencia;
using Shapi.Contratos.Redis;

namespace Shapi.Api.Tests.Cache;

// JG-04: criterios 1, 2 y 3. La compuerta lee lo que publica la API de control (RNF-02, RNF-04).
[Collection(nameof(RedisCache))]
public sealed class PublicadorCacheRedisTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis)
{
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fin = new(2026, 10, 1, 6, 0, 0, TimeSpan.Zero);

    // ---------- Criterio 1: api:{id}, api:{id}:rutas y api:host:* ----------

    // H-92 (07 §4): quitar un dominio propio deja de enrutarlo de inmediato, pero sin quitarle el host a otra API.
    [Fact]
    public async Task RNF_04_EliminarHost_BorraSoloSiApuntaALaApi()
    {
        var propia = Guid.NewGuid();
        var otra = Guid.NewGuid();
        await Redis.StringSetAsync(LlavesRedis.ApiPorHost("api.envios-xelaju.localhost"), propia.ToString());
        await Redis.StringSetAsync(LlavesRedis.ApiPorHost("api.agro.localhost"), otra.ToString());
        using var alcance = Servicios.CreateScope();

        await Publicador(alcance).EliminarHost("API.Envios-Xelaju.localhost", propia);
        await Publicador(alcance).EliminarHost("api.agro.localhost", propia);

        (await Redis.KeyExistsAsync(LlavesRedis.ApiPorHost("api.envios-xelaju.localhost"))).Should().BeFalse();
        (await Redis.StringGetAsync(LlavesRedis.ApiPorHost("api.agro.localhost"))).ToString().Should().Be(otra.ToString());
    }

    [Fact]
    public async Task RNF_04_PublicarApi_ApiPublicada_EscribeElHashConElSecretoDescifradoYElHostDelPortal()
    {
        // RNF-04
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApiPublicada(organizacion, secreto: "shps_secretoDeOrigenDePrueba0001");

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarApi(api);

        var campos = await Hash(LlavesRedis.Api(api));
        campos.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            [ContextoApi.CampoOrganizacionId] = organizacion.ToString(),
            [ContextoApi.CampoEstado] = "publicada",
            [ContextoApi.CampoUrlOrigen] = "https://origen.ejemplo.com",
            [ContextoApi.CampoSecreto] = "shps_secretoDeOrigenDePrueba0001",
            [ContextoApi.CampoPortalHost] = "envios.shapi.localhost",
            [ContextoApi.CampoVersion] = "1",
        });
        (await Redis.StringGetAsync(LlavesRedis.ApiPorHost("envios.api.shapi.localhost"))).ToString()
            .Should().Be(api.ToString());
    }

    [Fact]
    public async Task RNF_04_PublicarApi_ConRutas_EscribeTodasLasRutasEnJsonTambienLasOcultas()
    {
        // RNF-04
        var api = await NuevaApiPublicada(await NuevaOrganizacion());
        var cotizar = await NuevaRutaCompleta(api, "POST", "/cotizaciones", expuesta: true, limiteMinuto: 30, peso: 2);
        var guia = await NuevaRutaCompleta(api, "GET", "/guias/{numero}", expuesta: true, cacheSegundos: 60);
        var interna = await NuevaRutaCompleta(api, "DELETE", "/interno", expuesta: false);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarApi(api);

        var rutas = RutaCache.Deserializar((await Redis.StringGetAsync(LlavesRedis.RutasApi(api))).ToString());
        rutas.Should().BeEquivalentTo(new[]
        {
            new RutaCache(cotizar, "POST", "/cotizaciones", true, 30, 0, 2),
            new RutaCache(guia, "GET", "/guias/{numero}", true, null, 60, 1),
            new RutaCache(interna, "DELETE", "/interno", false, null, 0, 1),
        });
    }

    [Fact]
    public async Task RNF_04_PublicarApi_DosVeces_IncrementaLaVersion()
    {
        // RNF-04
        var api = await NuevaApiPublicada(await NuevaOrganizacion());

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarApi(api);
        await Publicador(alcance).PublicarApi(api);

        (await Redis.HashGetAsync(LlavesRedis.Api(api), ContextoApi.CampoVersion)).ToString().Should().Be("2");
    }

    [Fact]
    public async Task RNF_04_PublicarApi_ConCamposViejos_BorraElHashAntesDeEscribirlo()
    {
        // RNF-04: DEL y luego HSET, para que no quede un campo que ya no existe (un secreto borrado).
        var api = await NuevaApiPublicada(await NuevaOrganizacion());
        await Redis.HashSetAsync(LlavesRedis.Api(api), [new("campo_viejo", "1"), new(ContextoApi.CampoVersion, "7")]);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarApi(api);

        var campos = await Hash(LlavesRedis.Api(api));
        campos.Should().NotContainKey("campo_viejo");
        campos[ContextoApi.CampoVersion].Should().Be("8");
    }

    [Fact]
    public async Task RF_12_PublicarApi_DominioPropioVerificado_TambienEscribeSuHost()
    {
        // RF-12
        var api = await NuevaApiPublicada(await NuevaOrganizacion());
        await NuevoDominioPropio(api, "api.envios-xelaju.localhost", "verificado");

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarApi(api);

        (await Redis.StringGetAsync(LlavesRedis.ApiPorHost("api.envios-xelaju.localhost"))).ToString().Should().Be(api.ToString());
    }

    [Fact]
    public async Task RF_12_PublicarApi_DominioPropioPendiente_NoEscribeSuHost()
    {
        // RF-12
        var api = await NuevaApiPublicada(await NuevaOrganizacion());
        await NuevoDominioPropio(api, "api.envios-xelaju.localhost", "pendiente");

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarApi(api);

        (await Redis.KeyExistsAsync(LlavesRedis.ApiPorHost("api.envios-xelaju.localhost"))).Should().BeFalse();
    }

    [Fact]
    public async Task RF_14_PublicarApi_Despublicada_DejaElEstadoDespublicada()
    {
        // RF-14: la compuerta responde 404 (lo comprueba ResincronizacionConCompuertaTests).
        var api = await NuevaApiPublicada(await NuevaOrganizacion());
        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarApi(api);

        await CambiarEstado("api", api, "despublicada");
        await Publicador(alcance).PublicarApi(api);

        (await Redis.HashGetAsync(LlavesRedis.Api(api), ContextoApi.CampoEstado)).ToString().Should().Be("despublicada");
        (await Redis.StringGetAsync(LlavesRedis.ApiPorHost("envios.api.shapi.localhost"))).ToString().Should().Be(api.ToString());
    }

    // ---------- Criterio 2: clave:{sha256}, susc:{id} y org:{id} ----------

    [Fact]
    public async Task RF_26_PublicarClave_Activa_EscribeElHashSinVencimiento()
    {
        // RF-26
        var datos = await SuscripcionConApi();
        var clave = await NuevaClaveCon(datos.Suscripcion, "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e");

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarClave(clave);

        var llave = LlavesRedis.Clave(ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e"));
        ContextoClave.DesdeCampos(await Hash(llave)).Should().Be(new ContextoClave(
            clave, datos.Suscripcion, datos.Api, datos.Organizacion, datos.Consumidor, ContextoClave.TipoProduccion));
        (await Redis.KeyTimeToLiveAsync(llave)).Should().BeNull();
    }

    [Fact]
    public async Task RF_27_PublicarClave_Rotada_PoneElVencimientoDeLaRotacion()
    {
        // RF-27
        var datos = await SuscripcionConApi();
        var expira = Reloj.Ahora.AddHours(24);
        var clave = await NuevaClaveCon(datos.Suscripcion, "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e", estado: "rotada", expiraEn: expira);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarClave(clave);

        // EXPIREAT en la hora real: lo que le falta según IReloj, que puede ir adelantado en el modo demostración.
        var llave = LlavesRedis.Clave(ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e"));
        (await Redis.KeyExpireTimeAsync(llave)).Should().BeCloseTo(DateTime.UtcNow + (expira - Reloj.Ahora), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RF_28_PublicarClave_Revocada_BorraLaLlave()
    {
        // RF-28
        var datos = await SuscripcionConApi();
        var clave = await NuevaClaveCon(datos.Suscripcion, "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e");
        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarClave(clave);

        await CambiarEstado("clave", clave, "revocada");
        await Publicador(alcance).PublicarClave(clave);

        (await Redis.KeyExistsAsync(LlavesRedis.Clave(ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e"))))
            .Should().BeFalse();
    }

    [Fact]
    public async Task RF_27_ExpirarClave_PoneExpireAtEnElInstanteSegunElReloj()
    {
        // RF-27
        var hash = ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e");
        await Redis.HashSetAsync(LlavesRedis.Clave(hash), ContextoClave.CampoTipo, ContextoClave.TipoProduccion);
        var instante = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).ExpirarClave(hash, instante);

        (await Redis.KeyExpireTimeAsync(LlavesRedis.Clave(hash))).Should()
            .BeCloseTo(DateTime.UtcNow + (instante - Reloj.Ahora), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RF_27_ExpirarClave_InstanteQueYaPasoSegunElReloj_BorraLaLlave()
    {
        // RF-27: con el reloj del modo demostración adelantado, una rotación vencida deja de funcionar de inmediato.
        var hash = ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e");
        await Redis.HashSetAsync(LlavesRedis.Clave(hash), ContextoClave.CampoTipo, ContextoClave.TipoProduccion);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).ExpirarClave(hash, Reloj.Ahora.AddMinutes(-1));

        (await Redis.KeyExistsAsync(LlavesRedis.Clave(hash))).Should().BeFalse();
    }

    [Fact]
    public async Task RF_28_EliminarClave_BorraLaLlave()
    {
        // RF-28
        var hash = ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e");
        await Redis.HashSetAsync(LlavesRedis.Clave(hash), ContextoClave.CampoTipo, ContextoClave.TipoProduccion);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).EliminarClave(hash);

        (await Redis.KeyExistsAsync(LlavesRedis.Clave(hash))).Should().BeFalse();
    }

    [Fact]
    public async Task RF_23_PublicarSuscripcion_EnGracia_EscribeElPlanElCicloYLosLimites()
    {
        // RF-23
        var datos = await SuscripcionConApi(estado: "en_gracia");

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarSuscripcion(datos.Suscripcion);

        ContextoSuscripcion.DesdeCampos(datos.Suscripcion, await Hash(LlavesRedis.Suscripcion(datos.Suscripcion)))
            .Should().Be(new ContextoSuscripcion(datos.Suscripcion, datos.Plan, "Comercio", "en_gracia",
                Inicio.ToUnixTimeSeconds(), Fin.ToUnixTimeSeconds(), 5000, 60));
    }

    [Fact]
    public async Task RF_29_PublicarSuscripcion_Finalizada_BorraLaLlave()
    {
        // RF-29
        var datos = await SuscripcionConApi();
        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarSuscripcion(datos.Suscripcion);

        await CambiarEstado("suscripcion_api", datos.Suscripcion, "finalizada");
        await Publicador(alcance).PublicarSuscripcion(datos.Suscripcion);

        (await Redis.KeyExistsAsync(LlavesRedis.Suscripcion(datos.Suscripcion))).Should().BeFalse();
    }

    [Fact]
    public async Task RF_23_PublicarSuscripcion_DePlataforma_PublicaLaOrganizacion()
    {
        // RF-23: la suscripción de plataforma no tiene susc:{id}; su estado va en el estado efectivo de org:{id}.
        var organizacion = await NuevaOrganizacion();
        var suscripcion = await NuevaSuscripcionPlataformaEn(
            organizacion, await NuevoPlanPlataformaCon(100_000), "suspendida", Inicio, Fin);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarSuscripcion(suscripcion);

        (await Redis.KeyExistsAsync(LlavesRedis.Suscripcion(suscripcion))).Should().BeFalse();
        (await Redis.HashGetAsync(LlavesRedis.Organizacion(organizacion), ContextoOrganizacion.CampoEstadoEfectivo))
            .ToString().Should().Be("suspendida");
    }

    [Fact]
    public async Task RF_43_PublicarOrganizacion_Activa_EscribeElEstadoLaCuotaYElCiclo()
    {
        // RF-43
        var organizacion = await NuevaOrganizacion();
        await NuevaSuscripcionPlataformaEn(organizacion, await NuevoPlanPlataformaCon(100_000), "en_gracia", Inicio, Fin);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarOrganizacion(organizacion);

        ContextoOrganizacion.DesdeCampos(organizacion, await Hash(LlavesRedis.Organizacion(organizacion)))
            .Should().Be(new ContextoOrganizacion(organizacion, "activa", 100_000, Inicio.ToUnixTimeSeconds(),
                Fin.ToUnixTimeSeconds()));
    }

    [Fact]
    public async Task RF_38_PublicarOrganizacion_SuspendidaPorElAdministrador_EstadoEfectivoSuspendida()
    {
        // RF-38
        var organizacion = await NuevaOrganizacion();
        await NuevaSuscripcionPlataformaEn(organizacion, await NuevoPlanPlataformaCon(100_000), "activa", Inicio, Fin);
        await Ejecutar($"UPDATE organizacion SET estado_admin = 'suspendida' WHERE id = '{organizacion}'");

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarOrganizacion(organizacion);

        (await Redis.HashGetAsync(LlavesRedis.Organizacion(organizacion), ContextoOrganizacion.CampoEstadoEfectivo))
            .ToString().Should().Be("suspendida");
    }

    [Fact]
    public async Task RF_23_PublicarOrganizacion_SuscripcionDePlataformaSuspendida_EstadoEfectivoSuspendida()
    {
        // RF-23
        var organizacion = await NuevaOrganizacion();
        await NuevaSuscripcionPlataformaEn(organizacion, await NuevoPlanPlataformaCon(100_000), "finalizada",
            Inicio.AddDays(-30), Inicio);
        await NuevaSuscripcionPlataformaEn(organizacion, await NuevoPlanPlataformaCon(100_000), "suspendida", Inicio, Fin);

        using var alcance = Servicios.CreateScope();
        await Publicador(alcance).PublicarOrganizacion(organizacion);

        (await Redis.HashGetAsync(LlavesRedis.Organizacion(organizacion), ContextoOrganizacion.CampoEstadoEfectivo))
            .ToString().Should().Be("suspendida");
    }

    // ---------- Criterio 3: Redis falla después del commit ----------

    [Fact]
    public async Task RNF_05_Publicador_RedisNoDisponible_ReintentaTresVecesRegistraElErrorYNoLanza()
    {
        // RNF-05: la operación de negocio ya se confirmó en PostgreSQL; el publicador no la revierte.
        var datos = await SuscripcionConApi();
        await using var servicios = CrearServicios("127.0.0.1:1,connectTimeout=200");

        using var alcance = servicios.CreateScope();
        var publicar = () => Publicador(alcance).PublicarSuscripcion(datos.Suscripcion);

        await publicar.Should().NotThrowAsync();
        Registros.Entradas.Count(e => e.Nivel == LogLevel.Warning && e.Texto.Contains("reintenta", StringComparison.Ordinal))
            .Should().Be(3);
        Registros.Entradas.Should().ContainSingle(e => e.Nivel == LogLevel.Error
            && e.Texto.Contains(nameof(Shapi.Aplicacion.Comun.IPublicadorCache.PublicarSuscripcion), StringComparison.Ordinal));
    }

    [Fact]
    public async Task RNF_05_Publicador_RedisNoDisponible_NoEscribeElHashDeLaClaveEnLosRegistros()
    {
        // RNF-05 y 10 §3: los registros nunca llevan el hash de una clave.
        var hash = ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e");
        await using var servicios = CrearServicios("127.0.0.1:1,connectTimeout=200");

        using var alcance = servicios.CreateScope();
        await Publicador(alcance).EliminarClave(hash);
        await Publicador(alcance).ExpirarClave(hash, Reloj.Ahora);

        Registros.Entradas.Should().Contain(e => e.Nivel == LogLevel.Error);
        Registros.Entradas.Should().NotContain(e => e.Texto.Contains(hash, StringComparison.Ordinal));
    }

    private async Task<(Guid Organizacion, Guid Api, Guid Consumidor, Guid Plan, Guid Suscripcion)> SuscripcionConApi(
        string estado = "activa")
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApiPublicada(organizacion);
        var consumidor = await NuevoConsumidor(organizacion);
        var plan = await NuevoPlanApiCon(api, "Comercio", 5000, 60);
        var suscripcion = await NuevaSuscripcionApiEn(consumidor, api, plan, estado, Inicio, Fin);
        return (organizacion, api, consumidor, plan, suscripcion);
    }
}
