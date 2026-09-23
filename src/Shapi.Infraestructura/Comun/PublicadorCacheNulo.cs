using Microsoft.Extensions.Logging;
using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Comun;

/// <summary>Implementación por defecto mientras no exista el publicador en Redis (lo reemplaza JG-04).</summary>
public sealed class PublicadorCacheNulo(ILogger<PublicadorCacheNulo> registro) : IPublicadorCache
{
    public Task PublicarApi(Guid apiId, CancellationToken cancelacion = default) =>
        Registrar(nameof(PublicarApi), apiId.ToString());

    public Task PublicarClave(Guid claveId, CancellationToken cancelacion = default) =>
        Registrar(nameof(PublicarClave), claveId.ToString());

    // El hash de la clave no se escribe en el log.
    public Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default) =>
        Registrar(nameof(ExpirarClave), "-");

    public Task EliminarClave(string hashClave, CancellationToken cancelacion = default) =>
        Registrar(nameof(EliminarClave), "-");

    public Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default) =>
        Registrar(nameof(PublicarSuscripcion), suscripcionId.ToString());

    public Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default) =>
        Registrar(nameof(PublicarOrganizacion), organizacionId.ToString());

    private Task Registrar(string operacion, string id)
    {
        registro.LogInformation("Caché no publicada (publicador nulo): {Operacion} {Id}", operacion, id);
        return Task.CompletedTask;
    }
}
