namespace Shapi.Dominio.Correo;

public class CorreoSaliente
{
    private static readonly TimeSpan[] EsperasReintento =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromHours(1),
    ];

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


    public CorreoSaliente(
        string plantilla,
        string destinatario,
        string datos,
        string asunto,
        DateTimeOffset proximoIntentoEn)
    {
        Id = Guid.NewGuid();
        Estado = EstadoCorreo.Pendiente;
        Plantilla = plantilla;
        Destinatario = destinatario;
        Datos = datos;
        Asunto = asunto;
        ProximoIntentoEn = proximoIntentoEn;
    }

    protected CorreoSaliente() { }

    public void MarcarEnviado(DateTimeOffset enviadoEn)
    {
        Estado = EstadoCorreo.Enviado;
        EnviadoEn = enviadoEn;
        ProximoIntentoEn = null;
        UltimoError = null;
    }

    public void RegistrarFallo(string error, DateTimeOffset ahora)
    {
        if (Estado != EstadoCorreo.Pendiente)
        {
            throw new InvalidOperationException("Solo un correo pendiente puede registrar un fallo.");
        }

        Intentos++;
        UltimoError = error;
        ProximoIntentoEn = ahora + EsperasReintento[Math.Min(Intentos - 1, EsperasReintento.Length - 1)];

        if (Intentos >= EsperasReintento.Length)
        {
            Estado = EstadoCorreo.Fallido;
        }
    }
}
