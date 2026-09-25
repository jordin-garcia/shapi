namespace Shapi.Dominio.Pagos;

public class MedioPago
{
    public Guid Id { get; set; }
    public Guid? OrganizacionId { get; set; }
    public Guid? ConsumidorId { get; set; }
    public string TokenPasarela { get; set; } = null!;
    public MarcaTarjeta Marca { get; set; }
    public string Ultimos4 { get; set; } = null!;
    public string Titular { get; set; } = null!;
    public short MesVencimiento { get; set; }
    public short AnioVencimiento { get; set; }
    public DateTimeOffset CreadoEn { get; set; }

    public MedioPago() { }
}
