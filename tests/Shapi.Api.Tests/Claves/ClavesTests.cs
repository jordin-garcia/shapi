extern alias compuerta;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Shapi.Api.Identidad;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Claves;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using Shapi.Trabajador.Resincronizacion;
using ProgramaCompuerta = compuerta::Program;

namespace Shapi.Api.Tests.Claves;

/// <summary>
/// JG-07: emisión, rotación y revocación de claves con PostgreSQL y Redis reales. La mayoría de las pruebas usan
/// <see cref="SesionConsumidorDePrueba"/>, que pone los mismos claims que la sesión del portal (el consumidor, su
/// organización y el ámbito <c>Consumidor</c>); <c>RF_27_Rotar_ConLaCookieRealDelPortal_Funciona</c> usa la cookie
/// <c>portal_sesion</c> verdadera de EM-05.
/// </summary>
[Collection(nameof(RedisCache))]
public sealed class ClavesTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis), IAsyncLifetime
{
    private const string HostPortal = "envios.shapi.localhost";
    private const string HostApi = "envios.api.shapi.localhost";
    private const string NombreApi = "API de Cotización de Envíos";

    private WebApplicationFactory<Program>? _fabrica;
    private HttpClient? _cliente;

    private WebApplicationFactory<Program> Fabrica => _fabrica ??= new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
    {
        web.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        web.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
        web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
        web.UseSetting("SHAPI_DOMINIO_BASE", DominioBase);
        web.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
        web.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
        web.UseSetting("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");
        web.ConfigureLogging(registros => registros.SetMinimumLevel(LogLevel.Trace).AddProvider(Registros));
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

    // ---------- RF-26 · Emisión al activarse la suscripción ----------

    [Fact]
    public async Task RF_26_EmitirClavesParaSuscripcion_EmiteProduccionYPruebasYGuardaSoloElHash()
    {
        // RF-26 · CA1: shp_prod_/shp_prueba_ + 26 base62; en la base de datos solo el prefijo, los últimos 4 y el hash.
        var e = await CrearEscenario();

        var emitidas = await EmitirClavesParaSuscripcion(e);

        emitidas.Select(c => c.Tipo).Should().Equal("produccion", "pruebas");
        emitidas[0].Clave.Should().MatchRegex("^shp_prod_[0-9A-Za-z]{26}$");
        emitidas[1].Clave.Should().MatchRegex("^shp_prueba_[0-9A-Za-z]{26}$");
        emitidas.Should().OnlyContain(c => c.Estado == "activa" && c.ClaveEnmascarada == Enmascarar(c.Clave));

        foreach (var emitida in emitidas)
        {
            var fila = await Fila($"SELECT prefijo, ultimos4, hash_sha256, estado FROM clave WHERE id = '{emitida.Id}'");
            fila.Should().Equal(
                emitida.Clave[..emitida.Clave.LastIndexOf('_')] + "_", emitida.Clave[^4..],
                ContextoClave.CalcularHash(emitida.Clave), "activa");
        }

        var todo = await Escalar<string>("SELECT string_agg(row_to_json(clave)::text, '') FROM clave");
        todo.Should().NotContain(emitidas[0].Clave).And.NotContain(emitidas[1].Clave);
    }

    [Fact]
    public async Task RF_26_EmitirClavesParaSuscripcion_PublicaLasClavesEnRedis()
    {
        // RF-26 · CA1: las publica en Redis, sin vencimiento, con los campos que lee la compuerta (07 §4).
        var e = await CrearEscenario();

        var emitidas = await EmitirClavesParaSuscripcion(e);

        foreach (var emitida in emitidas)
        {
            var llave = LlavesRedis.Clave(ContextoClave.CalcularHash(emitida.Clave));
            var contexto = ContextoClave.DesdeCampos(await Hash(llave));
            contexto.Should().Be(new ContextoClave(emitida.Id, e.SuscripcionId, e.ApiId, e.OrganizacionId, e.ConsumidorId, emitida.Tipo));
            (await Redis.KeyTimeToLiveAsync(llave)).Should().BeNull();
        }
    }

    [Fact]
    public async Task RF_26_EmitirClavesParaSuscripcion_TipoConClaveActiva_SoloEmiteElQueFalta()
    {
        var e = await CrearEscenario();
        var primeras = await EmitirClavesParaSuscripcion(e);
        await Revocar(e, primeras[1].Id);

        var segundas = await EmitirClavesParaSuscripcion(e);

        segundas.Should().ContainSingle().Which.Tipo.Should().Be("pruebas");
    }

    // ---------- RF-27 · Rotación ----------

    [Fact]
    public async Task RF_27_Rotar_DevuelveLaNuevaYDejaLaAnteriorRotada24Horas()
    {
        // RF-27 · CA2 y 06 §5.4.
        var e = await CrearEscenario();
        var produccion = (await EmitirClavesParaSuscripcion(e))[0];

        using var respuesta = await Portal(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/rotar", e);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var nueva = json.GetProperty("clave").GetString()!;
        nueva.Should().MatchRegex("^shp_prod_[0-9A-Za-z]{26}$").And.NotBe(produccion.Clave);
        json.GetProperty("tipo").GetString().Should().Be("produccion");
        json.GetProperty("estado").GetString().Should().Be("activa");
        json.GetProperty("claveEnmascarada").GetString().Should().Be(Enmascarar(nueva));
        var anterior = json.GetProperty("anterior");
        anterior.GetProperty("id").GetGuid().Should().Be(produccion.Id);
        anterior.GetProperty("claveEnmascarada").GetString().Should().Be(produccion.ClaveEnmascarada);
        anterior.GetProperty("estado").GetString().Should().Be("rotada");
        anterior.GetProperty("expiraEn").GetDateTimeOffset().Should().Be(Reloj.Ahora.AddHours(24));

        (await Fila($"SELECT estado, expira_en FROM clave WHERE id = '{produccion.Id}'"))
            .Should().Equal("rotada", Reloj.Ahora.AddHours(24).UtcDateTime);
        (await Escalar<long>("SELECT count(*) FROM clave WHERE estado = 'activa' AND tipo = 'produccion'")).Should().Be(1);
    }

    [Fact]
    public async Task RF_27_Rotar_PublicaLaNuevaYPoneExpireatALaAnterior()
    {
        // RF-27 · CA2: HSET clave:{hash_nuevo} y EXPIREAT clave:{hash_anterior} (ahora + 24 h).
        var e = await CrearEscenario();
        var produccion = (await EmitirClavesParaSuscripcion(e))[0];

        using var respuesta = await Portal(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/rotar", e);
        var nueva = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("clave").GetString()!;

        var llaveAnterior = LlavesRedis.Clave(ContextoClave.CalcularHash(produccion.Clave));
        var vida = await Redis.KeyTimeToLiveAsync(llaveAnterior);
        vida.Should().NotBeNull();
        vida!.Value.Should().BeCloseTo(TimeSpan.FromHours(24), TimeSpan.FromMinutes(1));
        (await Hash(llaveAnterior)).Should().NotBeEmpty("la anterior sigue funcionando 24 horas");
        (await Hash(LlavesRedis.Clave(ContextoClave.CalcularHash(nueva)))).Should().NotBeEmpty();
    }

    [Fact]
    public async Task RF_27_Rotar_ClaveYaRotadaORevocada_Responde422ClaveNoRotable()
    {
        // RF-27 · CA2: rotar una clave que ya está rotada → 422 clave_no_rotable.
        var e = await CrearEscenario();
        var emitidas = await EmitirClavesParaSuscripcion(e);
        (await Portal(HttpMethod.Post, $"/api/portal/claves/{emitidas[0].Id}/rotar", e)).Dispose();
        await Revocar(e, emitidas[1].Id);

        using var rotada = await Portal(HttpMethod.Post, $"/api/portal/claves/{emitidas[0].Id}/rotar", e);
        using var revocada = await Portal(HttpMethod.Post, $"/api/portal/claves/{emitidas[1].Id}/rotar", e);

        await AfirmarProblema(rotada, HttpStatusCode.UnprocessableEntity, "clave_no_rotable");
        await AfirmarProblema(revocada, HttpStatusCode.UnprocessableEntity, "clave_no_rotable");
        (await Escalar<long>("SELECT count(*) FROM clave")).Should().Be(3);
    }

    [Fact]
    public async Task RF_27_RotarOtraVezAntesDe24Horas_LaPrimeraRotadaDejaDeFuncionar()
    {
        // RF-27: durante las 24 horas coexisten como máximo dos claves del mismo tipo (y B2.5: "puede rotarla otra vez").
        var e = await CrearEscenario();
        var primera = (await EmitirClavesParaSuscripcion(e))[0];
        using var rotar1 = await Portal(HttpMethod.Post, $"/api/portal/claves/{primera.Id}/rotar", e);
        var segunda = (await rotar1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Reloj.Ahora = Reloj.Ahora.AddHours(2);

        using var rotar2 = await Portal(HttpMethod.Post, $"/api/portal/claves/{segunda}/rotar", e);

        rotar2.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Fila($"SELECT estado, expira_en FROM clave WHERE id = '{primera.Id}'"))
            .Should().Equal("rotada", Reloj.Ahora.UtcDateTime);
        (await Redis.KeyExistsAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(primera.Clave)))).Should().BeFalse();
        (await Escalar<long>($"""
            SELECT count(*) FROM clave
            WHERE tipo = 'produccion' AND (estado = 'activa' OR estado = 'rotada' AND expira_en > '{Reloj.Ahora:O}')
            """)).Should().Be(2);
        using var listar = await Portal(HttpMethod.Get, "/api/portal/claves", e);
        Resumen(await listar.Content.ReadFromJsonAsync<JsonElement>()).Select(c => (c.Tipo, c.Estado))
            .Should().Equal(("produccion", "activa"), ("produccion", "rotada"), ("pruebas", "activa"));
    }

    [Fact]
    public async Task RF_27_Rotar_DosALaVez_SoloUnaRota()
    {
        // RF-27: durante las 24 horas coexisten como máximo dos claves del mismo tipo.
        var e = await CrearEscenario();
        var produccion = (await EmitirClavesParaSuscripcion(e))[0];

        var respuestas = await Task.WhenAll(
            Portal(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/rotar", e),
            Portal(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/rotar", e));

        respuestas.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity]);
        (await Escalar<long>("SELECT count(*) FROM clave WHERE tipo = 'produccion'")).Should().Be(2);
        foreach (var respuesta in respuestas)
        {
            respuesta.Dispose();
        }
    }

    // ---------- RF-28 · Revocación ----------

    [Fact]
    public async Task RF_28_RevocarPropia_MarcaRevocadaPorElConsumidorYLaBorraDeRedis()
    {
        // RF-28 · CA3: revocada, revocada_por = consumidor y fuera de Redis.
        var e = await CrearEscenario();
        var pruebas = (await EmitirClavesParaSuscripcion(e))[1];

        using var respuesta = await Portal(HttpMethod.Post, $"/api/portal/claves/{pruebas.Id}/revocar", e);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("estado").GetString().Should().Be("revocada");
        json.GetProperty("revocadaEn").GetDateTimeOffset().Should().Be(Reloj.Ahora);
        json.GetProperty("claveEnmascarada").GetString().Should().Be(pruebas.ClaveEnmascarada);
        (await Fila($"SELECT estado, revocada_por, revocada_en FROM clave WHERE id = '{pruebas.Id}'"))
            .Should().Equal("revocada", "consumidor", Reloj.Ahora.UtcDateTime);
        (await Redis.KeyExistsAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(pruebas.Clave)))).Should().BeFalse();
    }

    [Theory]
    [InlineData("propietario")]
    [InlineData("editor")]
    public async Task RF_28_RevocarDeConsumidor_PropietarioOEditor_MarcaRevocadaPorElProveedor(string rol)
    {
        // RF-28 · CA3: el propietario o el editor revocan las claves de sus consumidores.
        var e = await CrearEscenario();
        var pruebas = (await EmitirClavesParaSuscripcion(e))[1];
        var cookie = await SesionPersonal(e.OrganizacionId, rol);

        using var respuesta = await Panel(HttpMethod.Post, $"/api/apis/{e.ApiId}/claves/{pruebas.Id}/revocar", cookie);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Fila($"SELECT estado, revocada_por FROM clave WHERE id = '{pruebas.Id}'")).Should().Equal("revocada", "proveedor");
        (await Redis.KeyExistsAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(pruebas.Clave)))).Should().BeFalse();
    }

    [Fact]
    public async Task RF_28_RevocarDeConsumidor_Lector_Responde403()
    {
        // 04 §3.1: el lector ve las claves, pero no las revoca.
        var e = await CrearEscenario();
        var pruebas = (await EmitirClavesParaSuscripcion(e))[1];
        var cookie = await SesionPersonal(e.OrganizacionId, "lector");

        using var revocar = await Panel(HttpMethod.Post, $"/api/apis/{e.ApiId}/claves/{pruebas.Id}/revocar", cookie);
        using var listar = await Panel(HttpMethod.Get, $"/api/apis/{e.ApiId}/claves", cookie);

        revocar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        listar.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Escalar<string>($"SELECT estado FROM clave WHERE id = '{pruebas.Id}'")).Should().Be("activa");
    }

    [Fact]
    public async Task RF_28_RevocarRotada_TambienLaBorraDeRedis()
    {
        // 07 §5: rotada → revocada; deja de funcionar antes de sus 24 horas.
        var e = await CrearEscenario();
        var produccion = (await EmitirClavesParaSuscripcion(e))[0];
        (await Portal(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/rotar", e)).Dispose();

        using var respuesta = await Portal(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/revocar", e);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Escalar<string>($"SELECT estado FROM clave WHERE id = '{produccion.Id}'")).Should().Be("revocada");
        (await Redis.KeyExistsAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(produccion.Clave)))).Should().BeFalse();
    }

    [Fact]
    public async Task RF_28_Revocar_ClaveYaRevocada_RespondeIgualSinOtraEntradaEnLaBitacora()
    {
        var e = await CrearEscenario();
        var pruebas = (await EmitirClavesParaSuscripcion(e))[1];
        await Revocar(e, pruebas.Id);

        using var respuesta = await Portal(HttpMethod.Post, $"/api/portal/claves/{pruebas.Id}/revocar", e);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Fila($"SELECT revocada_por, revocada_en FROM clave WHERE id = '{pruebas.Id}'"))
            .Should().Equal("consumidor", Reloj.Ahora.UtcDateTime);
        (await Escalar<long>("SELECT count(*) FROM bitacora")).Should().Be(1);
    }

    [Fact]
    public async Task RF_28_Compuerta_ClaveRevocada_Responde401ClaveInvalida()
    {
        // RF-28 · CA3: después de revocarla, la compuerta responde 401 clave_invalida (en menos de 10 s: al instante).
        var e = await CrearEscenario(urlOrigen: "http://origen.prueba");
        await NuevaRutaCompleta(e.ApiId, "GET", "/guias/{numero}", expuesta: true);
        var produccion = (await EmitirClavesParaSuscripcion(e))[0];
        await ResincronizarAsync();
        await using var compuerta = new WebApplicationFactory<ProgramaCompuerta>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
            web.ConfigureTestServices(s => s.AddSingleton(new HttpMessageInvoker(new OrigenQueResponde200())));
        });
        using var clienteCompuerta = compuerta.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{HostApi}") });

        var antes = await LlamarCompuerta(clienteCompuerta, produccion.Clave);
        (await Portal(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/revocar", e)).Dispose();
        var despues = await LlamarCompuerta(clienteCompuerta, produccion.Clave);

        antes.Codigo.Should().Be(HttpStatusCode.OK);
        despues.Codigo.Should().Be(HttpStatusCode.Unauthorized);
        despues.Cuerpo.Should().Contain("clave_invalida");
    }

    // ---------- CU-13 · Emitir una clave nueva ----------

    [Fact]
    public async Task RF_28_EmitirDespuesDeRevocar_EmiteUnaClaveNuevaDelMismoTipo()
    {
        // RF-28 · CA4: después de revocar una clave, el consumidor puede emitir otra del mismo tipo.
        var e = await CrearEscenario();
        var pruebas = (await EmitirClavesParaSuscripcion(e))[1];
        await Revocar(e, pruebas.Id);

        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/claves/emitir", e, new { tipo = "pruebas" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var clave = json.GetProperty("clave").GetString()!;
        clave.Should().MatchRegex("^shp_prueba_[0-9A-Za-z]{26}$");
        json.GetProperty("estado").GetString().Should().Be("activa");
        (await Hash(LlavesRedis.Clave(ContextoClave.CalcularHash(clave)))).Should().NotBeEmpty();
    }

    [Fact]
    public async Task RF_28_Emitir_ConOtraActivaDelMismoTipo_Responde409()
    {
        // CU-13 · CA4: solo si no hay otra activa del mismo tipo.
        var e = await CrearEscenario();
        await EmitirClavesParaSuscripcion(e);

        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/claves/emitir", e, new { tipo = "produccion" });

        await AfirmarProblema(respuesta, HttpStatusCode.Conflict, "clave_activa_existente");
        (await Escalar<long>("SELECT count(*) FROM clave")).Should().Be(2);
    }

    [Theory]
    [InlineData("{\"tipo\":\"otro\"}")]
    [InlineData("{}")]
    public async Task RF_28_Emitir_TipoInvalido_Responde400ConErrorEnTipo(string cuerpo)
    {
        var e = await CrearEscenario();

        using var peticion = PeticionPortal(HttpMethod.Post, "/api/portal/claves/emitir", e);
        peticion.Content = new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json");
        using var respuesta = await Cliente.SendAsync(peticion);

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        problema.GetProperty("errores").TryGetProperty("tipo", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RF_28_Emitir_SinSuscripcion_Responde404()
    {
        var e = await CrearEscenario();
        await CambiarEstado("suscripcion_api", e.SuscripcionId, "finalizada");

        using var respuesta = await Portal(HttpMethod.Post, "/api/portal/claves/emitir", e, new { tipo = "pruebas" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Bitácora (10 §7) ----------

    [Fact]
    public async Task RF_41_Bitacora_RegistraRotarYRevocarConLosTextosDe10Seccion7()
    {
        // JG-07 · CA6: clave.rotada, clave.revocada_por_consumidor y clave.revocada_por_proveedor.
        var e = await CrearEscenario(empresa: "Boutique Cayalá");
        var emitidas = await EmitirClavesParaSuscripcion(e);
        var cookie = await SesionPersonal(e.OrganizacionId, "propietario", "Ana Lucía Morales");

        (await Portal(HttpMethod.Post, $"/api/portal/claves/{emitidas[0].Id}/rotar", e)).Dispose();
        (await Portal(HttpMethod.Post, $"/api/portal/claves/{emitidas[0].Id}/revocar", e)).Dispose();
        (await Panel(HttpMethod.Post, $"/api/apis/{e.ApiId}/claves/{emitidas[1].Id}/revocar", cookie)).Dispose();

        var entradas = await Filas("""
            SELECT accion, descripcion, actor_tipo, actor_nombre, organizacion_id, objetivo_tipo, objetivo_id
            FROM bitacora ORDER BY id
            """);
        entradas.Should().HaveCount(3);
        entradas[0].Should().Equal("clave.rotada",
            $"Boutique Cayalá rotó su clave de producción en la {NombreApi}",
            "consumidor", "María José Quiñónez", e.OrganizacionId, "clave", emitidas[0].Id);
        entradas[1].Should().Equal("clave.revocada_por_consumidor",
            $"Boutique Cayalá revocó su clave de producción en la {NombreApi}",
            "consumidor", "María José Quiñónez", e.OrganizacionId, "clave", emitidas[0].Id);
        entradas[2].Should().Equal("clave.revocada_por_proveedor",
            $"Revocó la clave de pruebas de Boutique Cayalá en la {NombreApi}",
            "usuario", "Ana Lucía Morales", e.OrganizacionId, "clave", emitidas[1].Id);
    }

    // ---------- Listados (A4.3 y B2.3) ----------

    [Fact]
    public async Task RF_26_ClavesDeApi_AgrupaPorConsumidorConPlanTipoYEstadoEnmascaradas()
    {
        // JG-07 · CA5: enmascaradas y agrupadas por consumidor, con el plan, el tipo y el estado (A4.3).
        var e = await CrearEscenario(empresa: "Mercadito Antigua");
        var otro = await OtroConsumidor(e, "Boutique Cayalá", "Básico");
        var deE = await EmitirClavesParaSuscripcion(e);
        var deOtro = await EmitirClavesParaSuscripcion(otro);
        (await Portal(HttpMethod.Post, $"/api/portal/claves/{deOtro[0].Id}/rotar", otro)).Dispose();
        await Revocar(otro, deOtro[1].Id);
        var cookie = await SesionPersonal(e.OrganizacionId, "lector");

        using var respuesta = await Panel(HttpMethod.Get, $"/api/apis/{e.ApiId}/claves", cookie);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var texto = await respuesta.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(texto).RootElement;
        json.GetProperty("total").GetInt32().Should().Be(2);
        var filas = json.GetProperty("elementos").EnumerateArray().ToArray();
        filas.Select(f => f.GetProperty("consumidor").GetString()).Should().Equal("Mercadito Antigua", "Boutique Cayalá");
        filas.Select(f => f.GetProperty("plan").GetString()).Should().Equal("Comercio", "Básico");
        filas[0].GetProperty("consumidorId").GetGuid().Should().Be(e.ConsumidorId);
        Resumen(filas[0]).Should().Equal(("produccion", "activa", deE[0].ClaveEnmascarada), ("pruebas", "activa", deE[1].ClaveEnmascarada));
        Resumen(filas[1]).Select(c => (c.Tipo, c.Estado)).Should().Equal(
            ("produccion", "activa"), ("produccion", "rotada"), ("pruebas", "revocada"));
        foreach (var clave in deE.Concat(deOtro))
        {
            texto.Should().NotContain(clave.Clave, "una clave completa nunca vuelve a aparecer (JG-07 · CA7)");
        }
    }

    [Fact]
    public async Task RF_27_ClavesDelConsumidor_OcultaRotadasVencidasYRevocadasReemplazadas()
    {
        // B2.3: activas, rotadas durante sus 24 horas y, si un tipo no tiene activa, la última revocada.
        var e = await CrearEscenario();
        var emitidas = await EmitirClavesParaSuscripcion(e);
        (await Portal(HttpMethod.Post, $"/api/portal/claves/{emitidas[0].Id}/rotar", e)).Dispose();
        await Revocar(e, emitidas[1].Id);
        (await Portal(HttpMethod.Post, "/api/portal/claves/emitir", e, new { tipo = "pruebas" })).Dispose();

        using var conRotada = await Portal(HttpMethod.Get, "/api/portal/claves", e);
        Reloj.Ahora = Reloj.Ahora.AddHours(24);
        using var sinRotada = await Portal(HttpMethod.Get, "/api/portal/claves", e);

        conRotada.StatusCode.Should().Be(HttpStatusCode.OK);
        var antes = Resumen((await conRotada.Content.ReadFromJsonAsync<JsonElement>()));
        antes.Select(c => (c.Tipo, c.Estado)).Should().Equal(("produccion", "activa"), ("produccion", "rotada"), ("pruebas", "activa"));
        var despues = Resumen((await sinRotada.Content.ReadFromJsonAsync<JsonElement>()));
        despues.Select(c => (c.Tipo, c.Estado)).Should().Equal(("produccion", "activa"), ("pruebas", "activa"));
    }

    [Fact]
    public async Task RF_26_ClavesDelConsumidor_SinSuscripcion_ListaVacia()
    {
        var e = await CrearEscenario();
        await CambiarEstado("suscripcion_api", e.SuscripcionId, "finalizada");

        using var respuesta = await Portal(HttpMethod.Get, "/api/portal/claves", e);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("elementos").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task RF_26_ClavesDeApi_PaginaPorConsumidor()
    {
        var e = await CrearEscenario(empresa: "Mercadito Antigua");
        await OtroConsumidor(e, "Boutique Cayalá", "Básico");
        await OtroConsumidor(e, "Ferretería Zona 11", "Volumen");
        var cookie = await SesionPersonal(e.OrganizacionId, "editor");

        using var respuesta = await Panel(HttpMethod.Get, $"/api/apis/{e.ApiId}/claves?pagina=2&tamano=1", cookie);
        using var invalida = await Panel(HttpMethod.Get, $"/api/apis/{e.ApiId}/claves?tamano=101", cookie);

        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("total").GetInt32().Should().Be(3);
        json.GetProperty("elementos").EnumerateArray().Single().GetProperty("consumidor").GetString().Should().Be("Boutique Cayalá");
        await AfirmarProblema(invalida, HttpStatusCode.BadRequest, "datos_invalidos");
    }

    // ---------- JG-07 · CA7: la clave completa no se registra ----------

    [Fact]
    public async Task RNF_07_Registros_NuncaContienenLaClaveCompleta()
    {
        var e = await CrearEscenario();
        var emitidas = await EmitirClavesParaSuscripcion(e);
        using var rotar = await Portal(HttpMethod.Post, $"/api/portal/claves/{emitidas[0].Id}/rotar", e);
        var nueva = (await rotar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("clave").GetString()!;
        await Revocar(e, emitidas[1].Id);
        using var emitir = await Portal(HttpMethod.Post, "/api/portal/claves/emitir", e, new { tipo = "pruebas" });
        var otra = (await emitir.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("clave").GetString()!;

        Registros.Entradas.Should().NotBeEmpty();
        foreach (var clave in emitidas.Select(c => c.Clave).Append(nueva).Append(otra))
        {
            Registros.Entradas.Should().NotContain(r => r.Texto.Contains(clave, StringComparison.Ordinal));
            Registros.Entradas.Should().NotContain(r => r.Texto.Contains(ContextoClave.CalcularHash(clave), StringComparison.Ordinal));
        }
    }

    // ---------- H-97: con la cookie portal_sesion real (EM-05) ----------

    [Fact]
    public async Task RF_27_Rotar_ConLaCookieRealDelPortal_Funciona()
    {
        var e = await CrearEscenario();
        var produccion = (await EmitirClavesParaSuscripcion(e))[0];
        var valor = Guid.NewGuid().ToString("N");
        using (var alcance = Fabrica.Services.CreateScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
            db.Add(Sesion.IniciarConsumidor(SeguridadTokens.HashearToken(valor), e.ConsumidorId, HostPortal, null, null, Reloj.Ahora));
            await db.SaveChangesAsync();
        }

        var peticion = new HttpRequestMessage(HttpMethod.Post, $"/api/portal/claves/{produccion.Id}/rotar");
        peticion.Headers.Host = HostPortal;
        peticion.Headers.Add("X-Requested-With", "shapi");
        peticion.Headers.Add("Cookie", $"{ConsumidorAutenticacionOpciones.Cookie}={valor}");
        using var respuesta = await Cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK, "respuesta API: {0}", await respuesta.Content.ReadAsStringAsync());
        (await Escalar<string>($"SELECT estado FROM clave WHERE id = '{produccion.Id}'")).Should().Be("rotada");
    }

    // ---------- JG-07 · CA8: aislamiento (RNF-08) ----------

    // H-96: revocar la clave de otra organización responde 404 sin intentar bloquear su fila. Si la bloqueara, la
    // petición esperaría a que termine la transacción que la tiene tomada.
    [Fact]
    public async Task RNF_08_Proveedor_ClaveDeOtraOrganizacion_NoBloqueaSuFila()
    {
        var e = await CrearEscenario();
        var claveAjena = (await EmitirClavesParaSuscripcion(e))[0];
        var otra = await CrearEscenario(subdominio: "agro");
        var cookieOtra = await SesionPersonal(otra.OrganizacionId, "propietario");
        await using var conexion = new NpgsqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();
        await using (var bloqueo = new NpgsqlCommand($"SELECT 1 FROM clave WHERE id = '{claveAjena.Id}' FOR UPDATE", conexion, transaccion))
        {
            await bloqueo.ExecuteNonQueryAsync();
        }

        var revocar = Panel(HttpMethod.Post, $"/api/apis/{otra.ApiId}/claves/{claveAjena.Id}/revocar", cookieOtra);
        var terminada = await Task.WhenAny(revocar, Task.Delay(TimeSpan.FromSeconds(5)));

        try
        {
            terminada.Should().BeSameAs(revocar, "la fila de otra organización no se debe bloquear");
            (await revocar).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await transaccion.RollbackAsync();
            (await revocar).Dispose();
        }
    }

    [Fact]
    public async Task RNF_08_Proveedor_ApiOClaveDeOtraOrganizacion_Responde404()
    {
        var e = await CrearEscenario();
        var claveAjena = (await EmitirClavesParaSuscripcion(e))[0];
        var otra = await CrearEscenario(subdominio: "agro");
        var cookieOtra = await SesionPersonal(otra.OrganizacionId, "propietario");

        using var listar = await Panel(HttpMethod.Get, $"/api/apis/{e.ApiId}/claves", cookieOtra);
        using var revocarConApiAjena = await Panel(HttpMethod.Post, $"/api/apis/{e.ApiId}/claves/{claveAjena.Id}/revocar", cookieOtra);
        using var revocarConApiPropia = await Panel(HttpMethod.Post, $"/api/apis/{otra.ApiId}/claves/{claveAjena.Id}/revocar", cookieOtra);

        listar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        revocarConApiAjena.StatusCode.Should().Be(HttpStatusCode.NotFound);
        revocarConApiPropia.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Escalar<string>($"SELECT estado FROM clave WHERE id = '{claveAjena.Id}'")).Should().Be("activa");
    }

    [Fact]
    public async Task RNF_08_Proveedor_ClaveDeOtraApiDeLaMismaOrganizacion_Responde404()
    {
        var e = await CrearEscenario();
        var clave = (await EmitirClavesParaSuscripcion(e))[0];
        var otraApi = await NuevaApiPublicada(e.OrganizacionId, subdominio: "rastreo");
        var cookie = await SesionPersonal(e.OrganizacionId, "propietario");

        using var respuesta = await Panel(HttpMethod.Post, $"/api/apis/{otraApi}/claves/{clave.Id}/revocar", cookie);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RNF_08_Consumidor_ClaveDeOtroConsumidor_Responde404()
    {
        var e = await CrearEscenario();
        var claveAjena = (await EmitirClavesParaSuscripcion(e))[0];
        var otro = await OtroConsumidor(e, "Ferretería Zona 11", "Volumen");
        await EmitirClavesParaSuscripcion(otro);

        using var rotar = await Portal(HttpMethod.Post, $"/api/portal/claves/{claveAjena.Id}/rotar", otro);
        using var revocar = await Portal(HttpMethod.Post, $"/api/portal/claves/{claveAjena.Id}/revocar", otro);
        using var listar = await Portal(HttpMethod.Get, "/api/portal/claves", otro);

        rotar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        revocar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var ids = (await listar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("elementos").EnumerateArray()
            .Select(c => c.GetProperty("id").GetGuid());
        ids.Should().NotContain(claveAjena.Id);
        (await Escalar<string>($"SELECT estado FROM clave WHERE id = '{claveAjena.Id}'")).Should().Be("activa");
    }

    [Fact]
    public async Task RNF_08_Consumidor_SesionDeOtraOrganizacionEnEsteHost_Responde404()
    {
        // 10 §2: la sesión del consumidor debe pertenecer a la organización del host.
        var e = await CrearEscenario();
        var clave = (await EmitirClavesParaSuscripcion(e))[0];
        var intruso = await CrearEscenario(subdominio: "agro");

        using var listar = await Portal(HttpMethod.Get, "/api/portal/claves", intruso, host: HostPortal);
        using var revocar = await Portal(HttpMethod.Post, $"/api/portal/claves/{clave.Id}/revocar", intruso, host: HostPortal);

        listar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        revocar.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RNF_08_Consumidor_ClaveDeOtraApiDeLaMismaOrganizacion_Responde404()
    {
        // En el portal de una API solo se administran las claves de esa API.
        var e = await CrearEscenario();
        var otraApi = await NuevaApiPublicada(e.OrganizacionId, subdominio: "rastreo");
        var plan = await NuevoPlanApiCon(otraApi, "Comercio", 5000, 60);
        var suscripcion = await NuevaSuscripcionApiEn(e.ConsumidorId, otraApi, plan, "activa", Reloj.Ahora.AddDays(-1), Reloj.Ahora.AddDays(29));
        var claveDeOtraApi = (await EmitirClavesParaSuscripcion(e with { ApiId = otraApi, SuscripcionId = suscripcion }))[0];

        using var respuesta = await Portal(HttpMethod.Post, $"/api/portal/claves/{claveDeOtraApi.Id}/revocar", e);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RF_07_Portal_SesionDelPersonal_Responde401()
    {
        // 04 §3.3: los endpoints del portal son del ámbito consumidor. Desde H-05 (auditoría del 3 oct), /api/portal/*
        // solo lee portal_sesion, así que la sesión del personal no autentica: 401, no 403. Antes daba 403 solo porque el
        // esquema de prueba evaluaba shapi_sesion en el portal.
        var e = await CrearEscenario();
        var cookie = await SesionPersonal(e.OrganizacionId, "propietario");

        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/portal/claves");
        peticion.Headers.Host = HostPortal;
        peticion.Headers.Add("Cookie", cookie);
        using var respuesta = await Cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- Datos ----------

    private sealed record Escenario(Guid OrganizacionId, Guid ApiId, Guid ConsumidorId, Guid SuscripcionId);

    /// <summary>Una organización con la API de Cotización de Envíos publicada, el plan Comercio y un consumidor suscrito.</summary>
    private async Task<Escenario> CrearEscenario(
        string subdominio = "envios", string empresa = "Boutique Cayalá", string urlOrigen = "https://origen.ejemplo.com")
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApiPublicada(organizacion, subdominio: subdominio, urlOrigen: urlOrigen);
        await Ejecutar($"UPDATE api SET nombre = '{NombreApi}' WHERE id = '{api}'");
        var e = new Escenario(organizacion, api, Guid.Empty, Guid.Empty);
        return await OtroConsumidor(e, empresa, "Comercio");
    }

    /// <summary>Otro consumidor de la misma API, suscrito a un plan nuevo.</summary>
    private async Task<Escenario> OtroConsumidor(Escenario e, string empresa, string plan)
    {
        var consumidor = await Escalar<Guid>($"""
            INSERT INTO consumidor (id, organizacion_id, nombre, nombre_empresa, correo, hash_contrasena, estado)
            VALUES (gen_random_uuid(), '{e.OrganizacionId}', 'María José Quiñónez', '{empresa}',
                    gen_random_uuid() || '@ejemplo.com', 'hash', 'activo')
            RETURNING id
            """);
        var planId = await NuevoPlanApiCon(e.ApiId, plan, 5000, 60);
        // Cada consumidor contrata un minuto después que el anterior: A4.3 los ordena por contratación.
        var contratados = await Escalar<long>($"SELECT count(*) FROM suscripcion_api WHERE api_id = '{e.ApiId}'");
        var inicio = Reloj.Ahora.AddDays(-1).AddMinutes(contratados);
        var suscripcion = await NuevaSuscripcionApiEn(consumidor, e.ApiId, planId, "activa", inicio, inicio.AddDays(30));
        return e with { ConsumidorId = consumidor, SuscripcionId = suscripcion };
    }

    /// <summary>
    /// <see cref="IServicioClaves.EmitirClavesParaSuscripcion"/>, como lo llamará la contratación (EM-08): dentro de la
    /// petición del consumidor, con el contexto de su organización.
    /// </summary>
    private async Task<IReadOnlyList<ClaveEmitida>> EmitirClavesParaSuscripcion(Escenario e)
    {
        using var alcance = Fabrica.Services.CreateScope();
        var contexto = new DefaultHttpContext { User = SesionConsumidorDePrueba.Principal(e.ConsumidorId, e.OrganizacionId) };
        alcance.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = contexto;
        return await alcance.ServiceProvider.GetRequiredService<IServicioClaves>().EmitirClavesParaSuscripcion(e.SuscripcionId);
    }

    private async Task Revocar(Escenario e, Guid claveId)
    {
        using var respuesta = await Portal(HttpMethod.Post, $"/api/portal/claves/{claveId}/revocar", e);
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> SesionPersonal(Guid organizacionId, string rol, string nombre = "Ana Lucía Morales")
    {
        var usuario = await Escalar<Guid>($"""
            INSERT INTO usuario (id, nombre, correo, estado)
            VALUES (gen_random_uuid(), '{nombre}', gen_random_uuid() || '@ejemplo.com', 'activo') RETURNING id
            """);
        await NuevaMembresia(usuario, organizacionId, rol);
        var valor = Guid.NewGuid().ToString("N");
        using var alcance = Fabrica.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
        db.Add(Sesion.IniciarPersonal(SeguridadTokens.HashearToken(valor), usuario, "localhost", null, null, Reloj.Ahora));
        await db.SaveChangesAsync();
        return $"shapi_sesion={valor}";
    }

    private async Task ResincronizarAsync()
    {
        await using var servicios = CrearServicios(ajustar: s => s.AgregarResincronizacion());
        using var alcance = servicios.CreateScope();
        await alcance.ServiceProvider.GetRequiredService<ResincronizarCache>().EjecutarAsync(CancellationToken.None);
    }

    // ---------- HTTP ----------

    private Task<HttpResponseMessage> Panel(HttpMethod metodo, string url, string cookie)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Add("Cookie", cookie);
        peticion.Headers.Add("X-Requested-With", "shapi");
        return Cliente.SendAsync(peticion);
    }

    private HttpRequestMessage PeticionPortal(HttpMethod metodo, string url, Escenario e, string host = HostPortal)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Host = host;
        peticion.Headers.Add("X-Requested-With", "shapi");
        peticion.Headers.Add(SesionConsumidorDePrueba.Cabecera, $"{e.ConsumidorId}:{e.OrganizacionId}");
        return peticion;
    }

    private Task<HttpResponseMessage> Portal(HttpMethod metodo, string url, Escenario e, object? cuerpo = null, string host = HostPortal)
    {
        var peticion = PeticionPortal(metodo, url, e, host);
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        return Cliente.SendAsync(peticion);
    }

    private static async Task<(HttpStatusCode Codigo, string Cuerpo)> LlamarCompuerta(HttpClient cliente, string clave)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/guias/GT123");
        peticion.Headers.Add("X-Api-Key", clave);
        using var respuesta = await cliente.SendAsync(peticion);
        return (respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync());
    }

    private static async Task<JsonElement> AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado);
        respuesta.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("codigo").GetString().Should().Be(codigo);
        return json;
    }

    private static string Enmascarar(string clave) => $"{clave[..(clave.LastIndexOf('_') + 1)]}••••{clave[^4..]}";

    private static (string Tipo, string Estado, string Enmascarada)[] Resumen(JsonElement conClaves)
    {
        var claves = conClaves.TryGetProperty("claves", out var deFila) ? deFila : conClaves.GetProperty("elementos");
        return [.. claves.EnumerateArray().Select(c => (
            c.GetProperty("tipo").GetString()!, c.GetProperty("estado").GetString()!, c.GetProperty("claveEnmascarada").GetString()!))];
    }

    private async Task<object?[]> Fila(string sql) => (await Filas(sql)).Single();

    private async Task<List<object?[]>> Filas(string sql)
    {
        await using var conexion = new NpgsqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        var filas = new List<object?[]>();
        while (await lector.ReadAsync())
        {
            var valores = new object?[lector.FieldCount];
            for (var i = 0; i < lector.FieldCount; i++)
            {
                valores[i] = lector.IsDBNull(i) ? null : lector.GetValue(i);
            }

            filas.Add(valores);
        }

        return filas;
    }

    private sealed class OrigenQueResponde200 : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
    }
}

/// <summary>
/// Atajo de las pruebas para la sesión del consumidor: autentica con la cabecera <see cref="Cabecera"/>
/// (<c>{consumidorId}:{organizacionId}</c>) y pone los claims que el módulo Claves espera de la sesión del portal. Sin
/// la cabecera, la petición sigue con la selección real de la aplicación (personal o <c>portal_sesion</c>, según la ruta).
/// </summary>
internal sealed class SesionConsumidorDePrueba(
    IOptionsMonitor<AuthenticationSchemeOptions> opciones, ILoggerFactory registros, UrlEncoder codificador)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, registros, codificador)
{
    public const string Esquema = "ConsumidorDePrueba";
    public const string Cabecera = "X-Prueba-Consumidor";
    private const string Seleccion = "SeleccionDePrueba";

    public static void Registrar(IServiceCollection servicios) =>
        servicios.AddAuthentication(o =>
            {
                o.DefaultScheme = Seleccion;
                o.DefaultAuthenticateScheme = Seleccion;
                o.DefaultChallengeScheme = Seleccion;
                o.DefaultForbidScheme = Seleccion;
            })
            .AddScheme<AuthenticationSchemeOptions, SesionConsumidorDePrueba>(Esquema, null)
            // Sin la cabecera, la petición sigue con la selección real de la aplicación (por la ruta, H-05), así que una
            // cookie portal_sesion verdadera también funciona en /api/portal/* (H-97).
            .AddPolicyScheme(Seleccion, null, o => o.ForwardDefaultSelector = contexto =>
                contexto.Request.Headers.ContainsKey(Cabecera) ? Esquema : EsquemaAutenticacionPortal.Esquema);

    public static ClaimsPrincipal Principal(Guid consumidorId, Guid organizacionId) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, consumidorId.ToString()),
            new Claim(PoliticasAutorizacion.ClaimOrganizacion, organizacionId.ToString()),
            new Claim(PoliticasAutorizacion.ClaimAmbito, nameof(AmbitoSesion.Consumidor)),
        ], Esquema));

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var partes = Request.Headers[Cabecera].ToString().Split(':');
        if (partes.Length != 2 || !Guid.TryParse(partes[0], out var consumidorId) || !Guid.TryParse(partes[1], out var organizacionId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Cabecera de prueba inválida."));
        }

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Principal(consumidorId, organizacionId), Esquema)));
    }
}
