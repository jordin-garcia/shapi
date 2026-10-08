using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Organizaciones;

public class Membresia : IPerteneceAOrganizacion
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Rol Rol { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Membresia() { }

    public Membresia(Guid usuarioId, Guid organizacionId, Rol rol)
    {
        Id = Guid.CreateVersion7();
        UsuarioId = usuarioId;
        OrganizacionId = organizacionId;
        Rol = rol;
    }

    public void CambiarRol(Rol rol)
    {
        if (Rol == Rol.Propietario)
        {
            throw new InvalidOperationException("El rol del propietario no se puede cambiar.");
        }

        if (rol is not (Rol.Editor or Rol.Lector))
        {
            throw new ArgumentOutOfRangeException(nameof(rol));
        }

        Rol = rol;
    }
}
