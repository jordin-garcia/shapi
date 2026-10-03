namespace Shapi.Compuerta.Tests.Soporte;

/// <summary>
/// Un reloj que marca la hora que diga la prueba, para que el minuto de los límites (08 §3) no cambie en medio de una
/// prueba. Solo cambia <see cref="GetUtcNow"/>: las marcas de tiempo (<c>CacheRutas</c>) siguen siendo las del sistema.
/// </summary>
public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;

    public override DateTimeOffset GetUtcNow() => Ahora;
}
