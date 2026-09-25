namespace Shapi.Dominio.Consumo;

public class LoteConsolidado
{
    public Guid LoteId { get; private set; }
    public DateTimeOffset ProcesadoEn { get; private set; }

    protected LoteConsolidado() { }
}
