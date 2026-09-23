namespace Shapi.Compuerta.Filtros;

/// <summary><c>Continuar</c> o <c>Rechazar(código, error)</c> (08 §3).</summary>
public sealed record ResultadoFiltro
{
    private static readonly IReadOnlyDictionary<string, string> SinCabeceras = new Dictionary<string, string>();

    private ResultadoFiltro(bool continua, int estado, string codigo, string mensaje,
        IReadOnlyDictionary<string, string> cabeceras)
    {
        Continua = continua;
        Estado = estado;
        Codigo = codigo;
        Mensaje = mensaje;
        Cabeceras = cabeceras;
    }

    public static ResultadoFiltro Continuar { get; } = new(true, 0, "", "", SinCabeceras);

    public bool Continua { get; }

    /// <summary>El estado HTTP del rechazo.</summary>
    public int Estado { get; }

    /// <summary>Uno de <c>Shapi.Contratos.CodigosError</c>.</summary>
    public string Codigo { get; }

    public string Mensaje { get; }

    /// <summary>Cabeceras adicionales del rechazo (08 §4), por ejemplo <c>WWW-Authenticate</c>.</summary>
    public IReadOnlyDictionary<string, string> Cabeceras { get; }

    public static ResultadoFiltro Rechazar(int estado, string codigo, string mensaje,
        IReadOnlyDictionary<string, string>? cabeceras = null) =>
        new(false, estado, codigo, mensaje, cabeceras ?? SinCabeceras);
}
