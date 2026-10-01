using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Rutas;

/// <summary>Las rutas de una API, listas para buscar la que coincide con una petición (08 §1).</summary>
public sealed class TablaRutas
{
    private TablaRutas(IReadOnlyList<RutaCache> rutas)
    {
        Rutas = rutas;
    }

    public static TablaRutas Vacia { get; } = new([]);

    public IReadOnlyList<RutaCache> Rutas { get; }

    public IReadOnlyList<string> MetodosExpuestos => [];

    public static TablaRutas Crear(IEnumerable<RutaCache> rutas) => new([.. rutas]);

    public static TablaRutas DesdeJson(string? json) => Vacia;

    public RutaCache? Buscar(string metodo, string camino) => null;
}
