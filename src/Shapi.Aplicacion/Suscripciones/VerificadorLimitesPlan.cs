using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;

namespace Shapi.Aplicacion.Suscripciones;

public interface IVerificadorLimitesPlan
{
    Task<Error?> VerificarRegistroApi(Guid organizacionId, CancellationToken cancelacion = default);

    Task<bool> PermiteDominioPropio(Guid organizacionId, CancellationToken cancelacion = default);
}

public sealed class VerificadorLimitesPlan(IRepositorioApis repositorio) : IVerificadorLimitesPlan
{
    public async Task<Error?> VerificarRegistroApi(Guid organizacionId, CancellationToken cancelacion = default)
    {
        await repositorio.BloquearRegistroApi(organizacionId, cancelacion);
        var (total, maximo) = await repositorio.ContarApisYObtenerLimite(organizacionId, cancelacion);
        return maximo is not null && total >= maximo
            ? new Error(CodigosError.LimiteDelPlan, "Su plan llegó al límite de APIs.", new { limite = "apis" })
            : null;
    }

    public Task<bool> PermiteDominioPropio(Guid organizacionId, CancellationToken cancelacion = default) =>
        repositorio.PlanPermiteDominioPropio(organizacionId, cancelacion);
}
