namespace Shapi.Compuerta.Reenvio;

/// <summary>
/// Tiempos de espera con el origen (08 §1): <see cref="Total"/> para la respuesta completa y <see cref="Conexion"/>
/// para abrir la conexión.
/// </summary>
public sealed record TiemposOrigen(TimeSpan Total, TimeSpan Conexion)
{
    public static TiemposOrigen PorDefecto { get; } = new(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));
}
