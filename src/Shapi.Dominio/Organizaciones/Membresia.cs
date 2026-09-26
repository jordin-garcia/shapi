using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Organizaciones;

public class Membresia : IPerteneceAOrganizacion
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Rol Rol { get; private set; }

    protected Membresia() { }

    public Membresia(Guid usuarioId, Guid organizacionId, Rol rol)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        OrganizacionId = organizacionId;
        Rol = rol;
    }
}
