using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Rutas;

/// <summary>
/// Las rutas de una API (<c>api:{id}:rutas</c>), expuestas y ocultas, listas para buscar la que coincide con una
/// petición (08 §1).
/// </summary>
public sealed class TablaRutas
{
    private readonly (RutaCache Ruta, PatronRuta Patron)[] _rutas;

    private TablaRutas((RutaCache Ruta, PatronRuta Patron)[] rutas)
    {
        _rutas = rutas;
        MetodosExpuestos =
        [
            .. rutas.Where(r => r.Ruta.Expuesta).Select(r => r.Ruta.Metodo.ToUpperInvariant())
                .Distinct().Order(StringComparer.Ordinal),
        ];
    }

    public static TablaRutas Vacia { get; } = new([]);

    /// <summary>Los métodos de las rutas expuestas, para <c>Access-Control-Allow-Methods</c> (08 §6).</summary>
    public IReadOnlyList<string> MetodosExpuestos { get; }

    /// <summary>Los patrones que no son válidos se ignoran: ninguna petición coincide con ellos.</summary>
    public static TablaRutas Crear(IEnumerable<RutaCache> rutas) =>
        new([.. rutas.Select(r => (Ruta: r, Patron: PatronRuta.Crear(r.Patron)))
            .Where(r => r.Patron is not null)
            .Select(r => (r.Ruta, r.Patron!))]);

    /// <summary>El JSON de <c>api:{id}:rutas</c>; si falta o no es válido, ninguna ruta.</summary>
    public static TablaRutas DesdeJson(string? json) =>
        json is null ? Vacia : RutaCache.Deserializar(json) is { } rutas ? Crear(rutas) : Vacia;

    /// <summary>
    /// La ruta del método que mejor coincide con el camino, esté expuesta u oculta: la de más segmentos literales y,
    /// si empatan, la de menos parámetros (08 §1). Si la que gana está oculta, la petición se rechaza aunque otro
    /// patrón más general esté expuesto (RF-10); en un empate total, gana la oculta.
    /// </summary>
    public RutaCache? Buscar(string metodo, string camino) =>
        _rutas
            .Where(r => string.Equals(r.Ruta.Metodo, metodo, StringComparison.OrdinalIgnoreCase) && r.Patron.Coincide(camino))
            .OrderByDescending(r => r.Patron.Literales)
            .ThenBy(r => r.Patron.Parametros)
            .ThenBy(r => r.Ruta.Expuesta)
            .Select(r => r.Ruta)
            .FirstOrDefault();
}
