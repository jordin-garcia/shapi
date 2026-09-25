using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Identidad;

public class Consumidor : IPerteneceAOrganizacion
{
    public Guid Id { get; set; }
    public Guid OrganizacionId { get; set; }
    public string Nombre { get; set; } = null!;
    public string NombreEmpresa { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string HashContrasena { get; set; } = null!;
    public DateTimeOffset? CorreoVerificadoEn { get; set; }
    public EstadoCuenta Estado { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }

    public Consumidor() { }
}
