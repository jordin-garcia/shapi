namespace Shapi.Aplicacion.Comun;

/// <summary>
/// Publica en Redis la vista que lee la compuerta (07 §4). Es la única forma de escribir Redis
/// desde el plano de control (convenciones §6). Se llama después de confirmar el cambio en PostgreSQL.
/// </summary>
public interface IPublicadorCache
{
    /// <summary>Escribe <c>api:{id}</c>, <c>api:{id}:rutas</c> y <c>api:host:*</c>.</summary>
    Task PublicarApi(Guid apiId, CancellationToken cancelacion = default);

    /// <summary>Escribe <c>clave:{sha256}</c> de una clave activa.</summary>
    Task PublicarClave(Guid claveId, CancellationToken cancelacion = default);

    /// <summary>Pone <c>EXPIREAT</c> a <c>clave:{sha256}</c> de una clave rotada.</summary>
    Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default);

    /// <summary>Borra <c>clave:{sha256}</c> de una clave revocada.</summary>
    Task EliminarClave(string hashClave, CancellationToken cancelacion = default);

    /// <summary>Escribe <c>susc:{id}</c>.</summary>
    Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default);

    /// <summary>Escribe <c>org:{id}</c> con su estado efectivo.</summary>
    Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default);
}
