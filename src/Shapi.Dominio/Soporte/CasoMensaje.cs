namespace Shapi.Dominio.Soporte;

public class CasoMensaje
{
    public Guid Id { get; set; }
    public Guid CasoId { get; set; }
    public Guid AutorId { get; set; }
    public string Cuerpo { get; set; } = null!;
    public DateTimeOffset CreadoEn { get; set; }

    public CasoMensaje() { }
}
