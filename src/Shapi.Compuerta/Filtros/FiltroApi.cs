using Shapi.Contratos;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Filtros;

/// <summary>Filtro 1 (08 §3): <c>host → api_id → api:{id}</c>. Exige que la API esté publicada (RF-29, RF-14).</summary>
public sealed class FiltroApi(IConnectionMultiplexer redis) : IFiltroCompuerta
{
    private static readonly ResultadoFiltro NoEncontrada = ResultadoFiltro.Rechazar(
        StatusCodes.Status404NotFound, CodigosError.ApiNoEncontrada, "No hay ninguna API publicada en este dominio.");

    public async ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        var db = redis.GetDatabase();
        var apiId = await db.StringGetAsync(LlavesRedis.ApiPorHost(contexto.Http.Request.Host.Host));
        if (!Guid.TryParse(apiId.ToString(), out var id))
        {
            return NoEncontrada;
        }

        var campos = await db.HashGetAllAsync(LlavesRedis.Api(id));
        var api = ContextoApi.DesdeCampos(id, campos.ACampos());
        if (api is null || !api.EstaPublicada)
        {
            return NoEncontrada;
        }

        contexto.Api = api;
        return ResultadoFiltro.Continuar;
    }
}
