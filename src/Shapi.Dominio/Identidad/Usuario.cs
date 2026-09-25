namespace Shapi.Dominio.Identidad;

public class Usuario
{
    /// <summary>Intentos fallidos seguidos que bloquean la cuenta (10 §1).</summary>
    public const int IntentosAntesDeBloquear = 5;

    /// <summary>Duración del bloqueo tras los intentos fallidos (10 §1).</summary>
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Correo { get; private set; } = null!;
    public string? HashContrasena { get; private set; }
    public DateTimeOffset? CorreoVerificadoEn { get; private set; }
    public EstadoCuenta Estado { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    protected Usuario() { }

    public Usuario(string nombre, string correo)
    {
        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        Correo = NormalizarCorreo(correo);
        Estado = EstadoCuenta.Activo;
    }

    /// <summary>Los correos se comparan y se guardan en minúsculas y sin espacios (07 §3.1: UNIQUE lower(correo)).</summary>
    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    public void DefinirHashContrasena(string hash) => HashContrasena = hash;

    public void VerificarCorreo(DateTimeOffset ahora) => CorreoVerificadoEn ??= ahora;

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta > ahora;

    /// <summary>Cuenta un intento fallido. Al quinto seguido, bloquea la cuenta 15 minutos y reinicia el contador.</summary>
    public void RegistrarIntentoFallido(DateTimeOffset ahora)
    {
        IntentosFallidos++;
        if (IntentosFallidos >= IntentosAntesDeBloquear)
        {
            BloqueadoHasta = ahora + DuracionBloqueo;
            IntentosFallidos = 0;
        }
    }

    public void RegistrarInicioExitoso()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }
}
