using Microsoft.Net.Http.Headers;
using Shapi.Contratos;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 2 (08 §3): calcula el SHA-256 de <c>X-Api-Key</c> y busca <c>clave:{hash}</c>. La clave debe ser de la
/// API que resolvió <see cref="FiltroApi"/>. La clave en claro nunca se guarda ni se registra (RF-29).
/// </summary>
public sealed class FiltroClave(IConnectionMultiplexer redis) : IFiltroCompuerta
{
    private static readonly Dictionary<string, string> CabecerasAutenticacion = new()
    {
        [HeaderNames.WWWAuthenticate] = $"ApiKey header=\"{CabecerasCompuerta.ApiKey}\"",
    };

    private static readonly ResultadoFiltro Ausente = ResultadoFiltro.Rechazar(
        StatusCodes.Status401Unauthorized, CodigosError.ClaveAusente,
        $"Falta la cabecera {CabecerasCompuerta.ApiKey} con su clave de acceso.", CabecerasAutenticacion);

    private static readonly ResultadoFiltro Invalida = ResultadoFiltro.Rechazar(
        StatusCodes.Status401Unauthorized, CodigosError.ClaveInvalida,
        "La clave de acceso no es válida para esta API.", CabecerasAutenticacion);

    public async ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        var api = contexto.Api ?? throw new InvalidOperationException("FiltroClave debe ir después de FiltroApi.");
        var valores = contexto.Http.Request.Headers[CabecerasCompuerta.ApiKey];
        if (valores.Count == 0 || valores.All(string.IsNullOrWhiteSpace))
        {
            return Ausente;
        }

        if (valores.Count > 1)
        {
            return Invalida;
        }

        var campos = await redis.GetDatabase().HashGetAllAsync(LlavesRedis.Clave(ContextoClave.CalcularHash(valores[0]!)));
        var clave = ContextoClave.DesdeCampos(campos.ACampos());
        if (clave is null || clave.ApiId != api.ApiId)
        {
            return Invalida;
        }

        contexto.Clave = clave;
        return ResultadoFiltro.Continuar;
    }
}
