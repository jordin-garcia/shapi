using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Comun;
using StackExchange.Redis;

namespace Shapi.Api.Suscripciones;

public sealed record PeticionRelojDemo(int AdelantarDias);

public sealed class RelojDemoApi(IConfiguration configuracion, IConnectionMultiplexer redis, RelojSistema reloj)
{
    public IResult Consultar() => string.Equals(configuracion["SHAPI_MODO_DEMO"], "true", StringComparison.OrdinalIgnoreCase)
        ? ConsultarFecha(reloj)
        : TypedResults.NotFound();

    public static IResult ConsultarFecha(Shapi.Aplicacion.Comun.IReloj reloj)
    {
        var ahora = reloj.Ahora;
        return TypedResults.Ok(new { ahora, inicioCiclo = Suscripcion.InicioDeCiclo(ahora), finClaveAnteriorPrevisto = ahora.AddHours(24) });
    }

    public async Task<IResult> Adelantar(PeticionRelojDemo peticion, CancellationToken cancelacion)
    {
        if (!string.Equals(configuracion["SHAPI_MODO_DEMO"], "true", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.NotFound();
        }
        if (peticion.AdelantarDias is <= 0 or > 365)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Indique entre 1 y 365 días.", extensions: new Dictionary<string, object?> { ["codigo"] = "datos_invalidos" });
        }

        var segundos = checked((long)peticion.AdelantarDias * 24 * 60 * 60);
        var db = redis.GetDatabase();
        var total = await db.StringIncrementAsync(LlavesRedis.DemoRelojDesplazamiento, segundos);
        reloj.AplicarDesplazamiento(total);
        await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), total);
        return TypedResults.NoContent();
    }
}
