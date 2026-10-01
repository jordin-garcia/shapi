using Shapi.Contratos;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 3 (08 §3): la organización proveedora debe tener <c>estado_efectivo = activa</c> (07 §3.1). Si
/// <c>org:{id}</c> no está en Redis, no se puede comprobar y tampoco se deja pasar.
/// </summary>
public sealed class FiltroOrganizacion : IFiltroCompuerta
{
    private static readonly ResultadoFiltro NoDisponible = ResultadoFiltro.Rechazar(
        StatusCodes.Status403Forbidden, CodigosError.ApiNoDisponible,
        "Esta API no está disponible en este momento. Comuníquese con el proveedor.");

    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto) =>
        ValueTask.FromResult(contexto.Organizacion is { EstaActiva: true } ? ResultadoFiltro.Continuar : NoDisponible);
}
