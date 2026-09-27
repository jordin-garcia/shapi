using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;
using Shapi.Contratos;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 2 (08 §3): calcula el SHA-256 de <c>X-Api-Key</c> y busca <c>clave:{hash}</c>. La clave debe ser de la
/// API que resolvió <see cref="FiltroApi"/>. La clave en claro nunca se guarda ni se registra (RF-29).
/// Una clave en la query string se rechaza sin reenviar, aunque también venga la cabecera (08 §1).
/// </summary>
public sealed partial class FiltroClave(IConnectionMultiplexer redis) : IFiltroCompuerta
{
    private static readonly Dictionary<string, string> CabecerasAutenticacion = new()
    {
        [HeaderNames.WWWAuthenticate] = $"ApiKey header=\"{CabecerasCompuerta.ApiKey}\"",
    };

    private static readonly ResultadoFiltro Ausente = ResultadoFiltro.Rechazar(
        StatusCodes.Status401Unauthorized, CodigosError.ClaveAusente,
        $"Falta la cabecera {CabecerasCompuerta.ApiKey} con su clave de acceso.", CabecerasAutenticacion);

    private static readonly ResultadoFiltro EnUrl = ResultadoFiltro.Rechazar(
        StatusCodes.Status401Unauthorized, CodigosError.ClaveEnUrl,
        $"No envíe su clave de acceso en la URL: envíela en la cabecera {CabecerasCompuerta.ApiKey}.", CabecerasAutenticacion);

    private static readonly ResultadoFiltro Invalida = ResultadoFiltro.Rechazar(
        StatusCodes.Status401Unauthorized, CodigosError.ClaveInvalida,
        "La clave de acceso no es válida para esta API.", CabecerasAutenticacion);

    public async ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        var api = contexto.Api ?? throw new InvalidOperationException("FiltroClave debe ir después de FiltroApi.");
        if (TieneClaveEnLaQuery(contexto.Http.Request.Query))
        {
            return EnUrl;
        }

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

    /// <summary>Un nombre o un valor de la query con el formato de clave de 08 §2, sea cual sea el parámetro.</summary>
    private static bool TieneClaveEnLaQuery(IQueryCollection query) =>
        query.Any(parametro => FormatoClave().IsMatch(parametro.Key)
            || parametro.Value.Any(valor => valor is not null && FormatoClave().IsMatch(valor)));

    [GeneratedRegex("^shp_(?:prod|prueba)_[0-9A-Za-z]{26}$", RegexOptions.CultureInvariant)]
    private static partial Regex FormatoClave();
}
