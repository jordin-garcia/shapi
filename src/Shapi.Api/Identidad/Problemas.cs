using Microsoft.AspNetCore.Http;

namespace Shapi.Api.Identidad;

/// <summary>
/// Errores de la API de control en <c>application/problem+json</c>, con <c>codigo</c> y, en las validaciones,
/// <c>errores</c> por campo (convenciones §5).
/// </summary>
public static class Problemas
{
    public static IResult Crear(int estado, string codigo, string titulo, IDictionary<string, string[]>? errores = null)
    {
        var extensiones = new Dictionary<string, object?> { ["codigo"] = codigo };
        if (errores is not null)
        {
            extensiones["errores"] = errores;
        }
        return TypedResults.Problem(statusCode: estado, title: titulo, type: "about:blank", extensions: extensiones);
    }

    /// <summary>Escribe el error desde un middleware, fuera de un endpoint.</summary>
    public static Task Escribir(HttpContext context, int estado, string codigo, string titulo) =>
        Crear(estado, codigo, titulo).ExecuteAsync(context);
}
