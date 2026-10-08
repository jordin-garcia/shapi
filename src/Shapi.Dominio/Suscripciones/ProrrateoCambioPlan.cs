namespace Shapi.Dominio.Suscripciones;

/// <summary>Calcula el crédito y el cobro al subir de plan (09 §5).</summary>
public static class ProrrateoCambioPlan
{
    public static ResultadoProrrateo Calcular(decimal precioActual, int vigenciaActual, decimal precioNuevo,
        int vigenciaNueva, int diasRestantes)
    {
        if (vigenciaActual <= 0 || vigenciaNueva <= 0 || diasRestantes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(diasRestantes));
        }

        var credito = decimal.Round(precioActual * diasRestantes / vigenciaActual, 2, MidpointRounding.AwayFromZero);
        var cargo = vigenciaNueva == vigenciaActual
            ? decimal.Round(precioNuevo * diasRestantes / vigenciaNueva, 2, MidpointRounding.AwayFromZero)
            : precioNuevo;
        return new ResultadoProrrateo(credito, cargo, Math.Max(cargo - credito, 0));
    }
}

public sealed record ResultadoProrrateo(decimal Credito, decimal Cargo, decimal APagar);
