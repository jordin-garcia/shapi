namespace Shapi.Aplicacion.Comun;

/// <summary>
/// Publica en Redis la vista que lee la compuerta (07 §4). Es la única forma de escribir Redis
/// desde el plano de control (convenciones §6). Se llama después de confirmar el cambio en PostgreSQL. Publica el
/// estado actual de la base de datos y no lanza si Redis falla: reintenta y lo registra (07 §4).
/// </summary>
public interface IPublicadorCache
{
    /// <summary>Escribe <c>api:{id}</c>, <c>api:{id}:rutas</c> y <c>api:host:*</c>.</summary>
    Task PublicarApi(Guid apiId, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra <c>api:host:{host}</c> si todavía apunta a <paramref name="apiId"/>: un dominio propio que se desconecta o se
    /// reemplaza deja de enrutar de inmediato (07 §4), sin esperar la resincronización.
    /// <see cref="PublicarApi"/> no lo hace, porque solo escribe los hosts actuales.
    /// </summary>
    Task EliminarHost(string host, Guid apiId, CancellationToken cancelacion = default);

    /// <summary>
    /// Escribe <c>clave:{sha256}</c> según el estado actual de la clave: sin vencimiento si está activa, con
    /// <c>EXPIREAT</c> si está rotada y la borra si está revocada.
    /// </summary>
    Task PublicarClave(Guid claveId, CancellationToken cancelacion = default);

    /// <summary>Pone <c>EXPIREAT</c> a <c>clave:{sha256}</c> de una clave rotada.</summary>
    Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default);

    /// <summary>Borra <c>clave:{sha256}</c> de una clave revocada.</summary>
    Task EliminarClave(string hashClave, CancellationToken cancelacion = default);

    /// <summary>
    /// Escribe <c>susc:{id}</c> de una suscripción de API (la borra si está finalizada). Con una suscripción de
    /// plataforma, publica <c>org:{id}</c>, porque su estado cambia el estado efectivo de la organización.
    /// </summary>
    Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default);

    /// <summary>Escribe <c>org:{id}</c> con su estado efectivo.</summary>
    Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default);
}
