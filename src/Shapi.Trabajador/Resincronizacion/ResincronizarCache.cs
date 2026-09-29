using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Cache;
using StackExchange.Redis;

namespace Shapi.Trabajador.Resincronizacion;

/// <summary>
/// Reescribe desde PostgreSQL todas las llaves de configuración de Redis (07 §4): las APIs publicadas, las claves
/// activas y las rotadas vigentes, las suscripciones sin finalizar y las organizaciones. Si Redis perdió sus datos, o
/// si una publicación falló después del <i>commit</i>, la compuerta vuelve a funcionar al terminar (RNF-05).
/// Los contadores (<c>cuota:*</c>, <c>rl:*</c>, <c>dia:*</c>, <c>met:*</c> y <c>cache:*</c>) no se tocan.
/// </summary>
public sealed class ResincronizarCache(
    IConnectionMultiplexer redis,
    LectorCacheBaseDatos lector,
    EscritorCacheRedis escritor,
    ILogger<ResincronizarCache> registro)
{
    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        // 1. Las llaves de configuración que ya existen, leídas ANTES que PostgreSQL. Así, una llave que otro proceso
        //    publique durante la resincronización nunca se toma por sobrante: o no se vio aquí, o su commit fue anterior
        //    a la lectura de PostgreSQL y está entre las que se escriben.
        var existentes = await LlavesDeConfiguracionAsync(cancelacion);

        // 2. Lo que debe haber, según PostgreSQL.
        var apis = await lector.ApisPublicadasAsync(cancelacion);
        var claves = await lector.ClavesVigentesAsync(cancelacion);
        var suscripciones = await lector.SuscripcionesVigentesAsync(cancelacion);
        var organizaciones = await lector.OrganizacionesAsync(cancelacion);

        // 3. Se escribe todo.
        var escritas = new HashSet<string>(StringComparer.Ordinal);
        foreach (var api in apis)
        {
            await escritor.EscribirApiAsync(api);
            escritas.Add(LlavesRedis.Api(api.Contexto.ApiId));
            escritas.Add(LlavesRedis.RutasApi(api.Contexto.ApiId));
            escritas.UnionWith(api.Hosts.Select(LlavesRedis.ApiPorHost));
        }

        foreach (var clave in claves)
        {
            await escritor.EscribirClaveAsync(clave);
            escritas.Add(LlavesRedis.Clave(clave.Hash));
        }

        foreach (var suscripcion in suscripciones)
        {
            await escritor.EscribirSuscripcionAsync(suscripcion);
            escritas.Add(LlavesRedis.Suscripcion(suscripcion.SuscripcionId));
        }

        foreach (var organizacion in organizaciones)
        {
            await escritor.EscribirOrganizacionAsync(organizacion);
            escritas.Add(LlavesRedis.Organizacion(organizacion.OrganizacionId));
        }

        // 4. Las claves que se revocaron o rotaron mientras tanto: el paso 3 pudo reescribirlas con su estado anterior
        //    encima de la publicación de la API de control. Una clave revocada no debe seguir funcionando (RF-28).
        var escritasPorHash = claves.ToDictionary(c => c.Hash, StringComparer.Ordinal);
        var cambiadas = (await lector.ClavesAsync(escritasPorHash.Keys, cancelacion))
            .Where(actual => escritasPorHash[actual.Hash] != actual)
            .ToList();
        foreach (var clave in cambiadas)
        {
            await escritor.EscribirClaveAsync(clave);
        }

        // 5. Las llaves de configuración que ya no corresponden: una clave revocada o un dominio propio quitado cuando
        //    Redis falló, una API despublicada, una suscripción finalizada.
        var sobrantes = existentes.Where(llave => !escritas.Contains(llave)).Select(llave => (RedisKey)llave).ToArray();
        if (sobrantes.Length > 0)
        {
            await escritor.EliminarAsync(sobrantes);
        }

        registro.LogInformation(
            "Caché resincronizada: {Apis} APIs, {Claves} claves, {Suscripciones} suscripciones, {Organizaciones} "
            + "organizaciones y {Sobrantes} llaves sobrantes borradas",
            apis.Count, claves.Count, suscripciones.Count, organizaciones.Count, sobrantes.Length);
    }

    private async Task<HashSet<string>> LlavesDeConfiguracionAsync(CancellationToken cancelacion)
    {
        var llaves = new HashSet<string>(StringComparer.Ordinal);
        foreach (var servidor in redis.GetServers().Where(s => s.IsConnected && !s.IsReplica))
        {
            foreach (var patron in LlavesRedis.PatronesConfiguracion)
            {
                // SCAN, que no bloquea Redis como KEYS.
                await foreach (var llave in servidor.KeysAsync(pattern: patron, pageSize: 500).WithCancellation(cancelacion))
                {
                    llaves.Add(llave.ToString());
                }
            }
        }

        return llaves;
    }
}
