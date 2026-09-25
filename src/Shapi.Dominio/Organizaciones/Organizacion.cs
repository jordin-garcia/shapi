using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Organizaciones;

public class Organizacion
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public TipoOrganizacion Tipo { get; private set; }
    public EstadoAdmin EstadoAdmin { get; private set; }
    public string? MotivoSuspension { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Organizacion() { }

    public Organizacion(string nombre, TipoOrganizacion tipo)
    {
        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        Tipo = tipo;
        EstadoAdmin = EstadoAdmin.Activa;
    }
}
