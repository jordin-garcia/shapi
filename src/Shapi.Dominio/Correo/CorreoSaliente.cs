namespace Shapi.Dominio.Correo;

public class CorreoSaliente
{
    public Guid Id { get; private set; }
    public string Destinatario { get; private set; } = null!;
    public string Asunto { get; private set; } = null!;
    public string Plantilla { get; private set; } = null!;
    public string Datos { get; private set; } = null!; // JSONB
    public EstadoCorreo Estado { get; private set; }
    public int Intentos { get; private set; }
    public DateTimeOffset? ProximoIntentoEn { get; private set; }
    public string? UltimoError { get; private set; }
    public DateTimeOffset? EnviadoEn { get; private set; }


    public CorreoSaliente(EstadoCorreo estado, string plantilla, string destinatario, string datos, string asunto)
    {
        Id = Guid.NewGuid();
        Estado = estado;
        Plantilla = plantilla;
        Destinatario = destinatario;
        Datos = datos;
        Asunto = asunto;
    }

    protected CorreoSaliente() { }
}
