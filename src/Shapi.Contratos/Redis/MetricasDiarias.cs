using System.Globalization;

namespace Shapi.Contratos.Redis;

/// <summary>Identidad y contadores de una instantánea de métricas; no contiene claves ni datos del consumidor.</summary>
public sealed record MetricasDiarias(
    DateOnly Fecha, Guid ApiId, Guid? RutaId, Guid? SuscripcionId, string Entorno,
    IReadOnlyDictionary<string, long> Contadores)
{
    public static IReadOnlyDictionary<string, string> Columnas { get; } = new Dictionary<string, string>
    {
        ["peticiones"] = "peticiones",
        ["llamadas"] = "llamadas",
        ["bytes_entrada"] = "bytes_entrada",
        ["bytes_salida"] = "bytes_salida",
        ["r401"] = "rechazos_401",
        ["r403"] = "rechazos_403",
        ["r404"] = "rechazos_404",
        ["r429"] = "rechazos_429",
        ["o2xx"] = "origen_2xx",
        ["o3xx"] = "origen_3xx",
        ["o4xx"] = "origen_4xx",
        ["o5xx"] = "origen_5xx",
        ["ofallo"] = "origen_fallo",
        ["lt_suma"] = "latencia_total_suma_ms",
        ["lc_suma"] = "latencia_compuerta_suma_ms",
    };

    public long Contador(string campo) => Contadores.GetValueOrDefault(campo);

    public int[] Histograma(string prefijo) => [.. Enumerable.Range(0, 10).Select(i => checked((int)Contador($"{prefijo}{i}")))];

    public static MetricasDiarias DesdeLlave(string llave, IReadOnlyDictionary<string, long> contadores)
    {
        var partes = llave.Split(':');
        if (partes.Length != 6 || partes[0] != "met" || partes[5] is not ("produccion" or "pruebas"))
        {
            throw new FormatException("Llave de métricas inválida.");
        }
        return new MetricasDiarias(DateOnly.ParseExact(partes[1], "yyyyMMdd", CultureInfo.InvariantCulture),
            Guid.Parse(partes[2]), IdONulo(partes[3]), IdONulo(partes[4]), partes[5], contadores);
    }

    private static Guid? IdONulo(string texto) => texto == "-" ? null : Guid.Parse(texto);
}
