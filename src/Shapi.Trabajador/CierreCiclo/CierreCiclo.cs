using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;
using AplicacionEntradaBitacora = Shapi.Aplicacion.Comun.EntradaBitacora;

namespace Shapi.Trabajador.CierreCiclo;

public sealed class OpcionesCierreCiclo
{
    public TimeSpan Intervalo { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan DiasGracia { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan DiasAvisoPrueba { get; init; } = TimeSpan.FromDays(7);
    public string DominioBase { get; init; } = "shapi.localhost";
}

/// <summary>Renueva y suspende suscripciones de plataforma y API (06 §5.5).</summary>
public sealed class CerrarCiclo(
    ShapiDbContext db,
    IReloj reloj,
    IPasarelaPagos pasarela,
    IPublicadorCache publicador,
    IColaCorreo correo,
    IBitacora bitacora,
    OpcionesCierreCiclo opciones)
{
    private readonly List<Guid> _publicarDespuesDelCommit = [];
    private string? _cobroPendiente;

    public async Task EjecutarAsync(CancellationToken cancelacion = default)
    {
        _publicarDespuesDelCommit.Clear();
        await RenovarPlataformaAsync(cancelacion);
        await RenovarApiAsync(cancelacion);
        await SuspenderPlataformaAsync(cancelacion);
        await SuspenderApiAsync(cancelacion);
        await AvisarPruebasAsync(cancelacion);
    }

    private async Task RenovarPlataformaAsync(CancellationToken ct)
    {
        var ahora = reloj.Ahora;
        var ids = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.Estado == EstadoSuscripcion.Activa && s.Fin <= ahora)
            .OrderBy(s => s.Fin).ThenBy(s => s.Id).Select(s => s.Id).ToListAsync(ct);
        foreach (var id in ids)
        {
            await ConfirmarRenovacionAsync(async () =>
            {
                var suscripcion = await db.Set<SuscripcionPlataforma>()
                    .FromSqlInterpolated($"SELECT * FROM suscripcion_plataforma WHERE id = {id} AND estado = 'activa' AND fin <= {ahora} FOR UPDATE SKIP LOCKED")
                    .IgnoreQueryFilters().SingleOrDefaultAsync(ct);
                if (suscripcion is null)
                {
                    return;
                }

                if (suscripcion.PlanSiguienteId is Guid siguiente)
                {
                    var planSiguiente = await db.Set<PlanPlataforma>().IgnoreQueryFilters().SingleAsync(x => x.Id == siguiente, ct);
                    suscripcion.AplicarPlanProgramado(siguiente, ahora);
                    if (planSiguiente.EsPrueba)
                    {
                        suscripcion.EntrarEnGracia(opciones.DiasGracia, ahora);
                        await EncolarCorreoPlataformaAsync(suscripcion, "suscripcion_en_gracia", planSiguiente.Nombre, ct);
                        _publicarDespuesDelCommit.Add(suscripcion.Id);
                    }
                    else if (planSiguiente.Precio <= 0)
                    {
                        suscripcion.Renovar(planSiguiente.VigenciaDias, ahora);
                        _publicarDespuesDelCommit.Add(suscripcion.Id);
                    }
                    else
                    {
                        await CobrarPlataformaAsync(suscripcion, planSiguiente, ahora, ct);
                    }
                    return;
                }

                var plan = await db.Set<PlanPlataforma>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.PlanId, ct);
                if (plan.EsPrueba)
                {
                    suscripcion.EntrarEnGracia(opciones.DiasGracia, ahora);
                    await EncolarCorreoPlataformaAsync(suscripcion, "suscripcion_en_gracia", plan.Nombre, ct);
                    _publicarDespuesDelCommit.Add(suscripcion.Id);
                }
                else if (plan.Precio <= 0)
                {
                    suscripcion.Renovar(plan.VigenciaDias, ahora);
                    _publicarDespuesDelCommit.Add(suscripcion.Id);
                }
                else
                {
                    await CobrarPlataformaAsync(suscripcion, plan, ahora, ct);
                }
            }, ct);
        }
    }

    private async Task CobrarPlataformaAsync(SuscripcionPlataforma suscripcion, PlanPlataforma plan, DateTimeOffset ahora, CancellationToken ct)
    {
        if (suscripcion.MedioPagoId is not Guid medioId)
        {
            db.Add(Pago.PlataformaRechazado(suscripcion.Id, ConceptoPago.Renovacion, plan.Precio,
                $"Renovación · {plan.Nombre}", "medio_pago_no_registrado"));
            suscripcion.EntrarEnGracia(opciones.DiasGracia, ahora);
            await EncolarCorreoPlataformaAsync(suscripcion, "pago_rechazado", plan.Nombre, ct);
            await EncolarCorreoPlataformaAsync(suscripcion, "suscripcion_en_gracia", plan.Nombre, ct);
            _publicarDespuesDelCommit.Add(suscripcion.Id);
            return;
        }

        var medio = await db.Set<MedioPago>().IgnoreQueryFilters().SingleAsync(x => x.Id == medioId, ct);
        var finAnterior = suscripcion.Fin;
        var finNuevo = finAnterior.AddDays(plan.VigenciaDias);
        var referencia = $"renovacion:{suscripcion.Id}:{finAnterior.ToUnixTimeSeconds()}";
        var resultado = await pasarela.CobrarAsync(medio.TokenPasarela, plan.Precio, referencia, esRenovacion: true);
        if (resultado.Exitoso)
        {
            _cobroPendiente = resultado.Referencia;
            db.Add(Pago.PlataformaAutorizado(suscripcion.Id, medio.Id, ConceptoPago.Renovacion, plan.Precio,
                $"Renovación · {plan.Nombre}", resultado.Referencia!, finAnterior, finNuevo));
            suscripcion.Renovar(plan.VigenciaDias, ahora);
        }
        else
        {
            db.Add(Pago.PlataformaRechazado(suscripcion.Id, ConceptoPago.Renovacion, plan.Precio,
                $"Renovación · {plan.Nombre}", resultado.Error ?? "pago_rechazado"));
            suscripcion.EntrarEnGracia(opciones.DiasGracia, ahora);
            await EncolarCorreoPlataformaAsync(suscripcion, "pago_rechazado", plan.Nombre, ct);
            await EncolarCorreoPlataformaAsync(suscripcion, "suscripcion_en_gracia", plan.Nombre, ct);
        }

        _publicarDespuesDelCommit.Add(suscripcion.Id);
    }

    private async Task RenovarApiAsync(CancellationToken ct)
    {
        var ahora = reloj.Ahora;
        var ids = await db.Set<SuscripcionApi>().IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.Estado == EstadoSuscripcion.Activa && s.Fin <= ahora)
            .OrderBy(s => s.Fin).ThenBy(s => s.Id).Select(s => s.Id).ToListAsync(ct);
        foreach (var id in ids)
        {
            await ConfirmarRenovacionAsync(async () =>
            {
                var suscripcion = await db.Set<SuscripcionApi>()
                    .FromSqlInterpolated($"SELECT * FROM suscripcion_api WHERE id = {id} AND estado = 'activa' AND fin <= {ahora} FOR UPDATE SKIP LOCKED")
                    .IgnoreQueryFilters().SingleOrDefaultAsync(ct);
                if (suscripcion is null)
                {
                    return;
                }

                if (suscripcion.PlanSiguienteId is Guid siguiente)
                {
                    suscripcion.AplicarPlanProgramado(siguiente, ahora);
                }

                var plan = await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.PlanId, ct);
                if (plan.EsGratuito || plan.Precio <= 0)
                {
                    suscripcion.Renovar(plan.VigenciaDias, ahora);
                    _publicarDespuesDelCommit.Add(suscripcion.Id);
                }
                else
                {
                    await CobrarApiAsync(suscripcion, plan, ahora, ct);
                }
            }, ct);
        }
    }

    private async Task CobrarApiAsync(SuscripcionApi suscripcion, PlanApi plan, DateTimeOffset ahora, CancellationToken ct)
    {
        var finAnterior = suscripcion.Fin;
        if (suscripcion.MedioPagoId is not Guid medioId)
        {
            db.Add(Pago.ApiRechazado(suscripcion.Id, ConceptoPago.Renovacion, plan.Precio,
                $"Renovación · {plan.Nombre}", "medio_pago_no_registrado"));
            suscripcion.EntrarEnGracia(opciones.DiasGracia, ahora);
            await EncolarCorreoApiAsync(suscripcion, "pago_rechazado", plan.Nombre, ct);
            await EncolarCorreoApiAsync(suscripcion, "suscripcion_en_gracia", plan.Nombre, ct);
            _publicarDespuesDelCommit.Add(suscripcion.Id);
            return;
        }

        var medio = await db.Set<MedioPago>().IgnoreQueryFilters().SingleAsync(x => x.Id == medioId, ct);
        var finNuevo = finAnterior.AddDays(plan.VigenciaDias);
        var referencia = $"renovacion:{suscripcion.Id}:{finAnterior.ToUnixTimeSeconds()}";
        var resultado = await pasarela.CobrarAsync(medio.TokenPasarela, plan.Precio, referencia, esRenovacion: true);
        if (resultado.Exitoso)
        {
            _cobroPendiente = resultado.Referencia;
            db.Add(Pago.ApiAutorizado(suscripcion.Id, medio.Id, ConceptoPago.Renovacion, plan.Precio,
                $"Renovación · {plan.Nombre}", resultado.Referencia!, finAnterior, finNuevo));
            suscripcion.Renovar(plan.VigenciaDias, ahora);
        }
        else
        {
            db.Add(Pago.ApiRechazado(suscripcion.Id, ConceptoPago.Renovacion, plan.Precio,
                $"Renovación · {plan.Nombre}", resultado.Error ?? "pago_rechazado"));
            suscripcion.EntrarEnGracia(opciones.DiasGracia, ahora);
            await EncolarCorreoApiAsync(suscripcion, "pago_rechazado", plan.Nombre, ct);
            await EncolarCorreoApiAsync(suscripcion, "suscripcion_en_gracia", plan.Nombre, ct);
        }

        _publicarDespuesDelCommit.Add(suscripcion.Id);
    }

    private async Task ConfirmarRenovacionAsync(Func<Task> renovar, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        _cobroPendiente = null;
        try
        {
            await renovar();
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            if (_cobroPendiente is not null)
            {
                await pasarela.ReembolsarAsync(_cobroPendiente);
            }

            _publicarDespuesDelCommit.Clear();
            throw;
        }
        finally
        {
            _cobroPendiente = null;
            db.ChangeTracker.Clear();
        }
        await PublicarCambiosAsync(ct);
    }

    private async Task SuspenderPlataformaAsync(CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var ahora = reloj.Ahora;
        var suscripciones = await db.Set<SuscripcionPlataforma>()
            .FromSqlInterpolated($"SELECT * FROM suscripcion_plataforma WHERE estado = 'en_gracia' AND gracia_hasta <= {ahora} FOR UPDATE SKIP LOCKED")
            .IgnoreQueryFilters().ToListAsync(ct);
        foreach (var suscripcion in suscripciones)
        {
            suscripcion.Suspender(ahora);
            var org = await db.Set<Organizacion>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.OrganizacionId, ct);
            var plan = await db.Set<PlanPlataforma>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.PlanId, ct);
            await EncolarCorreoPlataformaAsync(suscripcion, "suscripcion_suspendida", plan.Nombre, ct);
            await bitacora.Registrar(new AplicacionEntradaBitacora(TipoActor.Sistema, null, "Sistema", org.Id,
                AccionesBitacora.SuscripcionSuspendida, $"Suspendió por falta de pago la suscripción de {org.Nombre}.")
            { ObjetivoTipo = "suscripcion_plataforma", ObjetivoId = suscripcion.Id }, ct);
            _publicarDespuesDelCommit.Add(suscripcion.Id);
        }
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        await PublicarCambiosAsync(ct);
    }

    private async Task SuspenderApiAsync(CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var ahora = reloj.Ahora;
        var suscripciones = await db.Set<SuscripcionApi>()
            .FromSqlInterpolated($"SELECT * FROM suscripcion_api WHERE estado = 'en_gracia' AND gracia_hasta <= {ahora} FOR UPDATE SKIP LOCKED")
            .IgnoreQueryFilters().ToListAsync(ct);
        foreach (var suscripcion in suscripciones)
        {
            suscripcion.Suspender(ahora);
            var api = await db.Set<Api>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.ApiId, ct);
            var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.ConsumidorId, ct);
            var plan = await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.PlanId, ct);
            await EncolarCorreoApiAsync(suscripcion, "suscripcion_suspendida", plan.Nombre, ct);
            await bitacora.Registrar(new AplicacionEntradaBitacora(TipoActor.Sistema, null, "Sistema", api.OrganizacionId,
                AccionesBitacora.SuscripcionSuspendida, $"Suspendió por falta de pago la suscripción de {consumidor.Nombre} en {api.Nombre}.")
            { ObjetivoTipo = "suscripcion_api", ObjetivoId = suscripcion.Id }, ct);
            _publicarDespuesDelCommit.Add(suscripcion.Id);
        }
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        await PublicarCambiosAsync(ct);
    }

    private async Task PublicarCambiosAsync(CancellationToken ct)
    {
        foreach (var id in _publicarDespuesDelCommit.Distinct())
        {
            await publicador.PublicarSuscripcion(id, ct);
        }

        _publicarDespuesDelCommit.Clear();
    }

    private async Task AvisarPruebasAsync(CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var ahora = reloj.Ahora;
        var candidatas = await db.Set<SuscripcionPlataforma>()
            .FromSqlInterpolated($"""
                SELECT s.* FROM suscripcion_plataforma s
                WHERE s.estado = 'activa' AND s.fin > {ahora} AND s.fin <= {ahora + opciones.DiasAvisoPrueba}
                  AND EXISTS (SELECT 1 FROM plan_plataforma p WHERE p.id = s.plan_id AND p.es_prueba)
                FOR UPDATE OF s SKIP LOCKED
                """)
            .IgnoreQueryFilters().ToListAsync(ct);
        foreach (var suscripcion in candidatas)
        {
            var yaEncolado = await db.Set<Shapi.Dominio.Correo.CorreoSaliente>().IgnoreQueryFilters()
                .AnyAsync(x => x.Plantilla == "prueba_por_vencer" && EF.Functions.JsonContains(x.Datos, $"{{\"suscripcionId\":\"{suscripcion.Id}\"}}"), ct);
            if (yaEncolado)
            {
                continue;
            }

            var propietario = await CorreoPropietarioAsync(suscripcion.OrganizacionId, ct);
            if (propietario is null)
            {
                continue;
            }

            await correo.Encolar("prueba_por_vencer", propietario.Correo, new
            {
                nombre = propietario.Nombre,
                fechaFin = Fecha(suscripcion.Fin),
                enlace = $"https://{opciones.DominioBase}/panel/suscripcion",
                suscripcionId = suscripcion.Id.ToString(),
            }, ct);
        }
        await transaccion.CommitAsync(ct);
    }

    private async Task EncolarCorreoPlataformaAsync(SuscripcionPlataforma suscripcion, string plantilla, string nombrePlan, CancellationToken ct)
    {
        var propietario = await CorreoPropietarioAsync(suscripcion.OrganizacionId, ct);
        if (propietario is null)
        {
            return;
        }

        object datos = plantilla switch
        {
            "pago_rechazado" => new { nombre = propietario.Nombre, nombrePlan, motivo = "No pudimos procesar el cobro de renovación.", enlace = $"https://{opciones.DominioBase}/panel/suscripcion" },
            "suscripcion_en_gracia" => new { nombre = propietario.Nombre, nombrePlan, fechaSuspension = Fecha(suscripcion.GraciaHasta ?? suscripcion.Fin), enlace = $"https://{opciones.DominioBase}/panel/suscripcion" },
            _ => new { nombre = propietario.Nombre, nombrePlan, fechaSuspension = "", enlace = $"https://{opciones.DominioBase}/panel/suscripcion" },
        };
        await correo.Encolar(plantilla, propietario.Correo, datos, ct);
    }

    private async Task EncolarCorreoApiAsync(SuscripcionApi suscripcion, string plantilla, string nombrePlan, CancellationToken ct)
    {
        var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.ConsumidorId, ct);
        var api = await db.Set<Api>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.ApiId, ct);
        object datos = plantilla switch
        {
            "pago_rechazado" => new { nombre = consumidor.Nombre, nombrePlan, motivo = "No pudimos procesar el cobro de renovación.", enlace = $"https://{api.Subdominio}.{opciones.DominioBase}/cuenta/suscripcion", hostPortal = $"{api.Subdominio}.{opciones.DominioBase}", nombrePortal = api.PortalNombre ?? api.Nombre, colorPortal = api.PortalColor, logoPortal = api.PortalLogo is null ? null : "true" },
            "suscripcion_en_gracia" => new { nombre = consumidor.Nombre, nombrePlan, fechaSuspension = Fecha(suscripcion.GraciaHasta ?? suscripcion.Fin), enlace = $"https://{api.Subdominio}.{opciones.DominioBase}/cuenta/suscripcion", hostPortal = $"{api.Subdominio}.{opciones.DominioBase}", nombrePortal = api.PortalNombre ?? api.Nombre, colorPortal = api.PortalColor, logoPortal = api.PortalLogo is null ? null : "true" },
            _ => new { nombre = consumidor.Nombre, nombrePlan, enlace = $"https://{api.Subdominio}.{opciones.DominioBase}/cuenta/suscripcion", hostPortal = $"{api.Subdominio}.{opciones.DominioBase}", nombrePortal = api.PortalNombre ?? api.Nombre, colorPortal = api.PortalColor, logoPortal = api.PortalLogo is null ? null : "true" },
        };
        await correo.Encolar(plantilla, consumidor.Correo, datos, ct);
    }

    private Task<Usuario?> CorreoPropietarioAsync(Guid organizacionId, CancellationToken ct) =>
        (from m in db.Set<Membresia>().IgnoreQueryFilters()
         join u in db.Set<Usuario>().IgnoreQueryFilters() on m.UsuarioId equals u.Id
         where m.OrganizacionId == organizacionId && m.Rol == Rol.Propietario
         select u).SingleOrDefaultAsync(ct);

    private static string Fecha(DateTimeOffset fecha)
    {
        var fechaGuatemala = fecha.ToOffset(TimeSpan.FromHours(-6));
        var meses = new[] { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
        return $"{fechaGuatemala.Day} de {meses[fechaGuatemala.Month - 1]} de {fechaGuatemala.Year}";
    }
}
