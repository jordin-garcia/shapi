namespace Shapi.Dominio.Identidad;

public class Token
{
    public Guid Id { get; private set; }
    public TipoToken Tipo { get; private set; }
    public string HashToken { get; private set; } = null!;
    public Guid? UsuarioId { get; private set; }
    public Guid? ConsumidorId { get; private set; }
    public Guid? OrganizacionId { get; private set; }
    public string Correo { get; private set; } = null!;
    public string? Rol { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? UsadoEn { get; private set; }

    protected Token() { }
}
