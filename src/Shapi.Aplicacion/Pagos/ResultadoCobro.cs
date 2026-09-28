namespace Shapi.Aplicacion.Pagos;

public class ResultadoCobro
{
    public bool Exitoso { get; set; }
    public string? Referencia { get; set; }
    public string? Error { get; set; }
}
