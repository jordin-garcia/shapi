using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Consumo;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Consumo;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Consumo;

public sealed class ConsultaConsumo(ShapiDbContext db) : IConsultaConsumo
{
    public Task<ConsumoPeriodo> PorApiAsync(Guid organizacionId, Guid apiId, DateOnly desde, DateOnly hasta,
        EntornoConsumo? entorno = null, CancellationToken cancelacion = default) =>
        ConsultarAsync(organizacionId, desde, hasta, entorno, x => x.ApiId == apiId, cancelacion);

    public Task<ConsumoPeriodo> PorSuscripcionAsync(Guid organizacionId, Guid suscripcionId, DateOnly desde, DateOnly hasta,
        EntornoConsumo? entorno = null, CancellationToken cancelacion = default) =>
        ConsultarAsync(organizacionId, desde, hasta, entorno, x => x.SuscripcionId == suscripcionId, cancelacion);

    private async Task<ConsumoPeriodo> ConsultarAsync(Guid organizacionId, DateOnly desde, DateOnly hasta,
        EntornoConsumo? entorno, System.Linq.Expressions.Expression<Func<ConsumoDiario, bool>> filtro,
        CancellationToken cancelacion)
    {
        // La organización es un argumento explícito de este servicio; se aplica aquí incluso sin contexto HTTP.
        var consulta = db.Set<ConsumoDiario>().IgnoreQueryFilters().AsNoTracking().Where(filtro)
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta)
            .Where(x => db.Set<Api>().IgnoreQueryFilters().Any(api => api.Id == x.ApiId && api.OrganizacionId == organizacionId));
        if (entorno is not null)
        {
            consulta = consulta.Where(x => x.Entorno == entorno);
        }
        return new ConsumoPeriodo(await consulta.OrderBy(x => x.Fecha).ThenBy(x => x.Id).ToListAsync(cancelacion));
    }
}
