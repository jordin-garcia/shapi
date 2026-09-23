namespace Shapi.Aplicacion.Comun;

/// <summary>Error de un caso de uso. <see cref="Codigo"/> es una constante de <c>Shapi.Contratos.CodigosError</c>.</summary>
/// <param name="Detalle">Datos adicionales que se devuelven en <c>detalle</c> (convenciones §5), por ejemplo <c>new { limite = "apis" }</c>.</param>
public sealed record Error(string Codigo, string Mensaje, object? Detalle = null);
