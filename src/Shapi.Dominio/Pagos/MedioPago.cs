namespace Shapi.Dominio.Pagos;

public class MedioPago
{
    public Guid Id { get; private set; }
    public Guid? OrganizacionId { get; private set; }
    public Guid? ConsumidorId { get; private set; }
    public string TokenPasarela { get; private set; } = null!;
    public MarcaTarjeta Marca { get; private set; }
    public string Ultimos4 { get; private set; } = null!;
    public string Titular { get; private set; } = null!;
    public short MesVencimiento { get; private set; }
    public short AnioVencimiento { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    protected MedioPago() { }
}
