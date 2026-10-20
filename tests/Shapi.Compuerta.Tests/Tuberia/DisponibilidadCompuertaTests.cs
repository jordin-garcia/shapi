using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Shapi.Compuerta.Tests.Soporte;
using Shapi.Contratos.Redis;
using Testcontainers.PostgreSql;

namespace Shapi.Compuerta.Tests.Tuberia;

// JG-19: solo se inicia la compuerta; la API de control y el trabajador nunca se ponen en marcha.
// Redis y PostgreSQL son contenedores exclusivos de estas pruebas; no se detienen servicios del equipo.
public class DisponibilidadCompuertaTests(EntornoCompuerta entorno) : IClassFixture<EntornoCompuerta>, IAsyncLifetime
{
    private const string CuerpoOrigen = "respuesta del origen sin el plano de control";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private OrigenReal _origen = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _origen = await OrigenReal.IniciarAsync(http => http.Response.WriteAsync(CuerpoOrigen));
    }

    public async Task DisposeAsync()
    {
        if (_origen is not null)
        {
            await _origen.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task RNF_02_PostgreSqlSeDetiene_EnrutaYConservaLosRechazosDeClaveYRuta()
    {
        var (host, clave) = await SembrarAsync();
        await using var fabrica = CrearFabrica();
        using var cliente = EntornoCompuerta.Cliente(fabrica, host);

        // Primero se comprueba que el origen responde mientras PostgreSQL está en marcha.
        await VerificarOrigenAsync(cliente, clave);
        await DetenerPostgreSqlAsync();

        await VerificarOrigenAsync(cliente, clave);
        using var invalida = await EnviarAsync(cliente, "shp_prod_00000000000000000000000000");
        await VerificarErrorAsync(invalida, HttpStatusCode.Unauthorized, "clave_invalida");
        using var oculta = await EnviarAsync(cliente, clave, "/tarifas");
        await VerificarErrorAsync(oculta, HttpStatusCode.Forbidden, "ruta_no_permitida");
        _origen.Peticiones.Should().Be(2, "los rechazos no deben llegar al origen");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RNF_04_SinApiDeControlNiTrabajador_EnrutaConPostgreSqlApagado(bool arrancarDespuesDeLaCaida)
    {
        var (host, clave) = await SembrarAsync();
        await using var fabrica = CrearFabrica();
        HttpClient cliente;
        if (arrancarDespuesDeLaCaida)
        {
            await DetenerPostgreSqlAsync();
            cliente = EntornoCompuerta.Cliente(fabrica, host);
        }
        else
        {
            cliente = EntornoCompuerta.Cliente(fabrica, host);
            await VerificarOrigenAsync(cliente, clave);
            await DetenerPostgreSqlAsync();
        }

        using (cliente)
        {
            // También cubre el arranque en frío: ninguna configuración se precargó en la compuerta.
            await VerificarOrigenAsync(cliente, clave);
            await VerificarOrigenAsync(cliente, clave);
        }

        _origen.Peticiones.Should().Be(arrancarDespuesDeLaCaida ? 2 : 3);
    }

    private async Task DetenerPostgreSqlAsync()
    {
        // No basta con omitir la conexión: PostgreSQL estuvo disponible y se apaga de verdad.
        _postgres.State.Should().Be(DotNet.Testcontainers.Containers.TestcontainersStates.Running);
        await _postgres.StopAsync();
        _postgres.State.Should().Be(DotNet.Testcontainers.Containers.TestcontainersStates.Exited);
        (await entorno.Redis.GetDatabase().PingAsync()).Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    private WebApplicationFactory<Program> CrearFabrica()
    {
        // Testcontainers deja de informar el puerto publicado después de StopAsync. Se conserva el destino
        // antes de apagar PostgreSQL, porque WebApplicationFactory configura el host al crear el primer cliente.
        var cadenaPostgres = _postgres.GetConnectionString();
        return new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", entorno.CadenaRedis);
            // Si apareciera una dependencia de PostgreSQL, se encontraría con el contenedor apagado.
            web.UseSetting("SHAPI_POSTGRES_CADENA", cadenaPostgres);
            web.UseSetting("SHAPI_MODO_DEMO", "true");
            web.UseSetting("SHAPI_ORIGENES_PERMITIDOS", $"localhost:{_origen.Puerto}");
        });
    }

    private async Task<(string Host, string Clave)> SembrarAsync()
    {
        var host = $"api{Guid.NewGuid():N}.api.shapi.localhost";
        var clave = $"shp_prod_{Guid.NewGuid():N}"[..35];
        var api = await entorno.SembrarApiAsync(host, urlOrigen: $"http://localhost:{_origen.Puerto}", rutas:
        [
            EntornoCompuerta.Ruta("GET", "/cotizaciones"),
            EntornoCompuerta.Ruta("GET", "/tarifas", expuesta: false),
        ]);
        await entorno.SembrarClaveAsync(api, ContextoClave.CalcularHash(clave));
        return (host, clave);
    }

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string clave, string ruta = "/cotizaciones")
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        peticion.Headers.Add("X-Api-Key", clave);
        return await cliente.SendAsync(peticion);
    }

    private static async Task VerificarOrigenAsync(HttpClient cliente, string clave)
    {
        using var respuesta = await EnviarAsync(cliente, clave);
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadAsStringAsync()).Should().Be(CuerpoOrigen);
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        respuesta.StatusCode.Should().Be(estado);
        respuesta.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var error = documento.RootElement.GetProperty("error");
        error.GetProperty("codigo").GetString().Should().Be(codigo);
        error.GetProperty("estado").GetInt32().Should().Be((int)estado);
    }
}
