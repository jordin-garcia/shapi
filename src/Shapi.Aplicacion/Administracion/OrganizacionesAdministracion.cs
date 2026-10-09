using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Aplicacion.Administracion;

public sealed record OrganizacionAdministracion(
    Guid Id,
    string Nombre,
    string PropietarioCorreo,
    int NumeroApis,
    int NumeroApisPublicadas,
    int NumeroConsumidores,
    string Plan,
    DateTimeOffset CicloInicio,
    DateTimeOffset CicloFin,
    string Estado,
    bool SuspendidaAdministrativamente,
    string? MotivoSuspension);

public sealed record SolicitudSuspenderOrganizacion(string Motivo);

public sealed record PropietarioOrganizacion(string Nombre, string Correo);

public interface ITransaccionAdministracion : IAsyncDisposable
{
    Task Confirmar(CancellationToken cancelacion = default);
}

public interface IRepositorioOrganizacionesAdministracion
{
    Task<IReadOnlyList<OrganizacionAdministracion>> Listar(CancellationToken cancelacion = default);
    Task<Organizacion?> ObtenerProveedor(Guid id, CancellationToken cancelacion = default);
    Task<PropietarioOrganizacion?> ObtenerPropietario(Guid organizacionId, CancellationToken cancelacion = default);
    Task<ITransaccionAdministracion> IniciarTransaccion(CancellationToken cancelacion = default);
    Task Guardar(CancellationToken cancelacion = default);
}

public interface IEnlacesAdministracion
{
    string Soporte { get; }
}

public sealed class GestionarOrganizaciones(
    IRepositorioOrganizacionesAdministracion repositorio,
    IBitacora bitacora,
    IColaCorreo colaCorreo,
    IPublicadorCache publicadorCache,
    IEnlacesAdministracion enlaces)
{
    public Task<IReadOnlyList<OrganizacionAdministracion>> Listar(CancellationToken cancelacion) =>
        repositorio.Listar(cancelacion);

    public async Task<ResultadoCambioOrganizacion> Suspender(
        Guid id,
        string? motivo,
        Guid actorId,
        string actorNombre,
        string? ip,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return ResultadoCambioOrganizacion.MotivoInvalido;
        }

        var organizacion = await repositorio.ObtenerProveedor(id, cancelacion);
        if (organizacion is null)
        {
            return ResultadoCambioOrganizacion.NoEncontrada;
        }
        var propietario = await repositorio.ObtenerPropietario(id, cancelacion);

        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        organizacion.Suspender(motivo);
        await repositorio.Guardar(cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario,
            actorId,
            actorNombre,
            id,
            AccionesBitacora.OrganizacionSuspendida,
            $"Suspendió la organización {organizacion.Nombre}")
        {
            ObjetivoTipo = "organizacion",
            ObjetivoId = id,
            Detalle = new { motivo = organizacion.MotivoSuspension },
            Ip = ip,
        }, cancelacion);
        if (propietario is not null)
        {
            await colaCorreo.Encolar("organizacion_suspendida", propietario.Correo, new
            {
                nombre = propietario.Nombre,
                nombreOrganizacion = organizacion.Nombre,
                motivo = organizacion.MotivoSuspension,
                enlace = enlaces.Soporte,
            }, cancelacion);
        }
        await transaccion.Confirmar(cancelacion);
        await publicadorCache.PublicarOrganizacion(id, cancelacion);
        return ResultadoCambioOrganizacion.Exito;
    }

    public async Task<ResultadoCambioOrganizacion> Reactivar(
        Guid id,
        Guid actorId,
        string actorNombre,
        string? ip,
        CancellationToken cancelacion)
    {
        var organizacion = await repositorio.ObtenerProveedor(id, cancelacion);
        if (organizacion is null)
        {
            return ResultadoCambioOrganizacion.NoEncontrada;
        }

        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        organizacion.Reactivar();
        await repositorio.Guardar(cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario,
            actorId,
            actorNombre,
            id,
            AccionesBitacora.OrganizacionReactivada,
            $"Reactivó la organización {organizacion.Nombre}")
        {
            ObjetivoTipo = "organizacion",
            ObjetivoId = id,
            Ip = ip,
        }, cancelacion);
        await transaccion.Confirmar(cancelacion);
        await publicadorCache.PublicarOrganizacion(id, cancelacion);
        return ResultadoCambioOrganizacion.Exito;
    }
}

public enum ResultadoCambioOrganizacion
{
    Exito,
    NoEncontrada,
    MotivoInvalido,
}
