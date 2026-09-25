using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Apis;

public class Api : IPerteneceAOrganizacion
{
    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Subdominio { get; private set; } = null!;
    public string UrlOrigen { get; private set; } = null!;
    public EstadoApi Estado { get; private set; }
    public string? Especificacion { get; private set; }
    public EspecificacionFormato? EspecificacionFormato { get; private set; }
    public string? EspecificacionTitulo { get; private set; }
    public string? EspecificacionDescripcion { get; private set; }
    public string? EspecificacionVersion { get; private set; }
    public DateTimeOffset? EspecificacionCargadaEn { get; private set; }
    public string? PortalNombre { get; private set; }
    public string PortalColor { get; private set; } = "#3B6FF0";
    public byte[]? PortalLogo { get; private set; }
    public LogoTipo? PortalLogoTipo { get; private set; }
    public string? PortalBienvenida { get; private set; }
    public string SecretoOrigenCifrado { get; private set; } = null!;
    public DateTimeOffset? PublicadaEn { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Api() { }
}
