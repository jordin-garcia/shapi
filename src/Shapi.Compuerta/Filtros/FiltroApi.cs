using Shapi.Contratos;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 1 (08 §3): el host debe corresponder a una API publicada (RF-29, RF-14). <c>ILectorContexto</c> ya leyó
/// <c>api:host:{host}</c> y <c>api:{id}</c>.
/// </summary>
public sealed class FiltroApi : IFiltroCompuerta
{
    private static readonly ResultadoFiltro NoEncontrada = ResultadoFiltro.Rechazar(
        StatusCodes.Status404NotFound, CodigosError.ApiNoEncontrada, "No hay ninguna API publicada en este dominio.");

    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto) =>
        ValueTask.FromResult(contexto.Api is { EstaPublicada: true } ? ResultadoFiltro.Continuar : NoEncontrada);
}
