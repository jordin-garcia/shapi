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

public sealed record OperacionEspecificacion(
    MetodoHttp Metodo,
    string Patron,
    string? Resumen,
    string? Descripcion,
    string Definicion,
    int Orden);

public sealed record EspecificacionLeida(
    EspecificacionFormato Formato,
    string VersionOpenApi,
    string Titulo,
    string? Descripcion,
    string Version,
    IReadOnlyList<OperacionEspecificacion> Operaciones);

/// <summary>
/// <paramref name="Mensaje"/> va siempre en español (RNF-12). Si el error viene de Microsoft.OpenApi o de SharpYaml,
/// su texto original, en inglés, va aparte en <paramref name="DetalleTecnico"/>.
/// </summary>
public sealed record ErrorLecturaEspecificacion(string Ubicacion, string Mensaje, string? DetalleTecnico = null);

public sealed record ResultadoLecturaEspecificacion(EspecificacionLeida? Especificacion, ErrorLecturaEspecificacion? Error)
{
    public bool EsValida => Especificacion is not null;
}

public sealed record RutaAdministrada(
    Guid Id,
    string Metodo,
    string Patron,
    string? Resumen,
    string? Descripcion,
    bool Expuesta,
    int Orden);

/// <summary>La especificación que la API tiene cargada, para la tarjeta del archivo de A3.3.</summary>
public sealed record ResumenEspecificacion(
    string Titulo,
    string Version,
    string VersionOpenApi,
    string Formato,
    DateTimeOffset CargadaEn,
    int TamanoBytes);

public sealed record ListaRutas(
    Guid ApiId,
    string ApiNombre,
    IReadOnlyList<RutaAdministrada> Elementos,
    int TotalExpuestas,
    int TotalOcultas,
    ResumenEspecificacion? Especificacion);

public sealed record EspecificacionCargada(
    Guid ApiId,
    string ApiNombre,
    string Titulo,
    string? Descripcion,
    string Version,
    string VersionOpenApi,
    string Formato,
    DateTimeOffset CargadaEn,
    int TotalRutas,
    IReadOnlyList<RutaAdministrada> Rutas);

/// <summary>Los dos campos son anulables para responder 400 si faltan, en vez de tomar <c>false</c> sin avisar.</summary>
public sealed record CambioExposicionRuta(Guid? RutaId, bool? Expuesta);

public sealed record SolicitudExposicionRutas(IReadOnlyList<CambioExposicionRuta?> Cambios, ActorRegistroApi Actor);

public interface ILectorEspecificacionOpenApi
{
    Task<ResultadoLecturaEspecificacion> Leer(
        string contenido,
        string nombreArchivo,
        CancellationToken cancelacion = default);

    /// <summary>La versión de OpenAPI de un documento que ya se validó al cargarlo (el campo <c>openapi</c> de la raíz).</summary>
    string LeerVersionOpenApi(string contenido);
}

public interface IProbadorOrigen
{
    Task<ResultadoPruebaOrigen> Probar(DireccionOrigenValidada origen, CancellationToken cancelacion = default);
}

public interface IRepositorioApis
{
    Task<bool> ExisteSubdominio(string subdominio, CancellationToken cancelacion = default);

    Task<bool> Agregar(Api api, CancellationToken cancelacion = default);

    Task<ITransaccionApis> IniciarTransaccion(CancellationToken cancelacion = default);

    /// <summary>
    /// Bloquea la fila de la API hasta el final de la transacción (<c>FOR UPDATE</c>) y vuelve a leer la entidad, que pudo
    /// cambiar mientras se esperaba el bloqueo.
    /// </summary>
    Task BloquearApi(Api api, CancellationToken cancelacion = default);

    Task<ListaApis> Listar(Guid organizacionId, CancellationToken cancelacion = default);

    Task<string?> ObtenerNombreUsuario(Guid usuarioId, CancellationToken cancelacion = default);

    Task<Api?> Obtener(Guid apiId, Guid organizacionId, CancellationToken cancelacion = default);

    Task<List<Ruta>> ObtenerRutas(Guid apiId, CancellationToken cancelacion = default);

    Task ConsolidarConsumoRutas(
        IReadOnlyCollection<Guid> rutaIds,
        DateTimeOffset actualizadoEn,
        CancellationToken cancelacion = default);

    void AgregarRuta(Ruta ruta);

    void EliminarRuta(Ruta ruta);

    Task Guardar(CancellationToken cancelacion = default);
}

public interface ITransaccionApis : IAsyncDisposable
{
    Task Confirmar(CancellationToken cancelacion = default);
}
