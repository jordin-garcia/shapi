namespace Shapi.Dominio.Pagos;

public class Pago
{
    public Guid Id { get; private set; }
    public Guid? SuscripcionPlataformaId { get; private set; }
    public Guid? SuscripcionApiId { get; private set; }
    public Guid? MedioPagoId { get; private set; }
    /// <summary>Consumidor y API de un intento rechazado que todavía no tiene suscripción.</summary>
    public Guid? ConsumidorId { get; private set; }
    public Guid? ApiId { get; private set; }
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

    public static Pago ContratacionAutorizada(Guid suscripcionId, Guid medioPagoId, decimal monto, string descripcion,
        string referencia, DateTimeOffset inicio, DateTimeOffset fin) => new()
        {
            Id = Guid.CreateVersion7(),
            SuscripcionApiId = suscripcionId,
            MedioPagoId = medioPagoId,
            Concepto = ConceptoPago.Contratacion,
            Descripcion = descripcion,
            Monto = monto,
            Estado = EstadoPago.Autorizado,
            ReferenciaPasarela = referencia,
            PeriodoInicio = inicio,
            PeriodoFin = fin,
        };

    public static Pago ContratacionRechazada(Guid consumidorId, Guid apiId, decimal monto, string descripcion, string motivo) => new()
    {
        Id = Guid.CreateVersion7(),
        ConsumidorId = consumidorId,
        ApiId = apiId,
        Concepto = ConceptoPago.Contratacion,
        Descripcion = descripcion,
        Monto = monto,
        Estado = EstadoPago.Rechazado,
        MotivoRechazo = motivo,
    };
}
