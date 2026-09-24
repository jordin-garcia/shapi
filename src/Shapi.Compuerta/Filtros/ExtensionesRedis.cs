using StackExchange.Redis;

namespace Shapi.Compuerta.Filtros;

internal static class ExtensionesRedis
{
    /// <summary>Convierte un hash de Redis en los campos que leen <c>ContextoApi</c> y <c>ContextoClave</c>.</summary>
    public static IReadOnlyDictionary<string, string> ACampos(this HashEntry[] entradas) =>
        entradas.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
}
