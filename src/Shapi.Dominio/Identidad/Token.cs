namespace Shapi.Dominio.Identidad;

public class Token
{
    /// <summary>Vigencia del enlace de verificación de correo (10 §1).</summary>
    public static readonly TimeSpan VigenciaVerificacionCorreo = TimeSpan.FromHours(24);

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

    /// <summary>Enlace de verificación del correo de un usuario del personal. Solo se guarda el hash del valor del enlace.</summary>
    public static Token VerificacionCorreo(string hashToken, Usuario usuario, DateTimeOffset ahora) => new()
    {
        Id = Guid.NewGuid(),
        Tipo = TipoToken.VerificacionCorreo,
        HashToken = hashToken,
        UsuarioId = usuario.Id,
        Correo = usuario.Correo,
        ExpiraEn = ahora + VigenciaVerificacionCorreo,
    };

    public bool EsValido(DateTimeOffset ahora) => UsadoEn is null && ExpiraEn > ahora;
}
