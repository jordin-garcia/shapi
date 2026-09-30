using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Shapi.Aplicacion.Apis;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Apis;

public sealed class RepositorioApis(ShapiDbContext db) : IRepositorioApis
{
    public Task<bool> ExisteSubdominio(string subdominio, CancellationToken cancelacion = default) =>
        db.Set<Api>().IgnoreQueryFilters().AnyAsync(a => a.Subdominio == subdominio, cancelacion);

    public async Task<bool> Agregar(Api api, CancellationToken cancelacion = default)
    {
        db.Add(api);
        try
        {
            await db.SaveChangesAsync(cancelacion);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, TableName: "api" })
        {
            db.Entry(api).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<ITransaccionApis> IniciarTransaccion(CancellationToken cancelacion = default) =>
        new TransaccionApis(await db.Database.BeginTransactionAsync(cancelacion));

    public async Task<ListaApis> Listar(Guid organizacionId, CancellationToken cancelacion = default)
    {
        var elementos = await db.Set<Api>()
            .AsNoTracking()
            .Where(a => a.OrganizacionId == organizacionId)
            .OrderBy(a => a.Nombre)
            .ThenBy(a => a.Id)
            .Select(a => new ApiListado(a.Id, a.Nombre, a.Subdominio, a.Estado))
            .ToListAsync(cancelacion);

        var plan = await (
            from suscripcion in db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
            join planPlataforma in db.Set<PlanPlataforma>().AsNoTracking()
                on suscripcion.PlanId equals planPlataforma.Id
            where suscripcion.OrganizacionId == organizacionId
                && suscripcion.Estado != EstadoSuscripcion.Finalizada
            orderby suscripcion.CreadoEn descending
            select new { planPlataforma.Nombre, planPlataforma.MaxApis })
            .FirstOrDefaultAsync(cancelacion)
            ?? throw new InvalidOperationException("La organización no tiene una suscripción de plataforma vigente.");

        return new ListaApis(elementos, elementos.Count, plan.Nombre, plan.MaxApis);
    }

    public Task<string?> ObtenerNombreUsuario(Guid usuarioId, CancellationToken cancelacion = default) =>
        db.Set<Usuario>()
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => u.Nombre)
            .FirstOrDefaultAsync(cancelacion);

    private sealed class TransaccionApis(IDbContextTransaction transaccion) : ITransaccionApis
    {
        public Task Confirmar(CancellationToken cancelacion = default) => transaccion.CommitAsync(cancelacion);

        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}
