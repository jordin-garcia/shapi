using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Compuerta.Contexto;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Medicion;
using Shapi.Compuerta.Reenvio;

namespace Shapi.Compuerta.Tests.Tuberia;

// RNF-13: la tubería de filtros (08 §3).
public class TuberiaCompuertaTests
{
    private sealed class MedicionDePrueba : IMedicionPeticion
    {
        public Task MedirAsync(ContextoPeticion contexto, Func<Task> siguiente) => siguiente();
    }

    [Fact]
    public async Task RNF_13_Procesar_UnFiltroRechaza_DetieneLaCadenaYNoReenvia()
    {
        var registro = new List<string>();
        var reenvio = new ReenvioRegistrado(registro);
        var tuberia = new TuberiaCompuerta(
            new LectorSinDatos(),
            [
                new FiltroDePrueba("primero", registro, ResultadoFiltro.Continuar),
                new FiltroDePrueba("segundo", registro, ResultadoFiltro.Rechazar(403, "prueba_rechazo", "Rechazado.")),
                new FiltroDePrueba("tercero", registro, ResultadoFiltro.Continuar),
            ],
            reenvio,
            NullLogger<TuberiaCompuerta>.Instance, new MedicionDePrueba());
        var http = NuevoContexto();

        await tuberia.ProcesarAsync(http);

        registro.Should().Equal("primero", "segundo");
        http.Response.StatusCode.Should().Be(403);
        var error = await LeerErrorAsync(http);
        error.GetProperty("codigo").GetString().Should().Be("prueba_rechazo");
        error.GetProperty("estado").GetInt32().Should().Be(403);
    }

    [Fact]
    public async Task RNF_13_Procesar_TodosContinuan_EvaluaEnOrdenYReenvia()
    {
        var registro = new List<string>();
        var tuberia = new TuberiaCompuerta(
            new LectorSinDatos(),
            [
                new FiltroDePrueba("primero", registro, ResultadoFiltro.Continuar),
                new FiltroDePrueba("segundo", registro, ResultadoFiltro.Continuar),
            ],
            new ReenvioRegistrado(registro),
            NullLogger<TuberiaCompuerta>.Instance, new MedicionDePrueba());

        await tuberia.ProcesarAsync(NuevoContexto());

        registro.Should().Equal("primero", "segundo", "reenvio");
    }

    [Fact]
    public async Task RNF_13_Procesar_RechazoConCabeceras_LasAgregaALaRespuesta()
    {
        var rechazo = ResultadoFiltro.Rechazar(401, "clave_ausente", "Falta la clave.",
            new Dictionary<string, string> { ["WWW-Authenticate"] = "ApiKey header=\"X-Api-Key\"" });
        var tuberia = new TuberiaCompuerta(
            new LectorSinDatos(),
            [new FiltroDePrueba("unico", [], rechazo)], new ReenvioRegistrado([]), NullLogger<TuberiaCompuerta>.Instance, new MedicionDePrueba());
        var http = NuevoContexto();

        await tuberia.ProcesarAsync(http);

        http.Response.StatusCode.Should().Be(401);
        http.Response.Headers.WWWAuthenticate.ToString().Should().Be("ApiKey header=\"X-Api-Key\"");
        http.Response.ContentType.Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task RNF_13_Procesar_FiltroQueRespondeSinError_EscribeElEstadoSinCuerpoNiReenvia()
    {
        // Criterio 7 de JG-05: el preflight de CORS se contesta con 204 y sin el JSON de error.
        var registro = new List<string>();
        var tuberia = new TuberiaCompuerta(new LectorSinDatos(),
            [new FiltroDePrueba("cors", registro, ResultadoFiltro.Responder(204))], new ReenvioRegistrado(registro),
            NullLogger<TuberiaCompuerta>.Instance, new MedicionDePrueba());
        var http = NuevoContexto();

        await tuberia.ProcesarAsync(http);

        registro.Should().Equal("cors");
        http.Response.StatusCode.Should().Be(204);
        http.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task RNF_13_Procesar_LeeElContextoAntesDeLosFiltros()
    {
        // Criterio 9 de JG-05: los filtros ya no van a Redis; el lector deja los datos en el contexto.
        var registro = new List<string>();
        var tuberia = new TuberiaCompuerta(new LectorSinDatos(registro),
            [new FiltroDePrueba("primero", registro, ResultadoFiltro.Continuar)], new ReenvioRegistrado(registro),
            NullLogger<TuberiaCompuerta>.Instance, new MedicionDePrueba());

        await tuberia.ProcesarAsync(NuevoContexto());

        registro.Should().Equal("lector", "primero", "reenvio");
    }

    [Fact]
    public async Task RF_31_Procesar_ContentLengthDeMasDeDiezMegabytes_Responde413SinLeerRedis()
    {
        // Criterio 6 de JG-05 y 08 §1
        var registro = new List<string>();
        var tuberia = new TuberiaCompuerta(new LectorSinDatos(registro),
            [new FiltroDePrueba("primero", registro, ResultadoFiltro.Continuar)], new ReenvioRegistrado(registro),
            NullLogger<TuberiaCompuerta>.Instance, new MedicionDePrueba());
        var http = NuevoContexto();
        http.Request.ContentLength = TuberiaCompuerta.LimiteCuerpo + 1;

        await tuberia.ProcesarAsync(http);

        registro.Should().BeEmpty();
        http.Response.StatusCode.Should().Be(413);
        (await LeerErrorAsync(http)).GetProperty("codigo").GetString().Should().Be("cuerpo_demasiado_grande");
    }

    [Fact]
    public void RNF_13_Orden_DefinidoEnUnSoloLugar()
    {
        // 08 §3: filtros 0 a 6. Agregar una regla es agregar una clase y una línea (RNF-13).
        TuberiaCompuerta.Orden.Should().Equal(typeof(FiltroCors), typeof(FiltroApi), typeof(FiltroClave),
            typeof(FiltroOrganizacion), typeof(FiltroSuscripcion), typeof(FiltroRuta), typeof(FiltroLimitesYCuotas));
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

    private sealed class LectorSinDatos(List<string>? registro = null) : ILectorContexto
    {
        public Task LeerAsync(ContextoPeticion contexto)
        {
            registro?.Add("lector");
            return Task.CompletedTask;
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
