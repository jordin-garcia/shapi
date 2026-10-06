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

    public async Task BloquearApi(Api api, CancellationToken cancelacion = default)
    {
        await db.Database.ExecuteSqlAsync($"SELECT 1 FROM api WHERE id = {api.Id} FOR UPDATE", cancelacion);
        await db.Entry(api).ReloadAsync(cancelacion);
    }

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

    public Task<bool> CorreoVerificado(Guid usuarioId, CancellationToken cancelacion = default) =>
        db.Set<Usuario>()
            .AnyAsync(u => u.Id == usuarioId && u.CorreoVerificadoEn != null, cancelacion);

    public Task<bool> ExistePlanActivo(Guid apiId, CancellationToken cancelacion = default) =>
        db.Set<PlanApi>().AnyAsync(p => p.ApiId == apiId && p.Activo, cancelacion);

    public Task<Api?> Obtener(Guid apiId, Guid organizacionId, CancellationToken cancelacion = default) =>
        db.Set<Api>().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);

    public Task<List<Ruta>> ObtenerRutas(Guid apiId, CancellationToken cancelacion = default) =>
        db.Set<Ruta>().Where(r => r.ApiId == apiId).ToListAsync(cancelacion);

    public async Task ConsolidarConsumoRutas(
        IReadOnlyCollection<Guid> rutaIds,
        DateTimeOffset actualizadoEn,
        CancellationToken cancelacion = default)
    {
        if (rutaIds.Count == 0)
        {
            return;
        }

        var ids = rutaIds.ToArray();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO consumo_diario
                (fecha, api_id, ruta_id, suscripcion_id, entorno,
                 peticiones, llamadas, bytes_entrada, bytes_salida,
                 rechazos_401, rechazos_403, rechazos_404, rechazos_429,
                 origen_2xx, origen_3xx, origen_4xx, origen_5xx, origen_fallo,
                 hist_latencia_total, hist_latencia_compuerta,
                 latencia_total_suma_ms, latencia_compuerta_suma_ms, creado_en, actualizado_en)
            SELECT fecha, api_id, NULL, suscripcion_id, entorno,
                   sum(peticiones), sum(llamadas), sum(bytes_entrada), sum(bytes_salida),
                   sum(rechazos_401), sum(rechazos_403), sum(rechazos_404), sum(rechazos_429),
                   sum(origen_2xx), sum(origen_3xx), sum(origen_4xx), sum(origen_5xx), sum(origen_fallo),
                   ARRAY[
                       sum(hist_latencia_total[1])::integer, sum(hist_latencia_total[2])::integer,
                       sum(hist_latencia_total[3])::integer, sum(hist_latencia_total[4])::integer,
                       sum(hist_latencia_total[5])::integer, sum(hist_latencia_total[6])::integer,
                       sum(hist_latencia_total[7])::integer, sum(hist_latencia_total[8])::integer,
                       sum(hist_latencia_total[9])::integer, sum(hist_latencia_total[10])::integer
                   ],
                   ARRAY[
                       sum(hist_latencia_compuerta[1])::integer, sum(hist_latencia_compuerta[2])::integer,
                       sum(hist_latencia_compuerta[3])::integer, sum(hist_latencia_compuerta[4])::integer,
                       sum(hist_latencia_compuerta[5])::integer, sum(hist_latencia_compuerta[6])::integer,
                       sum(hist_latencia_compuerta[7])::integer, sum(hist_latencia_compuerta[8])::integer,
                       sum(hist_latencia_compuerta[9])::integer, sum(hist_latencia_compuerta[10])::integer
                   ],
                   sum(latencia_total_suma_ms), sum(latencia_compuerta_suma_ms), {actualizadoEn}, {actualizadoEn}
            FROM consumo_diario
            WHERE ruta_id = ANY({ids})
            GROUP BY fecha, api_id, suscripcion_id, entorno
            ON CONFLICT (fecha, api_id, ruta_id, suscripcion_id, entorno) DO UPDATE SET
                peticiones = consumo_diario.peticiones + EXCLUDED.peticiones,
                llamadas = consumo_diario.llamadas + EXCLUDED.llamadas,
                bytes_entrada = consumo_diario.bytes_entrada + EXCLUDED.bytes_entrada,
                bytes_salida = consumo_diario.bytes_salida + EXCLUDED.bytes_salida,
                rechazos_401 = consumo_diario.rechazos_401 + EXCLUDED.rechazos_401,
                rechazos_403 = consumo_diario.rechazos_403 + EXCLUDED.rechazos_403,
                rechazos_404 = consumo_diario.rechazos_404 + EXCLUDED.rechazos_404,
                rechazos_429 = consumo_diario.rechazos_429 + EXCLUDED.rechazos_429,
                origen_2xx = consumo_diario.origen_2xx + EXCLUDED.origen_2xx,
                origen_3xx = consumo_diario.origen_3xx + EXCLUDED.origen_3xx,
                origen_4xx = consumo_diario.origen_4xx + EXCLUDED.origen_4xx,
                origen_5xx = consumo_diario.origen_5xx + EXCLUDED.origen_5xx,
                origen_fallo = consumo_diario.origen_fallo + EXCLUDED.origen_fallo,
                hist_latencia_total = ARRAY[
                    consumo_diario.hist_latencia_total[1] + EXCLUDED.hist_latencia_total[1],
                    consumo_diario.hist_latencia_total[2] + EXCLUDED.hist_latencia_total[2],
                    consumo_diario.hist_latencia_total[3] + EXCLUDED.hist_latencia_total[3],
                    consumo_diario.hist_latencia_total[4] + EXCLUDED.hist_latencia_total[4],
                    consumo_diario.hist_latencia_total[5] + EXCLUDED.hist_latencia_total[5],
                    consumo_diario.hist_latencia_total[6] + EXCLUDED.hist_latencia_total[6],
                    consumo_diario.hist_latencia_total[7] + EXCLUDED.hist_latencia_total[7],
                    consumo_diario.hist_latencia_total[8] + EXCLUDED.hist_latencia_total[8],
                    consumo_diario.hist_latencia_total[9] + EXCLUDED.hist_latencia_total[9],
                    consumo_diario.hist_latencia_total[10] + EXCLUDED.hist_latencia_total[10]
                ],
                hist_latencia_compuerta = ARRAY[
                    consumo_diario.hist_latencia_compuerta[1] + EXCLUDED.hist_latencia_compuerta[1],
                    consumo_diario.hist_latencia_compuerta[2] + EXCLUDED.hist_latencia_compuerta[2],
                    consumo_diario.hist_latencia_compuerta[3] + EXCLUDED.hist_latencia_compuerta[3],
                    consumo_diario.hist_latencia_compuerta[4] + EXCLUDED.hist_latencia_compuerta[4],
                    consumo_diario.hist_latencia_compuerta[5] + EXCLUDED.hist_latencia_compuerta[5],
                    consumo_diario.hist_latencia_compuerta[6] + EXCLUDED.hist_latencia_compuerta[6],
                    consumo_diario.hist_latencia_compuerta[7] + EXCLUDED.hist_latencia_compuerta[7],
                    consumo_diario.hist_latencia_compuerta[8] + EXCLUDED.hist_latencia_compuerta[8],
                    consumo_diario.hist_latencia_compuerta[9] + EXCLUDED.hist_latencia_compuerta[9],
                    consumo_diario.hist_latencia_compuerta[10] + EXCLUDED.hist_latencia_compuerta[10]
                ],
                latencia_total_suma_ms = consumo_diario.latencia_total_suma_ms + EXCLUDED.latencia_total_suma_ms,
                latencia_compuerta_suma_ms = consumo_diario.latencia_compuerta_suma_ms + EXCLUDED.latencia_compuerta_suma_ms,
                actualizado_en = EXCLUDED.actualizado_en;

            DELETE FROM consumo_diario WHERE ruta_id = ANY({ids});
            """, cancelacion);
    }

    public void AgregarRuta(Ruta ruta) => db.Add(ruta);

    public void EliminarRuta(Ruta ruta) => db.Remove(ruta);

    public Task Guardar(CancellationToken cancelacion = default) => db.SaveChangesAsync(cancelacion);

    private sealed class TransaccionApis(IDbContextTransaction transaccion) : ITransaccionApis
    {
        public Task Confirmar(CancellationToken cancelacion = default) => transaccion.CommitAsync(cancelacion);

        public ValueTask DisposeAsync() => transaccion.DisposeAsync();
    }
}
