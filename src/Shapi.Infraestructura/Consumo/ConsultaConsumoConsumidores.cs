using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Consumo;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Consumo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Consumo;

public sealed class ConsultaConsumoConsumidores(ShapiDbContext db) : IConsultaConsumoConsumidores
{
    public async Task<IReadOnlyList<FilaConsumoConsumidor>> ConsultarAsync(Guid organizacionId, Guid apiId, DateOnly hoy,
        CancellationToken cancelacion = default)
    {
        // Aislamiento explícito también cuando se consulta sin un contexto HTTP de organización.
        var filas = await (from suscripcion in db.Set<SuscripcionApi>().IgnoreQueryFilters().AsNoTracking()
                           join api in db.Set<Api>() on suscripcion.ApiId equals api.Id
                           join consumidor in db.Set<Consumidor>() on suscripcion.ConsumidorId equals consumidor.Id
                           join plan in db.Set<PlanApi>() on suscripcion.PlanId equals plan.Id
                           where api.Id == apiId && api.OrganizacionId == organizacionId
                               && consumidor.OrganizacionId == organizacionId && plan.ApiId == apiId
                               && suscripcion.Estado != EstadoSuscripcion.Finalizada
                           orderby consumidor.NombreEmpresa, suscripcion.Id
                           select new FilaConsumoConsumidor(suscripcion.Id, consumidor.Id, consumidor.NombreEmpresa,
                               plan.Nombre, suscripcion.Inicio, suscripcion.Fin, 0, plan.CuotaLlamadas,
                               db.Set<Pago>().IgnoreQueryFilters()
                                   .Where(p => p.SuscripcionApiId == suscripcion.Id && p.Estado == EstadoPago.Autorizado
                                       && p.PeriodoInicio == suscripcion.Inicio && p.PeriodoFin == suscripcion.Fin)
                                   .Sum(p => p.Monto),
                               db.Set<Pago>().IgnoreQueryFilters().Where(p => p.SuscripcionApiId == suscripcion.Id)
                                   .OrderByDescending(p => p.CreadoEn).ThenByDescending(p => p.Id)
                                   .Select(p => (EstadoPago?)p.Estado).FirstOrDefault(), suscripcion.Estado))
            .ToListAsync(cancelacion);
        if (filas.Count == 0)
        {
            return filas;
        }
        static DateOnly Dia(DateTimeOffset instante) => DateOnly.FromDateTime(instante.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var ids = filas.Select(x => x.SuscripcionId).ToArray();
        var desde = filas.Min(x => Dia(x.Inicio));
        var hasta = filas.Max(x => Dia(x.Fin.AddDays(-1)));
        if (hoy > hasta)
        {
            hasta = hoy;
        }
        // Dos consultas en total, sin una consulta por consumidor. Cada ciclo filtra sus propios días al sumar.
        var dias = await db.Set<ConsumoDiario>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.ApiId == apiId && x.SuscripcionId != null && ids.Contains(x.SuscripcionId.Value)
                && x.Entorno == EntornoConsumo.Produccion && x.Fecha >= desde && x.Fecha <= hasta)
            .GroupBy(x => new { x.SuscripcionId, x.Fecha })
            .Select(g => new { g.Key.SuscripcionId, g.Key.Fecha, Llamadas = g.Sum(x => x.Llamadas) })
            .ToListAsync(cancelacion);
        var porSuscripcion = dias.ToLookup(x => x.SuscripcionId!.Value);
        return filas.Select(fila =>
        {
            var inicio = Dia(fila.Inicio);
            var fin = Dia(fila.Fin.AddDays(-1));
            var limite = hoy > fin ? hoy : fin;
            return fila with { Llamadas = porSuscripcion[fila.SuscripcionId].Where(x => x.Fecha >= inicio && x.Fecha <= limite).Sum(x => x.Llamadas) };
        }).ToArray();
    }
}
