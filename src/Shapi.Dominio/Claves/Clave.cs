namespace Shapi.Dominio.Claves;

public class Clave
{
    public Guid Id { get; set; }
    public Guid SuscripcionId { get; set; }
    public TipoClave Tipo { get; set; }
    public string Prefijo { get; set; } = null!;
    public string Ultimos4 { get; set; } = null!;
    public string HashSha256 { get; set; } = null!;
    public EstadoClave Estado { get; set; }
    public DateTimeOffset? ExpiraEn { get; set; }
    public DateTimeOffset? RevocadaEn { get; set; }
    public RevocadaPor? RevocadaPor { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Clave() { }
}
