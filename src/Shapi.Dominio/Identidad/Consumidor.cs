using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Identidad;

public class Consumidor : IPerteneceAOrganizacion
{
    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string NombreEmpresa { get; private set; } = null!;
    public string Correo { get; private set; } = null!;
    public string HashContrasena { get; private set; } = null!;
    public DateTimeOffset? CorreoVerificadoEn { get; private set; }
    public EstadoCuenta Estado { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    protected Consumidor() { }
}
