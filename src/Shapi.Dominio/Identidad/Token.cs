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
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Token() { }

    /// <summary>Enlace de verificación del correo de un usuario del personal. Solo se guarda el hash del valor del enlace.</summary>
    public static Token VerificacionCorreo(string hashToken, Usuario usuario, DateTimeOffset ahora) => new()
    {
        Id = Guid.CreateVersion7(),
        Tipo = TipoToken.VerificacionCorreo,
        HashToken = hashToken,
        UsuarioId = usuario.Id,
        Correo = usuario.Correo,
        ExpiraEn = ahora + VigenciaVerificacionCorreo,
    };

    /// <summary>Enlace de recuperación de contraseña de un usuario o consumidor. Solo se guarda el hash del valor del enlace.</summary>
    public static Token Recuperacion(string hashToken, Guid? usuarioId, Guid? consumidorId, Guid? organizacionId, string correo, DateTimeOffset ahora) => new()
    {
        Id = Guid.CreateVersion7(),
        Tipo = TipoToken.Recuperacion,
        HashToken = hashToken,
        UsuarioId = usuarioId,
        ConsumidorId = consumidorId,
        OrganizacionId = organizacionId,
        Correo = correo,
        ExpiraEn = ahora + TimeSpan.FromMinutes(60),
    };

    public bool EsValido(DateTimeOffset ahora) => UsadoEn is null && ExpiraEn > ahora;
}
