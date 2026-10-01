namespace Shapi.Compuerta.Filtros;

/// <summary>
/// <c>Continuar</c> o <c>Rechazar(código, error)</c> (08 §3). Un filtro también puede contestar sin error y sin
/// reenviar, como el preflight de CORS (<see cref="Responder"/>).
/// </summary>
public sealed record ResultadoFiltro
{
    private static readonly IReadOnlyDictionary<string, string> SinCabeceras = new Dictionary<string, string>();

    private ResultadoFiltro(bool continua, bool esError, int estado, string codigo, string mensaje,
        IReadOnlyDictionary<string, string> cabeceras)
    {
        Continua = continua;
        EsError = esError;
        Estado = estado;
        Codigo = codigo;
        Mensaje = mensaje;
        Cabeceras = cabeceras;
    }

    public static ResultadoFiltro Continuar { get; } = new(true, false, 0, "", "", SinCabeceras);

    public bool Continua { get; }

    /// <summary>Si la respuesta lleva el JSON de error de 08 §4. Es falso en <see cref="Responder"/>.</summary>
    public bool EsError { get; }

    /// <summary>El estado HTTP del rechazo o de la respuesta.</summary>
    public int Estado { get; }

    /// <summary>Uno de <c>Shapi.Contratos.CodigosError</c>.</summary>
    public string Codigo { get; }

    public string Mensaje { get; }

    /// <summary>Cabeceras adicionales del rechazo (08 §4), por ejemplo <c>WWW-Authenticate</c>.</summary>
    public IReadOnlyDictionary<string, string> Cabeceras { get; }

    public static ResultadoFiltro Rechazar(int estado, string codigo, string mensaje,
        IReadOnlyDictionary<string, string>? cabeceras = null) =>
        new(false, true, estado, codigo, mensaje, cabeceras ?? SinCabeceras);

    /// <summary>Detiene la tubería y responde <paramref name="estado"/> sin cuerpo y sin reenviar al origen.</summary>
    public static ResultadoFiltro Responder(int estado) => new(false, false, estado, "", "", SinCabeceras);
}
