namespace Shapi.Dominio.Soporte;

public class CasoMensaje
{
    public Guid Id { get; private set; }
    public Guid CasoId { get; private set; }
    public Guid AutorId { get; private set; }
    public string Cuerpo { get; private set; } = null!;
    public DateTimeOffset CreadoEn { get; private set; }

    protected CasoMensaje() { }
}
