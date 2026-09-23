using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Reenvio;

namespace Shapi.Compuerta.Tests.Tuberia;

// RNF-13: la tubería de filtros (08 §3).
public class TuberiaCompuertaTests
{
    [Fact]
    public async Task Procesar_UnFiltroRechaza_DetieneLaCadenaYNoReenvia()
    {
        var registro = new List<string>();
        var reenvio = new ReenvioRegistrado(registro);
        var tuberia = new TuberiaCompuerta(
            [
                new FiltroDePrueba("primero", registro, ResultadoFiltro.Continuar),
                new FiltroDePrueba("segundo", registro, ResultadoFiltro.Rechazar(403, "prueba_rechazo", "Rechazado.")),
                new FiltroDePrueba("tercero", registro, ResultadoFiltro.Continuar),
            ],
            reenvio);
        var http = NuevoContexto();

        await tuberia.ProcesarAsync(http);

        registro.Should().Equal("primero", "segundo");
        http.Response.StatusCode.Should().Be(403);
        var error = await LeerErrorAsync(http);
        error.GetProperty("codigo").GetString().Should().Be("prueba_rechazo");
        error.GetProperty("estado").GetInt32().Should().Be(403);
    }

    [Fact]
    public async Task Procesar_TodosContinuan_EvaluaEnOrdenYReenvia()
    {
        var registro = new List<string>();
        var tuberia = new TuberiaCompuerta(
            [
                new FiltroDePrueba("primero", registro, ResultadoFiltro.Continuar),
                new FiltroDePrueba("segundo", registro, ResultadoFiltro.Continuar),
            ],
            new ReenvioRegistrado(registro));

        await tuberia.ProcesarAsync(NuevoContexto());

        registro.Should().Equal("primero", "segundo", "reenvio");
    }

    [Fact]
    public async Task Procesar_RechazoConCabeceras_LasAgregaALaRespuesta()
    {
        var rechazo = ResultadoFiltro.Rechazar(401, "clave_ausente", "Falta la clave.",
            new Dictionary<string, string> { ["WWW-Authenticate"] = "ApiKey header=\"X-Api-Key\"" });
        var tuberia = new TuberiaCompuerta([new FiltroDePrueba("unico", [], rechazo)], new ReenvioRegistrado([]));
        var http = NuevoContexto();

        await tuberia.ProcesarAsync(http);

        http.Response.StatusCode.Should().Be(401);
        http.Response.Headers.WWWAuthenticate.ToString().Should().Be("ApiKey header=\"X-Api-Key\"");
        http.Response.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public void Orden_DefinidoEnUnSoloLugar_ApiLuegoClave()
    {
        // 08 §3: filtros 1 y 2. Agregar una regla es agregar una clase y una línea (RNF-13).
        TuberiaCompuerta.Orden.Should().Equal(typeof(FiltroApi), typeof(FiltroClave));
        TuberiaCompuerta.Orden.Should().OnlyContain(t => typeof(IFiltroCompuerta).IsAssignableFrom(t));
    }

    private static DefaultHttpContext NuevoContexto()
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        return http;
    }

    private static async Task<JsonElement> LeerErrorAsync(HttpContext http)
    {
        http.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(http.Response.Body);
        return documento.RootElement.GetProperty("error").Clone();
    }

    private sealed class FiltroDePrueba(string nombre, List<string> registro, ResultadoFiltro resultado) : IFiltroCompuerta
    {
        public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
        {
            registro.Add(nombre);
            return ValueTask.FromResult(resultado);
        }
    }

    private sealed class ReenvioRegistrado(List<string> registro) : IReenvioOrigen
    {
        public Task ReenviarAsync(ContextoPeticion contexto)
        {
            registro.Add("reenvio");
            return Task.CompletedTask;
        }
    }
}
