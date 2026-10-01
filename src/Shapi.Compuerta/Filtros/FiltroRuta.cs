using Shapi.Contratos;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 5 (08 §3): el método y el camino deben coincidir con una ruta expuesta (RF-10), con la regla de
/// especificidad de 08 §1. Deja la ruta en <see cref="ContextoPeticion.Ruta"/>, para los límites (JG-06) y la
/// medición.
/// </summary>
public sealed class FiltroRuta : IFiltroCompuerta
{
    private static readonly ResultadoFiltro NoPermitida = ResultadoFiltro.Rechazar(
        StatusCodes.Status403Forbidden, CodigosError.RutaNoPermitida,
        "Esta API no permite el método y la ruta solicitados.");

    public ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        var peticion = contexto.Http.Request;
        var ruta = contexto.Rutas.Buscar(peticion.Method, peticion.Path.Value ?? "/");
        if (ruta is not { Expuesta: true })
        {
            return ValueTask.FromResult(NoPermitida);
        }

        contexto.Ruta = ruta;
        return ValueTask.FromResult(ResultadoFiltro.Continuar);
    }
}
