using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Organizaciones;

public class Organizacion
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public TipoOrganizacion Tipo { get; set; }
    public EstadoAdmin EstadoAdmin { get; set; }
    public string? MotivoSuspension { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Organizacion() { }
}
