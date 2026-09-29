using Shapi.Dominio.Apis;

namespace Shapi.Aplicacion.Portal;

/// <summary>Resuelve la API publicada y su organización a partir del host del portal.</summary>
public interface IResolutorPortal
{
    Task<PortalResuelto?> Resolver(string host, CancellationToken cancelacion = default);
}

/// <summary>Datos de la API y de la organización que comparten los endpoints del portal.</summary>
public sealed record PortalResuelto(
    Guid ApiId,
    Guid OrganizacionId,
    string NombreOrganizacion,
    string Subdominio,
    string NombreApi,
    string? DescripcionApi,
    string NombrePortal,
    string ColorPrincipal,
    string? Bienvenida,
    byte[]? Logo,
    LogoTipo? TipoLogo,
    string HostPortal,
    string HostApi);
