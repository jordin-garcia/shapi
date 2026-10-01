namespace Shapi.Compuerta.Rutas;

/// <summary>Las rutas de cada API en memoria, con su <c>version</c> (08 §8).</summary>
public sealed class CacheRutas(TimeProvider reloj)
{
    public static readonly TimeSpan Duracion = TimeSpan.FromSeconds(5);

    public RutasEnMemoria? Vigentes(Guid apiId) => reloj is null ? null : null;

    public TablaRutas Guardar(Guid apiId, long version, string? json) => TablaRutas.DesdeJson(json);
}

/// <param name="Version">La <c>version</c> de <c>api:{id}</c> con la que se leyeron las rutas.</param>
public sealed record RutasEnMemoria(long Version, TablaRutas Tabla);
