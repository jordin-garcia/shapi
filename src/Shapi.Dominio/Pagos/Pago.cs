namespace Shapi.Dominio.Pagos;

public class Pago
{
    public Guid Id { get; set; }
    public Guid? SuscripcionPlataformaId { get; set; }
    public Guid? SuscripcionApiId { get; set; }
    public Guid? MedioPagoId { get; set; }
    public ConceptoPago Concepto { get; set; }
    public string Descripcion { get; set; } = null!;
    public decimal Monto { get; set; }
    public EstadoPago Estado { get; set; }
    public string? ReferenciaPasarela { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTimeOffset? PeriodoInicio { get; set; }
    public DateTimeOffset? PeriodoFin { get; set; }
    public DateTimeOffset? RevertidoEn { get; set; }
    public Guid? RevertidoPor { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Pago() { }
}
