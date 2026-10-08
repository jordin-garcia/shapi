namespace Shapi.Aplicacion.Soporte;

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
