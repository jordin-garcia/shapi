using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;
using Shapi.Contratos;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 2 (08 §3): la clave de <c>X-Api-Key</c>, que <c>ILectorContexto</c> buscó por su SHA-256 en
/// <c>clave:{hash}</c>, debe ser de la API que resolvió <see cref="FiltroApi"/>. La clave en claro nunca se guarda ni
/// se registra (RF-29). Una clave en la query string se rechaza sin reenviar, aunque también venga la cabecera (08 §1).
/// </summary>
public sealed partial class FiltroClave : IFiltroCompuerta
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

    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto) => ValueTask.FromResult(Evaluar(contexto));

    private static ResultadoFiltro Evaluar(ContextoPeticion contexto)
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

        // Con varias X-Api-Key, el lector no buscó ninguna.
        var clave = contexto.Clave;
        if (valores.Count > 1 || clave is null || clave.ApiId != api.ApiId || clave.OrganizacionId != api.OrganizacionId)
        {
            return Invalida;
        }

        contexto.ClaveValidada = true;
        return ResultadoFiltro.Continuar;
    }

    /// <summary>
    /// Una clave con el formato de 08 §2 en cualquier nombre o valor de la query, sola o dentro de un texto más largo
    /// ("Bearer shp_prod_…", "1;k=shp_prod_…"). <see cref="HttpRequest.Query"/> ya decodifica el <c>%xx</c>.
    /// </summary>
    private static bool TieneClaveEnLaQuery(IQueryCollection query) =>
        query.Any(parametro => FormatoClave().IsMatch(parametro.Key)
            || parametro.Value.Any(valor => valor is not null && FormatoClave().IsMatch(valor)));

    [GeneratedRegex("(?<![0-9A-Za-z])shp_(?:prod|prueba)_[0-9A-Za-z]{26}(?![0-9A-Za-z])", RegexOptions.CultureInvariant)]
    private static partial Regex FormatoClave();
}
