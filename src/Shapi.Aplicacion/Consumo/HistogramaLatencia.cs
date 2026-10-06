using Shapi.Contratos.Redis;

namespace Shapi.Aplicacion.Consumo;

/// <summary>Sin peticiones no hay percentil; el rango abierto se presenta como «&gt; 2500 ms».</summary>
public sealed record Percentil95(decimal? Milisegundos, bool Supera2500 = false);

public static class HistogramaLatencia
{
    public static Percentil95 CalcularP95(IEnumerable<IReadOnlyList<int>> histogramas)
    {
        var sumas = new long[10];
        foreach (var histograma in histogramas)
        {
            if (histograma.Count != 10 || histograma.Any(x => x < 0))
            {
                throw new ArgumentException("Un histograma necesita diez contadores no negativos.", nameof(histogramas));
            }
            for (var i = 0; i < sumas.Length; i++)
            {
                sumas[i] = checked(sumas[i] + histograma[i]);
            }
        }
        var total = sumas.Sum();
        if (total == 0)
        {
            return new Percentil95(null);
        }
        var objetivo = total * 0.95m;
        long acumulado = 0;
        for (var i = 0; i < sumas.Length; i++)
        {
            if (acumulado + sumas[i] >= objetivo)
            {
                if (i == 9)
                {
                    return new Percentil95(null, true);
                }
                var inferior = i == 0 ? 0 : HistogramaMetricas.Limites[i - 1];
                var superior = HistogramaMetricas.Limites[i];
                return new Percentil95(inferior + (objetivo - acumulado) / sumas[i] * (superior - inferior));
            }
            acumulado += sumas[i];
        }
        throw new InvalidOperationException("El histograma no contiene el percentil calculado.");
    }
}
