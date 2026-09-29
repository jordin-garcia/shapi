namespace Shapi.Aplicacion.Bitacora;

/// <summary>Actor que se muestra en B3.2 (RF-41).</summary>
public sealed record ActorBitacora(string Nombre, string Rol, string? Organizacion);

/// <summary>Acción sensible que se muestra en B3.2 (RF-41).</summary>
public sealed record EntradaBitacoraListado(
    DateTimeOffset Fecha,
    ActorBitacora Actor,
    string Accion,
    string Descripcion);

/// <summary>Respuesta paginada según las convenciones de la API de control.</summary>
public sealed record PaginaBitacora(IReadOnlyList<EntradaBitacoraListado> Elementos, int Total);
