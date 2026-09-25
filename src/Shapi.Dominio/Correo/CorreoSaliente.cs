namespace Shapi.Dominio.Correo;

public class CorreoSaliente
{
    public Guid Id { get; set; }
    public string Destinatario { get; set; } = null!;
    public string Asunto { get; set; } = null!;
    public string Plantilla { get; set; } = null!;
    public string Datos { get; set; } = null!; // JSONB
    public EstadoCorreo Estado { get; set; }
    public int Intentos { get; set; }
    public DateTimeOffset? ProximoIntentoEn { get; set; }
    public string? UltimoError { get; set; }
    public DateTimeOffset? EnviadoEn { get; set; }


    public CorreoSaliente(EstadoCorreo estado, string plantilla, string destinatario, string datos, string asunto)
    {
        Id = Guid.NewGuid();
        Estado = estado;
        Plantilla = plantilla;
        Destinatario = destinatario;
        Datos = datos;
        Asunto = asunto;
    }

    public CorreoSaliente() { }
}
