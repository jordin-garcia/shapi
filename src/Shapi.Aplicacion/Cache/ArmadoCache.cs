using Shapi.Contratos.Redis;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Aplicacion.Cache;

/// <summary>
/// Arma, a partir de las entidades, lo que se publica en Redis (07 §4). No lee la base de datos ni Redis: lo usan el
/// publicador y la resincronización.
/// </summary>
public static class ArmadoCache
{
    /// <summary>
    /// Estado efectivo de una organización (07 §3.1): <c>suspendida</c> si la suspendió el administrador o si su
    /// suscripción de plataforma sin finalizar está suspendida; <c>activa</c> en cualquier otro caso.
    /// </summary>
    /// <param name="suscripcionPlataforma">El estado de su suscripción de plataforma sin finalizar, si tiene.</param>
    public static string EstadoEfectivo(EstadoAdmin estadoAdmin, EstadoSuscripcion? suscripcionPlataforma) =>
        estadoAdmin == EstadoAdmin.Suspendida || suscripcionPlataforma == EstadoSuscripcion.Suspendida
            ? ContextoOrganizacion.EstadoSuspendida
            : ContextoOrganizacion.EstadoActiva;

    /// <summary>Host de la API: <c>{sub}.api.{dominio_base}</c> (RF-11).</summary>
    public static string HostApi(string subdominio, string dominioBase) => $"{subdominio}.api.{dominioBase}".ToLowerInvariant();

    /// <summary>Host del portal: <c>{sub}.{dominio_base}</c> (RF-11). Es el origen que acepta CORS (08 §6).</summary>
    public static string HostPortal(string subdominio, string dominioBase) => $"{subdominio}.{dominioBase}".ToLowerInvariant();

    /// <summary>
    /// El hash <c>api:{id}</c>. La <c>version</c> queda en 0: la asigna quien escribe en Redis, porque se incrementa
    /// en cada publicación.
    /// </summary>
    /// <param name="secreto">El secreto de origen ya descifrado.</param>
    public static ContextoApi Api(Api api, string? secreto, string dominioBase) => new(
        api.Id,
        api.OrganizacionId,
        Texto(api.Estado),
        api.UrlOrigen,
        secreto,
        HostPortal(api.Subdominio, dominioBase),
        0);

    /// <summary>Todas las rutas de la API, expuestas u ocultas, ordenadas por patrón y método para que el JSON sea estable.</summary>
    public static IReadOnlyList<RutaCache> Rutas(IEnumerable<Ruta> rutas) =>
    [
        .. rutas
            .OrderBy(r => r.Patron, StringComparer.Ordinal)
            .ThenBy(r => r.Metodo)
            .Select(r => new RutaCache(r.Id, Texto(r.Metodo), r.Patron, r.Expuesta, r.LimiteMinuto, r.CacheSegundos,
                r.PesoLlamadas)),
    ];

    /// <summary>Los hosts que apuntan a la API: el suyo y, si está verificado, el dominio propio (RF-11, RF-12).</summary>
    public static IReadOnlyList<string> Hosts(Api api, DominioPropio? dominioPropio, string dominioBase)
    {
        var hosts = new List<string> { HostApi(api.Subdominio, dominioBase) };
        if (dominioPropio is { Estado: EstadoDominio.Verificado })
        {
            hosts.Add(dominioPropio.Dominio.ToLowerInvariant());
        }

        return hosts;
    }

    public static ContextoClave Clave(Clave clave, SuscripcionApi suscripcion, Guid organizacionId) => new(
        clave.Id,
        suscripcion.Id,
        suscripcion.ApiId,
        organizacionId,
        suscripcion.ConsumidorId,
        clave.Tipo == TipoClave.Produccion ? ContextoClave.TipoProduccion : ContextoClave.TipoPruebas);

    /// <summary>
    /// Si la clave va en Redis: sí si está activa (sin vencimiento) o rotada y vigente (con <c>EXPIREAT</c> en
    /// <paramref name="expiraEn"/>); no si está revocada o si su periodo de rotación ya terminó (07 §4).
    /// </summary>
    public static bool SePublica(Clave clave, DateTimeOffset ahora, out DateTimeOffset? expiraEn)
    {
        expiraEn = null;
        switch (clave.Estado)
        {
            case EstadoClave.Activa:
                return true;
            case EstadoClave.Rotada when clave.ExpiraEn > ahora:
                expiraEn = clave.ExpiraEn;
                return true;
            default:
                return false;
        }
    }

    /// <summary>El hash <c>susc:{id}</c>. Una suscripción finalizada no se publica.</summary>
    public static ContextoSuscripcion Suscripcion(SuscripcionApi suscripcion, PlanApi plan) => new(
        suscripcion.Id,
        plan.Id,
        plan.Nombre,
        Texto(suscripcion.Estado),
        suscripcion.Inicio.ToUnixTimeSeconds(),
        suscripcion.Fin.ToUnixTimeSeconds(),
        plan.CuotaLlamadas,
        plan.LimiteMinuto);

    /// <summary>El hash <c>org:{id}</c>, con la cuota y el ciclo de su suscripción de plataforma sin finalizar, si tiene.</summary>
    public static ContextoOrganizacion Organizacion(
        Organizacion organizacion, SuscripcionPlataforma? suscripcion, PlanPlataforma? plan) => new(
        organizacion.Id,
        EstadoEfectivo(organizacion.EstadoAdmin, suscripcion?.Estado),
        suscripcion is null ? null : plan?.CuotaPeticiones,
        suscripcion?.Inicio.ToUnixTimeSeconds(),
        suscripcion?.Fin.ToUnixTimeSeconds());

    /// <summary>Los mismos textos que guarda la base de datos (07 §3.2).</summary>
    public static string Texto(EstadoApi estado) => estado switch
    {
        EstadoApi.Borrador => ContextoApi.EstadoBorrador,
        EstadoApi.Publicada => ContextoApi.EstadoPublicada,
        _ => ContextoApi.EstadoDespublicada,
    };

    public static string Texto(MetodoHttp metodo) => metodo.ToString().ToUpperInvariant();

    /// <summary>Los mismos textos que guarda la base de datos (07 §3.3).</summary>
    public static string Texto(EstadoSuscripcion estado) => estado switch
    {
        EstadoSuscripcion.Activa => ContextoSuscripcion.EstadoActiva,
        EstadoSuscripcion.EnGracia => ContextoSuscripcion.EstadoEnGracia,
        EstadoSuscripcion.Suspendida => ContextoSuscripcion.EstadoSuspendida,
        _ => ContextoSuscripcion.EstadoFinalizada,
    };
}
