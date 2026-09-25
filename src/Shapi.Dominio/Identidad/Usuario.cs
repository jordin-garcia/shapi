namespace Shapi.Dominio.Identidad;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string? HashContrasena { get; set; }
    public DateTimeOffset? CorreoVerificadoEn { get; set; }
    public EstadoCuenta Estado { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }

    public Usuario() { }
}
