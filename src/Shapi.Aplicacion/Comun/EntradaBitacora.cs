namespace Shapi.Aplicacion.Comun;

/// <summary>Una entrada de la tabla <c>bitacora</c> (07 §3.6).</summary>
/// <param name="ActorId">Nulo si el actor es el sistema.</param>
/// <param name="ActorNombre">Copia del nombre, para que se lea aunque la cuenta cambie.</param>
/// <param name="Accion">Una constante de <see cref="AccionesBitacora"/>.</param>
/// <param name="Descripcion">El texto que se ve en B3.2.</param>
public sealed record EntradaBitacora(
    TipoActor ActorTipo,
    Guid? ActorId,
    string ActorNombre,
    Guid? OrganizacionId,
    string Accion,
    string Descripcion)
{
    public string? ObjetivoTipo { get; init; }

    public Guid? ObjetivoId { get; init; }

    /// <summary>Datos adicionales; se guardan como <c>jsonb</c>.</summary>
    public object? Detalle { get; init; }

    public string? Ip { get; init; }
}

/// <summary>Valores de <c>bitacora.actor_tipo</c>.</summary>
public enum TipoActor
{
    Usuario,
    Consumidor,
    Sistema,
}
