using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shapi.Aplicacion.Administracion;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;
using ApiDominio = Shapi.Dominio.Apis.Api;

namespace Shapi.Api.Administracion;

public sealed class RepositorioOrganizacionesAdministracion(ShapiDbContext db, IReloj reloj)
    : IRepositorioOrganizacionesAdministracion
{
    public async Task<IReadOnlyList<OrganizacionAdministracion>> Listar(CancellationToken cancelacion = default)
    {
        var organizaciones = await db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.Tipo == TipoOrganizacion.Proveedor)
            .OrderBy(o => o.CreadoEn)
            .ThenBy(o => o.Id)
            .ToListAsync(cancelacion);
        var ids = organizaciones.Select(o => o.Id).ToArray();

        var suscripciones = (await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
                .Where(s => ids.Contains(s.OrganizacionId) && s.Estado != EstadoSuscripcion.Finalizada)
                .OrderByDescending(s => s.CreadoEn)
                .ToListAsync(cancelacion))
            .GroupBy(s => s.OrganizacionId)
            .ToDictionary(g => g.Key, g => g.First());
        var planesIds = suscripciones.Values.Select(s => s.PlanId).Distinct().ToArray();
        var planes = await db.Set<PlanPlataforma>().AsNoTracking()
            .Where(p => planesIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancelacion);
        var propietarios = await (
            from membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking()
            join usuario in db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking()
                on membresia.UsuarioId equals usuario.Id
            where ids.Contains(membresia.OrganizacionId) && membresia.Rol == Rol.Propietario
            select new { membresia.OrganizacionId, usuario.Correo })
            .ToDictionaryAsync(x => x.OrganizacionId, x => x.Correo, cancelacion);
        var apis = await db.Set<ApiDominio>().IgnoreQueryFilters().AsNoTracking()
            .Where(a => ids.Contains(a.OrganizacionId))
            .GroupBy(a => a.OrganizacionId)
            .Select(g => new { OrganizacionId = g.Key, Total = g.Count(), Publicadas = g.Count(a => a.Estado == EstadoApi.Publicada) })
            .ToDictionaryAsync(x => x.OrganizacionId, cancelacion);
        var consumidores = await db.Set<Consumidor>().IgnoreQueryFilters().AsNoTracking()
            .Where(c => ids.Contains(c.OrganizacionId))
            .GroupBy(c => c.OrganizacionId)
            .Select(g => new { OrganizacionId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.OrganizacionId, x => x.Total, cancelacion);

        return organizaciones.Select(organizacion =>
        {
            suscripciones.TryGetValue(organizacion.Id, out var suscripcion);
            var estado = organizacion.EstadoAdmin == EstadoAdmin.Suspendida
                || suscripcion?.Estado == EstadoSuscripcion.Suspendida
                    ? "suspendida"
                    : suscripcion?.Estado == EstadoSuscripcion.EnGracia ? "en_gracia" : "activa";
            var conteoApis = apis.GetValueOrDefault(organizacion.Id);
            return new OrganizacionAdministracion(
                organizacion.Id,
                organizacion.Nombre,
                propietarios.GetValueOrDefault(organizacion.Id) ?? "Sin propietario",
                conteoApis?.Total ?? 0,
                conteoApis?.Publicadas ?? 0,
                consumidores.GetValueOrDefault(organizacion.Id),
                suscripcion is not null && planes.TryGetValue(suscripcion.PlanId, out var plan) ? plan.Nombre : "Sin plan",
                suscripcion?.Inicio ?? organizacion.CreadoEn,
                suscripcion?.Fin ?? reloj.Ahora,
                estado,
                organizacion.EstadoAdmin == EstadoAdmin.Suspendida,
                organizacion.MotivoSuspension);
        }).ToArray();
    }

    public Task<Organizacion?> ObtenerProveedor(Guid id, CancellationToken cancelacion = default) =>
        db.Set<Organizacion>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(o => o.Id == id && o.Tipo == TipoOrganizacion.Proveedor, cancelacion);

    public Task<PropietarioOrganizacion?> ObtenerPropietario(Guid organizacionId, CancellationToken cancelacion = default) => (
        from membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking()
        join usuario in db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking()
            on membresia.UsuarioId equals usuario.Id
        where membresia.OrganizacionId == organizacionId && membresia.Rol == Rol.Propietario
        select new PropietarioOrganizacion(usuario.Nombre, usuario.Correo))
        .SingleOrDefaultAsync(cancelacion);

    public async Task<ITransaccionAdministracion> IniciarTransaccion(CancellationToken cancelacion = default) =>
        new TransaccionAdministracion(await db.Database.BeginTransactionAsync(cancelacion));

    public Task Guardar(CancellationToken cancelacion = default) => db.SaveChangesAsync(cancelacion);

    private sealed class TransaccionAdministracion(IDbContextTransaction transaccion) : ITransaccionAdministracion
    {
        public Task Confirmar(CancellationToken cancelacion = default) => transaccion.CommitAsync(cancelacion);
        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}

public sealed class EnlacesAdministracion(IConfiguration configuracion) : IEnlacesAdministracion
{
    public string Soporte => $"https://{configuracion["SHAPI_DOMINIO_BASE"] ?? "shapi.localhost"}/panel/soporte";
}
