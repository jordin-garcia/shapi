namespace Shapi.Dominio.Pagos;

public class Pago
{
    public Guid Id { get; private set; }
    public Guid? SuscripcionPlataformaId { get; private set; }
    public Guid? SuscripcionApiId { get; private set; }
    public Guid? MedioPagoId { get; private set; }
    public ConceptoPago Concepto { get; private set; }
    public string Descripcion { get; private set; } = null!;
    public decimal Monto { get; private set; }
    public EstadoPago Estado { get; private set; }
    public string? ReferenciaPasarela { get; private set; }
    public string? MotivoRechazo { get; private set; }
    public DateTimeOffset? PeriodoInicio { get; private set; }
    public DateTimeOffset? PeriodoFin { get; private set; }
    public DateTimeOffset? RevertidoEn { get; private set; }
    public Guid? RevertidoPor { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Pago() { }
}
