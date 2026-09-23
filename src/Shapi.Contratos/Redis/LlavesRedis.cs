using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Shapi.Contratos.Redis;

/// <summary>
/// Única definición del formato de las llaves de Redis (07 §4). La usan la API de control,
/// la compuerta y el trabajador; nunca se escribe una llave "a mano".
/// </summary>
public static class LlavesRedis
{
    public const string MetricasPendientes = "met:pendientes";
    public const string PatronLotes = "met:lote:*";
    public const string SaludTrabajador = "salud:trabajador";

    /// <summary>Desplazamiento del reloj del modo demostración (09 §9).</summary>
    public const string DemoRelojDesplazamiento = "demo:reloj:desplazamiento";

    /// <summary><c>api:host:{host}</c>. El host se normaliza a minúsculas.</summary>
    public static string ApiPorHost(string host) => $"api:host:{host.ToLowerInvariant()}";

    public static string Api(Guid apiId) => $"api:{apiId}";

    public static string RutasApi(Guid apiId) => $"api:{apiId}:rutas";

    /// <summary><c>clave:{sha256}</c>, con el hash en hex minúsculas.</summary>
    public static string Clave(string hashSha256) => $"clave:{hashSha256.ToLowerInvariant()}";

    public static string Suscripcion(Guid suscripcionId) => $"susc:{suscripcionId}";

    public static string Organizacion(Guid organizacionId) => $"org:{organizacionId}";

    /// <param name="inicioEpoch">El campo <c>inicio</c> de <c>susc:{id}</c>, en segundos Unix.</param>
    public static string CuotaSuscripcion(Guid suscripcionId, long inicioEpoch) =>
        $"cuota:susc:{suscripcionId}:{inicioEpoch.ToString(CultureInfo.InvariantCulture)}";

    /// <param name="cicloInicioEpoch">El campo <c>ciclo_inicio</c> de <c>org:{id}</c>, en segundos Unix.</param>
    public static string CuotaOrganizacion(Guid organizacionId, long cicloInicioEpoch) =>
        $"cuota:org:{organizacionId}:{cicloInicioEpoch.ToString(CultureInfo.InvariantCulture)}";

    /// <param name="minutoEpoch"><c>floor(unix / 60)</c> (08 §3).</param>
    public static string LimiteMinutoSuscripcion(Guid suscripcionId, long minutoEpoch) =>
        $"rl:s:{suscripcionId}:{minutoEpoch.ToString(CultureInfo.InvariantCulture)}";

    public static string LimiteMinutoRuta(Guid suscripcionId, Guid rutaId, long minutoEpoch) =>
        $"rl:r:{suscripcionId}:{rutaId}:{minutoEpoch.ToString(CultureInfo.InvariantCulture)}";

    public static string LimiteMinutoPruebas(Guid claveId, long minutoEpoch) =>
        $"rl:p:{claveId}:{minutoEpoch.ToString(CultureInfo.InvariantCulture)}";

    /// <param name="dia">El día en la zona America/Guatemala (07 §4); lo calcula quien llama.</param>
    public static string LimiteDiaPruebas(Guid claveId, DateOnly dia) =>
        $"dia:p:{claveId}:{Aaaammdd(dia)}";

    /// <summary>
    /// <c>cache:{api_id}:{ruta_id}:{sha256(metodo+ruta+query)}</c>. El método se normaliza a mayúsculas
    /// y la query incluye el <c>?</c> inicial, tal como llega en la petición.
    /// </summary>
    public static string Cache(Guid apiId, Guid rutaId, string metodo, string ruta, string? query)
    {
        var contenido = Encoding.UTF8.GetBytes(metodo.ToUpperInvariant() + ruta + query);
        var hash = Convert.ToHexStringLower(SHA256.HashData(contenido));
        return $"cache:{apiId}:{rutaId}:{hash}";
    }

    /// <summary><c>met:{aaaammdd}:{api}:{ruta|-}:{susc|-}:{entorno}</c>.</summary>
    /// <param name="fecha">El día en la zona America/Guatemala, que termina en <c>consumo_diario.fecha</c>; lo calcula quien llama.</param>
    public static string Metricas(DateOnly fecha, Guid apiId, Guid? rutaId, Guid? suscripcionId, string entorno) =>
        $"met:{Aaaammdd(fecha)}:{apiId}:{rutaId?.ToString() ?? "-"}:{suscripcionId?.ToString() ?? "-"}:{entorno}";

    public static string LoteMetricas(Guid loteId, string llave) => $"met:lote:{loteId}:{llave}";

    public static string PatronLote(Guid loteId) => $"met:lote:{loteId}:*";

    public static string SaludCompuerta(string instancia) => $"salud:compuerta:{instancia}";

    private static string Aaaammdd(DateOnly fecha) => fecha.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
}
