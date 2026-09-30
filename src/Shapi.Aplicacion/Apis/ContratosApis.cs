using Shapi.Contratos.Red;
using Shapi.Dominio.Apis;

namespace Shapi.Aplicacion.Apis;

public sealed record SolicitudRegistroApi(string? Nombre, string? UrlOrigen, string? Subdominio);

public sealed record ActorRegistroApi(Guid Id, string NombreAlterno, string? Ip);

public sealed record ConfiguracionProteccionOrigen(bool ModoDemo, IReadOnlyList<string> OrigenesPermitidos);

public sealed record ResultadoPruebaOrigen(bool EsExitosa, long Milisegundos, string? Detalle)
{
    public static ResultadoPruebaOrigen Exito(long milisegundos) => new(true, milisegundos, null);

    public static ResultadoPruebaOrigen Fallo(string detalle) => new(false, 0, detalle);
}

public sealed record ApiRegistrada(
    Guid Id,
    string Nombre,
    string Subdominio,
    EstadoApi Estado,
    string SecretoOrigen,
    long ConexionMilisegundos);

public sealed record ApiListado(Guid Id, string Nombre, string Subdominio, EstadoApi Estado);

public sealed record ListaApis(IReadOnlyList<ApiListado> Elementos, int Total, string PlanNombre, int? MaxApis);

public interface IProbadorOrigen
{
    Task<ResultadoPruebaOrigen> Probar(DireccionOrigenValidada origen, CancellationToken cancelacion = default);
}

public interface IRepositorioApis
{
    Task<bool> ExisteSubdominio(string subdominio, CancellationToken cancelacion = default);

    Task<bool> Agregar(Api api, CancellationToken cancelacion = default);

    Task<ITransaccionApis> IniciarTransaccion(CancellationToken cancelacion = default);

    Task<ListaApis> Listar(Guid organizacionId, CancellationToken cancelacion = default);

    Task<string?> ObtenerNombreUsuario(Guid usuarioId, CancellationToken cancelacion = default);
}

public interface ITransaccionApis : IAsyncDisposable
{
    Task Confirmar(CancellationToken cancelacion = default);
}
