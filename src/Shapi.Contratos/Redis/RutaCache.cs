using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shapi.Contratos.Redis;

/// <summary>
/// Un elemento del arreglo JSON de <c>api:{api_id}:rutas</c> (07 §4). Se publican todas las rutas de la API, también
/// las ocultas, con <see cref="Expuesta"/> en <c>false</c>.
/// </summary>
/// <param name="Metodo">En mayúsculas, como en 07 §3.2 (<c>GET</c>, <c>POST</c>…).</param>
/// <param name="Patron">En sintaxis OpenAPI (<c>/guias/{numero}</c>).</param>
/// <param name="LimiteMinuto">El límite propio de la ruta; <c>null</c> si solo aplica el del plan.</param>
/// <param name="Peso">Las llamadas que descuenta cada petición (<c>peso_llamadas</c>).</param>
public sealed record RutaCache(
    [property: JsonPropertyName("ruta_id")] Guid RutaId,
    [property: JsonPropertyName("metodo")] string Metodo,
    [property: JsonPropertyName("patron")] string Patron,
    [property: JsonPropertyName("expuesta")] bool Expuesta,
    [property: JsonPropertyName("limite_minuto")] int? LimiteMinuto,
    [property: JsonPropertyName("cache_segundos")] int CacheSegundos,
    [property: JsonPropertyName("peso")] int Peso)
{
    public static string Serializar(IEnumerable<RutaCache> rutas) => JsonSerializer.Serialize(rutas);

    /// <returns><c>null</c> si el texto no es un arreglo de rutas válido.</returns>
    public static IReadOnlyList<RutaCache>? Deserializar(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<RutaCache>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
