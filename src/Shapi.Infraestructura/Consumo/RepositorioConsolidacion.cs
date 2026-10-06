using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Consumo;

/// <summary>El marcador del lote y todos sus UPSERT comparten la transacción (RNF-05).</summary>
public sealed class RepositorioConsolidacion(ShapiDbContext db, IReloj reloj)
{
    public Task<bool> YaAplicadoAsync(Guid loteId, CancellationToken cancelacion) =>
        db.Set<Shapi.Dominio.Consumo.LoteConsolidado>().AnyAsync(x => x.LoteId == loteId, cancelacion);

    private static readonly string Columnas = string.Join(", ", MetricasDiarias.Columnas.Values);
    private static readonly string Parametros = string.Join(", ", MetricasDiarias.Columnas.Keys.Select(x => "@" + x));
    private static readonly string Sumas = string.Join(", ", MetricasDiarias.Columnas.Values
        .Select(x => $"{x} = consumo_diario.{x} + EXCLUDED.{x}"));

    private static readonly string Upsert = $"""
        INSERT INTO consumo_diario
            (fecha, api_id, ruta_id, suscripcion_id, entorno, {Columnas},
             hist_latencia_total, hist_latencia_compuerta, creado_en, actualizado_en)
        VALUES (@fecha, @api, @ruta, @suscripcion, @entorno, {Parametros}, @hist_t, @hist_c, @ahora, @ahora)
        ON CONFLICT (fecha, api_id, ruta_id, suscripcion_id, entorno) DO UPDATE SET
            {Sumas},
            hist_latencia_total = ARRAY(SELECT consumo_diario.hist_latencia_total[i] + EXCLUDED.hist_latencia_total[i] FROM generate_series(1,10) AS i),
            hist_latencia_compuerta = ARRAY(SELECT consumo_diario.hist_latencia_compuerta[i] + EXCLUDED.hist_latencia_compuerta[i] FROM generate_series(1,10) AS i),
            actualizado_en = EXCLUDED.actualizado_en
        """;

    public async Task GuardarAsync(Guid loteId, IReadOnlyList<MetricasDiarias> filas, CancellationToken cancelacion)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var ahora = reloj.Ahora;
        var insertadas = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO lote_consolidado (lote_id, procesado_en) VALUES ({loteId}, {ahora})
            ON CONFLICT (lote_id) DO NOTHING
            """, cancelacion);
        if (insertadas == 0)
        {
            await transaccion.CommitAsync(cancelacion);
            return;
        }

        // El mismo orden en todos los lotes evita invertir los bloqueos entre dos consolidadores.
        foreach (var grupo in filas.GroupBy(x => x.ApiId).OrderBy(x => x.Key))
        {
            // La recarga de OpenAPI bloquea la API antes de retirar rutas. Este bloqueo compartido mantiene estables
            // esas rutas hasta el COMMIT. No se usa FOR UPDATE: las FK también bloquean la API (H-112).
            var conexion = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var protegerApi = new NpgsqlCommand("SELECT id FROM api WHERE id = @api FOR KEY SHARE", conexion,
                (NpgsqlTransaction)transaccion.GetDbTransaction());
            protegerApi.Parameters.AddWithValue("api", grupo.Key);
            if (await protegerApi.ExecuteScalarAsync(cancelacion) is null)
            {
                throw new InvalidOperationException("La API de las métricas ya no existe.");
            }
            var rutasVigentes = await db.Set<Shapi.Dominio.Apis.Ruta>().IgnoreQueryFilters().Where(x => x.ApiId == grupo.Key)
                .Select(x => x.Id).ToListAsync(cancelacion);
            foreach (var fila in grupo.OrderBy(x => x.Fecha).ThenBy(x => x.RutaId).ThenBy(x => x.SuscripcionId).ThenBy(x => x.Entorno))
            {
                var ruta = fila.RutaId is { } id && rutasVigentes.Contains(id) ? fila.RutaId : null;
                List<object> parametros =
                [
                    new NpgsqlParameter("fecha", NpgsqlDbType.Date) { Value = fila.Fecha },
                    new NpgsqlParameter("api", NpgsqlDbType.Uuid) { Value = fila.ApiId },
                    new NpgsqlParameter("ruta", NpgsqlDbType.Uuid) { Value = (object?)ruta ?? DBNull.Value },
                    new NpgsqlParameter("suscripcion", NpgsqlDbType.Uuid) { Value = (object?)fila.SuscripcionId ?? DBNull.Value },
                    new NpgsqlParameter("entorno", fila.Entorno), new NpgsqlParameter("ahora", ahora),
                    new NpgsqlParameter("hist_t", fila.Histograma("h_t_")), new NpgsqlParameter("hist_c", fila.Histograma("h_c_")),
                    .. MetricasDiarias.Columnas.Keys.Select(x => new NpgsqlParameter(x, fila.Contador(x))),
                ];
                await db.Database.ExecuteSqlRawAsync(Upsert, parametros, cancelacion);
            }
        }
        await transaccion.CommitAsync(cancelacion);
    }
}
