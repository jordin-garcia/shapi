namespace Shapi.Aplicacion.Pagos;

public class DatosTarjeta
{
    public string Numero { get; set; } = string.Empty;
    public string MesVencimiento { get; set; } = string.Empty;
    public string AnioVencimiento { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;
}
