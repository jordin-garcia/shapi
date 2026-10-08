using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Soporte;

public class Caso : IPerteneceAOrganizacion
{
    private readonly List<CasoMensaje> _mensajes = [];

    public Guid Id { get; private set; }
    public int Numero { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Guid? ApiId { get; private set; }
    public Guid CreadoPor { get; private set; }
    public Guid? AsignadoA { get; private set; }
    public string Asunto { get; private set; } = null!;
    public EstadoCaso Estado { get; private set; }
    public DateTimeOffset? CerradoEn { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }
    public IReadOnlyCollection<CasoMensaje> ObtenerMensajes() => _mensajes;

    protected Caso() { }

    public static Caso Abrir(
        Guid organizacionId,
        Guid? apiId,
        Guid creadoPor,
        string asunto,
        string descripcion,
        DateTimeOffset ahora)
    {
        if (organizacionId == Guid.Empty)
        {
            throw new ArgumentException("La organizacion es obligatoria.", nameof(organizacionId));
        }

        if (creadoPor == Guid.Empty)
        {
            throw new ArgumentException("El autor es obligatorio.", nameof(creadoPor));
        }

        if (string.IsNullOrWhiteSpace(asunto))
        {
            throw new ArgumentException("El asunto es obligatorio.", nameof(asunto));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripcion es obligatoria.", nameof(descripcion));
        }

        var caso = new Caso
        {
            Id = Guid.CreateVersion7(),
            OrganizacionId = organizacionId,
            ApiId = apiId,
            CreadoPor = creadoPor,
            Asunto = asunto.Trim(),
            Estado = EstadoCaso.Abierto,
            CreadoEn = ahora,
            ActualizadoEn = ahora,
        };
        caso._mensajes.Add(new CasoMensaje(caso.Id, creadoPor, descripcion.Trim(), ahora));
        return caso;
    }

    public CasoMensaje Responder(Guid autorId, string cuerpo, DateTimeOffset ahora)
    {
        if (Estado == EstadoCaso.Cerrado)
        {
            throw new InvalidOperationException("caso_cerrado");
        }

        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            throw new ArgumentException("El mensaje es obligatorio.", nameof(cuerpo));
        }

        var mensaje = new CasoMensaje(Id, autorId, cuerpo.Trim(), ahora);
        _mensajes.Add(mensaje);
        ActualizadoEn = ahora;
        return mensaje;
    }

    public void Asignar(Guid usuarioId, DateTimeOffset ahora)
    {
        AsignadoA = usuarioId;
        ActualizadoEn = ahora;
    }

    public void Cerrar(DateTimeOffset ahora)
    {
        if (Estado == EstadoCaso.Cerrado)
        {
            return;
        }

        Estado = EstadoCaso.Cerrado;
        CerradoEn = ahora;
        ActualizadoEn = ahora;
    }
}
