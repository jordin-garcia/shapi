using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Shapi.Compuerta.Tests.Tuberia;

// 08 §4 (auditoría H-79): sin Redis la compuerta no puede validar nada (RF-29), y responde con el contrato de errores.
public class RedisNoDisponibleTests
{
    [Fact]
    public async Task RF_29_RedisNoDisponible_Responde503ServicioNoDisponibleEnJson()
    {
        // Un puerto donde no escucha nadie, con tiempos cortos para que la prueba no espere.
        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
            web.UseSetting("SHAPI_REDIS", "127.0.0.1:1,connectTimeout=300,syncTimeout=500,asyncTimeout=500"));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://envios.api.shapi.localhost"),
        });
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e");

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        respuesta.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
        respuesta.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(5));
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var error = documento.RootElement.GetProperty("error");
        error.GetProperty("codigo").GetString().Should().Be("servicio_no_disponible");
        error.GetProperty("estado").GetInt32().Should().Be(503);
        error.GetProperty("mensaje").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
