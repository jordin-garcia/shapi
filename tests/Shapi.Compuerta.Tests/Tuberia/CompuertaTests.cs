using System.Net;
using System.Text;
using System.Text.Json;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Tuberia;

// JG-02: filtros 1 (API), 2 (clave) y 8 (reenvío) de 08 §3, con Redis real y un origen en memoria.
public class CompuertaTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>
{
    private const string ClaveDemo = "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e";

    // SHA-256 de ClaveDemo, calculado aparte con sha256sum.
    private const string HashClaveDemo = "47d162d7ace0b83b8235011dc58124d30b62dd0fde19b1f38b99ca7d3195a971";

    [Fact]
    public async Task Reenviar_ClaveValida_ConservaMetodoRutaQueryYCuerpoYDevuelveLaRespuestaDelOrigen()
    {
        // RF-31 (criterio 1)
        var (host, _, clave) = await SembrarApiYClaveAsync();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/cotizaciones?moneda=GTQ&urgente=1")
        {
            Content = new StringContent("{\"origen\":\"0901\",\"destino\":\"0301\",\"peso_kg\":2.5}", Encoding.UTF8,
                "application/json"),
        };
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.GetValues("X-Origen").Should().Equal("falso");
        (await respuesta.Content.ReadAsStringAsync()).Should().Be(OrigenFalso.CuerpoRespuesta);
        var recibida = entorno.Origen.Ultima!;
        recibida.Metodo.Should().Be("POST");
        recibida.Ruta.Should().Be("/cotizaciones");
        recibida.Query.Should().Be("?moneda=GTQ&urgente=1");
        recibida.Cuerpo.Should().Be("{\"origen\":\"0901\",\"destino\":\"0301\",\"peso_kg\":2.5}");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Reenviar_OtrosMetodos_ConservaMetodoRutaYQuery(string metodo)
    {
        // RF-31 (criterio 1)
        var (host, _, clave) = await SembrarApiYClaveAsync();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), "/rastreo/GT123?detalle=1");
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        entorno.Origen.Ultima!.Metodo.Should().Be(metodo);
        entorno.Origen.Ultima.Ruta.Should().Be("/rastreo/GT123");
        entorno.Origen.Ultima.Query.Should().Be("?detalle=1");
    }

    [Fact]
    public async Task ResolverApi_HostInexistente_Responde404ApiNoEncontrada()
    {
        // RF-29 (criterio 2)
        using var cliente = entorno.Cliente($"{Guid.NewGuid():N}.api.shapi.localhost");
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", ClaveDemo);

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "api_no_encontrada");
    }

    [Theory]
    [InlineData("despublicada")]
    [InlineData("borrador")]
    public async Task ResolverApi_ApiNoPublicada_Responde404ApiNoEncontrada(string estado)
    {
        // RF-29 y RF-14 (criterio 2)
        var host = NuevoHost();
        var api = await entorno.SembrarApiAsync(host, estado);
        var clave = NuevaClave();
        await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(clave));
        entorno.Origen.Olvidar();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "api_no_encontrada");
        entorno.Origen.Ultima.Should().BeNull();
    }

    [Fact]
    public async Task ResolverApi_HostConMayusculas_EncuentraLaApi()
    {
        // 07 §4: el host de la llave va en minúsculas.
        var (host, _, clave) = await SembrarApiYClaveAsync();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", clave);
        peticion.Headers.Host = host.ToUpperInvariant();

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ResolverApi_RutaSaludEnElHostDeUnaApi_PasaPorLaTuberia()
    {
        // La ruta /salud de la compuerta no debe tapar una ruta /salud del proveedor.
        using var cliente = entorno.Cliente(NuevoHost());

        var respuesta = await cliente.GetAsync("/salud");

        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "api_no_encontrada");
    }

    [Fact]
    public async Task ValidarClave_SinCabecera_Responde401ClaveAusente()
    {
        // RF-29 (criterio 3)
        var (host, _, _) = await SembrarApiYClaveAsync();
        entorno.Origen.Olvidar();
        using var cliente = entorno.Cliente(host);

        var respuesta = await cliente.GetAsync("/cotizaciones");

        await VerificarErrorAsync(respuesta, HttpStatusCode.Unauthorized, "clave_ausente");
        VerificarWwwAuthenticate(respuesta);
        entorno.Origen.Ultima.Should().BeNull();
    }

    [Fact]
    public async Task ValidarClave_CabeceraVacia_Responde401ClaveAusente()
    {
        // RF-29 (criterio 3)
        var (host, _, _) = await SembrarApiYClaveAsync();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.TryAddWithoutValidation("X-Api-Key", "");

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.Unauthorized, "clave_ausente");
        VerificarWwwAuthenticate(respuesta);
    }

    [Fact]
    public async Task ValidarClave_ClaveQueNoEstaEnRedis_Responde401ClaveInvalida()
    {
        // RF-29 (criterio 3)
        var (host, _, _) = await SembrarApiYClaveAsync();
        entorno.Origen.Olvidar();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", NuevaClave());

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.Unauthorized, "clave_invalida");
        VerificarWwwAuthenticate(respuesta);
        entorno.Origen.Ultima.Should().BeNull();
    }

    [Fact]
    public async Task ValidarClave_ClaveDeOtraApi_Responde401ClaveInvalida()
    {
        // RF-29 (criterio 3): no se aceptan claves de otra API.
        var (host, _, _) = await SembrarApiYClaveAsync();
        var (_, _, claveDeOtraApi) = await SembrarApiYClaveAsync();
        entorno.Origen.Olvidar();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", claveDeOtraApi);

        var respuesta = await cliente.SendAsync(peticion);

        await VerificarErrorAsync(respuesta, HttpStatusCode.Unauthorized, "clave_invalida");
        VerificarWwwAuthenticate(respuesta);
        entorno.Origen.Ultima.Should().BeNull();
    }

    [Fact]
    public async Task ValidarClave_GuardadaPorSha256HexMinusculas_SeAcepta()
    {
        // Criterio 4: la llave usa el SHA-256 en hex minúsculas de la clave completa en UTF-8.
        var host = NuevoHost();
        var api = await entorno.SembrarApiAsync(host);
        await entorno.SembrarClaveAsync(api, HashClaveDemo);
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", ClaveDemo);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ValidarClave_PeticionesConClave_NuncaSeRegistraEnClaro()
    {
        // Criterio 4 y 10 §7: la clave nunca aparece en los registros.
        var (host, _, clave) = await SembrarApiYClaveAsync();
        var claveInvalida = NuevaClave();
        using var cliente = entorno.Cliente(host);

        foreach (var valor in new[] { clave, claveInvalida })
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
            peticion.Headers.Add("X-Api-Key", valor);
            await cliente.SendAsync(peticion);
        }

        entorno.Registros.Lineas.Should().NotBeEmpty();
        entorno.Registros.Lineas.Should().NotContain(linea => linea.Contains(clave, StringComparison.Ordinal));
        entorno.Registros.Lineas.Should().NotContain(linea => linea.Contains(claveInvalida, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ContextoClave.TipoProduccion, "produccion")]
    [InlineData(ContextoClave.TipoPruebas, "pruebas")]
    public async Task Reenviar_ClaveValida_ElOrigenNoRecibeLaClaveYRecibeConsumidorYEntorno(string tipo, string entornoEsperado)
    {
        // RF-31 y RF-45 (criterio 5)
        var host = NuevoHost();
        var api = await entorno.SembrarApiAsync(host);
        var clave = NuevaClave();
        var contextoClave = await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(clave), tipo);
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", clave);

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var cabeceras = entorno.Origen.Ultima!.Cabeceras;
        cabeceras.Should().NotContainKey("X-Api-Key");
        cabeceras.Values.Should().NotContain(valor => valor.Contains(clave, StringComparison.Ordinal));
        cabeceras["X-Shapi-Consumidor"].Should().Be(contextoClave.ConsumidorId.ToString());
        cabeceras["X-Shapi-Entorno"].Should().Be(entornoEsperado);
    }

    [Fact]
    public async Task Reenviar_ClaveValida_ElOrigenRecibeSuPropioHost()
    {
        // 08 §3, paso 8: el destino es url_origen; el host de la API no se reenvía como Host.
        var (host, _, clave) = await SembrarApiYClaveAsync();
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", clave);
        entorno.Origen.Olvidar();

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        entorno.Origen.Ultima!.Cabeceras["Host"].Should().Be(new Uri(EntornoCompuerta.UrlOrigen).Authority);
    }

    [Fact]
    public async Task Reenviar_ClienteEnviaCabecerasShapiFalsas_ElOrigenRecibeLasDeLaCompuerta()
    {
        // RF-31 (criterio 5): el consumidor no puede hacerse pasar por otro ni cambiar el entorno.
        var host = NuevoHost();
        var api = await entorno.SembrarApiAsync(host);
        var clave = NuevaClave();
        var contextoClave = await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(clave), ContextoClave.TipoPruebas);
        using var cliente = entorno.Cliente(host);
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", clave);
        peticion.Headers.Add("X-Shapi-Consumidor", Guid.NewGuid().ToString());
        peticion.Headers.Add("X-Shapi-Entorno", "produccion");

        await cliente.SendAsync(peticion);

        var cabeceras = entorno.Origen.Ultima!.Cabeceras;
        cabeceras["X-Shapi-Consumidor"].Should().Be(contextoClave.ConsumidorId.ToString());
        cabeceras["X-Shapi-Entorno"].Should().Be("pruebas");
    }

    private async Task<(string Host, ContextoApi Api, string Clave)> SembrarApiYClaveAsync()
    {
        var host = NuevoHost();
        var api = await entorno.SembrarApiAsync(host);
        var clave = NuevaClave();
        await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(clave));
        return (host, api, clave);
    }

    private static string NuevoHost() => $"api{Guid.NewGuid():N}.api.shapi.localhost";

    private static string NuevaClave() => $"shp_prod_{Guid.NewGuid():N}"[..35];

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

    private static void VerificarWwwAuthenticate(HttpResponseMessage respuesta)
    {
        respuesta.Headers.WwwAuthenticate.ToString().Should().Be("ApiKey header=\"X-Api-Key\"");
    }
}
