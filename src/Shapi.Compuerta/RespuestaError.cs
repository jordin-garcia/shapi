using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Shapi.Compuerta.Filtros;

namespace Shapi.Compuerta;

/// <summary>Escribe los rechazos de la compuerta con el contrato de errores (08 §4).</summary>
public static class RespuestaError
{
    public const string TipoContenido = "application/json; charset=utf-8";

    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        // Los mensajes van en español: se escriben las tildes tal cual en vez de á.
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static Task EscribirAsync(HttpContext http, ResultadoFiltro rechazo)
    {
        http.Response.StatusCode = rechazo.Estado;
        foreach (var (nombre, valor) in rechazo.Cabeceras)
        {
            http.Response.Headers[nombre] = valor;
        }

        var cuerpo = new CuerpoError(new DetalleError(rechazo.Codigo, rechazo.Mensaje, rechazo.Estado));
        return http.Response.WriteAsJsonAsync(cuerpo, Opciones, TipoContenido, http.RequestAborted);
    }

    private sealed record CuerpoError(DetalleError Error);

    private sealed record DetalleError(string Codigo, string Mensaje, int Estado);
}
