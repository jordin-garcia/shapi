using Shapi.Dominio.Soporte;

namespace Shapi.Aplicacion.Soporte;

public sealed record Pagina<T>(IReadOnlyList<T> Elementos, int Total);

public sealed record SolicitudAbrirCaso(string Asunto, Guid? ApiId, string Descripcion);

public sealed record SolicitudAbrirCasoAdministracion(Guid OrganizacionId, string Asunto, Guid? ApiId, string Descripcion);

public sealed record SolicitudMensajeCaso(string Cuerpo);

public sealed record CasoListado(
    int Numero,
    string Asunto,
    string? ApiNombre,
    string Estado,
    DateTimeOffset CreadoEn,
    int Respuestas);

public sealed record CasoListadoAdministracion(
    int Numero,
    string Asunto,
    string? ApiNombre,
    string Estado,
    DateTimeOffset CreadoEn,
    int Respuestas,
    string Organizacion);

public sealed record MensajeCaso(
    Guid Id,
    string Autor,
    string Rol,
    string Cuerpo,
    DateTimeOffset CreadoEn,
    bool EsPersonalPlataforma);

public sealed record CasoDetalle(
    int Numero,
    string Asunto,
    string Estado,
    string Organizacion,
    string? ApiNombre,
    string CreadoPor,
    string? AsignadoA,
    DateTimeOffset CreadoEn,
    IReadOnlyList<MensajeCaso> Mensajes);

public sealed record ResumenOrganizacionCaso(
    string Organizacion,
    string? ApiAfectada,
    string Plan,
    DateTimeOffset CicloInicio,
    DateTimeOffset CicloFin,
    string Estado,
    int NumeroApis,
    int NumeroConsumidores,
    string? DominioPropio,
    string? VerificacionDominio);

public sealed record OpcionApiCaso(Guid Id, string Nombre);

public sealed record OpcionOrganizacionCaso(Guid Id, string Nombre, IReadOnlyList<OpcionApiCaso> Apis);

public sealed record DestinatarioCaso(string Nombre, string Correo, bool Plataforma);

public interface ITransaccionSoporte : IAsyncDisposable
{
    Task Confirmar(CancellationToken cancelacion);
}

public interface IRepositorioSoporte
{
    Task<Pagina<CasoListado>> ListarProveedor(Guid organizacionId, int pagina, int tamano, CancellationToken cancelacion);
    Task<Pagina<CasoListadoAdministracion>> ListarAdministracion(int pagina, int tamano, CancellationToken cancelacion);
    Task<IReadOnlyList<OpcionOrganizacionCaso>> ListarOrganizaciones(CancellationToken cancelacion);
    Task<bool> ExisteOrganizacionProveedor(Guid organizacionId, CancellationToken cancelacion);
    Task<bool> ApiPertenece(Guid apiId, Guid organizacionId, CancellationToken cancelacion);
    Task Agregar(Caso caso, CancellationToken cancelacion);
    Task<Caso?> ObtenerProveedor(int numero, Guid organizacionId, bool bloquear, CancellationToken cancelacion);
    Task<Caso?> ObtenerAdministracion(int numero, bool bloquear, CancellationToken cancelacion);
    Task GuardarMensaje(CasoMensaje mensaje, CancellationToken cancelacion);
    Task GuardarCambios(CancellationToken cancelacion);
    Task<CasoDetalle?> Detalle(Caso caso, CancellationToken cancelacion);
    Task<ResumenOrganizacionCaso?> ResumenOrganizacion(int numero, CancellationToken cancelacion);
    Task<MensajeCaso> PresentarMensaje(CasoMensaje mensaje, Guid organizacionCasoId, CancellationToken cancelacion);
    Task<DestinatarioCaso?> DestinatarioProveedor(Caso caso, CancellationToken cancelacion);
    Task<DestinatarioCaso?> DestinatarioPlataforma(Guid? asignadoA, CancellationToken cancelacion);
    string EnlaceCaso(bool plataforma, int numero);
    Task<ITransaccionSoporte> IniciarTransaccion(CancellationToken cancelacion);
}
