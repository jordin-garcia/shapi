namespace Shapi.Dominio.Suscripciones;

public abstract class Suscripcion
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public EstadoSuscripcion Estado { get; set; }
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }
    public DateTimeOffset? GraciaHasta { get; set; }
    public Guid? PlanSiguienteId { get; set; }
    public Guid? MedioPagoId { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Suscripcion() { }
}
