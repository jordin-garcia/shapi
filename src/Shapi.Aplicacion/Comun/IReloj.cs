namespace Shapi.Aplicacion.Comun;

/// <summary>Única fuente de la hora (convenciones §6). Nunca se usa <c>DateTime.UtcNow</c> directo.</summary>
public interface IReloj
{
    /// <summary>La hora actual en UTC; en modo demostración incluye el desplazamiento (09 §9).</summary>
    DateTimeOffset Ahora { get; }
}
