namespace Shapi.Compuerta.Filtros;

public sealed record ResultadoFiltro
{
    public static ResultadoFiltro Continuar { get; } = new();
    public static ResultadoFiltro Rechazar(int estado, string codigo, string mensaje, IReadOnlyDictionary<string, string>? cabeceras = null) => throw new NotImplementedException();
}
