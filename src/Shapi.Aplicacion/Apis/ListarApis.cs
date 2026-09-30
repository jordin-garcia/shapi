namespace Shapi.Aplicacion.Apis;

public sealed class ListarApis(IRepositorioApis repositorio)
{
    public Task<ListaApis> Ejecutar(Guid organizacionId, CancellationToken cancelacion = default) =>
        repositorio.Listar(organizacionId, cancelacion);
}
