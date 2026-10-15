using System.Globalization;
using Shapi.Aplicacion.Consumo;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Infraestructura.Consumo;

public sealed class LectorCuotaConsumo(IConnectionMultiplexer redis) : ILectorCuotaConsumo
{
    public async Task<long?> LeerAsync(Guid suscripcionId, DateTimeOffset inicio, CancellationToken cancelacion = default)
    {
        try
        {
            var valor = await redis.GetDatabase().StringGetAsync(LlavesRedis.CuotaSuscripcion(suscripcionId, inicio.ToUnixTimeSeconds()))
                .WaitAsync(cancelacion);
            if (valor.IsNull)
            {
                return 0;
            }
            return long.TryParse(valor.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var usadas) && usadas >= 0
                ? usadas : null;
        }
        catch (Exception error) when (error is RedisException or TimeoutException)
        {
            return null;
        }
    }
}
