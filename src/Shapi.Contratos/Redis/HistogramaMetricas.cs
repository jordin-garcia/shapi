namespace Shapi.Contratos.Redis;

/// <summary>Los diez rangos de 07 §3.5 y 08 §7, compartidos por medición y consultas.</summary>
public static class HistogramaMetricas
{
    public static IReadOnlyList<int> Limites { get; } = [5, 10, 25, 50, 100, 250, 500, 1000, 2500];

    public static int Rango(TimeSpan latencia)
    {
        for (var i = 0; i < Limites.Count; i++)
        {
            if (latencia.TotalMilliseconds <= Limites[i])
            {
                return i;
            }
        }
        return 9;
    }
}
