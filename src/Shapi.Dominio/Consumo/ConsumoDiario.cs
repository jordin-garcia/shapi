namespace Shapi.Dominio.Consumo;

public class ConsumoDiario
{
    public long Id { get; set; }
    public DateOnly Fecha { get; set; }
    public Guid ApiId { get; set; }
    public Guid? RutaId { get; set; }
    public Guid? SuscripcionId { get; set; }
    public EntornoConsumo Entorno { get; set; }
    public long Peticiones { get; set; }
    public long Llamadas { get; set; }
    public long BytesEntrada { get; set; }
    public long BytesSalida { get; set; }
    public long Rechazos401 { get; set; }
    public long Rechazos403 { get; set; }
    public long Rechazos404 { get; set; }
    public long Rechazos429 { get; set; }
    public long Origen2xx { get; set; }
    public long Origen3xx { get; set; }
    public long Origen4xx { get; set; }
    public long Origen5xx { get; set; }
    public long OrigenFallo { get; set; }
    public int[] HistLatenciaTotal { get; set; } = new int[10];
    public int[] HistLatenciaCompuerta { get; set; } = new int[10];
    public long LatenciaTotalSumaMs { get; set; }
    public long LatenciaCompuertaSumaMs { get; set; }

    public ConsumoDiario() { }
}
