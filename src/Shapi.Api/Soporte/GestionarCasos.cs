using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Soporte;
using Shapi.Contratos;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Soporte;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;
using ApiDominio = Shapi.Dominio.Apis.Api;

namespace Shapi.Api.Soporte;

public sealed class ListarCasos(ShapiDbContext db)
{
    public Task<List<CasoListado>> Proveedor(Guid organizacionId, CancellationToken cancelacion) =>
        (from caso in db.Set<Caso>().AsNoTracking()
         join api in db.Set<ApiDominio>().AsNoTracking() on caso.ApiId equals api.Id into apis
         from api in apis.DefaultIfEmpty()
         where caso.OrganizacionId == organizacionId
         orderby caso.ActualizadoEn descending, caso.Numero descending
         select new CasoListado(
             caso.Numero,
             caso.Asunto,
             api == null ? null : api.Nombre,
             caso.Estado == EstadoCaso.Abierto ? "abierto" : "cerrado",
             caso.CreadoEn,
             Math.Max(0, db.Set<CasoMensaje>().Count(m => m.CasoId == caso.Id) - 1)))
        .ToListAsync(cancelacion);

    public Task<List<CasoListadoAdministracion>> Administracion(CancellationToken cancelacion) =>
        (from caso in db.Set<Caso>().IgnoreQueryFilters().AsNoTracking()
         join organizacion in db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
             on caso.OrganizacionId equals organizacion.Id
         join api in db.Set<ApiDominio>().IgnoreQueryFilters().AsNoTracking() on caso.ApiId equals api.Id into apis
         from api in apis.DefaultIfEmpty()
         orderby caso.ActualizadoEn descending, caso.Numero descending
         select new CasoListadoAdministracion(
             caso.Numero,
             caso.Asunto,
             api == null ? null : api.Nombre,
             caso.Estado == EstadoCaso.Abierto ? "abierto" : "cerrado",
             caso.CreadoEn,
             Math.Max(0, db.Set<CasoMensaje>().IgnoreQueryFilters().Count(m => m.CasoId == caso.Id) - 1),
             organizacion.Nombre))
        .ToListAsync(cancelacion);
}

public sealed class ListarOrganizacionesParaCaso(ShapiDbContext db)
{
    public async Task<IReadOnlyList<OpcionOrganizacionCaso>> Ejecutar(CancellationToken cancelacion)
    {
        var organizaciones = await db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.Tipo == TipoOrganizacion.Proveedor)
            .OrderBy(o => o.Nombre)
            .Select(o => new { o.Id, o.Nombre })
            .ToListAsync(cancelacion);
        var ids = organizaciones.Select(o => o.Id).ToArray();
        var apis = await db.Set<ApiDominio>().IgnoreQueryFilters().AsNoTracking()
            .Where(a => ids.Contains(a.OrganizacionId))
            .OrderBy(a => a.Nombre)
            .Select(a => new { a.Id, a.OrganizacionId, a.Nombre })
            .ToListAsync(cancelacion);
        return organizaciones.Select(o => new OpcionOrganizacionCaso(
            o.Id, o.Nombre, apis.Where(a => a.OrganizacionId == o.Id).Select(a => new OpcionApiCaso(a.Id, a.Nombre)).ToArray())).ToArray();
    }
}

public sealed class AbrirCaso(
    ShapiDbContext db,
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
        if (error is not null)
        {
            return error;
        }

        return await Guardar(organizacionId, usuarioId, usuarioNombre, solicitud.Asunto, solicitud.ApiId,
            solicitud.Descripcion, ip, false, cancelacion);
    }

    public async Task<Resultado<CasoDetalle>> Administracion(
        Guid usuarioId,
        string usuarioNombre,
        SolicitudAbrirCasoAdministracion solicitud,
        string? ip,
        CancellationToken cancelacion)
    {
        var existe = await db.Set<Organizacion>().IgnoreQueryFilters()
            .AnyAsync(o => o.Id == solicitud.OrganizacionId && o.Tipo == TipoOrganizacion.Proveedor, cancelacion);
        if (!existe)
        {
            return new Error(CodigosError.DatosInvalidos, "Revise los datos del caso.",
                new Dictionary<string, string[]> { ["organizacionId"] = ["La organizacion proveedora no existe."] });
        }

        var error = await Validar(solicitud.OrganizacionId, solicitud.Asunto, solicitud.ApiId, solicitud.Descripcion, cancelacion);
        if (error is not null)
        {
            return error;
        }

        return await Guardar(solicitud.OrganizacionId, usuarioId, usuarioNombre, solicitud.Asunto, solicitud.ApiId,
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
        var caso = Caso.Abrir(organizacionId, apiId, usuarioId, asunto, descripcion, reloj.Ahora);
        db.Add(caso);
        db.AddRange(caso.ObtenerMensajes());
        await db.SaveChangesAsync(cancelacion);

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

        await notificador.Notificar(caso, usuarioId, personalPlataforma, cancelacion);
        return (await ConsultasSoporte.Detalle(db, caso, cancelacion))!;
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

        if (apiId.HasValue && !await db.Set<ApiDominio>().IgnoreQueryFilters()
                .AnyAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion))
        {
            errores["apiId"] = ["La API no pertenece a la organizacion."];
        }

        return errores.Count == 0 ? null : new Error(CodigosError.DatosInvalidos, "Revise los datos del caso.", errores);
    }
}

public sealed class ConsultarCaso(ShapiDbContext db)
{
    public async Task<CasoDetalle?> Proveedor(int numero, Guid organizacionId, CancellationToken cancelacion)
    {
        var caso = await db.Set<Caso>().AsNoTracking()
            .SingleOrDefaultAsync(c => c.Numero == numero && c.OrganizacionId == organizacionId, cancelacion);
        return caso is null ? null : await ConsultasSoporte.Detalle(db, caso, cancelacion);
    }

    public async Task<CasoDetalle?> Administracion(int numero, CancellationToken cancelacion)
    {
        var caso = await db.Set<Caso>().IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(c => c.Numero == numero, cancelacion);
        return caso is null ? null : await ConsultasSoporte.Detalle(db, caso, cancelacion);
    }
}

public sealed class ResponderCaso(ShapiDbContext db, IReloj reloj, NotificadorCasos notificador)
{
    public async Task<Resultado<MensajeCaso?>> Proveedor(
        int numero, Guid organizacionId, Guid usuarioId, SolicitudMensajeCaso solicitud, CancellationToken cancelacion)
    {
        var caso = await db.Set<Caso>().SingleOrDefaultAsync(
            c => c.Numero == numero && c.OrganizacionId == organizacionId, cancelacion);
        return await Guardar(caso, usuarioId, solicitud, false, cancelacion);
    }

    public async Task<Resultado<MensajeCaso?>> Administracion(
        int numero, Guid usuarioId, SolicitudMensajeCaso solicitud, CancellationToken cancelacion)
    {
        var caso = await db.Set<Caso>().IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Numero == numero, cancelacion);
        return await Guardar(caso, usuarioId, solicitud, true, cancelacion);
    }

    private async Task<Resultado<MensajeCaso?>> Guardar(
        Caso? caso,
        Guid usuarioId,
        SolicitudMensajeCaso solicitud,
        bool personalPlataforma,
        CancellationToken cancelacion)
    {
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
        db.Add(mensaje);
        await db.SaveChangesAsync(cancelacion);
        await notificador.Notificar(caso, usuarioId, personalPlataforma, cancelacion);

        var usuario = await ConsultasSoporte.Autor(db, usuarioId, cancelacion);
        return new MensajeCaso(mensaje.Id, usuario.Nombre, usuario.Rol, mensaje.Cuerpo, mensaje.CreadoEn, usuario.Plataforma);
    }
}

public sealed class AsignarCaso(ShapiDbContext db, IReloj reloj)
{
    public async Task<bool> Ejecutar(int numero, Guid usuarioId, CancellationToken cancelacion)
    {
        var caso = await db.Set<Caso>().IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Numero == numero, cancelacion);
        if (caso is null)
        {
            return false;
        }

        caso.Asignar(usuarioId, reloj.Ahora);
        await db.SaveChangesAsync(cancelacion);
        return true;
    }
}

public sealed class CerrarCaso(ShapiDbContext db, IReloj reloj, IBitacora bitacora)
{
    public async Task<bool> Ejecutar(
        int numero, Guid usuarioId, string usuarioNombre, string? ip, CancellationToken cancelacion)
    {
        var caso = await db.Set<Caso>().IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Numero == numero, cancelacion);
        if (caso is null)
        {
            return false;
        }

        caso.Cerrar(reloj.Ahora);
        await db.SaveChangesAsync(cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario, usuarioId, usuarioNombre, caso.OrganizacionId,
            AccionesBitacora.CasoCerrado, $"Cerro el caso CAS-{caso.Numero}: {caso.Asunto}.")
        {
            ObjetivoTipo = "caso",
            ObjetivoId = caso.Id,
            Ip = ip,
        }, cancelacion);
        return true;
    }
}

public sealed class ConsultarOrganizacionCaso(ShapiDbContext db, IReloj reloj)
{
    public async Task<ResumenOrganizacionCaso?> Ejecutar(int numero, CancellationToken cancelacion)
    {
        var caso = await db.Set<Caso>().IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(c => c.Numero == numero, cancelacion);
        if (caso is null)
        {
            return null;
        }

        var organizacion = await db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(o => o.Id == caso.OrganizacionId, cancelacion);
        var api = caso.ApiId.HasValue
            ? await db.Set<ApiDominio>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(a => a.Id == caso.ApiId, cancelacion)
            : null;
        var suscripcion = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.OrganizacionId == caso.OrganizacionId && s.Estado != EstadoSuscripcion.Finalizada)
            .OrderByDescending(s => s.CreadoEn)
            .FirstOrDefaultAsync(cancelacion);
        var plan = suscripcion is null ? null : await db.Set<PlanPlataforma>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == suscripcion.PlanId, cancelacion);
        var dominio = caso.ApiId.HasValue
            ? await db.Set<DominioPropio>().IgnoreQueryFilters().AsNoTracking()
                .Where(d => d.ApiId == caso.ApiId).OrderByDescending(d => d.CreadoEn).FirstOrDefaultAsync(cancelacion)
            : null;

        var estado = organizacion.EstadoAdmin == EstadoAdmin.Suspendida || suscripcion?.Estado == EstadoSuscripcion.Suspendida
            ? "suspendida"
            : suscripcion?.Estado == EstadoSuscripcion.EnGracia ? "en_gracia" : "activa";
        var numeroApis = await db.Set<ApiDominio>().IgnoreQueryFilters().CountAsync(a => a.OrganizacionId == caso.OrganizacionId, cancelacion);
        var numeroConsumidores = await db.Set<Consumidor>().IgnoreQueryFilters().CountAsync(c => c.OrganizacionId == caso.OrganizacionId, cancelacion);

        return new ResumenOrganizacionCaso(
            organizacion.Nombre,
            api?.Nombre,
            plan?.Nombre ?? "Sin plan",
            suscripcion?.Inicio ?? caso.CreadoEn,
            suscripcion?.Fin ?? reloj.Ahora,
            estado,
            numeroApis,
            numeroConsumidores,
            dominio?.Dominio,
            dominio is null ? null : dominio.Estado.ToString().ToLowerInvariant());
    }
}

public sealed class NotificadorCasos(ShapiDbContext db, IColaCorreo correo, IConfiguration configuracion)
{
    public async Task Notificar(Caso caso, Guid autorId, bool autorEsPlataforma, CancellationToken cancelacion)
    {
        var destinatario = autorEsPlataforma
            ? await Propietario(caso.OrganizacionId, cancelacion)
            : await PersonalPlataforma(caso.AsignadoA, cancelacion);
        if (destinatario is null)
        {
            return;
        }

        var dominio = configuracion["SHAPI_DOMINIO_BASE"] ?? "shapi.localhost";
        var ruta = destinatario.Plataforma ? "admin/casos" : "panel/soporte";
        await correo.Encolar("respuesta_caso", destinatario.Correo, new
        {
            nombre = destinatario.Nombre,
            numeroCaso = $"CAS-{caso.Numero}",
            asunto = caso.Asunto,
            enlace = $"https://{dominio}/{ruta}/{caso.Numero}",
        }, cancelacion);
    }

    private Task<Destinatario?> Propietario(Guid organizacionId, CancellationToken cancelacion) =>
        (from membresia in db.Set<Membresia>().IgnoreQueryFilters()
         join usuario in db.Set<Usuario>().IgnoreQueryFilters() on membresia.UsuarioId equals usuario.Id
         where membresia.OrganizacionId == organizacionId && membresia.Rol == Rol.Propietario
         orderby usuario.CreadoEn
         select new Destinatario(usuario.Nombre, usuario.Correo, false))
        .FirstOrDefaultAsync(cancelacion);

    private async Task<Destinatario?> PersonalPlataforma(Guid? asignadoA, CancellationToken cancelacion)
    {
        var consulta =
            from membresia in db.Set<Membresia>().IgnoreQueryFilters()
            join usuario in db.Set<Usuario>().IgnoreQueryFilters() on membresia.UsuarioId equals usuario.Id
            join organizacion in db.Set<Organizacion>().IgnoreQueryFilters() on membresia.OrganizacionId equals organizacion.Id
            where organizacion.Tipo == TipoOrganizacion.Plataforma
                && (membresia.Rol == Rol.Soporte || membresia.Rol == Rol.Administrador)
            select new { usuario, membresia };
        if (asignadoA.HasValue)
        {
            var asignado = await consulta.Where(x => x.usuario.Id == asignadoA).Select(x => x.usuario).FirstOrDefaultAsync(cancelacion);
            if (asignado is not null)
            {
                return new(asignado.Nombre, asignado.Correo, true);
            }
        }
        return await consulta.OrderBy(x => x.membresia.Rol == Rol.Soporte ? 0 : 1)
            .ThenBy(x => x.usuario.CreadoEn)
            .Select(x => new Destinatario(x.usuario.Nombre, x.usuario.Correo, true))
            .FirstOrDefaultAsync(cancelacion);
    }

    private sealed record Destinatario(string Nombre, string Correo, bool Plataforma);
}

internal static class ConsultasSoporte
{
    public static async Task<CasoDetalle?> Detalle(ShapiDbContext db, Caso caso, CancellationToken cancelacion)
    {
        var organizacion = await db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(o => o.Id == caso.OrganizacionId, cancelacion);
        var apiNombre = caso.ApiId.HasValue
            ? await db.Set<ApiDominio>().IgnoreQueryFilters().Where(a => a.Id == caso.ApiId).Select(a => a.Nombre).SingleOrDefaultAsync(cancelacion)
            : null;
        var creadoPor = await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Id == caso.CreadoPor).Select(u => u.Nombre).SingleAsync(cancelacion);
        var asignadoA = caso.AsignadoA.HasValue
            ? await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Id == caso.AsignadoA).Select(u => u.Nombre).SingleOrDefaultAsync(cancelacion)
            : null;
        var mensajesBase = await db.Set<CasoMensaje>().IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.CasoId == caso.Id).OrderBy(m => m.CreadoEn).ToListAsync(cancelacion);
        var autores = new Dictionary<Guid, AutorSoporte>();
        foreach (var autorId in mensajesBase.Select(m => m.AutorId).Distinct())
        {
            autores[autorId] = await Autor(db, autorId, cancelacion);
        }

        var mensajes = mensajesBase.Select(m => new MensajeCaso(
            m.Id, autores[m.AutorId].Nombre, autores[m.AutorId].Rol, m.Cuerpo, m.CreadoEn, autores[m.AutorId].Plataforma)).ToArray();

        return new CasoDetalle(
            caso.Numero,
            caso.Asunto,
            caso.Estado == EstadoCaso.Abierto ? "abierto" : "cerrado",
            organizacion.Nombre,
            apiNombre,
            creadoPor,
            asignadoA,
            caso.CreadoEn,
            mensajes);
    }

    public static async Task<AutorSoporte> Autor(ShapiDbContext db, Guid autorId, CancellationToken cancelacion)
    {
        var autor = await (
            from usuario in db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking()
            join membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking() on usuario.Id equals membresia.UsuarioId
            join organizacion in db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking() on membresia.OrganizacionId equals organizacion.Id
            where usuario.Id == autorId
            select new AutorSoporte(
                usuario.Nombre,
                membresia.Rol.ToString().ToLower(),
                organizacion.Tipo == TipoOrganizacion.Plataforma))
            .FirstAsync(cancelacion);
        return autor;
    }

    internal sealed record AutorSoporte(string Nombre, string Rol, bool Plataforma);
}
