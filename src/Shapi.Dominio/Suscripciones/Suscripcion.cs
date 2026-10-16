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

    /// <summary>Aplica el plan programado al cerrar el ciclo (09 §5).</summary>
    public void AplicarPlanProgramado(Guid planId, DateTimeOffset ahora)
    {
        if (PlanSiguienteId != planId)
        {
            throw new InvalidOperationException("El plan no coincide con el cambio programado.");
        }

        PlanId = planId;
        PlanSiguienteId = null;
        ActualizadoEn = ahora;
    }

    /// <summary>Abre un ciclo autorizado desde el fin del ciclo anterior (09 §4).</summary>
    public void Renovar(int vigenciaDias, DateTimeOffset ahora)
    {
        if (vigenciaDias <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vigenciaDias));
        }

        Inicio = Fin;
        Fin = Inicio.AddDays(vigenciaDias);
        GraciaHasta = null;
        Estado = EstadoSuscripcion.Activa;
        ActualizadoEn = ahora;
    }

    /// <summary>Abre siete días de gracia desde el fin del ciclo que venció (09 §3).</summary>
    public void EntrarEnGracia(TimeSpan duracion, DateTimeOffset ahora)
    {
        if (duracion <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duracion));
        }

        Estado = EstadoSuscripcion.EnGracia;
        GraciaHasta = Fin + duracion;
        ActualizadoEn = ahora;
    }

    /// <summary>Fija el fin de gracia indicado por una regla externa, como el reembolso de un ciclo vigente (09 §7).</summary>
    public void EntrarEnGraciaHasta(DateTimeOffset graciaHasta, DateTimeOffset ahora)
    {
        if (graciaHasta <= ahora)
        {
            throw new ArgumentOutOfRangeException(nameof(graciaHasta));
        }

        Estado = EstadoSuscripcion.EnGracia;
        GraciaHasta = graciaHasta;
        ActualizadoEn = ahora;
    }

    /// <summary>Suspende al terminar el periodo de gracia (09 §3).</summary>
    public void Suspender(DateTimeOffset ahora)
    {
        Estado = EstadoSuscripcion.Suspendida;
        ActualizadoEn = ahora;
    }

    public void ProgramarCambio(Guid planId, DateTimeOffset ahora)
    {
        PlanSiguienteId = planId;
        ActualizadoEn = ahora;
    }

    public void CancelarCambio(DateTimeOffset ahora)
    {
        PlanSiguienteId = null;
        ActualizadoEn = ahora;
    }

    public void Finalizar(DateTimeOffset ahora)
    {
        Estado = EstadoSuscripcion.Finalizada;
        PlanSiguienteId = null;
        ActualizadoEn = ahora;
    }

    public void CambiarPlan(Guid planId, Guid? medioPagoId, DateTimeOffset? inicioNuevoCiclo, int vigenciaDias, DateTimeOffset ahora)
    {
        if (vigenciaDias <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vigenciaDias));
        }

        PlanId = planId;
        MedioPagoId = medioPagoId;
        PlanSiguienteId = null;
        if (inicioNuevoCiclo is not null)
        {
            Inicio = inicioNuevoCiclo.Value;
            Fin = Inicio.AddDays(vigenciaDias);
        }

        Estado = EstadoSuscripcion.Activa;
        GraciaHasta = null;
        ActualizadoEn = ahora;
    }

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
