namespace Shapi.Dominio.Suscripciones;

public abstract class Suscripcion
{
    public Guid Id { get; protected set; }
    public Guid PlanId { get; protected set; }
    public EstadoSuscripcion Estado { get; protected set; }
    public DateTimeOffset Inicio { get; protected set; }
    public DateTimeOffset Fin { get; protected set; }
    public DateTimeOffset? GraciaHasta { get; protected set; }
    public Guid? PlanSiguienteId { get; protected set; }
    public Guid? MedioPagoId { get; protected set; }
    public DateTimeOffset CreadoEn { get; protected set; }
    public DateTimeOffset ActualizadoEn { get; protected set; }

    protected Suscripcion() { }
}
