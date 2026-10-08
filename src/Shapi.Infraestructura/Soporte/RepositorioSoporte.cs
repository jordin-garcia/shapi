using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Soporte;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Soporte;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;
using ApiDominio = Shapi.Dominio.Apis.Api;

namespace Shapi.Infraestructura.Soporte;

public sealed class RepositorioSoporte(ShapiDbContext db, IReloj reloj, IConfiguration configuracion) : IRepositorioSoporte
{
    public async Task<Pagina<CasoListado>> ListarProveedor(
        Guid organizacionId, int pagina, int tamano, CancellationToken cancelacion)
    {
        var consulta =
            from caso in db.Set<Caso>().AsNoTracking()
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
                Math.Max(0, db.Set<CasoMensaje>().Count(m => m.CasoId == caso.Id) - 1));
        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(cancelacion);
        return new(elementos, total);
    }

    public async Task<Pagina<CasoListadoAdministracion>> ListarAdministracion(
        int pagina, int tamano, CancellationToken cancelacion)
    {
        var consulta =
            from caso in db.Set<Caso>().IgnoreQueryFilters().AsNoTracking()
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
                organizacion.Nombre);
        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(cancelacion);
        return new(elementos, total);
    }

    public async Task<IReadOnlyList<OpcionOrganizacionCaso>> ListarOrganizaciones(CancellationToken cancelacion)
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
            o.Id,
            o.Nombre,
            apis.Where(a => a.OrganizacionId == o.Id)
                .Select(a => new OpcionApiCaso(a.Id, a.Nombre))
                .ToArray())).ToArray();
    }

    public Task<bool> ExisteOrganizacionProveedor(Guid organizacionId, CancellationToken cancelacion) =>
        db.Set<Organizacion>().IgnoreQueryFilters()
            .AnyAsync(o => o.Id == organizacionId && o.Tipo == TipoOrganizacion.Proveedor, cancelacion);

    public Task<bool> ApiPertenece(Guid apiId, Guid organizacionId, CancellationToken cancelacion) =>
        db.Set<ApiDominio>().IgnoreQueryFilters()
            .AnyAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);

    public async Task Agregar(Caso caso, CancellationToken cancelacion)
    {
        db.Add(caso);
        db.AddRange(caso.ObtenerMensajes());
        await db.SaveChangesAsync(cancelacion);
    }

    public Task<Caso?> ObtenerProveedor(
        int numero, Guid organizacionId, bool bloquear, CancellationToken cancelacion) =>
        bloquear
            ? db.Set<Caso>().FromSqlInterpolated(
                    $"SELECT * FROM caso WHERE numero = {numero} AND organizacion_id = {organizacionId} FOR UPDATE")
                .SingleOrDefaultAsync(cancelacion)
            : db.Set<Caso>().AsNoTracking()
                .SingleOrDefaultAsync(c => c.Numero == numero && c.OrganizacionId == organizacionId, cancelacion);

    public Task<Caso?> ObtenerAdministracion(int numero, bool bloquear, CancellationToken cancelacion) =>
        bloquear
            ? db.Set<Caso>().FromSqlInterpolated(
                    $"SELECT * FROM caso WHERE numero = {numero} FOR UPDATE").IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancelacion)
            : db.Set<Caso>().IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefaultAsync(c => c.Numero == numero, cancelacion);

    public async Task GuardarMensaje(CasoMensaje mensaje, CancellationToken cancelacion)
    {
        db.Add(mensaje);
        await db.SaveChangesAsync(cancelacion);
    }

    public Task GuardarCambios(CancellationToken cancelacion) => db.SaveChangesAsync(cancelacion);

    public async Task<CasoDetalle?> Detalle(Caso caso, CancellationToken cancelacion)
    {
        var organizacion = await db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(o => o.Id == caso.OrganizacionId, cancelacion);
        var apiNombre = caso.ApiId.HasValue
            ? await db.Set<ApiDominio>().IgnoreQueryFilters().Where(a => a.Id == caso.ApiId)
                .Select(a => a.Nombre).SingleOrDefaultAsync(cancelacion)
            : null;
        var creadoPor = await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Id == caso.CreadoPor)
            .Select(u => u.Nombre).SingleAsync(cancelacion);
        var asignadoA = caso.AsignadoA.HasValue
            ? await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Id == caso.AsignadoA)
                .Select(u => u.Nombre).SingleOrDefaultAsync(cancelacion)
            : null;
        var mensajesBase = await db.Set<CasoMensaje>().IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.CasoId == caso.Id).OrderBy(m => m.CreadoEn).ToListAsync(cancelacion);
        var mensajes = new List<MensajeCaso>(mensajesBase.Count);
        foreach (var mensaje in mensajesBase)
        {
            mensajes.Add(await PresentarMensaje(mensaje, cancelacion));
        }

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

    public async Task<ResumenOrganizacionCaso?> ResumenOrganizacion(int numero, CancellationToken cancelacion)
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
            ? await db.Set<ApiDominio>().IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == caso.ApiId, cancelacion)
            : null;
        var suscripcion = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.OrganizacionId == caso.OrganizacionId && s.Estado != EstadoSuscripcion.Finalizada)
            .OrderByDescending(s => s.CreadoEn)
            .FirstOrDefaultAsync(cancelacion);
        var plan = suscripcion is null
            ? null
            : await db.Set<PlanPlataforma>().AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == suscripcion.PlanId, cancelacion);
        var dominio = caso.ApiId.HasValue
            ? await db.Set<DominioPropio>().IgnoreQueryFilters().AsNoTracking()
                .Where(d => d.ApiId == caso.ApiId)
                .OrderByDescending(d => d.CreadoEn)
                .FirstOrDefaultAsync(cancelacion)
            : null;
        var estado = organizacion.EstadoAdmin == EstadoAdmin.Suspendida || suscripcion?.Estado == EstadoSuscripcion.Suspendida
            ? "suspendida"
            : suscripcion?.Estado == EstadoSuscripcion.EnGracia ? "en_gracia" : "activa";
        var numeroApis = await db.Set<ApiDominio>().IgnoreQueryFilters()
            .CountAsync(a => a.OrganizacionId == caso.OrganizacionId, cancelacion);
        var numeroConsumidores = await db.Set<Consumidor>().IgnoreQueryFilters()
            .CountAsync(c => c.OrganizacionId == caso.OrganizacionId, cancelacion);
        return new(
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

    public async Task<MensajeCaso> PresentarMensaje(CasoMensaje mensaje, CancellationToken cancelacion)
    {
        var autor = await (
            from usuario in db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking()
            join membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking() on usuario.Id equals membresia.UsuarioId
            join organizacion in db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
                on membresia.OrganizacionId equals organizacion.Id
            where usuario.Id == mensaje.AutorId
            orderby organizacion.Tipo == TipoOrganizacion.Plataforma descending
            select new
            {
                usuario.Nombre,
                Rol = membresia.Rol.ToString().ToLower(),
                Plataforma = organizacion.Tipo == TipoOrganizacion.Plataforma,
            }).FirstAsync(cancelacion);
        return new(mensaje.Id, autor.Nombre, autor.Rol, mensaje.Cuerpo, mensaje.CreadoEn, autor.Plataforma);
    }

    public async Task<DestinatarioCaso?> DestinatarioProveedor(Caso caso, CancellationToken cancelacion)
    {
        var creador = await (
            from membresia in db.Set<Membresia>().IgnoreQueryFilters()
            join usuario in db.Set<Usuario>().IgnoreQueryFilters() on membresia.UsuarioId equals usuario.Id
            where membresia.OrganizacionId == caso.OrganizacionId && usuario.Id == caso.CreadoPor
            select new DestinatarioCaso(usuario.Nombre, usuario.Correo, false))
            .FirstOrDefaultAsync(cancelacion);
        if (creador is not null)
        {
            return creador;
        }

        return await (
            from membresia in db.Set<Membresia>().IgnoreQueryFilters()
            join usuario in db.Set<Usuario>().IgnoreQueryFilters() on membresia.UsuarioId equals usuario.Id
            where membresia.OrganizacionId == caso.OrganizacionId && membresia.Rol == Rol.Propietario
            orderby usuario.CreadoEn
            select new DestinatarioCaso(usuario.Nombre, usuario.Correo, false))
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task<DestinatarioCaso?> DestinatarioPlataforma(Guid? asignadoA, CancellationToken cancelacion)
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
            var asignado = await consulta.Where(x => x.usuario.Id == asignadoA)
                .Select(x => x.usuario).FirstOrDefaultAsync(cancelacion);
            if (asignado is not null)
            {
                return new(asignado.Nombre, asignado.Correo, true);
            }
        }

        return await consulta.OrderBy(x => x.membresia.Rol == Rol.Soporte ? 0 : 1)
            .ThenBy(x => x.usuario.CreadoEn)
            .Select(x => new DestinatarioCaso(x.usuario.Nombre, x.usuario.Correo, true))
            .FirstOrDefaultAsync(cancelacion);
    }

    public string EnlaceCaso(bool plataforma, int numero)
    {
        var dominio = configuracion["SHAPI_DOMINIO_BASE"] ?? "shapi.localhost";
        var ruta = plataforma ? "admin/casos" : "panel/soporte";
        return $"https://{dominio}/{ruta}/{numero}";
    }

    public async Task<ITransaccionSoporte> IniciarTransaccion(CancellationToken cancelacion) =>
        new TransaccionSoporte(await db.Database.BeginTransactionAsync(cancelacion));

    private sealed class TransaccionSoporte(IDbContextTransaction transaccion) : ITransaccionSoporte
    {
        public Task Confirmar(CancellationToken cancelacion) => transaccion.CommitAsync(cancelacion);
        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}
