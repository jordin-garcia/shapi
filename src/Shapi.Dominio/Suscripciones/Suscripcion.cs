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

    /// <summary>
    /// Inicio de un ciclo (09 §4): el momento de la contratación redondeado al inicio del día en America/Guatemala.
    /// El fin es <c>inicio + vigencia_dias</c>.
    /// </summary>
    public static DateTimeOffset InicioDeCiclo(DateTimeOffset momento)
    {
        var local = TimeZoneInfo.ConvertTime(momento, ZonaGuatemala);
        return new DateTimeOffset(local.Date, local.Offset).ToUniversalTime();
    }

    // Guatemala no tiene horario de verano (UTC−6). Si el sistema no trae la base de zonas, se usa ese desfase fijo.
    private static readonly TimeZoneInfo ZonaGuatemala = BuscarZonaGuatemala();

    private static TimeZoneInfo BuscarZonaGuatemala()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("America/Guatemala", TimeSpan.FromHours(-6), "America/Guatemala", "America/Guatemala");
        }
    }
}
