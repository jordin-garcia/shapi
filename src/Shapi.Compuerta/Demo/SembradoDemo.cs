using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Demo;

/// <summary>
/// Comando temporal <c>sembrar-demo</c> para la demostración del Avance 1: escribe en Redis la API de envíos y una
/// clave de producción. Lo elimina JG-04, cuando la configuración se publique desde PostgreSQL.
/// </summary>
public static class SembradoDemo
{
    public const string Comando = "sembrar-demo";
    public const string HostEnvios = "envios.api.shapi.localhost";

    // Clave de ejemplo de 08 §2; no es un secreto.
    private const string ClaveDemo = "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e";

    private static readonly Guid ApiId = Guid.Parse("0199b000-0000-7000-8000-00000000e001");
    private static readonly Guid OrganizacionId = Guid.Parse("0199b000-0000-7000-8000-00000000e002");
    private static readonly Guid SuscripcionId = Guid.Parse("0199b000-0000-7000-8000-00000000e003");
    private static readonly Guid ConsumidorId = Guid.Parse("0199b000-0000-7000-8000-00000000e004");
    private static readonly Guid ClaveId = Guid.Parse("0199b000-0000-7000-8000-00000000e005");

    /// <returns>El código de salida del proceso: 0 si sembró y 1 si no está en modo demostración.</returns>
    public static async Task<int> EjecutarAsync(IConfiguration configuracion, TextWriter salida)
    {
        if (!string.Equals(configuracion["SHAPI_MODO_DEMO"], "true", StringComparison.OrdinalIgnoreCase))
        {
            await salida.WriteLineAsync($"{Comando} solo funciona con SHAPI_MODO_DEMO=true.");
            return 1;
        }

        var urlOrigen = configuracion["SHAPI_URL_ORIGEN_ENVIOS"] ?? "http://localhost:5101";
        var api = new ContextoApi(ApiId, OrganizacionId, ContextoApi.EstadoPublicada, urlOrigen, null,
            "envios.shapi.localhost", 1);
        var clave = new ContextoClave(ClaveId, SuscripcionId, ApiId, OrganizacionId, ConsumidorId,
            ContextoClave.TipoProduccion);
        var llaveClave = LlavesRedis.Clave(ContextoClave.CalcularHash(ClaveDemo));

        await using var redis = await ConnectionMultiplexer.ConnectAsync(
            configuracion[ServiciosCompuerta.VariableRedis] ?? ServiciosCompuerta.RedisPorDefecto);
        var transaccion = redis.GetDatabase().CreateTransaction();
        _ = transaccion.StringSetAsync(LlavesRedis.ApiPorHost(HostEnvios), ApiId.ToString());
        _ = transaccion.KeyDeleteAsync([LlavesRedis.Api(ApiId), llaveClave]);
        _ = transaccion.HashSetAsync(LlavesRedis.Api(ApiId), AEntradas(api.ACampos()));
        _ = transaccion.HashSetAsync(llaveClave, AEntradas(clave.ACampos()));
        await transaccion.ExecuteAsync();

        await salida.WriteLineAsync($"Listo: {HostEnvios} → {urlOrigen}, con la clave de producción de demostración.");
        return 0;
    }

    private static HashEntry[] AEntradas(IReadOnlyDictionary<string, string> campos) =>
        [.. campos.Select(campo => new HashEntry(campo.Key, campo.Value))];
}
