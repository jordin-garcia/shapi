using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Pagos;

public sealed record PagoPlataformaAdministracion(
    Guid Id,
    DateTimeOffset Fecha,
    string Organizacion,
    string Plan,
    decimal Monto,
    string Moneda,
    string? Tarjeta,
    string Estado,
    DateTimeOffset? PeriodoInicio,
    DateTimeOffset? PeriodoFin);

public enum ResultadoReversionPago
{
    Exito,
    NoEncontrado,
    NoAutorizado,
    PasarelaNoDisponible,
}

public sealed class AdministrarPagosPlataforma(ShapiDbContext db, IPasarelaPagos pasarela, IBitacora bitacora,
    IPublicadorCache publicador, IReloj reloj)
{
    public async Task<IReadOnlyList<PagoPlataformaAdministracion>> Listar(CancellationToken ct)
    {
        var consulta =
            from pago in db.Set<Pago>().IgnoreQueryFilters().AsNoTracking()
            where pago.SuscripcionPlataformaId != null
            join suscripcion in db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
                on pago.SuscripcionPlataformaId equals (Guid?)suscripcion.Id
            join organizacion in db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
                on suscripcion.OrganizacionId equals organizacion.Id
            join plan in db.Set<PlanPlataforma>().IgnoreQueryFilters().AsNoTracking()
                on suscripcion.PlanId equals plan.Id
            join medio in db.Set<MedioPago>().IgnoreQueryFilters().AsNoTracking()
                on pago.MedioPagoId equals (Guid?)medio.Id into medios
            from medio in medios.DefaultIfEmpty()
            orderby pago.CreadoEn descending
            select new { pago, organizacion, plan, medio };

        var resultados = await consulta.Take(500).ToListAsync(ct);
        return resultados.Select(fila => new PagoPlataformaAdministracion(
            fila.pago.Id,
            fila.pago.CreadoEn,
            fila.organizacion.Nombre,
            NombrePlan(fila.pago.Descripcion),
            fila.pago.Monto,
            "GTQ",
            fila.medio is null ? null : $"{fila.medio.Marca} •••• {fila.medio.Ultimos4}",
            EstadoVisible(fila.pago.Estado),
            fila.pago.PeriodoInicio,
            fila.pago.PeriodoFin)).ToArray();
    }

    public async Task<ResultadoReversionPago> Revertir(Guid id, Guid actorId, string actorNombre,
        Guid organizacionPlataforma, string? ip, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var pago = await db.Set<Pago>().FromSqlInterpolated($"SELECT * FROM pago WHERE id = {id} FOR UPDATE")
            .IgnoreQueryFilters().SingleOrDefaultAsync(ct);
        if (pago is null || pago.SuscripcionPlataformaId is null)
        {
            return ResultadoReversionPago.NoEncontrado;
        }
        if (pago.Estado != EstadoPago.Autorizado || string.IsNullOrWhiteSpace(pago.ReferenciaPasarela))
        {
            return ResultadoReversionPago.NoAutorizado;
        }

        var reembolso = await pasarela.ReembolsarAsync(pago.ReferenciaPasarela);
        if (!reembolso.Exitoso)
        {
            return ResultadoReversionPago.PasarelaNoDisponible;
        }

        var ahora = reloj.Ahora;
        pago.Revertir(actorId, ahora);
        var suscripcionId = pago.SuscripcionPlataformaId.Value;
        var suscripcion = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.Id == suscripcionId, ct);
        var afectaCicloVigente = suscripcion is not null
            && pago.PeriodoInicio is not null
            && pago.PeriodoFin is not null
            && suscripcion.Inicio == pago.PeriodoInicio
            && suscripcion.Fin == pago.PeriodoFin
            && pago.PeriodoInicio <= ahora
            && ahora < pago.PeriodoFin;
        if (afectaCicloVigente)
        {
            suscripcion!.EntrarEnGraciaHasta(ahora.AddDays(7), ahora);
        }

        await bitacora.Registrar(new Shapi.Aplicacion.Comun.EntradaBitacora(TipoActor.Usuario, actorId, actorNombre, organizacionPlataforma,
            AccionesBitacora.PagoRevertido, $"Revirtió el pago de plataforma {pago.Id} por Q {pago.Monto:0.00}.")
        {
            ObjetivoTipo = "pago",
            ObjetivoId = pago.Id,
            Detalle = new { pago.Monto, afectaCicloVigente },
            Ip = ip,
        }, ct);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        if (afectaCicloVigente)
        {
            await publicador.PublicarSuscripcion(suscripcionId, ct);
        }

        return ResultadoReversionPago.Exito;
    }

    private static string EstadoVisible(EstadoPago estado) => estado switch
    {
        EstadoPago.Autorizado => "pagado",
        EstadoPago.Rechazado => "rechazado",
        EstadoPago.Revertido => "revertido",
        _ => throw new ArgumentOutOfRangeException(nameof(estado)),
    };

    private static string NombrePlan(string descripcion)
    {
        const string prefijoContratacion = "Contratación del plan ";
        const string prefijoReactivacion = "Reactivación del plan ";
        const string prefijoRenovacion = "Renovación · ";
        if (descripcion.StartsWith(prefijoContratacion, StringComparison.Ordinal))
        {
            return descripcion[prefijoContratacion.Length..];
        }
        if (descripcion.StartsWith(prefijoReactivacion, StringComparison.Ordinal))
        {
            return descripcion[prefijoReactivacion.Length..];
        }
        if (descripcion.StartsWith(prefijoRenovacion, StringComparison.Ordinal))
        {
            return descripcion[prefijoRenovacion.Length..];
        }

        var separador = descripcion.IndexOf('·');
        var cambioPlan = separador >= 0 ? descripcion[..separador].Trim() : descripcion;
        var flecha = cambioPlan.LastIndexOf('→');
        return flecha >= 0 ? cambioPlan[(flecha + 1)..].Trim() : cambioPlan;
    }
}
