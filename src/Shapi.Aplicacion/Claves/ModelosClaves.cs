namespace Shapi.Aplicacion.Claves;

/// <summary>Una clave enmascarada (<c>shp_prod_••••7c2e</c>), como se muestra en A4.3 y B2.3.</summary>
/// <param name="Tipo"><c>produccion</c> o <c>pruebas</c>.</param>
/// <param name="Estado"><c>activa</c>, <c>rotada</c> o <c>revocada</c>.</param>
/// <param name="ExpiraEn">En una clave rotada, la hora a la que deja de funcionar.</param>
public sealed record VistaClave(
    Guid Id,
    string Tipo,
    string ClaveEnmascarada,
    string Estado,
    DateTimeOffset? ExpiraEn,
    DateTimeOffset? RevocadaEn);

/// <summary>Una clave recién emitida. <see cref="Clave"/> es la clave completa: se devuelve una sola vez (RF-26).</summary>
public sealed record ClaveEmitida(Guid Id, string Tipo, string Clave, string ClaveEnmascarada, string Estado);

/// <summary>El resultado de rotar (B2.5): la clave nueva completa y la anterior, que funciona 24 horas más.</summary>
public sealed record ClaveRotada(
    Guid Id,
    string Tipo,
    string Clave,
    string ClaveEnmascarada,
    string Estado,
    VistaClave Anterior);

/// <summary>Una fila de A4.3: un consumidor de la API, su plan y sus claves.</summary>
/// <param name="Consumidor">El nombre de la empresa del consumidor.</param>
public sealed record ClavesDeConsumidor(Guid ConsumidorId, string Consumidor, string Plan, IReadOnlyList<VistaClave> Claves);

public sealed record PaginaClavesDeApi(IReadOnlyList<ClavesDeConsumidor> Elementos, int Total);

/// <summary>El consumidor de la sesión del portal y la API del host (10 §2).</summary>
public sealed record ConsumidorDelPortal(Guid ConsumidorId, Guid ApiId, string? Ip);

/// <summary>El miembro del personal que actúa desde el panel.</summary>
public sealed record MiembroDelPanel(Guid UsuarioId, string? Ip);

public sealed record PeticionEmitirClave(string? Tipo);
