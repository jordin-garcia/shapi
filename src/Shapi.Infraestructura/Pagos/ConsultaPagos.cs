using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Pagos;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Pagos;

/// <summary>Lee los pagos con el aislamiento de organización del contexto y una restricción explícita por dueño.</summary>
public sealed class ConsultaPagos(ShapiDbContext db) : IConsultaPagos
{
    public Task<PaginaPagos> DePlataforma(Guid organizacionId, int pagina, int tamano, CancellationToken cancelacion)
    {
        var pagos = db.Set<Pago>().AsNoTracking().Where(pago =>
            pago.SuscripcionPlataformaId != null
            && db.Set<SuscripcionPlataforma>().Any(s => s.Id == pago.SuscripcionPlataformaId && s.OrganizacionId == organizacionId));
        return Listar(pagos, pagina, tamano, cancelacion);
    }

    public Task<PaginaPagos> DeConsumidor(Guid consumidorId, Guid apiId, int pagina, int tamano, CancellationToken cancelacion)
    {
        var pagos = db.Set<Pago>().AsNoTracking().Where(pago =>
            (pago.SuscripcionApiId != null
                && db.Set<SuscripcionApi>().Any(s => s.Id == pago.SuscripcionApiId
                    && s.ConsumidorId == consumidorId && s.ApiId == apiId))
            || (pago.Estado == EstadoPago.Rechazado && pago.ConsumidorId == consumidorId && pago.ApiId == apiId));
        return Listar(pagos, pagina, tamano, cancelacion);
    }

    private async Task<PaginaPagos> Listar(IQueryable<Pago> pagos, int pagina, int tamano, CancellationToken cancelacion)
    {
        var total = await pagos.CountAsync(cancelacion);
        var filas = await (
            from pago in pagos
            join medio in db.Set<MedioPago>().AsNoTracking() on pago.MedioPagoId equals (Guid?)medio.Id into medios
            from medio in medios.DefaultIfEmpty()
            orderby pago.CreadoEn descending, pago.Id descending
            select new PagoHistorial(
                pago.Id,
                pago.CreadoEn,
                pago.Concepto,
                pago.Descripcion,
                pago.PeriodoInicio,
                pago.PeriodoFin,
                pago.Monto,
                pago.Estado,
                medio == null ? (MarcaTarjeta?)null : medio.Marca,
                medio == null ? null : medio.Ultimos4))
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);
        return new PaginaPagos(filas, total);
    }
}
