namespace Shapi.Dominio.Claves;

public class Clave
{
    public Guid Id { get; private set; }
    public Guid SuscripcionId { get; private set; }
    public TipoClave Tipo { get; private set; }
    public string Prefijo { get; private set; } = null!;
    public string Ultimos4 { get; private set; } = null!;
    public string HashSha256 { get; private set; } = null!;
    public EstadoClave Estado { get; private set; }
    public DateTimeOffset? ExpiraEn { get; private set; }
    public DateTimeOffset? RevocadaEn { get; private set; }
    public RevocadaPor? RevocadaPor { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Clave() { }
}
