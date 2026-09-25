using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Apis;

public class Api : IPerteneceAOrganizacion
{
    public Guid Id { get; set; }
    public Guid OrganizacionId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Subdominio { get; set; } = null!;
    public string UrlOrigen { get; set; } = null!;
    public EstadoApi Estado { get; set; }
    public string? Especificacion { get; set; }
    public EspecificacionFormato? EspecificacionFormato { get; set; }
    public string? EspecificacionTitulo { get; set; }
    public string? EspecificacionDescripcion { get; set; }
    public string? EspecificacionVersion { get; set; }
    public DateTimeOffset? EspecificacionCargadaEn { get; set; }
    public string? PortalNombre { get; set; }
    public string PortalColor { get; set; } = "#3B6FF0";
    public byte[]? PortalLogo { get; set; }
    public LogoTipo? PortalLogoTipo { get; set; }
    public string? PortalBienvenida { get; set; }
    public string SecretoOrigenCifrado { get; set; } = null!;
    public DateTimeOffset? PublicadaEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Api() { }
}
