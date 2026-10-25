using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Comun;
using StackExchange.Redis;

namespace Shapi.Api.Suscripciones;

public sealed record PeticionRelojDemo(int AdelantarDias);

public sealed class RelojDemoApi(IConfiguration configuracion, IConnectionMultiplexer redis, RelojSistema reloj)
{
    public async Task<IResult> Adelantar(PeticionRelojDemo peticion, CancellationToken cancelacion)
    {
        if (!string.Equals(configuracion["SHAPI_MODO_DEMO"], "true", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.NotFound();
        }
        if (peticion.AdelantarDias <= 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Los días deben ser mayores que cero.", extensions: new Dictionary<string, object?> { ["codigo"] = "datos_invalidos" });
        }

        var segundos = checked((long)peticion.AdelantarDias * 24 * 60 * 60);
        var db = redis.GetDatabase();
        var total = await db.StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, segundos);
        reloj.AplicarDesplazamiento(total);
        await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), total);
        return TypedResults.NoContent();
    }
}
