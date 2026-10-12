using Microsoft.Extensions.Configuration;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Infraestructura.Comun;

/// <summary>Reloj real en UTC. EM-10 le agrega el desplazamiento del modo demostración (09 §9).</summary>
public sealed class RelojSistema(TimeProvider proveedorTiempo, IConfiguration configuracion, IConnectionMultiplexer? redis = null) : IReloj
{
    public DateTimeOffset Ahora
    {
        get
        {
            var ahora = proveedorTiempo.GetUtcNow();
            if (!string.Equals(configuracion["SHAPI_MODO_DEMO"], "true", StringComparison.OrdinalIgnoreCase) || redis is null)
            {
                return ahora;
            }

            try
            {
                var desplazamiento = redis.GetDatabase().StringGet(LlavesRedis.DemoRelojDesplazamiento);
                return desplazamiento.TryParse(out long segundos) ? ahora.AddSeconds(segundos) : ahora;
            }
            catch (RedisException)
            {
                return ahora;
            }
        }
    }
}
