namespace Shapi.Dominio.Consumo;

public class ConsumoDiario
{
    public long Id { get; private set; }
    public DateOnly Fecha { get; private set; }
    public Guid ApiId { get; private set; }
    public Guid? RutaId { get; private set; }
    public Guid? SuscripcionId { get; private set; }
    public EntornoConsumo Entorno { get; private set; }
    public long Peticiones { get; private set; }
    public long Llamadas { get; private set; }
    public long BytesEntrada { get; private set; }
    public long BytesSalida { get; private set; }
    public long Rechazos401 { get; private set; }
    public long Rechazos403 { get; private set; }
    public long Rechazos404 { get; private set; }
    public long Rechazos429 { get; private set; }
    public long Origen2xx { get; private set; }
    public long Origen3xx { get; private set; }
    public long Origen4xx { get; private set; }
    public long Origen5xx { get; private set; }
    public long OrigenFallo { get; private set; }
    public int[] HistLatenciaTotal { get; private set; } = new int[10];
    public int[] HistLatenciaCompuerta { get; private set; } = new int[10];
    public long LatenciaTotalSumaMs { get; private set; }
    public long LatenciaCompuertaSumaMs { get; private set; }

    protected ConsumoDiario() { }
}
