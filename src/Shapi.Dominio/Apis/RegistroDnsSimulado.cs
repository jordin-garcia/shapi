namespace Shapi.Dominio.Apis;

public class RegistroDnsSimulado
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string Valor { get; set; } = null!;

    public RegistroDnsSimulado() { }
}
