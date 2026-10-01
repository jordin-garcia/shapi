using Shapi.Compuerta.Rutas;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Contexto;

/// <summary>
/// Lee el contexto de una petición en dos viajes a Redis (08 §8, criterio 9 de JG-05):
/// <list type="number">
/// <item>un lote con <c>api:host:{host}</c> y <c>clave:{hash}</c>;</item>
/// <item>un lote con lo que depende de esos valores: <c>api:{id}</c>, sus rutas, <c>org:{id}</c> y <c>susc:{id}</c>.</item>
/// </list>
/// Las rutas solo se piden si no están en <see cref="CacheRutas"/>. Si están pero su <c>version</c> ya no es la de
/// <c>api:{id}</c>, se vuelven a pedir en un tercer viaje: solo pasa en la primera petición después de publicar.
/// </summary>
public sealed class LectorContexto(IConnectionMultiplexer redis, CacheRutas cacheRutas) : ILectorContexto
{
    public async Task LeerAsync(ContextoPeticion contexto)
    {
        var db = redis.GetDatabase();
        var peticion = contexto.Http.Request;

        var primero = db.CreateBatch();
        var apiIdLeida = primero.StringGetAsync(LlavesRedis.ApiPorHost(peticion.Host.Host));
        var hash = HashDeLaClave(peticion.Headers[CabecerasCompuerta.ApiKey]);
        var claveLeida = hash is null ? null : primero.HashGetAllAsync(LlavesRedis.Clave(hash));
        primero.Execute();
        await Task.WhenAll(Pendientes(apiIdLeida, claveLeida));

        if (!Guid.TryParse((await apiIdLeida).ToString(), out var apiId))
        {
            return;
        }

        contexto.Clave = claveLeida is null ? null : ContextoClave.DesdeCampos((await claveLeida).ACampos());

        // org y susc solo se leen con una clave de esta API: con otra, FiltroClave la rechaza.
        var clave = contexto.Clave is { } leida && leida.ApiId == apiId ? leida : null;
        var rutasEnMemoria = cacheRutas.Vigentes(apiId);

        // api:{id} se pide antes que sus rutas: si el publicador escribe en medio (las dos en una transacción), las
        // rutas nuevas quedan con la version anterior y se vuelven a leer en la siguiente petición. Al revés, unas
        // rutas viejas podrían quedar con la version nueva.
        var segundo = db.CreateBatch();
        var apiLeida = segundo.HashGetAllAsync(LlavesRedis.Api(apiId));
        var rutasLeidas = rutasEnMemoria is null ? segundo.StringGetAsync(LlavesRedis.RutasApi(apiId)) : null;
        var organizacionLeida = clave is null ? null : segundo.HashGetAllAsync(LlavesRedis.Organizacion(clave.OrganizacionId));
        var suscripcionLeida = clave is null ? null : segundo.HashGetAllAsync(LlavesRedis.Suscripcion(clave.SuscripcionId));
        segundo.Execute();
        await Task.WhenAll(Pendientes(apiLeida, rutasLeidas, organizacionLeida, suscripcionLeida));

        var api = ContextoApi.DesdeCampos(apiId, (await apiLeida).ACampos());
        if (api is null)
        {
            return;
        }

        contexto.Api = api;
        if (rutasLeidas is not null)
        {
            contexto.Rutas = cacheRutas.Guardar(apiId, api.Version, await rutasLeidas);
        }
        else if (rutasEnMemoria!.Version == api.Version)
        {
            contexto.Rutas = rutasEnMemoria.Tabla;
        }
        else
        {
            contexto.Rutas = cacheRutas.Guardar(apiId, api.Version, await db.StringGetAsync(LlavesRedis.RutasApi(apiId)));
        }

        if (clave is not null)
        {
            contexto.Organizacion = ContextoOrganizacion.DesdeCampos(clave.OrganizacionId, (await organizacionLeida!).ACampos());
            contexto.Suscripcion = ContextoSuscripcion.DesdeCampos(clave.SuscripcionId, (await suscripcionLeida!).ACampos());
        }
    }

    /// <summary>El hash de la clave si llega una sola <c>X-Api-Key</c> con valor; si no, <c>FiltroClave</c> la rechaza.</summary>
    private static string? HashDeLaClave(Microsoft.Extensions.Primitives.StringValues valores) =>
        valores.Count == 1 && !string.IsNullOrWhiteSpace(valores[0]) ? ContextoClave.CalcularHash(valores[0]!) : null;

    private static IEnumerable<Task> Pendientes(params Task?[] tareas) => tareas.OfType<Task>();
}
