using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Api.Identidad;
using Shapi.Api.Tests.Persistencia;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Tests.Pagos;

public sealed class AuthHistorialPagosPrueba(
    IOptionsMonitor<AuthenticationSchemeOptions> opciones, ILoggerFactory registros, UrlEncoder codificador)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, registros, codificador)
{
    public const string Esquema = "HistorialPagosPrueba";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var partes = Request.Headers["X-Prueba-Identidad"].ToString().Split(':');
        if (partes.Length == 0 || partes[0] == "")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Claim[] claims;
        if (partes[0] == "personal" && partes.Length == 3 && Guid.TryParse(partes[2], out var organizacion))
        {
            claims = [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, partes[1]),
                new Claim(PoliticasAutorizacion.ClaimOrganizacion, organizacion.ToString()),
                new Claim(PoliticasAutorizacion.ClaimAmbito, nameof(AmbitoSesion.Personal)),
            ];
        }
        else if (partes[0] == "consumidor" && partes.Length == 3
            && Guid.TryParse(partes[1], out var consumidor) && Guid.TryParse(partes[2], out organizacion))
        {
            claims = [
                new Claim(ClaimTypes.NameIdentifier, consumidor.ToString()),
                new Claim(PoliticasAutorizacion.ClaimOrganizacion, organizacion.ToString()),
                new Claim(PoliticasAutorizacion.ClaimAmbito, nameof(AmbitoSesion.Consumidor)),
            ];
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Cabecera de prueba inválida."));
        }

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema)), Esquema)));
    }
}

[Collection(nameof(PostgresPersistencia))]
public sealed class HistorialPagosTests(PostgresPersistencia postgres) : BaseDePrueba(postgres), IAsyncLifetime
{
    private WebApplicationFactory<Program>? _fabrica;
    private WebApplicationFactory<Program> Fabrica => _fabrica ??= new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
    {
        web.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        web.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
        web.UseSetting("SHAPI_DOMINIO_BASE", "shapi.test");
        web.ConfigureTestServices(servicios => servicios.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = AuthHistorialPagosPrueba.Esquema;
            o.DefaultChallengeScheme = AuthHistorialPagosPrueba.Esquema;
            o.DefaultForbidScheme = AuthHistorialPagosPrueba.Esquema;
        }).AddScheme<AuthenticationSchemeOptions, AuthHistorialPagosPrueba>(AuthHistorialPagosPrueba.Esquema, _ => { }));
    });

    public new async Task DisposeAsync()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    // RF-21: propietario y lector consultan pagos de su organización con periodo, monto, medio y estado; otras organizaciones quedan aisladas.
    [Theory]
    [InlineData("Propietario")]
    [InlineData("Lector")]
    public async Task RF_21_PagosPlataforma_DevuelvePaginaConDatosYSoloDeLaOrganizacion(string rol)
    {
        var organizacion = await NuevaOrganizacion();
        var otraOrganizacion = await NuevaOrganizacion();
        var plan = await NuevoPlanPlataforma();
        var suscripcion = await NuevaSuscripcionPlataforma(organizacion, plan);
        var ajena = await NuevaSuscripcionPlataforma(otraOrganizacion, plan);
        var tarjeta = await NuevoMedioPago(organizacion, null);
        var pagoAntiguo = await InsertarPago(suscripcion, null, tarjeta, "contratacion", "Suscripción", "autorizado", 199, "2026-08-24T06:00:00Z", "2026-09-23T06:00:00Z", "2026-08-24T06:00:00Z");
        var pagoNuevo = await InsertarPago(suscripcion, null, tarjeta, "cambio_plan", "Cambio de plan", "autorizado", 173.34m, "2026-09-10T06:00:00Z", "2026-10-10T06:00:00Z", "2026-09-10T06:00:00Z");
        await InsertarPago(ajena, null, tarjeta, "contratacion", "No mostrar", "autorizado", 99, null, null, "2026-09-11T06:00:00Z");
        using var cliente = Fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Prueba-Identidad", $"personal:{rol}:{organizacion}");

        using var respuesta = await cliente.GetAsync("/api/pagos?pagina=1&tamano=1");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK, await respuesta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("total").GetInt32().Should().Be(2);
        var pago = json.RootElement.GetProperty("elementos").EnumerateArray().Single();
        pago.GetProperty("id").GetGuid().Should().Be(pagoNuevo);
        pago.GetProperty("concepto").GetString().Should().Be("cambioPlan");
        pago.GetProperty("descripcion").GetString().Should().Be("Cambio de plan");
        pago.GetProperty("periodoInicio").GetDateTimeOffset().Should().Be(DateTimeOffset.Parse("2026-09-10T06:00:00Z"));
        pago.GetProperty("periodoFin").GetDateTimeOffset().Should().Be(DateTimeOffset.Parse("2026-10-10T06:00:00Z"));
        pago.GetProperty("monto").GetDecimal().Should().Be(173.34m);
        pago.GetProperty("moneda").GetString().Should().Be("GTQ");
        pago.GetProperty("medioPago").GetProperty("marca").GetString().Should().Be("visa");
        pago.GetProperty("medioPago").GetProperty("ultimos4").GetString().Should().Be("4242");
        pago.GetProperty("estado").GetString().Should().Be("autorizado");
        using var pagina2 = await cliente.GetAsync("/api/pagos?pagina=2&tamano=1");
        var elementos2 = (await pagina2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("elementos");
        elementos2.EnumerateArray().Single().GetProperty("id").GetGuid().Should().Be(pagoAntiguo);
    }

    // RF-21: el lector no accede a pagos con parámetros inválidos y el rol editor no puede consultarlos.
    [Fact]
    public async Task RF_21_PagosPlataforma_ValidaPaginacionYPermisos()
    {
        var organizacion = await NuevaOrganizacion();
        using var cliente = Fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Prueba-Identidad", $"personal:Editor:{organizacion}");
        using var prohibido = await cliente.GetAsync("/api/pagos");
        prohibido.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        cliente.DefaultRequestHeaders.Remove("X-Prueba-Identidad");
        cliente.DefaultRequestHeaders.Add("X-Prueba-Identidad", $"personal:Lector:{organizacion}");
        using var invalido = await cliente.GetAsync("/api/pagos?pagina=0&tamano=101");
        invalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // RF-21: el portal devuelve los pagos de la suscripción de la API y los intentos rechazados propios, sin filtrar por otro consumidor.
    [Fact]
    public async Task RF_21_PagosPortal_AislaPorConsumidorYApiDelHost()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        await Ejecutar($"UPDATE api SET estado = 'publicada' WHERE id = '{api}'");
        var host = await Escalar<string>($"SELECT subdominio FROM api WHERE id = '{api}'") + ".shapi.test";
        var consumidor = await NuevoConsumidor(organizacion);
        var otroConsumidor = await NuevoConsumidor(organizacion);
        var plan = await NuevoPlanApi(api);
        var suscripcion = await NuevaSuscripcionApi(consumidor, api, plan);
        var tarjeta = await NuevoMedioPago(null, consumidor);
        var pago = await InsertarPago(null, suscripcion, tarjeta, "contratacion", "Contratación de API", "autorizado", 50, "2026-09-01T06:00:00Z", "2026-10-01T06:00:00Z", "2026-09-01T06:00:00Z");
        var subAjena = await NuevaSuscripcionApi(otroConsumidor, api, plan);
        await InsertarPago(null, subAjena, null, "contratacion", "Pago ajeno", "autorizado", 50, null, null, "2026-09-02T06:00:00Z");
        await InsertarRechazo(consumidor, api, "2026-09-03T06:00:00Z");
        using var cliente = Fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Prueba-Identidad", $"consumidor:{consumidor}:{organizacion}");
        cliente.DefaultRequestHeaders.Host = host;

        using var respuesta = await cliente.GetAsync("/api/portal/pagos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK, await respuesta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("total").GetInt32().Should().Be(2);
        var elementos = json.RootElement.GetProperty("elementos").EnumerateArray().ToArray();
        var elemento = elementos.Single(e => e.GetProperty("id").GetGuid() == pago);
        elemento.GetProperty("id").GetGuid().Should().Be(pago);
        elemento.GetProperty("medioPago").GetProperty("ultimos4").GetString().Should().Be("4242");
        elementos.Single(e => e.GetProperty("estado").GetString() == "rechazado")
            .GetProperty("medioPago").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private Task<Guid> InsertarPago(Guid? plataforma, Guid? api, Guid? medioPago, string concepto, string descripcion,
        string estado, decimal monto, string? inicio, string? fin, string creado) => Escalar<Guid>($"""
        INSERT INTO pago (id, suscripcion_plataforma_id, suscripcion_api_id, medio_pago_id, concepto, descripcion, monto, estado,
                          periodo_inicio, periodo_fin, creado_en)
        VALUES (gen_random_uuid(), {ValorONulo(plataforma)}, {ValorONulo(api)}, {ValorONulo(medioPago)}, '{concepto}', '{descripcion}', {monto}, '{estado}',
                {ValorTextoONulo(inicio)}, {ValorTextoONulo(fin)}, '{creado}') RETURNING id
        """);

    private static string ValorTextoONulo(string? valor) => valor is null ? "NULL" : $"'{valor}'";

    private Task<Guid> InsertarRechazo(Guid consumidorId, Guid apiId, string creado) => Escalar<Guid>($"""
        INSERT INTO pago (id, consumidor_id, api_id, concepto, descripcion, monto, estado, creado_en)
        VALUES (gen_random_uuid(), '{consumidorId}', '{apiId}', 'contratacion', 'Contratación rechazada', 50, 'rechazado', '{creado}')
        RETURNING id
        """);
}
