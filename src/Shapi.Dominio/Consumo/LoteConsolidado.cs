namespace Shapi.Dominio.Consumo;

public class LoteConsolidado
{
    public Guid LoteId { get; set; }
    public DateTimeOffset ProcesadoEn { get; set; }

    public LoteConsolidado() { }
}
