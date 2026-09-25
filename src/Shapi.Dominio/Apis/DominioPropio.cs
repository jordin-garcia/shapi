namespace Shapi.Dominio.Apis;

public class DominioPropio
{
    public Guid Id { get; private set; }
    public Guid ApiId { get; private set; }
    public string Dominio { get; private set; } = null!;
    public string DestinoCname { get; private set; } = null!;
    public EstadoDominio Estado { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset? VerificadoEn { get; private set; }
    public DateTimeOffset? UltimoIntentoEn { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected DominioPropio() { }
}
