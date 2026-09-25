namespace Shapi.Dominio.Identidad;

public class Token
{
    public Guid Id { get; set; }
    public TipoToken Tipo { get; set; }
    public string HashToken { get; set; } = null!;
    public Guid? UsuarioId { get; set; }
    public Guid? ConsumidorId { get; set; }
    public Guid? OrganizacionId { get; set; }
    public string Correo { get; set; } = null!;
    public string? Rol { get; set; }
    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset? UsadoEn { get; set; }

    public Token() { }
}
