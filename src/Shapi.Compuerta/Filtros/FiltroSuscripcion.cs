using Shapi.Contratos;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 4 (08 §3): la suscripción de la clave debe estar <c>activa</c> o <c>en_gracia</c>. Una suscripción
/// finalizada no tiene <c>susc:{id}</c> (07 §4). <b>No compara fechas</b>: los cambios de estado los hace el
/// trabajador (CU-16), así que si el trabajador está caído el servicio sigue funcionando (RNF-04).
/// </summary>
public sealed class FiltroSuscripcion : IFiltroCompuerta
{
    private static readonly ResultadoFiltro Inactiva = ResultadoFiltro.Rechazar(
        StatusCodes.Status403Forbidden, CodigosError.SuscripcionInactiva,
        "Su suscripción a esta API no está activa. Revise su plan en el portal de la API.");

    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto) =>
        ValueTask.FromResult(contexto.Suscripcion is { PermiteTrafico: true } ? ResultadoFiltro.Continuar : Inactiva);
}
