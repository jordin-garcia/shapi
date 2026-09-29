using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Infraestructura.Cache;

/// <summary>
/// Escribe en Redis las llaves de configuración de 07 §4 con los nombres de <see cref="LlavesRedis"/>. Cada hash se
/// reemplaza completo (<c>DEL</c> y luego <c>HSET</c> en una misma transacción), porque los campos opcionales vacíos no
/// se escriben y uno viejo seguiría en Redis. No reintenta: de eso se encarga quien lo llama.
/// </summary>
public sealed class EscritorCacheRedis(IConnectionMultiplexer redis, IReloj reloj, TimeProvider tiempo)
{
    // Si otra publicación de la misma API cambia la versión mientras tanto, se vuelve a leer y a intentar.
    private const int IntentosPorConflicto = 10;

    /// <summary>
    /// Escribe <c>api:{id}</c> con la versión siguiente, <c>api:{id}:rutas</c> y <c>api:host:{host}</c> de cada host,
    /// todo en una transacción. La versión solo sube: la compuerta la usa para saber si sus rutas en memoria siguen al día.
    /// </summary>
    /// <returns>La versión escrita.</returns>
    public async Task<long> EscribirApiAsync(ApiCache api)
    {
        var db = redis.GetDatabase();
        var llave = LlavesRedis.Api(api.Contexto.ApiId);
        for (var intento = 0; intento < IntentosPorConflicto; intento++)
        {
            var actual = await db.HashGetAsync(llave, ContextoApi.CampoVersion);
            var version = (actual.TryParse(out long anterior) ? anterior : 0) + 1;

            var transaccion = db.CreateTransaction();
            transaccion.AddCondition(actual.IsNull
                ? Condition.HashNotExists(llave, ContextoApi.CampoVersion)
                : Condition.HashEqual(llave, ContextoApi.CampoVersion, actual));
            _ = transaccion.KeyDeleteAsync(llave);
            _ = transaccion.HashSetAsync(llave, AEntradas((api.Contexto with { Version = version }).ACampos()));
            _ = transaccion.StringSetAsync(LlavesRedis.RutasApi(api.Contexto.ApiId), RutaCache.Serializar(api.Rutas));
            foreach (var host in api.Hosts)
            {
                _ = transaccion.StringSetAsync(LlavesRedis.ApiPorHost(host), api.Contexto.ApiId.ToString());
            }

            if (await transaccion.ExecuteAsync())
            {
                return version;
            }
        }

        throw new InvalidOperationException("La versión de la API cambió en cada intento de publicarla.");
    }

    /// <summary>
    /// Escribe <c>clave:{sha256}</c> y, si la clave está rotada, su vencimiento; si <see cref="ClaveCache.Contexto"/> es
    /// <c>null</c> (revocada o ya vencida), la borra.
    /// </summary>
    public async Task EscribirClaveAsync(ClaveCache clave)
    {
        var llave = LlavesRedis.Clave(clave.Hash);
        if (clave.Contexto is null)
        {
            await redis.GetDatabase().KeyDeleteAsync(llave);
            return;
        }

        var transaccion = redis.GetDatabase().CreateTransaction();
        _ = transaccion.KeyDeleteAsync(llave);
        _ = transaccion.HashSetAsync(llave, AEntradas(clave.Contexto.ACampos()));
        if (clave.ExpiraEn is { } expiraEn)
        {
            _ = transaccion.KeyExpireAsync(llave, EnHoraReal(expiraEn));
        }

        await transaccion.ExecuteAsync();
    }

    /// <summary>
    /// <c>EXPIREAT</c> de <c>clave:{sha256}</c>. El instante está en la hora de <see cref="IReloj"/>, que en el modo
    /// demostración va adelantada; Redis usa la hora real, así que se traslada (07 §4). Si ya pasó, Redis borra la llave.
    /// </summary>
    public Task ExpirarClaveAsync(string hash, DateTimeOffset instante) =>
        redis.GetDatabase().KeyExpireAsync(LlavesRedis.Clave(hash), EnHoraReal(instante));

    public Task EscribirSuscripcionAsync(ContextoSuscripcion suscripcion) =>
        ReemplazarHashAsync(LlavesRedis.Suscripcion(suscripcion.SuscripcionId), suscripcion.ACampos());

    public Task EscribirOrganizacionAsync(ContextoOrganizacion organizacion) =>
        ReemplazarHashAsync(LlavesRedis.Organizacion(organizacion.OrganizacionId), organizacion.ACampos());

    public Task EliminarAsync(params RedisKey[] llaves) => redis.GetDatabase().KeyDeleteAsync(llaves);

    private async Task ReemplazarHashAsync(RedisKey llave, IReadOnlyDictionary<string, string> campos)
    {
        var transaccion = redis.GetDatabase().CreateTransaction();
        _ = transaccion.KeyDeleteAsync(llave);
        _ = transaccion.HashSetAsync(llave, AEntradas(campos));
        await transaccion.ExecuteAsync();
    }

    private DateTime EnHoraReal(DateTimeOffset instante) => (tiempo.GetUtcNow() + (instante - reloj.Ahora)).UtcDateTime;

    private static HashEntry[] AEntradas(IReadOnlyDictionary<string, string> campos) =>
        [.. campos.Select(campo => new HashEntry(campo.Key, campo.Value))];
}
