using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Apis;

public class Api : IPerteneceAOrganizacion
{
    public const string PatronSubdominio = "^[a-z0-9][a-z0-9-]{1,28}[a-z0-9]$";
    public const string PatronColorPortal = "^#[0-9A-Fa-f]{6}$";
    public const int MaximoBienvenida = 280;
    public const int MaximoBytesLogo = 512 * 1024;

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Subdominio { get; private set; } = null!;
    public string UrlOrigen { get; private set; } = null!;
    public EstadoApi Estado { get; private set; }
    public string? Especificacion { get; private set; }
    public EspecificacionFormato? EspecificacionFormato { get; private set; }
    public string? EspecificacionTitulo { get; private set; }
    public string? EspecificacionDescripcion { get; private set; }
    public string? EspecificacionVersion { get; private set; }
    public DateTimeOffset? EspecificacionCargadaEn { get; private set; }
    public string? PortalNombre { get; private set; }
    public string PortalColor { get; private set; } = "#3B6FF0";
    public byte[]? PortalLogo { get; private set; }
    public LogoTipo? PortalLogoTipo { get; private set; }
    public string? PortalBienvenida { get; private set; }
    public string SecretoOrigenCifrado { get; private set; } = null!;
    public DateTimeOffset? PublicadaEn { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Api() { }

    /// <summary>Crea una API en borrador después de validar el origen y el subdominio (RF-08).</summary>
    public Api(
        Guid organizacionId,
        string nombre,
        string subdominio,
        string urlOrigen,
        string secretoOrigenCifrado,
        DateTimeOffset ahora)
    {
        if (organizacionId == Guid.Empty)
        {
            throw new ArgumentException("La organización es obligatoria.", nameof(organizacionId));
        }
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                subdominio,
                PatronSubdominio))
        {
            throw new ArgumentException("El subdominio no tiene el formato válido.", nameof(subdominio));
        }
        if (SubdominiosReservados.Contiene(subdominio))
        {
            throw new ArgumentException("El subdominio está reservado.", nameof(subdominio));
        }
        if (string.IsNullOrWhiteSpace(urlOrigen))
        {
            throw new ArgumentException("La URL de origen es obligatoria.", nameof(urlOrigen));
        }
        if (string.IsNullOrWhiteSpace(secretoOrigenCifrado))
        {
            throw new ArgumentException("El secreto cifrado es obligatorio.", nameof(secretoOrigenCifrado));
        }

        Id = Guid.CreateVersion7();
        OrganizacionId = organizacionId;
        Nombre = nombre;
        Subdominio = subdominio;
        UrlOrigen = urlOrigen;
        Estado = EstadoApi.Borrador;
        SecretoOrigenCifrado = secretoOrigenCifrado;
        CreadoEn = ahora;
        ActualizadoEn = ahora;
    }

    /// <summary>Guarda el archivo OpenAPI validado y sus datos de información (RF-09).</summary>
    public void CargarEspecificacion(
        string contenido,
        EspecificacionFormato formato,
        string titulo,
        string? descripcion,
        string version,
        DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contenido);
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        Especificacion = contenido;
        EspecificacionFormato = formato;
        EspecificacionTitulo = titulo;
        EspecificacionDescripcion = descripcion;
        EspecificacionVersion = version;
        EspecificacionCargadaEn = ahora;
        ActualizadoEn = ahora;
    }

    public bool Publicar(DateTimeOffset ahora)
    {
        if (Estado == EstadoApi.Publicada)
        {
            return false;
        }

        Estado = EstadoApi.Publicada;
        PublicadaEn = ahora;
        ActualizadoEn = ahora;
        return true;
    }

    public bool Despublicar(DateTimeOffset ahora)
    {
        if (Estado != EstadoApi.Publicada)
        {
            return false;
        }

        Estado = EstadoApi.Despublicada;
        ActualizadoEn = ahora;
        return true;
    }

    public void RegenerarSecretoOrigen(string secretoOrigenCifrado, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretoOrigenCifrado);
        SecretoOrigenCifrado = secretoOrigenCifrado;
        ActualizadoEn = ahora;
    }

    public void ActualizarPortal(string? nombre, string color, string? bienvenida, DateTimeOffset ahora)
    {
        if (string.IsNullOrWhiteSpace(color) || !System.Text.RegularExpressions.Regex.IsMatch(color, PatronColorPortal))
        {
            throw new ArgumentException("El color principal debe tener el formato #RRGGBB.", nameof(color));
        }

        var nombreNormalizado = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
        var bienvenidaNormalizada = string.IsNullOrWhiteSpace(bienvenida) ? null : bienvenida.Trim();
        if (bienvenidaNormalizada is not null && bienvenidaNormalizada.Length > MaximoBienvenida)
        {
            throw new ArgumentException("El texto de bienvenida no puede superar 280 caracteres.", nameof(bienvenida));
        }

        PortalNombre = nombreNormalizado;
        PortalColor = color.ToUpperInvariant();
        PortalBienvenida = bienvenidaNormalizada;
        ActualizadoEn = ahora;
    }

    public void ActualizarLogoPortal(byte[] logo, LogoTipo tipo, DateTimeOffset ahora)
    {
        if (logo is null || logo.Length == 0 || logo.Length > MaximoBytesLogo)
        {
            throw new ArgumentException("El logotipo no puede estar vacío ni superar 512 KB.", nameof(logo));
        }

        PortalLogo = logo;
        PortalLogoTipo = tipo;
        ActualizadoEn = ahora;
    }
}
