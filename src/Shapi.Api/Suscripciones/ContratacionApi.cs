using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    IServicioClaves servicioClaves,
    ILogger<ContratacionApi> registro)
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

        var plan = await db.Set<PlanApi>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.Id == peticion.PlanId && p.ApiId == portal.ApiId, cancelacion);
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
        if (!await BloquearConsumidor(consumidorId, cancelacion))
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
                return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Este plan requiere una tarjeta.",
                    new Dictionary<string, string[]> { ["tarjeta"] = ["Escriba los datos de la tarjeta."] });
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

            // convenciones §5: datos_invalidos es 400, con el error solo en el campo que falló.
            var errores = new Dictionary<string, string[]>();
            var titular = tokenizada.Titular ?? peticion.Tarjeta.Titular;
            if (string.IsNullOrWhiteSpace(titular))
            {
                errores["tarjeta.titular"] = ["Escriba el nombre del titular."];
            }

            if (!int.TryParse(peticion.Tarjeta.MesVencimiento, out var mes) || mes is < 1 or > 12
                || !int.TryParse(peticion.Tarjeta.AnioVencimiento, out var anio) || anio is < 0 or > 9999)
            {
                errores["tarjeta.vencimiento"] = ["Revise el mes y el año de vencimiento."];
                mes = anio = 0;
            }
            else if (anio < 100)
            {
                anio += 2000;
            }

            if (errores.Count > 0)
            {
                await transaccion.RollbackAsync(cancelacion);
                return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos,
                    "Los datos de vencimiento o titular de la tarjeta no son válidos.", errores);
            }

            medioPago = MedioPago.CrearParaConsumidor(consumidorId, tokenizada.Token!, tokenizada.Marca!, tokenizada.Ultimos4!,
                titular, mes, anio);

            var cobro = await pasarela.CobrarAsync(tokenizada.Token!, plan.Precio, $"ct_sim_{Guid.NewGuid():N}", esRenovacion: false);
            if (!cobro.Exitoso && cobro.Error == CodigosError.PasarelaNoDisponible)
            {
                // Igual que en la tokenización: la pasarela no respondió, así que no hubo un rechazo que registrar.
                await transaccion.RollbackAsync(cancelacion);
                return RespuestaErrorTarjeta(CodigosError.PasarelaNoDisponible);
            }

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
            db.Add(medioPago);
        }

        var inicio = SuscripcionApi.InicioDeCiclo(reloj.Ahora);
        var fin = inicio.AddDays(plan.VigenciaDias);
        var suscripcion = SuscripcionApi.Crear(consumidorId, portal.ApiId, plan.Id, inicio, fin, medioPago?.Id);
        IReadOnlyList<ClaveEmitida> claves;
        try
        {
            db.Add(suscripcion);
            if (medioPago is not null)
            {
                db.Add(Pago.ContratacionAutorizada(suscripcion.Id, medioPago.Id, plan.Precio, $"Contratación del plan {plan.Nombre}", referencia!, inicio, fin));
            }

            await db.SaveChangesAsync(cancelacion);
            claves = await servicioClaves.PrepararClavesParaSuscripcion(suscripcion.Id, cancelacion);
            await transaccion.CommitAsync(cancelacion);
        }
        catch (Exception ex) when (referencia is not null)
        {
            // El cobro ya se autorizó, pero no quedó registrado: se reembolsa para no cobrar sin suscripción.
            try
            {
                var reembolso = await pasarela.ReembolsarAsync(referencia);
                if (!reembolso.Exitoso)
                {
                    registro.LogError(ex, "No se pudo reembolsar el cobro {Referencia} de una contratación que falló: {Error}",
                        referencia, reembolso.Error);
                }
            }
            catch (Exception errorReembolso)
            {
                registro.LogError(new AggregateException(ex, errorReembolso),
                    "No se pudo reembolsar el cobro {Referencia} de una contratación que falló", referencia);
            }

            throw;
        }

        await publicador.PublicarSuscripcion(suscripcion.Id, cancelacion);
        foreach (var clave in claves)
        {
            await publicador.PublicarClave(clave.Id, cancelacion);
        }

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

    private async Task<bool> BloquearConsumidor(Guid consumidorId, CancellationToken cancelacion)
    {
        var comando = db.Database.GetDbConnection().CreateCommand();
        await using (comando)
        {
            comando.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            comando.CommandText = "SELECT id FROM consumidor WHERE id = @consumidorId FOR UPDATE";
            var parametro = comando.CreateParameter();
            parametro.ParameterName = "consumidorId";
            parametro.Value = consumidorId;
            comando.Parameters.Add(parametro);
            await using var lector = await comando.ExecuteReaderAsync(cancelacion);
            return await lector.ReadAsync(cancelacion);
        }
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
