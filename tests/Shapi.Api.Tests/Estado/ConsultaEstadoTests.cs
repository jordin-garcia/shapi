using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Estado;

namespace Shapi.Api.Tests.Estado;

public sealed class ConsultaEstadoTests
{
    // RF-39: la caché es compartida entre administrador y soporte, y vence exactamente a los 30 s.
    [Fact]
    public async Task RF_39_Cache_ComparteComprobacionConcurrenteYLaRenueva()
    {
        var tiempo = new Tiempo();
        var observador = new Observador();
        using var cache = new CacheEstado(tiempo);
        var consulta = new ConsultarEstado(observador, cache, new Reloj());
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => consulta.EjecutarAsync(CancellationToken.None)));
        Assert.Equal(1, observador.Llamadas);
        Assert.All(respuestas, x => Assert.Same(respuestas[0], x));
        Assert.Equal(7, respuestas[0].Componentes.Count);
        tiempo.Segundos = 29;
        await consulta.EjecutarAsync(CancellationToken.None);
        Assert.Equal(1, observador.Llamadas);
        tiempo.Segundos = 30;
        await consulta.EjecutarAsync(CancellationToken.None);
        Assert.Equal(2, observador.Llamadas);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Administrador", HttpStatusCode.OK)]
    [InlineData("Soporte", HttpStatusCode.OK)]
    [InlineData("Propietario", HttpStatusCode.Forbidden)]
    [InlineData("Editor", HttpStatusCode.Forbidden)]
    [InlineData("Lector", HttpStatusCode.Forbidden)]
    [InlineData("Consumidor", HttpStatusCode.Forbidden)]
    public async Task RF_39_Endpoint_SoloAdministradorYSoporte(string? rol, HttpStatusCode esperado)
    {
        await using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("environment", "Testing");
            web.ConfigureTestServices(s =>
            {
                s.AddSingleton<IObservadorEstado>(new Observador());
                s.AddSingleton<IReloj>(new Reloj());
                s.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = "PruebaEstado";
                    o.DefaultChallengeScheme = "PruebaEstado";
                    o.DefaultForbidScheme = "PruebaEstado";
                }).AddScheme<AuthenticationSchemeOptions, Autenticacion>("PruebaEstado", _ => { });
            });
        });
        using var cliente = fabrica.CreateClient();
        if (rol is not null)
        {
            cliente.DefaultRequestHeaders.Add("Rol-Prueba", rol);
        }

        var respuesta = await cliente.GetAsync("/api/admin/estado");
        Assert.Equal(esperado, respuesta.StatusCode);
        if (esperado == HttpStatusCode.OK)
        {
            var estado = await respuesta.Content.ReadFromJsonAsync<EstadoPlataforma>();
            Assert.Equal(7, estado!.Componentes.Count);
            Assert.Equal(new Reloj().Ahora, estado.ComprobadoEn);
        }
    }

    private sealed class Observador : IObservadorEstado
    {
        public int Llamadas { get; private set; }
        public async Task<ObservacionEstado> ObservarAsync(CancellationToken cancelacion)
        {
            Llamadas++;
            await Task.Yield();
            return new(true, true, 11, false, 1, 1, 1, false, null, false, true, false, false, null);
        }
    }
    private sealed class Reloj : IReloj
    {
        public DateTimeOffset Ahora => new(2026, 10, 12, 16, 0, 0, TimeSpan.Zero);
    }
    private sealed class Tiempo : TimeProvider
    {
        public int Segundos { get; set; }
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Segundos;
    }
    private sealed class Autenticacion(IOptionsMonitor<AuthenticationSchemeOptions> opciones, ILoggerFactory registros,
        UrlEncoder codificador) : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, registros, codificador)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var rol = Request.Headers["Rol-Prueba"].ToString();
            if (string.IsNullOrEmpty(rol))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var usuario = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(usuario, Scheme.Name)));
        }
    }
}
