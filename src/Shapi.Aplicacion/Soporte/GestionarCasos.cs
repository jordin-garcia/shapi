using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Soporte;

namespace Shapi.Aplicacion.Soporte;

public sealed class ListarCasos(IRepositorioSoporte repositorio)
{
    public Task<Pagina<CasoListado>> Proveedor(
        Guid organizacionId, int pagina, int tamano, CancellationToken cancelacion) =>
        repositorio.ListarProveedor(organizacionId, Pagina(pagina), Tamano(tamano), cancelacion);

    public Task<Pagina<CasoListadoAdministracion>> Administracion(
        int pagina, int tamano, CancellationToken cancelacion) =>
        repositorio.ListarAdministracion(Pagina(pagina), Tamano(tamano), cancelacion);

    private static int Pagina(int pagina) => Math.Max(1, pagina);
    private static int Tamano(int tamano) => Math.Clamp(tamano, 1, 100);
}

public sealed class ListarOrganizacionesParaCaso(IRepositorioSoporte repositorio)
{
    public Task<IReadOnlyList<OpcionOrganizacionCaso>> Ejecutar(CancellationToken cancelacion) =>
        repositorio.ListarOrganizaciones(cancelacion);
}

public sealed class AbrirCaso(
    IRepositorioSoporte repositorio,
    IReloj reloj,
    IBitacora bitacora,
    NotificadorCasos notificador)
{
    public async Task<Resultado<CasoDetalle>> Proveedor(
        Guid organizacionId,
        Guid usuarioId,
        string usuarioNombre,
        SolicitudAbrirCaso solicitud,
        string? ip,
        CancellationToken cancelacion)
    {
        var error = await Validar(organizacionId, solicitud.Asunto, solicitud.ApiId, solicitud.Descripcion, cancelacion);
        return error is not null
            ? error
            : await Guardar(organizacionId, usuarioId, usuarioNombre, solicitud.Asunto, solicitud.ApiId,
                solicitud.Descripcion, ip, false, cancelacion);
    }

    public async Task<Resultado<CasoDetalle>> Administracion(
        Guid usuarioId,
        string usuarioNombre,
        SolicitudAbrirCasoAdministracion solicitud,
        string? ip,
        CancellationToken cancelacion)
    {
        if (!await repositorio.ExisteOrganizacionProveedor(solicitud.OrganizacionId, cancelacion))
        {
            return new Error(CodigosError.DatosInvalidos, "Revise los datos del caso.",
                new Dictionary<string, string[]> { ["organizacionId"] = ["La organizacion proveedora no existe."] });
        }

        var error = await Validar(
            solicitud.OrganizacionId, solicitud.Asunto, solicitud.ApiId, solicitud.Descripcion, cancelacion);
        return error is not null
            ? error
            : await Guardar(solicitud.OrganizacionId, usuarioId, usuarioNombre, solicitud.Asunto, solicitud.ApiId,
                solicitud.Descripcion, ip, true, cancelacion);
    }

    private async Task<Resultado<CasoDetalle>> Guardar(
        Guid organizacionId,
        Guid usuarioId,
        string usuarioNombre,
        string asunto,
        Guid? apiId,
        string descripcion,
        string? ip,
        bool personalPlataforma,
        CancellationToken cancelacion)
    {
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        var caso = Caso.Abrir(organizacionId, apiId, usuarioId, asunto, descripcion, reloj.Ahora);
        await repositorio.Agregar(caso, cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario,
            usuarioId,
            usuarioNombre,
            organizacionId,
            AccionesBitacora.CasoAbierto,
            $"Abrio el caso CAS-{caso.Numero}: {caso.Asunto}.")
        {
            ObjetivoTipo = "caso",
            ObjetivoId = caso.Id,
            Ip = ip,
        }, cancelacion);
        await notificador.Notificar(caso, personalPlataforma, cancelacion);
        await transaccion.Confirmar(cancelacion);
        return (await repositorio.Detalle(caso, cancelacion))!;
    }

    private async Task<Error?> Validar(
        Guid organizacionId,
        string? asunto,
        Guid? apiId,
        string? descripcion,
        CancellationToken cancelacion)
    {
        var errores = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(asunto) || asunto.Trim().Length > 120)
        {
            errores["asunto"] = ["El asunto es obligatorio y debe tener maximo 120 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Trim().Length > 4000)
        {
            errores["descripcion"] = ["La descripcion es obligatoria y debe tener maximo 4000 caracteres."];
        }

        if (apiId.HasValue && !await repositorio.ApiPertenece(apiId.Value, organizacionId, cancelacion))
        {
            errores["apiId"] = ["La API no pertenece a la organizacion."];
        }

        return errores.Count == 0
            ? null
            : new Error(CodigosError.DatosInvalidos, "Revise los datos del caso.", errores);
    }
}

public sealed class ConsultarCaso(IRepositorioSoporte repositorio)
{
    public async Task<CasoDetalle?> Proveedor(int numero, Guid organizacionId, CancellationToken cancelacion)
    {
        var caso = await repositorio.ObtenerProveedor(numero, organizacionId, false, cancelacion);
        return caso is null ? null : await repositorio.Detalle(caso, cancelacion);
    }

    public async Task<CasoDetalle?> Administracion(int numero, CancellationToken cancelacion)
    {
        var caso = await repositorio.ObtenerAdministracion(numero, false, cancelacion);
        return caso is null ? null : await repositorio.Detalle(caso, cancelacion);
    }
}

public sealed class ResponderCaso(
    IRepositorioSoporte repositorio,
    IReloj reloj,
    NotificadorCasos notificador)
{
    public Task<Resultado<MensajeCaso?>> Proveedor(
        int numero, Guid organizacionId, Guid usuarioId, SolicitudMensajeCaso solicitud, CancellationToken cancelacion) =>
        Guardar(numero, organizacionId, usuarioId, solicitud, false, cancelacion);

    public Task<Resultado<MensajeCaso?>> Administracion(
        int numero, Guid usuarioId, SolicitudMensajeCaso solicitud, CancellationToken cancelacion) =>
        Guardar(numero, null, usuarioId, solicitud, true, cancelacion);

    private async Task<Resultado<MensajeCaso?>> Guardar(
        int numero,
        Guid? organizacionId,
        Guid usuarioId,
        SolicitudMensajeCaso solicitud,
        bool personalPlataforma,
        CancellationToken cancelacion)
    {
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        var caso = organizacionId.HasValue
            ? await repositorio.ObtenerProveedor(numero, organizacionId.Value, true, cancelacion)
            : await repositorio.ObtenerAdministracion(numero, true, cancelacion);
        if (caso is null)
        {
            return Resultado<MensajeCaso?>.Exito(null);
        }

        if (caso.Estado == EstadoCaso.Cerrado)
        {
            return new Error(CodigosError.CasoCerrado, "El caso esta cerrado.");
        }

        if (string.IsNullOrWhiteSpace(solicitud.Cuerpo) || solicitud.Cuerpo.Trim().Length > 4000)
        {
            return new Error(CodigosError.DatosInvalidos, "Revise el mensaje.",
                new Dictionary<string, string[]> { ["cuerpo"] = ["El mensaje es obligatorio y debe tener maximo 4000 caracteres."] });
        }

        var mensaje = caso.Responder(usuarioId, solicitud.Cuerpo, reloj.Ahora);
        await repositorio.GuardarMensaje(mensaje, cancelacion);
        await notificador.Notificar(caso, personalPlataforma, cancelacion);
        await transaccion.Confirmar(cancelacion);
        return await repositorio.PresentarMensaje(mensaje, cancelacion);
    }
}

public sealed class AsignarCaso(IRepositorioSoporte repositorio, IReloj reloj)
{
    public async Task<bool> Ejecutar(int numero, Guid usuarioId, CancellationToken cancelacion)
    {
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        var caso = await repositorio.ObtenerAdministracion(numero, true, cancelacion);
        if (caso is null)
        {
            return false;
        }

        caso.Asignar(usuarioId, reloj.Ahora);
        await repositorio.GuardarCambios(cancelacion);
        await transaccion.Confirmar(cancelacion);
        return true;
    }
}

public sealed class CerrarCaso(
    IRepositorioSoporte repositorio,
    IReloj reloj,
    IBitacora bitacora)
{
    public async Task<bool> Ejecutar(
        int numero, Guid usuarioId, string usuarioNombre, string? ip, CancellationToken cancelacion)
    {
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        var caso = await repositorio.ObtenerAdministracion(numero, true, cancelacion);
        if (caso is null)
        {
            return false;
        }

        if (caso.Estado == EstadoCaso.Cerrado)
        {
            return true;
        }

        caso.Cerrar(reloj.Ahora);
        await repositorio.GuardarCambios(cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario, usuarioId, usuarioNombre, caso.OrganizacionId,
            AccionesBitacora.CasoCerrado, $"Cerro el caso CAS-{caso.Numero}: {caso.Asunto}.")
        {
            ObjetivoTipo = "caso",
            ObjetivoId = caso.Id,
            Ip = ip,
        }, cancelacion);
        await transaccion.Confirmar(cancelacion);
        return true;
    }
}

public sealed class ConsultarOrganizacionCaso(IRepositorioSoporte repositorio)
{
    public Task<ResumenOrganizacionCaso?> Ejecutar(int numero, CancellationToken cancelacion) =>
        repositorio.ResumenOrganizacion(numero, cancelacion);
}

public sealed class NotificadorCasos(IRepositorioSoporte repositorio, IColaCorreo correo)
{
    public async Task Notificar(Caso caso, bool autorEsPlataforma, CancellationToken cancelacion)
    {
        var destinatario = autorEsPlataforma
            ? await repositorio.DestinatarioProveedor(caso, cancelacion)
            : await repositorio.DestinatarioPlataforma(caso.AsignadoA, cancelacion);
        if (destinatario is null)
        {
            return;
        }

        await correo.Encolar("respuesta_caso", destinatario.Correo, new
        {
            nombre = destinatario.Nombre,
            numeroCaso = $"CAS-{caso.Numero}",
            asunto = caso.Asunto,
            enlace = repositorio.EnlaceCaso(destinatario.Plataforma, caso.Numero),
        }, cancelacion);
    }
}
