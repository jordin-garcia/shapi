using Microsoft.Extensions.Logging;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;

namespace Shapi.Infraestructura.Cache;

/// <summary>
/// Publica en Redis la vista que lee la compuerta (07 §4), leyendo el estado actual de PostgreSQL. Se llama después
/// del <i>commit</i>: si Redis falla, reintenta 3 veces con espera y registra el error, <b>sin lanzar</b>, para no
/// revertir ni hacer fallar la operación de negocio; la resincronización lo corrige en 5 minutos como máximo (06 §5.2).
/// </summary>
public sealed class PublicadorCacheRedis(
    LectorCacheBaseDatos lector,
    EscritorCacheRedis escritor,
    OpcionesCacheRedis opciones,
    ILogger<PublicadorCacheRedis> registro) : IPublicadorCache
{
    public Task PublicarApi(Guid apiId, CancellationToken cancelacion = default) =>
        Publicar(nameof(PublicarApi), apiId.ToString(), async () =>
        {
            var api = await lector.ApiAsync(apiId, cancelacion);
            return api is null ? null : () => escritor.EscribirApiAsync(api);
        }, cancelacion);

    public Task EliminarHost(string host, Guid apiId, CancellationToken cancelacion = default) =>
        Publicar(nameof(EliminarHost), host, () => Task.FromResult<Func<Task>?>(
            () => escritor.EliminarHostAsync(host, apiId)), cancelacion);

    public Task PublicarClave(Guid claveId, CancellationToken cancelacion = default) =>
        Publicar(nameof(PublicarClave), claveId.ToString(), async () =>
        {
            var clave = await lector.ClaveAsync(claveId, cancelacion);
            return clave is null ? null : () => escritor.EscribirClaveAsync(clave);
        }, cancelacion);

    // El hash de la clave no se escribe en los registros.
    public Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default) =>
        Publicar(nameof(ExpirarClave), "-", () => Task.FromResult<Func<Task>?>(
            () => escritor.ExpirarClaveAsync(hashClave, instante)), cancelacion);

    public Task EliminarClave(string hashClave, CancellationToken cancelacion = default) =>
        Publicar(nameof(EliminarClave), "-", () => Task.FromResult<Func<Task>?>(
            () => escritor.EliminarAsync(LlavesRedis.Clave(hashClave))), cancelacion);

    /// <summary>
    /// Escribe <c>susc:{id}</c> de una suscripción de API, o la borra si está finalizada. Una suscripción de plataforma
    /// no tiene <c>susc:{id}</c>: su estado cambia el estado efectivo de la organización, así que se publica <c>org:{id}</c>.
    /// </summary>
    public Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default) =>
        Publicar(nameof(PublicarSuscripcion), suscripcionId.ToString(), async () =>
        {
            var (existe, contexto) = await lector.SuscripcionApiAsync(suscripcionId, cancelacion);
            if (existe)
            {
                return contexto is null
                    ? () => escritor.EliminarAsync(LlavesRedis.Suscripcion(suscripcionId))
                    : () => escritor.EscribirSuscripcionAsync(contexto);
            }

            var organizacionId = await lector.OrganizacionDeSuscripcionPlataformaAsync(suscripcionId, cancelacion);
            var organizacion = organizacionId is null ? null : await lector.OrganizacionAsync(organizacionId.Value, cancelacion);
            return organizacion is null ? null : () => escritor.EscribirOrganizacionAsync(organizacion);
        }, cancelacion);

    public Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default) =>
        Publicar(nameof(PublicarOrganizacion), organizacionId.ToString(), async () =>
        {
            var organizacion = await lector.OrganizacionAsync(organizacionId, cancelacion);
            return organizacion is null ? null : () => escritor.EscribirOrganizacionAsync(organizacion);
        }, cancelacion);

    /// <param name="preparar">
    /// Lee PostgreSQL una sola vez y devuelve la escritura en Redis, que es lo único que se reintenta; <c>null</c> si el
    /// registro no existe.
    /// </param>
    private async Task Publicar(string operacion, string id, Func<Task<Func<Task>?>> preparar, CancellationToken cancelacion)
    {
        try
        {
            var escritura = await preparar();
            if (escritura is null)
            {
                registro.LogWarning("{Operacion}: {Id} no existe en la base de datos; no se publica nada", operacion, id);
                return;
            }

            if (!await ReintentosRedis.EjecutarAsync(escritura, opciones.EsperasReintento, registro, operacion, cancelacion))
            {
                registro.LogError(
                    "{Operacion} {Id}: Redis siguió fallando después de {Reintentos} reintentos. La operación ya está "
                    + "guardada; la resincronización la publicará en 5 minutos como máximo",
                    operacion, id, opciones.EsperasReintento.Count);
            }
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            registro.LogError(excepcion, "{Operacion} {Id}: no se pudo publicar en Redis", operacion, id);
        }
    }
}
