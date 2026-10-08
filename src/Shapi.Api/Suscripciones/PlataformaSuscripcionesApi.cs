using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Contratos;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Suscripciones;

public sealed record PeticionSuscripcionPlataforma(Guid PlanId, DatosTarjeta? Tarjeta = null, bool UsarRegistrada = false);
public sealed record PeticionPagoPlataforma(DatosTarjeta? Tarjeta = null, bool UsarRegistrada = false);
public sealed record RespuestaPlanPlataforma(Guid Id, string Nombre, string Descripcion, decimal Precio, string Moneda,
    int VigenciaDias, int? MaxApis, int? MaxMiembros, long CuotaPeticiones, bool DominioPropio, bool EsPrueba,
    DateTimeOffset InicioCicloPrevisto);

public sealed class PlataformaSuscripcionesApi(ShapiDbContext db, IReloj reloj, IPasarelaPagos pasarela,
    IPublicadorCache publicador, IBitacora bitacora)
{
    public async Task<IResult> ListarPlanes(CancellationToken ct)
    {
        var inicioCiclo = Suscripcion.InicioDeCiclo(reloj.Ahora);
        var planes = await db.Set<PlanPlataforma>().AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Orden)
            .Select(p => new RespuestaPlanPlataforma(p.Id, p.Nombre, p.Descripcion, p.Precio, "GTQ", p.VigenciaDias,
                p.MaxApis, p.MaxMiembros, p.CuotaPeticiones, p.DominioPropio, p.EsPrueba, inicioCiclo)).ToListAsync(ct);
        return TypedResults.Ok(planes);
    }

    public async Task<IResult> Consultar(HttpContext http, CancellationToken ct)
    {
        var orgId = OrganizacionId(http.User);
        var s = await db.Set<SuscripcionPlataforma>().SingleOrDefaultAsync(x => x.OrganizacionId == orgId && x.Estado != EstadoSuscripcion.Finalizada, ct);
        if (s is null)
        {
            return Error(404, "suscripcion_no_encontrada", "No se encontró una suscripción vigente.");
        }

        var plan = await db.Set<PlanPlataforma>().SingleAsync(x => x.Id == s.PlanId, ct);
        var siguiente = s.PlanSiguienteId is null ? null : await db.Set<PlanPlataforma>().SingleOrDefaultAsync(x => x.Id == s.PlanSiguienteId, ct);
        var medio = s.MedioPagoId is null ? null : await db.Set<MedioPago>().SingleOrDefaultAsync(x => x.Id == s.MedioPagoId, ct);
        var ultimoRechazo = await db.Set<Pago>().IgnoreQueryFilters().Where(x => x.SuscripcionPlataformaId == s.Id && x.Estado == EstadoPago.Rechazado)
            .OrderByDescending(x => x.CreadoEn).Select(x => (DateTimeOffset?)x.CreadoEn).FirstOrDefaultAsync(ct);
        var suscripcionesApi = db.Set<SuscripcionApi>().IgnoreQueryFilters().Where(x => x.Estado != EstadoSuscripcion.Finalizada);
        var consumidoresAfectados = await db.Set<Shapi.Dominio.Identidad.Consumidor>().IgnoreQueryFilters()
            .Where(c => c.OrganizacionId == orgId && suscripcionesApi.Any(api => api.ConsumidorId == c.Id)).CountAsync(ct);
        return TypedResults.Ok(new
        {
            plan = new
            {
                id = plan.Id,
                nombre = plan.Nombre,
                descripcion = plan.Descripcion,
                precio = plan.Precio,
                moneda = "GTQ",
                vigenciaDias = plan.VigenciaDias,
                maxApis = plan.MaxApis,
                maxMiembros = plan.MaxMiembros,
                cuotaPeticiones = plan.CuotaPeticiones,
                dominioPropio = plan.DominioPropio,
                esPrueba = plan.EsPrueba
            },
            estado = Estado(s.Estado),
            periodo = new { inicio = s.Inicio, fin = s.Fin.AddDays(-1) },
            proximaRenovacion = s.Fin,
            tarjetaEnmascarada = medio is null ? null : $"{NombreMarca(medio.Marca)} •••• {medio.Ultimos4}",
            graciaHasta = s.GraciaHasta,
            diasRestantesCiclo = Math.Max(0, (int)Math.Ceiling((s.Fin - reloj.Ahora).TotalDays)),
            diasRestantes = s.Estado == EstadoSuscripcion.EnGracia && s.GraciaHasta is not null
                ? Math.Max(0, (int)Math.Ceiling((s.GraciaHasta.Value - reloj.Ahora).TotalDays)) : 0,
            ultimoRechazoEn = ultimoRechazo,
            consumidoresAfectados,
            cambioProgramado = siguiente is null ? null : new { planId = siguiente.Id, nombre = siguiente.Nombre, efectivoDesde = s.Fin }
        });
    }

    public async Task<IResult> Contratar(HttpContext http, PeticionSuscripcionPlataforma p, CancellationToken ct)
    {
        var org = OrganizacionId(http.User);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({org.ToString()}, 0))", ct);
        var anterior = await db.Set<SuscripcionPlataforma>().SingleOrDefaultAsync(x => x.OrganizacionId == org && x.Estado != EstadoSuscripcion.Finalizada, ct);
        var plan = await db.Set<PlanPlataforma>().SingleOrDefaultAsync(x => x.Id == p.PlanId && x.Activo && !x.EsPrueba, ct);
        if (plan is null)
        {
            return Error(404, "plan_no_encontrado", "No se encontró el plan de plataforma.");
        }

        if (anterior is not null)
        {
            var vigente = await db.Set<PlanPlataforma>().SingleAsync(x => x.Id == anterior.PlanId, ct);
            if (!vigente.EsPrueba && vigente.Precio > 0)
            {
                return Error(409, "suscripcion_existente", "Use el flujo de cambio de plan.");
            }
        }
        var prep = await PrepararMedio(org, anterior, p.Tarjeta, p.UsarRegistrada, ct);
        if (prep.Error is not null)
        {
            return prep.Error;
        }

        var medio = prep.Medio!;
        var cobro = await pasarela.CobrarAsync(medio.TokenPasarela, plan.Precio, $"plataforma:{org}:{Guid.NewGuid():N}", false);
        if (!cobro.Exitoso)
        {
            if (anterior is not null && cobro.Error != CodigosError.PasarelaNoDisponible)
            {
                db.Add(Pago.PlataformaRechazado(anterior.Id, ConceptoPago.Contratacion, plan.Precio,
                    $"Contratación del plan {plan.Nombre}", cobro.Error ?? CodigosError.PagoRechazado));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }

            return Error(cobro.Error == CodigosError.PasarelaNoDisponible ? 503 : 402,
            cobro.Error == CodigosError.PasarelaNoDisponible ? cobro.Error : CodigosError.PagoRechazado,
            cobro.Error == CodigosError.PasarelaNoDisponible ? "La pasarela no está disponible." : "La tarjeta fue rechazada.",
            new { motivo = cobro.Error });
        }

        var ahora = reloj.Ahora;
        var nueva = SuscripcionPlataforma.Contratar(org, plan, medio.Id, ahora);
        try
        {
            if (prep.EsNueva)
            {
                db.Add(medio);
            }

            anterior?.Finalizar(ahora);
            db.Add(nueva);
            db.Add(Pago.PlataformaAutorizado(nueva.Id, medio.Id, ConceptoPago.Contratacion, plan.Precio,
                $"Contratación del plan {plan.Nombre}", cobro.Referencia!, nueva.Inicio, nueva.Fin));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await pasarela.ReembolsarAsync(cobro.Referencia!);
            throw;
        }
        await publicador.PublicarSuscripcion(nueva.Id, ct);
        await Registrar(http, org, nueva.Id, AccionesBitacora.SuscripcionPlataformaContratada, $"Contrató el plan {plan.Nombre}.", ct);
        return TypedResults.Created("/api/suscripcion", new
        {
            id = nueva.Id,
            plan = new { id = plan.Id, nombre = plan.Nombre, precio = plan.Precio, moneda = "GTQ", vigenciaDias = plan.VigenciaDias },
            estado = "activa",
            periodo = new { inicio = nueva.Inicio, fin = nueva.Fin.AddDays(-1) },
            proximaRenovacion = nueva.Fin,
            tarjetaEnmascarada = $"{NombreMarca(medio.Marca)} •••• {medio.Ultimos4}"
        });
    }

    public async Task<IResult> Cambiar(HttpContext http, PeticionSuscripcionPlataforma p, CancellationToken ct)
    {
        var org = OrganizacionId(http.User);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({org.ToString()}, 0))", ct);
        var ahora = reloj.Ahora;
        var actual = await db.Set<SuscripcionPlataforma>().SingleOrDefaultAsync(x => x.OrganizacionId == org && x.Estado == EstadoSuscripcion.Activa, ct);
        if (actual is null)
        {
            return Error(404, "suscripcion_no_encontrada", "No se encontró una suscripción activa.");
        }

        var origen = await db.Set<PlanPlataforma>().SingleAsync(x => x.Id == actual.PlanId, ct);
        var destino = await db.Set<PlanPlataforma>().SingleOrDefaultAsync(x => x.Id == p.PlanId && x.Activo && !x.EsPrueba, ct);
        if (destino is null)
        {
            return Error(404, "plan_no_encontrado", "No se encontró el plan de plataforma.");
        }

        if (destino.Id == origen.Id)
        {
            return TypedResults.Ok(new { estado = "sin_cambios" });
        }

        if (origen.EsPrueba || origen.Precio == 0)
        {
            var prepCompleto = await PrepararMedio(org, actual, p.Tarjeta, p.UsarRegistrada, ct);
            if (prepCompleto.Error is not null)
            {
                return prepCompleto.Error;
            }

            var medioCompleto = prepCompleto.Medio!;
            var pagoCompleto = await pasarela.CobrarAsync(medioCompleto.TokenPasarela, destino.Precio,
                $"plataforma:{org}:{Guid.NewGuid():N}", false);
            if (!pagoCompleto.Exitoso)
            {
                if (pagoCompleto.Error != CodigosError.PasarelaNoDisponible)
                {
                    db.Add(Pago.PlataformaRechazado(actual.Id, ConceptoPago.Contratacion, destino.Precio,
                        $"Contratación del plan {destino.Nombre}", pagoCompleto.Error ?? CodigosError.PagoRechazado));
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                }

                return Error(pagoCompleto.Error == CodigosError.PasarelaNoDisponible ? 503 : 402,
                pagoCompleto.Error == CodigosError.PasarelaNoDisponible ? pagoCompleto.Error : CodigosError.PagoRechazado,
                "No se autorizó el cobro.", new { motivo = pagoCompleto.Error });
            }

            var inicioCompleto = Suscripcion.InicioDeCiclo(ahora);
            var nuevaCompleta = SuscripcionPlataforma.Contratar(org, destino, medioCompleto.Id, ahora);
            try
            {
                if (prepCompleto.EsNueva)
                {
                    db.Add(medioCompleto);
                }

                actual.Finalizar(ahora);
                db.Add(nuevaCompleta);
                db.Add(Pago.PlataformaAutorizado(nuevaCompleta.Id, medioCompleto.Id, ConceptoPago.Contratacion,
                    destino.Precio, $"Contratación del plan {destino.Nombre}", pagoCompleto.Referencia!,
                    inicioCompleto, nuevaCompleta.Fin));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await pasarela.ReembolsarAsync(pagoCompleto.Referencia!);
                throw;
            }
            await publicador.PublicarSuscripcion(nuevaCompleta.Id, ct);
            await Registrar(http, org, nuevaCompleta.Id, AccionesBitacora.SuscripcionPlataformaContratada,
                $"Contrató el plan de plataforma {destino.Nombre}.", ct);
            return TypedResults.Ok(new
            {
                id = nuevaCompleta.Id,
                plan = new { id = destino.Id, nombre = destino.Nombre, precio = destino.Precio, moneda = "GTQ", vigenciaDias = destino.VigenciaDias },
                estado = "activa",
                periodo = new { inicio = nuevaCompleta.Inicio, fin = nuevaCompleta.Fin.AddDays(-1) },
                proximaRenovacion = nuevaCompleta.Fin,
                tarjetaEnmascarada = $"{NombreMarca(medioCompleto.Marca)} •••• {medioCompleto.Ultimos4}"
            });
        }
        if (destino.Precio / destino.VigenciaDias <= origen.Precio / origen.VigenciaDias)
        {
            var apis = await db.Set<Shapi.Dominio.Apis.Api>().IgnoreQueryFilters().CountAsync(x => x.OrganizacionId == org, ct);
            var miembros = await db.Set<Shapi.Dominio.Organizaciones.Membresia>().IgnoreQueryFilters().CountAsync(x => x.OrganizacionId == org, ct);
            var invitaciones = await db.Set<Shapi.Dominio.Identidad.Token>().IgnoreQueryFilters()
                .CountAsync(x => x.OrganizacionId == org && x.Tipo == Shapi.Dominio.Identidad.TipoToken.InvitacionMiembro
                    && x.UsadoEn == null && x.ExpiraEn > ahora, ct);
            miembros += invitaciones;
            if ((destino.MaxApis is not null && apis > destino.MaxApis) || (destino.MaxMiembros is not null && miembros > destino.MaxMiembros))
            {
                return Error(422, CodigosError.ExcedeLimitesDelPlan, "La organización excede los límites del plan de destino.");
            }

            actual.ProgramarCambio(destino.Id, ahora);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            await Registrar(http, org, actual.Id, AccionesBitacora.SuscripcionPlataformaCambiada, $"Programó el cambio a {destino.Nombre}.", ct);
            return TypedResults.Ok(new { estado = "programado", planSiguienteId = destino.Id, efectivoDesde = actual.Fin });
        }
        var d = Math.Max(0, (int)Math.Ceiling((actual.Fin - ahora).TotalDays));
        var valores = ProrrateoCambioPlan.Calcular(origen.Precio, origen.VigenciaDias, destino.Precio, destino.VigenciaDias, d);
        var prep = await PrepararMedio(org, actual, p.Tarjeta, p.UsarRegistrada, ct, valores.APagar > 0);
        if (prep.Error is not null)
        {
            return prep.Error;
        }

        string? referencia = null;
        if (valores.APagar > 0)
        {
            var cobro = await pasarela.CobrarAsync(prep.Medio!.TokenPasarela, valores.APagar, $"cambio:{actual.Id}:{Guid.NewGuid():N}", false);
            if (!cobro.Exitoso)
            {
                if (cobro.Error != CodigosError.PasarelaNoDisponible)
                {
                    db.Add(Pago.PlataformaRechazado(actual.Id, ConceptoPago.CambioPlan, valores.APagar,
                        $"{origen.Nombre} → {destino.Nombre} · diferencia prorrateada", cobro.Error ?? CodigosError.PagoRechazado));
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                }

                return Error(cobro.Error == CodigosError.PasarelaNoDisponible ? 503 : 402,
                cobro.Error == CodigosError.PasarelaNoDisponible ? cobro.Error : CodigosError.PagoRechazado,
                "No se autorizó el cambio de plan.", new { motivo = cobro.Error });
            }

            referencia = cobro.Referencia;
        }
        var medioId = prep.Medio?.Id ?? actual.MedioPagoId;
        try
        {
            if (prep.EsNueva)
            {
                db.Add(prep.Medio!);
            }

            var nuevoInicio = origen.VigenciaDias == destino.VigenciaDias ? (DateTimeOffset?)null : Suscripcion.InicioDeCiclo(ahora);
            actual.CambiarPlan(destino, medioId, nuevoInicio, ahora);
            if (referencia is not null)
            {
                db.Add(Pago.PlataformaAutorizado(actual.Id, medioId!.Value, ConceptoPago.CambioPlan,
                    valores.APagar, $"{origen.Nombre} → {destino.Nombre} · diferencia prorrateada", referencia, actual.Inicio, actual.Fin));
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            if (referencia is not null)
            {
                await pasarela.ReembolsarAsync(referencia);
            }

            throw;
        }
        await publicador.PublicarSuscripcion(actual.Id, ct);
        await Registrar(http, org, actual.Id, AccionesBitacora.SuscripcionPlataformaCambiada, $"Cambió el plan {origen.Nombre} por {destino.Nombre}.", ct);
        return TypedResults.Ok(new { estado = "activa", credito = valores.Credito, cargo = valores.Cargo, aPagar = valores.APagar, inicio = actual.Inicio, fin = actual.Fin });
    }

    public async Task<IResult> CancelarCambio(HttpContext http, CancellationToken ct)
    {
        var org = OrganizacionId(http.User);
        var actual = await db.Set<SuscripcionPlataforma>().SingleOrDefaultAsync(x => x.OrganizacionId == org && x.Estado != EstadoSuscripcion.Finalizada, ct);
        if (actual is null)
        {
            return Error(404, "suscripcion_no_encontrada", "No se encontró una suscripción vigente.");
        }

        if (actual.PlanSiguienteId is null)
        {
            return TypedResults.NoContent();
        }

        actual.CancelarCambio(reloj.Ahora);
        await db.SaveChangesAsync(ct);
        await Registrar(http, org, actual.Id, AccionesBitacora.SuscripcionPlataformaCambiada, "Canceló el cambio de plan programado.", ct);
        return TypedResults.NoContent();
    }

    public async Task<IResult> Pagar(HttpContext http, PeticionPagoPlataforma p, CancellationToken ct)
    {
        var org = OrganizacionId(http.User);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({org.ToString()}, 0))", ct);
        var actual = await db.Set<SuscripcionPlataforma>().SingleOrDefaultAsync(x => x.OrganizacionId == org && (x.Estado == EstadoSuscripcion.EnGracia || x.Estado == EstadoSuscripcion.Suspendida), ct);
        if (actual is null)
        {
            return Error(404, "suscripcion_no_encontrada", "No hay una suscripción que requiera pago.");
        }

        var plan = await db.Set<PlanPlataforma>().SingleAsync(x => x.Id == actual.PlanId, ct);
        if (plan.EsPrueba || plan.Precio == 0)
        {
            return Error(422, CodigosError.RequierePlanDePago, "Contrate un plan de pago para reactivar sus APIs.");
        }

        var prep = await PrepararMedio(org, actual, p.Tarjeta, p.UsarRegistrada, ct);
        if (prep.Error is not null)
        {
            return prep.Error;
        }

        var cobro = await pasarela.CobrarAsync(prep.Medio!.TokenPasarela, plan.Precio, $"reactivacion:{actual.Id}:{Guid.NewGuid():N}", false);
        if (!cobro.Exitoso)
        {
            if (cobro.Error != CodigosError.PasarelaNoDisponible)
            {
                db.Add(Pago.PlataformaRechazado(actual.Id, ConceptoPago.Reactivacion, plan.Precio,
                    $"Reactivación del plan {plan.Nombre}", cobro.Error ?? CodigosError.PagoRechazado));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }

            return Error(cobro.Error == CodigosError.PasarelaNoDisponible ? 503 : 402,
            cobro.Error == CodigosError.PasarelaNoDisponible ? cobro.Error : CodigosError.PagoRechazado, "No se autorizó el pago.",
            new { motivo = cobro.Error });
        }

        var ahora = reloj.Ahora;
        try
        {
            if (prep.EsNueva)
            {
                db.Add(prep.Medio!);
            }

            actual.Reactivar(plan, prep.Medio!.Id, ahora);
            db.Add(Pago.PlataformaAutorizado(actual.Id, prep.Medio.Id, ConceptoPago.Reactivacion, plan.Precio,
                $"Reactivación del plan {plan.Nombre}", cobro.Referencia!, actual.Inicio, actual.Fin));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await pasarela.ReembolsarAsync(cobro.Referencia!);
            throw;
        }
        await publicador.PublicarSuscripcion(actual.Id, ct);
        await Registrar(http, org, actual.Id, AccionesBitacora.SuscripcionPlataformaCambiada,
            $"Reactivó la suscripción de plataforma del plan {plan.Nombre}.", ct);
        return TypedResults.Ok(new { estado = "activa", inicio = actual.Inicio, fin = actual.Fin });
    }

    private async Task<(MedioPago? Medio, bool EsNueva, IResult? Error)> PrepararMedio(Guid org, SuscripcionPlataforma? actual,
        DatosTarjeta? tarjeta, bool usarRegistrada, CancellationToken ct, bool requerido = true)
    {
        if (!requerido)
        {
            return (null, false, null);
        }

        if (usarRegistrada || tarjeta is null)
        {
            var guardado = actual?.MedioPagoId is Guid id ? await db.Set<MedioPago>().SingleOrDefaultAsync(x => x.Id == id && x.OrganizacionId == org, ct) : null;
            return guardado is null ? (null, false, Error(400, CodigosError.DatosInvalidos, "No hay tarjeta registrada.",
                errores: new Dictionary<string, string[]> { ["tarjeta"] = ["Registre una tarjeta."] }))
                : (guardado, false, null);
        }
        if (string.IsNullOrWhiteSpace(tarjeta.Titular))
        {
            return (null, false, Error(400, CodigosError.DatosInvalidos,
            "La tarjeta no es válida.", errores: new Dictionary<string, string[]> { ["tarjeta.titular"] = ["Indique el titular."] }));
        }

        var token = await pasarela.TokenizarAsync(tarjeta);
        if (!token.Exitoso)
        {
            return (null, false, Error(token.Error == CodigosError.PasarelaNoDisponible ? 503 : 422,
            token.Error ?? "tarjeta_invalida", "No se pudo validar la tarjeta."));
        }

        var mes = int.Parse(tarjeta.MesVencimiento);
        var anio = int.Parse(tarjeta.AnioVencimiento);
        if (anio < 100)
        {
            anio += 2000;
        }

        return (MedioPago.CrearParaOrganizacion(org, token.Token!, token.Marca!, token.Ultimos4!, tarjeta.Titular, mes, anio, reloj.Ahora), true, null);
    }

    private Task Registrar(HttpContext http, Guid org, Guid id, string accion, string descripcion, CancellationToken ct) =>
        bitacora.Registrar(new EntradaBitacora(TipoActor.Usuario, UsuarioId(http.User),
            http.User.FindFirstValue(ClaimTypes.Name) ?? "Propietario", org, accion, descripcion)
        { ObjetivoTipo = "suscripcion_plataforma", ObjetivoId = id, Ip = http.Connection.RemoteIpAddress?.ToString() }, ct);

    private static Guid OrganizacionId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(Identidad.PoliticasAutorizacion.ClaimOrganizacion)!);
    private static Guid UsuarioId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string Estado(EstadoSuscripcion x) => x switch { EstadoSuscripcion.Activa => "activa", EstadoSuscripcion.EnGracia => "en_gracia", EstadoSuscripcion.Suspendida => "suspendida", _ => "finalizada" };
    private static string NombreMarca(MarcaTarjeta x) => x switch { MarcaTarjeta.Visa => "Visa", MarcaTarjeta.Mastercard => "Mastercard", _ => "American Express" };
    private static IResult Error(int status, string codigo, string title, object? detalle = null,
        Dictionary<string, string[]>? errores = null)
    {
        var ext = new Dictionary<string, object?> { ["codigo"] = codigo };
        if (detalle is not null)
        {
            ext["detalle"] = detalle;
        }

        if (errores is not null)
        {
            ext["errores"] = errores;
        }

        return TypedResults.Problem(statusCode: status, title: title, type: "about:blank", extensions: ext);
    }
}
