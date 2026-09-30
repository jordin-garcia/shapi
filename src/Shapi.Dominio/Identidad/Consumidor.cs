using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Identidad;

public class Consumidor : IPerteneceAOrganizacion
{
    public const int IntentosAntesDeBloquear = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);
    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string NombreEmpresa { get; private set; } = null!;
    public string Correo { get; private set; } = null!;
    public string HashContrasena { get; private set; } = null!;
    public DateTimeOffset? CorreoVerificadoEn { get; private set; }
    public EstadoCuenta Estado { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Consumidor() { }

    public Consumidor(Guid organizacionId, string nombre, string nombreEmpresa, string correo, string hashContrasena)
    {
        Id = Guid.CreateVersion7();
        OrganizacionId = organizacionId;
        Nombre = nombre.Trim();
        NombreEmpresa = nombreEmpresa.Trim();
        Correo = Usuario.NormalizarCorreo(correo);
        HashContrasena = hashContrasena;
        Estado = EstadoCuenta.Activo;
    }

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta > ahora;
    public bool EstaActivo => Estado == EstadoCuenta.Activo;
    public void VerificarCorreo(DateTimeOffset ahora) => CorreoVerificadoEn ??= ahora;
    public void DefinirHashContrasena(string hash) => HashContrasena = hash;
    public void RegistrarInicioExitoso() { IntentosFallidos = 0; BloqueadoHasta = null; }
}
