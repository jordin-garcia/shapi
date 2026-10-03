using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Claves;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Aplicacion.Portal;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Suscripciones;

public sealed record PeticionContratarPlan(Guid PlanId, TarjetaContratacion? Tarjeta);
public sealed record TarjetaContratacion(string Numero, string MesVencimiento, string AnioVencimiento, string Cvv, string Titular);

public sealed class ContratacionApi(
    ShapiDbContext db,
    IResolutorPortal resolutor,
    IPasarelaPagos pasarela,
    IReloj reloj,
    IPublicadorCache publicador,
    IServicioClaves servicioClaves)
{
    public async Task<IResult> Contratar(HttpContext http, [FromBody] PeticionContratarPlan peticion, CancellationToken cancelacion)
    {
        var portal = await resolutor.Resolver(http.Request.Host.Host, cancelacion);
        if (portal is null)
        {
            return TypedResults.NotFound();
        }

        if (!Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var consumidorId)
            || !Guid.TryParse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion), out var organizacionId)
            || organizacionId != portal.OrganizacionId)
        {
            return TypedResults.NotFound();
        }

        var consumidor = await db.Set<Consumidor>().SingleOrDefaultAsync(c => c.Id == consumidorId, cancelacion);
        if (consumidor is null)
        {
            return TypedResults.NotFound();
        }

        if (consumidor.CorreoVerificadoEn is null)
        {
            return Problemas.Crear(StatusCodes.Status422UnprocessableEntity, CodigosError.CorreoNoVerificado, "Verifique su correo antes de contratar un plan.");
        }

        var plan = await db.Set<PlanApi>().SingleOrDefaultAsync(p => p.Id == peticion.PlanId && p.ApiId == portal.ApiId, cancelacion);
        if (plan is null)
        {
            return TypedResults.NotFound();
        }

        if (!plan.Activo)
        {
            return Problemas.Crear(StatusCodes.Status422UnprocessableEntity, CodigosError.PlanNoEncontrado, "El plan no está activo.");
        }

        // Serializa dos intentos simultáneos del mismo consumidor para la misma API.
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var bloqueado = await db.Set<Consumidor>().FromSqlInterpolated($"SELECT * FROM consumidor WHERE id = {consumidorId} FOR UPDATE")
            .IgnoreQueryFilters().SingleOrDefaultAsync(cancelacion);
        if (bloqueado is null)
        {
            return TypedResults.NotFound();
        }

        var existente = await db.Set<SuscripcionApi>().AnyAsync(s => s.ConsumidorId == consumidorId && s.ApiId == portal.ApiId
            && s.Estado != EstadoSuscripcion.Finalizada, cancelacion);
        if (existente)
        {
            return Problemas.Crear(StatusCodes.Status409Conflict, CodigosError.SuscripcionExistente, "Ya tiene una suscripción vigente en esta API.");
        }

        MedioPago? medioPago = null;
        string? referencia = null;
        if (!plan.EsGratuito)
        {
            if (peticion.Tarjeta is null)
            {
                return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Este plan requiere una tarjeta.");
            }

            var tokenizada = await pasarela.TokenizarAsync(new DatosTarjeta
            {
                Numero = peticion.Tarjeta.Numero,
                MesVencimiento = peticion.Tarjeta.MesVencimiento,
                AnioVencimiento = peticion.Tarjeta.AnioVencimiento,
                Cvv = peticion.Tarjeta.Cvv,
                Titular = peticion.Tarjeta.Titular,
            });
            if (!tokenizada.Exitoso)
            {
                await transaccion.RollbackAsync(cancelacion);
                return RespuestaErrorTarjeta(tokenizada.Error!);
            }

            var cobro = await pasarela.CobrarAsync(tokenizada.Token!, plan.Precio, $"ct_sim_{Guid.NewGuid():N}", esRenovacion: false);
            if (!cobro.Exitoso)
            {
                db.Add(Pago.ContratacionRechazada(consumidorId, portal.ApiId, plan.Precio,
                    $"Contratación del plan {plan.Nombre}", cobro.Error ?? "pago_rechazado"));
                await db.SaveChangesAsync(cancelacion);
                await transaccion.CommitAsync(cancelacion);
                return TypedResults.Problem(statusCode: StatusCodes.Status402PaymentRequired, title: "La tarjeta fue rechazada.",
                    type: "about:blank", extensions: new Dictionary<string, object?>
                    {
                        ["codigo"] = CodigosError.PagoRechazado,
                        ["detalle"] = new { motivo = cobro.Error ?? "pago_rechazado" },
                    });
            }

            referencia = cobro.Referencia;
            medioPago = MedioPago.CrearParaConsumidor(consumidorId, tokenizada.Token!, tokenizada.Marca!, tokenizada.Ultimos4!,
                tokenizada.Titular ?? peticion.Tarjeta.Titular, int.Parse(peticion.Tarjeta.MesVencimiento), int.Parse(peticion.Tarjeta.AnioVencimiento));
            db.Add(medioPago);
        }

        var inicio = SuscripcionApi.InicioDeCiclo(reloj.Ahora);
        var fin = inicio.AddDays(plan.VigenciaDias);
        var suscripcion = SuscripcionApi.Crear(consumidorId, portal.ApiId, plan.Id, inicio, fin, medioPago?.Id);
        db.Add(suscripcion);
        if (medioPago is not null)
        {
            db.Add(Pago.ContratacionAutorizada(suscripcion.Id, medioPago.Id, plan.Precio, $"Contratación del plan {plan.Nombre}", referencia!, inicio, fin));
        }

        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);

        await publicador.PublicarSuscripcion(suscripcion.Id, cancelacion);
        var claves = await servicioClaves.EmitirClavesParaSuscripcion(suscripcion.Id, cancelacion);
        return TypedResults.Created("/api/portal/suscripcion", new { suscripcion = Vista(suscripcion, plan), claves });
    }

    public async Task<IResult> Consultar(HttpContext http, CancellationToken cancelacion)
    {
        var portal = await resolutor.Resolver(http.Request.Host.Host, cancelacion);
        if (portal is null)
        {
            return TypedResults.NotFound();
        }

        if (!Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var consumidorId)
            || !Guid.TryParse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion), out var organizacionId)
            || organizacionId != portal.OrganizacionId)
        {
            return TypedResults.NotFound();
        }

        var fila = await (from s in db.Set<SuscripcionApi>()
                          join p in db.Set<PlanApi>() on s.PlanId equals p.Id
                          join m0 in db.Set<MedioPago>() on s.MedioPagoId equals m0.Id into medios
                          from m in medios.DefaultIfEmpty()
                          where s.ConsumidorId == consumidorId && s.ApiId == portal.ApiId && s.Estado != EstadoSuscripcion.Finalizada
                          select new { Suscripcion = s, Plan = p, Medio = m }).SingleOrDefaultAsync(cancelacion);
        if (fila is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new
        {
            plan = new { id = fila.Plan.Id, nombre = fila.Plan.Nombre, precio = fila.Plan.Precio, moneda = "GTQ" },
            estado = Estado(fila.Suscripcion.Estado),
            periodo = new { inicio = fila.Suscripcion.Inicio, fin = fila.Suscripcion.Fin.AddDays(-1) },
            proximaRenovacion = fila.Suscripcion.Fin,
            tarjetaEnmascarada = fila.Medio is null ? null : $"{Marca(fila.Medio.Marca)} •••• {fila.Medio.Ultimos4}",
            cuotaLlamadas = fila.Plan.CuotaLlamadas,
            limiteMinuto = fila.Plan.LimiteMinuto,
        });
    }

    private static object Vista(SuscripcionApi suscripcion, PlanApi plan) => new
    {
        id = suscripcion.Id,
        apiId = suscripcion.ApiId,
        planId = suscripcion.PlanId,
        nombrePlan = plan.Nombre,
        estado = Estado(suscripcion.Estado),
        inicio = suscripcion.Inicio,
        fin = suscripcion.Fin,
        proximaRenovacion = suscripcion.Fin,
    };

    private static IResult RespuestaErrorTarjeta(string codigo)
    {
        var estado = codigo == CodigosError.PasarelaNoDisponible ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status422UnprocessableEntity;
        return Problemas.Crear(estado, codigo, "No se pudo procesar la tarjeta.");
    }

    private static string Estado(EstadoSuscripcion estado) => estado switch
    {
        EstadoSuscripcion.Activa => "activa",
        EstadoSuscripcion.EnGracia => "en_gracia",
        EstadoSuscripcion.Suspendida => "suspendida",
        _ => "finalizada",
    };

    private static string Marca(MarcaTarjeta marca) => marca switch
    {
        MarcaTarjeta.Visa => "Visa",
        MarcaTarjeta.Mastercard => "Mastercard",
        _ => "American Express",
    };
}
