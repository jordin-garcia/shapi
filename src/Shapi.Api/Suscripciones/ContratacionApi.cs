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
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Suscripciones;

public sealed record PeticionContratarPlan(Guid PlanId, TarjetaContratacion? Tarjeta);
public sealed record PeticionPagarSuscripcionApi(TarjetaContratacion? Tarjeta);
public sealed record PeticionCambiarPlanApi(Guid PlanId, TarjetaContratacion? Tarjeta);
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

        return TypedResults.Created("/api/portal/suscripcion", new
        {
            suscripcion = Vista(suscripcion, plan),
            medioPago = medioPago is null ? null : new { marca = Marca(medioPago.Marca), ultimos4 = medioPago.Ultimos4 },
            claves,
        });
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

        var organizacion = await db.Set<Organizacion>().AsNoTracking()
            .SingleAsync(x => x.Id == portal.OrganizacionId, cancelacion);
        var suscripcionPlataforma = await db.Set<SuscripcionPlataforma>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizacionId == portal.OrganizacionId && x.Estado != EstadoSuscripcion.Finalizada, cancelacion);
        var suscripcionApiEnGracia = fila.Suscripcion.Estado == EstadoSuscripcion.EnGracia ? fila.Suscripcion : null;
        var suscripcionPlataformaEnGracia = suscripcionApiEnGracia is null
            && suscripcionPlataforma?.Estado == EstadoSuscripcion.EnGracia ? suscripcionPlataforma : null;
        var graciaHasta = suscripcionApiEnGracia?.GraciaHasta ?? suscripcionPlataformaEnGracia?.GraciaHasta;
        var ultimoRechazoEn = graciaHasta is not null
            ? await db.Set<Pago>().AsNoTracking()
                .Where(x => ((suscripcionApiEnGracia != null && x.SuscripcionApiId == suscripcionApiEnGracia.Id)
                    || (suscripcionPlataformaEnGracia != null && x.SuscripcionPlataformaId == suscripcionPlataformaEnGracia.Id))
                    && (x.Estado == EstadoPago.Rechazado || x.Estado == EstadoPago.Revertido))
                .Select(x => x.Estado == EstadoPago.Revertido ? x.RevertidoEn : x.CreadoEn)
                .MaxAsync(cancelacion)
            : null;
        var diasRestantes = graciaHasta is not null
            ? Math.Max(0, (int)Math.Ceiling((graciaHasta.Value - reloj.Ahora).TotalDays))
            : 0;

        return TypedResults.Ok(new
        {
            plan = new { id = fila.Plan.Id, nombre = fila.Plan.Nombre, precio = fila.Plan.Precio, moneda = "GTQ" },
            estado = Estado(fila.Suscripcion.Estado),
            periodo = new { inicio = fila.Suscripcion.Inicio, fin = fila.Suscripcion.Fin.AddDays(-1) },
            proximaRenovacion = fila.Suscripcion.Fin,
            tarjetaEnmascarada = fila.Medio is null ? null : $"{Marca(fila.Medio.Marca)} •••• {fila.Medio.Ultimos4}",
            cuotaLlamadas = fila.Plan.CuotaLlamadas,
            limiteMinuto = fila.Plan.LimiteMinuto,
            apiDisponible = organizacion.EstadoAdmin != EstadoAdmin.Suspendida
                && suscripcionPlataforma?.Estado != EstadoSuscripcion.Suspendida,
            graciaHasta,
            ultimoRechazoEn,
            diasRestantes,
        });
    }

    public async Task<IResult> Pagar(HttpContext http, [FromBody] PeticionPagarSuscripcionApi peticion, CancellationToken ct)
    {
        var portal = await resolutor.Resolver(http.Request.Host.Host, ct);
        if (portal is null
            || !Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var consumidorId)
            || !Guid.TryParse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion), out var organizacionId)
            || organizacionId != portal.OrganizacionId)
        {
            return TypedResults.NotFound();
        }

        var consumidor = await db.Set<Consumidor>().SingleOrDefaultAsync(x => x.Id == consumidorId, ct);
        if (consumidor is null)
        {
            return TypedResults.NotFound();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await BloquearConsumidor(consumidorId, ct))
        {
            return TypedResults.NotFound();
        }

        var suscripcion = await db.Set<SuscripcionApi>().SingleOrDefaultAsync(x => x.ConsumidorId == consumidorId
            && x.ApiId == portal.ApiId && (x.Estado == EstadoSuscripcion.EnGracia || x.Estado == EstadoSuscripcion.Suspendida), ct);
        if (suscripcion is null)
        {
            return Problemas.Crear(StatusCodes.Status404NotFound, "suscripcion_no_encontrada", "No hay una suscripción que requiera pago.");
        }

        var plan = await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(x => x.Id == suscripcion.PlanId, ct);
        if (plan.EsGratuito || plan.Precio <= 0)
        {
            return Problemas.Crear(StatusCodes.Status422UnprocessableEntity, CodigosError.RequierePlanDePago,
                "Solo una suscripción de pago se puede reactivar con tarjeta.");
        }

        var tarjeta = peticion.Tarjeta;
        if (tarjeta is null)
        {
            await tx.RollbackAsync(ct);
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos,
                "Escriba los datos de la tarjeta.", new Dictionary<string, string[]> { ["tarjeta"] = ["Escriba los datos de la tarjeta."] });
        }

        var tokenizada = await pasarela.TokenizarAsync(new DatosTarjeta
        {
            Numero = tarjeta.Numero,
            MesVencimiento = tarjeta.MesVencimiento,
            AnioVencimiento = tarjeta.AnioVencimiento,
            Cvv = tarjeta.Cvv,
            Titular = tarjeta.Titular,
        });
        if (!tokenizada.Exitoso)
        {
            await tx.RollbackAsync(ct);
            return RespuestaErrorTarjeta(tokenizada.Error!);
        }

        var titular = tokenizada.Titular ?? tarjeta.Titular;
        if (string.IsNullOrWhiteSpace(titular) || !int.TryParse(tarjeta.MesVencimiento, out var mes) || mes is < 1 or > 12
            || !int.TryParse(tarjeta.AnioVencimiento, out var anio) || anio is < 0 or > 9999)
        {
            await tx.RollbackAsync(ct);
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos,
                "Los datos de vencimiento o titular de la tarjeta no son válidos.",
                new Dictionary<string, string[]> { [string.IsNullOrWhiteSpace(titular) ? "tarjeta.titular" : "tarjeta.vencimiento"] = ["Revise los datos de la tarjeta."] });
        }

        if (anio < 100)
        {
            anio += 2000;
        }

        var medio = MedioPago.CrearParaConsumidor(consumidorId, tokenizada.Token!, tokenizada.Marca!, tokenizada.Ultimos4!, titular, mes, anio);
        var cobro = await pasarela.CobrarAsync(tokenizada.Token!, plan.Precio, $"reactivacion:{suscripcion.Id}:{Guid.NewGuid():N}", esRenovacion: false);
        if (!cobro.Exitoso)
        {
            if (cobro.Error == CodigosError.PasarelaNoDisponible)
            {
                await tx.RollbackAsync(ct);
                return RespuestaErrorTarjeta(cobro.Error);
            }

            db.Add(Pago.ApiRechazado(suscripcion.Id, ConceptoPago.Reactivacion, plan.Precio,
                $"Reactivación del plan {plan.Nombre}", cobro.Error ?? CodigosError.PagoRechazado));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return TypedResults.Problem(statusCode: StatusCodes.Status402PaymentRequired, title: "La tarjeta fue rechazada.",
                type: "about:blank", extensions: new Dictionary<string, object?>
                {
                    ["codigo"] = CodigosError.PagoRechazado,
                    ["detalle"] = new { motivo = cobro.Error ?? CodigosError.PagoRechazado }
                });
        }

        var ahora = reloj.Ahora;
        try
        {
            db.Add(medio);
            suscripcion.Reactivar(plan.VigenciaDias, medio.Id, ahora);
            db.Add(Pago.ApiAutorizado(suscripcion.Id, medio.Id, ConceptoPago.Reactivacion, plan.Precio,
                $"Reactivación del plan {plan.Nombre}", cobro.Referencia!, suscripcion.Inicio, suscripcion.Fin));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            try { await pasarela.ReembolsarAsync(cobro.Referencia!); }
            catch (Exception error) { registro.LogError(new AggregateException(ex, error), "No se pudo reembolsar la reactivación API {SuscripcionId}", suscripcion.Id); }
            throw;
        }

        await publicador.PublicarSuscripcion(suscripcion.Id, ct);
        return TypedResults.Ok(new { estado = "activa", inicio = suscripcion.Inicio, fin = suscripcion.Fin });
    }

    public async Task<IResult> CambiarPlan(HttpContext http, [FromBody] PeticionCambiarPlanApi peticion, CancellationToken ct)
    {
        var portal = await resolutor.Resolver(http.Request.Host.Host, ct);
        if (portal is null
            || !Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var consumidorId)
            || !Guid.TryParse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion), out var organizacionId)
            || organizacionId != portal.OrganizacionId)
        {
            return TypedResults.NotFound();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await BloquearConsumidor(consumidorId, ct))
        {
            return TypedResults.NotFound();
        }

        var actual = await db.Set<SuscripcionApi>().SingleOrDefaultAsync(x => x.ConsumidorId == consumidorId
            && x.ApiId == portal.ApiId && x.Estado == EstadoSuscripcion.Activa, ct);
        if (actual is null)
        {
            return Problemas.Crear(StatusCodes.Status404NotFound, CodigosError.SuscripcionNoEncontrada,
                "No se encontró una suscripción activa.");
        }

        var origen = await db.Set<PlanApi>().IgnoreQueryFilters().SingleAsync(x => x.Id == actual.PlanId, ct);
        var destino = await db.Set<PlanApi>().SingleOrDefaultAsync(x => x.Id == peticion.PlanId
            && x.ApiId == portal.ApiId && x.Activo, ct);
        if (destino is null)
        {
            return Problemas.Crear(StatusCodes.Status404NotFound, CodigosError.PlanNoEncontrado,
                "No se encontró un plan activo de esta API.");
        }

        if (destino.Id == origen.Id)
        {
            return TypedResults.Ok(new { estado = "sin_cambios" });
        }

        var ahora = reloj.Ahora;
        var sube = destino.Precio / destino.VigenciaDias > origen.Precio / origen.VigenciaDias;
        if (!sube)
        {
            actual.ProgramarCambio(destino.Id, ahora);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            await publicador.PublicarSuscripcion(actual.Id, ct);
            return TypedResults.Ok(new { estado = "programado", planSiguienteId = destino.Id, efectivoDesde = actual.Fin });
        }

        var desdeGratuito = origen.EsGratuito || origen.Precio <= 0;
        var diasRestantes = Math.Max(0, (int)Math.Ceiling((actual.Fin - ahora).TotalDays));
        var prorrateo = desdeGratuito
            ? new ResultadoProrrateo(0, destino.Precio, destino.Precio)
            : ProrrateoCambioPlan.Calcular(origen.Precio, origen.VigenciaDias, destino.Precio, destino.VigenciaDias, diasRestantes);

        var medioRegistrado = actual.MedioPagoId is Guid medioId
            ? await db.Set<MedioPago>().SingleOrDefaultAsync(x => x.Id == medioId && x.ConsumidorId == consumidorId, ct)
            : null;
        MedioPago? medioNuevo = null;
        if (prorrateo.APagar > 0 && peticion.Tarjeta is not null)
        {
            var tarjeta = peticion.Tarjeta;
            var resultado = await pasarela.TokenizarAsync(new DatosTarjeta
            {
                Numero = tarjeta.Numero,
                MesVencimiento = tarjeta.MesVencimiento,
                AnioVencimiento = tarjeta.AnioVencimiento,
                Cvv = tarjeta.Cvv,
                Titular = tarjeta.Titular,
            });
            if (!resultado.Exitoso)
            {
                await tx.RollbackAsync(ct);
                return RespuestaErrorTarjeta(resultado.Error!);
            }

            var titular = resultado.Titular ?? tarjeta.Titular;
            if (string.IsNullOrWhiteSpace(titular)
                || !int.TryParse(tarjeta.MesVencimiento, out var mes) || mes is < 1 or > 12
                || !int.TryParse(tarjeta.AnioVencimiento, out var anio) || anio is < 0 or > 9999)
            {
                await tx.RollbackAsync(ct);
                return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos,
                    "Los datos de vencimiento o titular de la tarjeta no son válidos.",
                    new Dictionary<string, string[]> { [string.IsNullOrWhiteSpace(titular) ? "tarjeta.titular" : "tarjeta.vencimiento"] = ["Revise los datos de la tarjeta."] });
            }

            if (anio < 100)
            {
                anio += 2000;
            }
            medioNuevo = MedioPago.CrearParaConsumidor(consumidorId, resultado.Token!, resultado.Marca!, resultado.Ultimos4!, titular, mes, anio);
        }
        else if (prorrateo.APagar > 0 && medioRegistrado is null)
        {
            await tx.RollbackAsync(ct);
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos,
                "Escriba los datos de la tarjeta.", new Dictionary<string, string[]> { ["tarjeta"] = ["Escriba los datos de la tarjeta."] });
        }

        ResultadoCobro? cobro = null;
        if (prorrateo.APagar > 0)
        {
            var token = medioNuevo?.TokenPasarela ?? medioRegistrado!.TokenPasarela;
            cobro = await pasarela.CobrarAsync(token, prorrateo.APagar, $"cambio:{actual.Id}:{Guid.NewGuid():N}", esRenovacion: false);
            if (!cobro.Exitoso)
            {
                if (cobro.Error == CodigosError.PasarelaNoDisponible)
                {
                    await tx.RollbackAsync(ct);
                    return RespuestaErrorTarjeta(cobro.Error);
                }

                db.Add(Pago.ApiRechazado(actual.Id, ConceptoPago.CambioPlan, prorrateo.APagar,
                    $"{origen.Nombre} → {destino.Nombre} · diferencia prorrateada", cobro.Error ?? CodigosError.PagoRechazado));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return TypedResults.Problem(statusCode: StatusCodes.Status402PaymentRequired, title: "La tarjeta fue rechazada.",
                    type: "about:blank", extensions: new Dictionary<string, object?>
                    {
                        ["codigo"] = CodigosError.PagoRechazado,
                        ["detalle"] = new { motivo = cobro.Error ?? CodigosError.PagoRechazado },
                    });
            }
        }

        var medioAplicado = medioNuevo ?? medioRegistrado;
        var nuevoInicio = desdeGratuito || origen.VigenciaDias != destino.VigenciaDias
            ? SuscripcionApi.InicioDeCiclo(ahora)
            : (DateTimeOffset?)null;
        var referencia = cobro?.Referencia;
        SuscripcionApi? reemplazo = null;
        IReadOnlyList<ClaveEmitida> claves = [];
        try
        {
            if (medioNuevo is not null)
            {
                db.Add(medioNuevo);
            }

            if (desdeGratuito)
            {
                actual.Finalizar(ahora);
                reemplazo = SuscripcionApi.Crear(consumidorId, portal.ApiId, destino.Id,
                    nuevoInicio!.Value, nuevoInicio.Value.AddDays(destino.VigenciaDias), medioAplicado!.Id);
                db.Add(reemplazo);
            }
            else
            {
                actual.CambiarPlan(destino.Id, medioAplicado?.Id, nuevoInicio, destino.VigenciaDias, ahora);
            }

            var suscripcionPago = reemplazo ?? actual;
            if (referencia is not null && medioAplicado is not null)
            {
                db.Add(Pago.ApiAutorizado(suscripcionPago.Id, medioAplicado.Id, ConceptoPago.CambioPlan,
                    prorrateo.APagar, $"{origen.Nombre} → {destino.Nombre} · diferencia prorrateada", referencia,
                    suscripcionPago.Inicio, suscripcionPago.Fin));
            }
            await db.SaveChangesAsync(ct);
            if (reemplazo is not null)
            {
                claves = await servicioClaves.PrepararClavesParaSuscripcion(reemplazo.Id, ct);
            }

            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            if (referencia is not null)
            {
                try { await pasarela.ReembolsarAsync(referencia); }
                catch (Exception error) { registro.LogError(new AggregateException(ex, error), "No se pudo reembolsar el cambio de plan de API {SuscripcionId}", actual.Id); }
            }

            throw;
        }

        await publicador.PublicarSuscripcion(actual.Id, ct);
        if (reemplazo is not null)
        {
            await publicador.PublicarSuscripcion(reemplazo.Id, ct);
            foreach (var clave in claves)
            {
                await publicador.PublicarClave(clave.Id, ct);
            }
        }

        return TypedResults.Ok(new
        {
            estado = "activa",
            credito = prorrateo.Credito,
            cargo = prorrateo.Cargo,
            aPagar = prorrateo.APagar,
            inicio = (reemplazo ?? actual).Inicio,
            fin = (reemplazo ?? actual).Fin,
            claves,
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
