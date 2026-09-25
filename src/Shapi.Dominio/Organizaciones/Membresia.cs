using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Organizaciones;

public class Membresia : IPerteneceAOrganizacion
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid OrganizacionId { get; set; }
    public Rol Rol { get; set; }

    public Membresia() { }
}
