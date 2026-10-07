using System.Text.Json;

namespace Shapi.Aplicacion.Portal;

/// <summary>Genera la documentación pública de las rutas expuestas de un portal.</summary>
public interface IConsultorDocumentacionPortal
{
    Task<DocumentacionPortal> Consultar(PortalResuelto portal, CancellationToken cancelacion = default);
}

public sealed record DocumentacionPortal(IReadOnlyList<RutaDocumentada> Rutas);

public sealed record RutaDocumentada(
    string Metodo,
    string Patron,
    string? Resumen,
    string? Descripcion,
    IReadOnlyList<ParametroDocumentado> Parametros,
    JsonElement? EjemploPeticion,
    JsonElement? EjemploRespuesta,
    int? CodigoRespuesta,
    int PesoLlamadas,
    string UrlCompleta);

public sealed record ParametroDocumentado(
    string Nombre,
    string Tipo,
    bool Obligatorio,
    string? Descripcion);
