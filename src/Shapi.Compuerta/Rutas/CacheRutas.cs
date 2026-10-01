using System.Collections.Concurrent;

namespace Shapi.Compuerta.Rutas;

/// <summary>
/// Las rutas de cada API en memoria, durante <see cref="Duracion"/> como máximo y con la <c>version</c> de
/// <c>api:{id}</c> con la que se leyeron (08 §8). Es lo único que la compuerta guarda en memoria: claves,
/// suscripciones y organizaciones se leen en cada petición (ADR-22).
/// </summary>
public sealed class CacheRutas(TimeProvider reloj)
{
    public static readonly TimeSpan Duracion = TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<Guid, (RutasEnMemoria Rutas, long Marca)> _entradas = new();

    /// <returns>Las rutas guardadas hace menos de <see cref="Duracion"/>; si no hay, <c>null</c>.</returns>
    public RutasEnMemoria? Vigentes(Guid apiId) =>
        _entradas.TryGetValue(apiId, out var entrada) && reloj.GetElapsedTime(entrada.Marca) < Duracion
            ? entrada.Rutas
            : null;

    public TablaRutas Guardar(Guid apiId, long version, string? json)
    {
        var rutas = new RutasEnMemoria(version, TablaRutas.DesdeJson(json));
        _entradas[apiId] = (rutas, reloj.GetTimestamp());
        return rutas.Tabla;
    }
}

/// <param name="Version">La <c>version</c> de <c>api:{id}</c> con la que se leyeron las rutas.</param>
public sealed record RutasEnMemoria(long Version, TablaRutas Tabla);
