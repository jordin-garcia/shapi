namespace Shapi.Dominio.Apis;

public class DominioPropio
{
    public Guid Id { get; set; }
    public Guid ApiId { get; set; }
    public string Dominio { get; set; } = null!;
    public string DestinoCname { get; set; } = null!;
    public EstadoDominio Estado { get; set; }
    public string? Motivo { get; set; }
    public DateTimeOffset? VerificadoEn { get; set; }
    public DateTimeOffset? UltimoIntentoEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public DominioPropio() { }
}
