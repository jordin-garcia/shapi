using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Tuberia;

// JG-05: filtros 0, 3, 4 y 5, cabeceras hacia el origen y viajes a Redis (08 §3, §5, §6 y §8), con Redis real.
public class ValidacionesCompuertaTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>
{
    private const string Portal = "tienda.shapi.localhost";
    private const string OrigenPortal = "https://tienda.shapi.localhost";

    [Fact]
    public async Task RF_29_Organizacion_Suspendida_Responde403ApiNoDisponibleYNoReenvia()
    {
        // Criterio 1
        var (host, api, clave) = await SembrarAsync();
        await entorno.SembrarOrganizacionAsync(api.OrganizacionId, ContextoOrganizacion.EstadoSuspendida);

        var respuesta = await EnviarAsync(host, clave);

        await VerificarErrorAsync(respuesta, HttpStatusCode.Forbidden, "api_no_disponible");
        entorno.Origen.Ultima.Should().BeNull();
    }

    [Fact]
    public async Task RF_29_Organizacion_SinLlaveEnRedis_Responde403ApiNoDisponible()
    {
        var (host, api, clave) = await SembrarAsync();
        await entorno.Redis.GetDatabase().KeyDeleteAsync(LlavesRedis.Organizacion(api.OrganizacionId));

        var respuesta = await EnviarAsync(host, clave);

        await VerificarErrorAsync(respuesta, HttpStatusCode.Forbidden, "api_no_disponible");
    }

    [Fact]
    public async Task RF_29_Organizacion_SuspendidaAlInstante_SinEsperarCache()
    {
        // 08 §8 y RF-28: la compuerta no guarda en memoria las organizaciones.
        var (host, api, clave) = await SembrarAsync();
        (await EnviarAsync(host, clave)).StatusCode.Should().Be(HttpStatusCode.Created);

        await entorno.SembrarOrganizacionAsync(api.OrganizacionId, ContextoOrganizacion.EstadoSuspendida);

        await VerificarErrorAsync(await EnviarAsync(host, clave), HttpStatusCode.Forbidden, "api_no_disponible");
    }

    [Theory]
    [InlineData(ContextoSuscripcion.EstadoSuspendida)]
    [InlineData(null)]
    public async Task RF_29_Suscripcion_SuspendidaOFinalizada_Responde403SuscripcionInactiva(string? estado)
    {
        // Criterio 2: una suscripción finalizada no tiene llave en Redis (07 §4).
        var (host, _, clave) = await SembrarAsync(estadoSuscripcion: estado);

        var respuesta = await EnviarAsync(host, clave);

        await VerificarErrorAsync(respuesta, HttpStatusCode.Forbidden, "suscripcion_inactiva");
        entorno.Origen.Ultima.Should().BeNull();
    }

    [Fact]
    public async Task RF_29_Suscripcion_EnGraciaYConElCicloVencido_SeReenviaPorqueNoComparaFechas()
    {
        // Criterio 2 y 08 §3, filtro 4.
        var (host, _, clave) = await SembrarAsync();
        var suscripcionId = (await ClaveEnRedisAsync(clave)).SuscripcionId;
        await entorno.SembrarSuscripcionAsync(suscripcionId, ContextoSuscripcion.EstadoEnGracia, inicio: 1_000_000, fin: 1_000_100);

        var respuesta = await EnviarAsync(host, clave);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("GET", "/guias/GT123", HttpStatusCode.Created)]
    [InlineData("POST", "/cotizaciones", HttpStatusCode.Created)]
    [InlineData("GET", "/tarifas", HttpStatusCode.Forbidden)]
    [InlineData("DELETE", "/guias/GT123", HttpStatusCode.Forbidden)]
    [InlineData("GET", "/no-existe", HttpStatusCode.Forbidden)]
    public async Task RF_29_Ruta_SoloLasExpuestasPasan(string metodo, string camino, HttpStatusCode esperado)
    {
        // Criterio 3 y RF-10
        var (host, _, clave) = await SembrarAsync(rutas:
        [
            EntornoCompuerta.Ruta("GET", "/guias/{numero}"),
            EntornoCompuerta.Ruta("POST", "/cotizaciones"),
            EntornoCompuerta.Ruta("GET", "/tarifas", expuesta: false),
        ]);

        var respuesta = await EnviarAsync(host, clave, new HttpMethod(metodo), camino);

        if (esperado == HttpStatusCode.Forbidden)
        {
            await VerificarErrorAsync(respuesta, HttpStatusCode.Forbidden, "ruta_no_permitida");
            entorno.Origen.Ultima.Should().BeNull();
        }
        else
        {
            respuesta.StatusCode.Should().Be(esperado);
            entorno.Origen.Ultima!.Ruta.Should().Be(camino);
        }
    }

    [Fact]
    public async Task RF_29_Rutas_EnMemoriaHastaQueCambiaLaVersion()
    {
        // Criterio 3 y 08 §8: las rutas se guardan en memoria, pero un cambio de version se aplica en la siguiente
        // petición.
        var (host, api, clave) = await SembrarAsync(rutas: [EntornoCompuerta.Ruta("GET", "/guias")]);
        (await EnviarAsync(host, clave, HttpMethod.Get, "/guias")).StatusCode.Should().Be(HttpStatusCode.Created);

        // Sin cambiar la version, la compuerta sigue usando las rutas que ya tiene (hasta 5 s).
        await entorno.SembrarRutasAsync(api, [EntornoCompuerta.Ruta("GET", "/guias", expuesta: false)]);
        (await EnviarAsync(host, clave, HttpMethod.Get, "/guias")).StatusCode.Should().Be(HttpStatusCode.Created);

        // El publicador sube la version en la misma transacción (JG-04): se aplica al instante.
        await entorno.CambiarVersionAsync(api, 2);
        await VerificarErrorAsync(await EnviarAsync(host, clave, HttpMethod.Get, "/guias"), HttpStatusCode.Forbidden,
            "ruta_no_permitida");
    }

    [Fact]
    public async Task RNF_01_ContextoDeRedis_DosViajesPorPeticion()
    {
        // Criterio 9 y 08 §8: un pipeline con api:host y clave, y otro con api, sus rutas, org y susc.
        var contador = new ContadorRedis();
        using var fabrica = entorno.Fabrica.WithWebHostBuilder(web => web.ConfigureTestServices(servicios =>
            servicios.AddSingleton(contador.Envolver(entorno.Redis))));
        var (host, api, clave) = await SembrarAsync();
        var claveRedis = await ClaveEnRedisAsync(clave);
        using var cliente = EntornoCompuerta.Cliente(fabrica, host);

        foreach (var vez in new[] { "primera (sin rutas en memoria)", "segunda (con rutas en memoria)" })
        {
            contador.Reiniciar();
            using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
            peticion.Headers.Add("X-Api-Key", clave);

            var respuesta = await cliente.SendAsync(peticion);

            respuesta.StatusCode.Should().Be(HttpStatusCode.Created, vez);
            var viajes = contador.Viajes;
            viajes.Should().HaveCount(2, vez);
            viajes[0].Should().BeEquivalentTo(
                [$"StringGetAsync {LlavesRedis.ApiPorHost(host)}", $"HashGetAllAsync {LlavesRedis.Clave(ContextoClave.CalcularHash(clave))}"],
                vez);
            viajes[1].Should().Contain(
                [
                    $"HashGetAllAsync {LlavesRedis.Api(api.ApiId)}",
                    $"HashGetAllAsync {LlavesRedis.Organizacion(api.OrganizacionId)}",
                    $"HashGetAllAsync {LlavesRedis.Suscripcion(claveRedis.SuscripcionId)}",
                ],
                vez);
        }

        contador.Viajes[1].Should().NotContain($"StringGetAsync {LlavesRedis.RutasApi(api.ApiId)}",
            "la segunda vez las rutas ya están en memoria");
    }

    [Fact]
    public async Task RF_31_HaciaElOrigen_QuitaXApiKeyYCabecerasShapiDelCliente()
    {
        // Criterio 4: el cliente no puede mandarle al origen ninguna X-Shapi-*, ni un secreto falso.
        var (host, _, clave) = await SembrarAsync();

        await EnviarAsync(host, clave, cabeceras: new()
        {
            ["X-Shapi-Secreto"] = "secreto-falso",
            ["X-Shapi-Otra"] = "valor",
            ["x-shapi-minusculas"] = "valor",
            ["X-Cliente"] = "se conserva",
        });

        var cabeceras = entorno.Origen.Ultima!.Cabeceras;
        cabeceras.Should().NotContainKeys("X-Api-Key", "X-Shapi-Secreto", "X-Shapi-Otra", "x-shapi-minusculas");
        cabeceras.Keys.Where(c => c.StartsWith("X-Shapi-", StringComparison.OrdinalIgnoreCase))
            .Should().BeEquivalentTo(["X-Shapi-Consumidor", "X-Shapi-Entorno"]);
        cabeceras["X-Cliente"].Should().Be("se conserva");
    }

    [Fact]
    public async Task RF_31_HaciaElOrigen_ConSecreto_AgregaXShapiSecreto()
    {
        // Criterio 4
        var (host, _, clave) = await SembrarAsync(secreto: "shps_secreto_de_origen");

        await EnviarAsync(host, clave, cabeceras: new() { ["X-Shapi-Secreto"] = "secreto-falso" });

        entorno.Origen.Ultima!.Cabeceras["X-Shapi-Secreto"].Should().Be("shps_secreto_de_origen");
    }

    [Fact]
    public async Task RF_31_HaciaElOrigen_AgregaXForwarded()
    {
        // Criterio 4 y 08 §5: el host original viaja en X-Forwarded-Host; la IP del cliente se agrega a la cadena.
        var (host, _, clave) = await SembrarAsync();

        await EnviarAsync(host, clave);
        var sinCadena = entorno.Origen.Ultima!.Cabeceras;
        await EnviarAsync(host, clave, cabeceras: new() { ["X-Forwarded-For"] = "198.51.100.20" });
        var conCadena = entorno.Origen.Ultima!.Cabeceras;

        sinCadena["X-Forwarded-For"].Should().Be(EntornoCompuerta.IpCliente.ToString());
        sinCadena["X-Forwarded-Proto"].Should().Be("http");
        sinCadena["X-Forwarded-Host"].Should().Be(host);
        conCadena["X-Forwarded-For"].Should().Be($"198.51.100.20, {EntornoCompuerta.IpCliente}");
    }

    [Fact]
    public async Task RF_31_HaciaElOrigen_ProtoDelBorde_SeConserva()
    {
        // 08 §5: detrás de Caddy, la compuerta recibe http; el esquema original lo manda el borde.
        var (host, _, clave) = await SembrarAsync();

        await EnviarAsync(host, clave, cabeceras: new() { ["X-Forwarded-Proto"] = "https" });

        entorno.Origen.Ultima!.Cabeceras["X-Forwarded-Proto"].Should().Be("https");
    }

    [Fact]
    public async Task RF_31_HaciaElOrigen_QuitaLasCookiesDeShapiYConservaLasDemas()
    {
        // Criterio 8 y 08 §5
        var (host, _, clave) = await SembrarAsync();

        await EnviarAsync(host, clave, cabeceras: new()
        {
            ["Cookie"] = "preferencia=oscuro; shapi_sesion=abc123; portal_sesion=def456; carrito=7",
        });

        var cookies = entorno.Origen.Ultima!.Cabeceras["Cookie"];
        cookies.Should().Be("preferencia=oscuro; carrito=7");
        cookies.Should().NotContain("abc123").And.NotContain("def456");
    }

    [Fact]
    public async Task RF_31_HaciaElOrigen_SoloCookiesDeShapi_NoEnviaCookie()
    {
        // Criterio 8
        var (host, _, clave) = await SembrarAsync();

        await EnviarAsync(host, clave, cabeceras: new() { ["Cookie"] = "shapi_sesion=abc123; portal_sesion=def456" });

        entorno.Origen.Ultima!.Cabeceras.Should().NotContainKey("Cookie");
    }

    [Fact]
    public async Task RF_29_Cors_PreflightDelPortal_Responde204ConLasCabecerasYSinClave()
    {
        // Criterio 7 y 08 §6
        var (host, _, _) = await SembrarAsync(portalHost: Portal, rutas:
        [
            EntornoCompuerta.Ruta("POST", "/cotizaciones"),
            EntornoCompuerta.Ruta("GET", "/rastreo"),
            EntornoCompuerta.Ruta("DELETE", "/guias/{numero}", expuesta: false),
        ]);
        using var cliente = entorno.Cliente(host);
        using var peticion = Preflight(OrigenPortal);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Cabecera(respuesta, "Access-Control-Allow-Origin").Should().Be(OrigenPortal);
        Cabecera(respuesta, "Access-Control-Allow-Methods").Should().Be("GET, POST");
        Cabecera(respuesta, "Access-Control-Allow-Headers").Should().Be("X-Api-Key, Content-Type, Accept");
        Cabecera(respuesta, "Access-Control-Max-Age").Should().Be("600");
        Cabecera(respuesta, "Vary").Should().Contain("Origin");
        (await respuesta.Content.ReadAsStringAsync()).Should().BeEmpty();
        entorno.Origen.Ultima.Should().BeNull("el preflight no llega al origen");
    }

    [Fact]
    public async Task RF_29_Cors_PreflightDeOtroOrigen_Responde204SinAllowOrigin()
    {
        // Criterio 7: otros orígenes no reciben Access-Control-Allow-Origin.
        var (host, _, _) = await SembrarAsync(portalHost: Portal);
        using var cliente = entorno.Cliente(host);
        using var peticion = Preflight("https://otro.ejemplo.com");

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Cabecera(respuesta, "Access-Control-Allow-Origin").Should().BeNull();
        Cabecera(respuesta, "Access-Control-Allow-Methods").Should().BeNull();
    }

    [Fact]
    public async Task RF_29_Cors_PeticionDelPortal_AgregaAllowOriginYExposeHeaders()
    {
        // Criterio 7 y 08 §6: la respuesta del origen lleva las cabeceras de CORS de la compuerta.
        var (host, _, clave) = await SembrarAsync(portalHost: Portal);

        var respuesta = await EnviarAsync(host, clave, cabeceras: new() { ["Origin"] = OrigenPortal });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        Cabecera(respuesta, "Access-Control-Allow-Origin").Should().Be(OrigenPortal);
        Cabecera(respuesta, "Access-Control-Expose-Headers").Should().Be(
            "X-Shapi-Plan, X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset, X-Cuota-Limite, "
            + "X-Cuota-Restante, X-Cuota-Reinicio, X-Shapi-Cache");
    }

    [Fact]
    public async Task RF_29_Cors_RechazoParaElPortal_TambienLlevaAllowOrigin()
    {
        // 08 §6: el portal (consola de pruebas, DC-10) tiene que poder leer los errores de la compuerta.
        var (host, _, _) = await SembrarAsync(portalHost: Portal);

        var respuesta = await EnviarAsync(host, clave: null, cabeceras: new() { ["Origin"] = OrigenPortal });

        await VerificarErrorAsync(respuesta, HttpStatusCode.Unauthorized, "clave_ausente");
        Cabecera(respuesta, "Access-Control-Allow-Origin").Should().Be(OrigenPortal);
    }

    [Theory]
    [InlineData("https://otro.ejemplo.com")]
    [InlineData(null)]
    public async Task RF_29_Cors_OtroOrigenOSinOrigin_PasaSinAllowOrigin(string? origen)
    {
        // Criterio 7: las peticiones sin Origin pasan normal; las de otro origen, sin las cabeceras de CORS.
        var (host, _, clave) = await SembrarAsync(portalHost: Portal);
        var cabeceras = new Dictionary<string, string>();
        if (origen is not null)
        {
            cabeceras["Origin"] = origen;
        }

        var respuesta = await EnviarAsync(host, clave, cabeceras: cabeceras);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        Cabecera(respuesta, "Access-Control-Allow-Origin").Should().BeNull();
    }

    [Fact]
    public async Task RF_29_Cors_ElOrigenNoPuedeAbrirElCors()
    {
        // 08 §6: solo el portal de la API. Un "Access-Control-Allow-Origin: *" del origen no llega al navegador.
        var (host, _, clave) = await SembrarAsync(portalHost: Portal);
        entorno.Origen.Responder = http =>
        {
            http.Response.Headers["Access-Control-Allow-Origin"] = "*";
            http.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            return Task.CompletedTask;
        };

        try
        {
            var otro = await EnviarAsync(host, clave, cabeceras: new() { ["Origin"] = "https://otro.ejemplo.com" }, olvidar: false);
            var portal = await EnviarAsync(host, clave, cabeceras: new() { ["Origin"] = OrigenPortal }, olvidar: false);

            Cabecera(otro, "Access-Control-Allow-Origin").Should().BeNull();
            Cabecera(otro, "Access-Control-Allow-Credentials").Should().BeNull();
            Cabecera(portal, "Access-Control-Allow-Origin").Should().Be(OrigenPortal);
            Cabecera(portal, "Access-Control-Allow-Credentials").Should().BeNull();
        }
        finally
        {
            entorno.Origen.Olvidar();
        }
    }

    private static HttpRequestMessage Preflight(string origen)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Options, "/cotizaciones");
        peticion.Headers.Add("Origin", origen);
        peticion.Headers.Add("Access-Control-Request-Method", "POST");
        peticion.Headers.Add("Access-Control-Request-Headers", "x-api-key, content-type");
        return peticion;
    }

    private static string? Cabecera(HttpResponseMessage respuesta, string nombre) =>
        respuesta.Headers.TryGetValues(nombre, out var valores) || respuesta.Content.Headers.TryGetValues(nombre, out valores)
            ? string.Join(", ", valores)
            : null;

    private async Task<(string Host, ContextoApi Api, string Clave)> SembrarAsync(
        string? estadoSuscripcion = ContextoSuscripcion.EstadoActiva, IReadOnlyList<RutaCache>? rutas = null,
        string? secreto = null, string? portalHost = null)
    {
        var host = $"api{Guid.NewGuid():N}.api.shapi.localhost";
        var api = await entorno.SembrarApiAsync(host, secreto: secreto, portalHost: portalHost, rutas: rutas);
        var clave = $"shp_prod_{Guid.NewGuid():N}"[..35];
        await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(clave), estadoSuscripcion: estadoSuscripcion);
        return (host, api, clave);
    }

    private async Task<ContextoClave> ClaveEnRedisAsync(string clave)
    {
        var campos = await entorno.Redis.GetDatabase().HashGetAllAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(clave)));
        return ContextoClave.DesdeCampos(campos.ToDictionary(c => c.Name.ToString(), c => c.Value.ToString()))!;
    }

    private async Task<HttpResponseMessage> EnviarAsync(string host, string? clave, HttpMethod? metodo = null,
        string camino = "/cotizaciones", Dictionary<string, string>? cabeceras = null, bool olvidar = true)
    {
        if (olvidar)
        {
            entorno.Origen.Olvidar();
        }

        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(metodo ?? HttpMethod.Get, camino);
        if (clave is not null)
        {
            peticion.Headers.Add("X-Api-Key", clave);
        }

        foreach (var (nombre, valor) in cabeceras ?? [])
        {
            peticion.Headers.TryAddWithoutValidation(nombre, valor);
        }

        return await cliente.SendAsync(peticion);
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado);
        respuesta.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var error = documento.RootElement.GetProperty("error");
        error.GetProperty("codigo").GetString().Should().Be(codigo);
        error.GetProperty("estado").GetInt32().Should().Be((int)estado);
        error.GetProperty("mensaje").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
