using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Planes;

public record SolicitudPlanApi(
    string Nombre,
    string Descripcion,
    decimal Precio,
    bool EsGratuito,
    int VigenciaDias,
    long CuotaLlamadas,
    int LimiteMinuto);

public class ListarPlanes(ShapiDbContext db)
{
    public async Task<Resultado<List<PlanApi>>> Ejecutar(Guid apiId, Guid organizacionId, CancellationToken cancelacion = default)
    {
        var api = await db.Set<Dominio.Apis.Api>().IgnoreQueryFilters().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);
        if (api is null)
        {
            return Resultado<List<PlanApi>>.Fallo(new Error(CodigosError.ApiNoEncontrada, "No se encontró la API."));
        }

        return await db.Set<PlanApi>().IgnoreQueryFilters().Where(p => p.ApiId == apiId && p.Activo).ToListAsync(cancelacion);
    }
}

public class CrearPlan(ShapiDbContext db, IReloj reloj)
{
    public async Task<Resultado<PlanApi>> Ejecutar(Guid apiId, Guid organizacionId, SolicitudPlanApi solicitud, CancellationToken cancelacion = default)
    {
        var api = await db.Set<Dominio.Apis.Api>().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);
        if (api is null)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.ApiNoEncontrada, "No se encontró la API."));
        }

        var errores = ValidarPlan(solicitud);
        if (errores is not null)
        {
            return Resultado<PlanApi>.Fallo(errores);
        }

        var existeNombre = await db.Set<PlanApi>().AnyAsync(p => p.ApiId == apiId && p.Activo && p.Nombre == solicitud.Nombre, cancelacion);
        if (existeNombre)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.DatosInvalidos, "Ya existe un plan activo con ese nombre."));
        }

        var plan = PlanApi.Crear(
            Guid.NewGuid(),
            apiId,
            solicitud.Nombre,
            solicitud.Descripcion,
            solicitud.Precio,
            solicitud.EsGratuito,
            solicitud.VigenciaDias,
            solicitud.CuotaLlamadas,
            solicitud.LimiteMinuto);

        db.Add(plan);
        db.Add(new Dominio.Bitacora.EntradaBitacora(
            reloj.Ahora,
            Dominio.Bitacora.ActorTipo.Sistema,
            null,
            "Sistema",
            organizacionId,
            "plan_api.creado",
            "plan_api",
            plan.Id,
            $"Se creó el plan {plan.Nombre}.",
            null,
            null));

        await db.SaveChangesAsync(cancelacion);
        return plan;
    }

    public static Error? ValidarPlan(SolicitudPlanApi s)
    {
        if (s.EsGratuito && s.Precio != 0)
        {
            return new Error(CodigosError.DatosInvalidos, "El plan gratuito debe tener precio 0.");
        }

        if (s.VigenciaDias < 1 || s.VigenciaDias > 366)
        {
            return new Error(CodigosError.DatosInvalidos, "La vigencia debe estar entre 1 y 366 días.");
        }

        if (s.CuotaLlamadas <= 0)
        {
            return new Error(CodigosError.DatosInvalidos, "La cuota debe ser mayor a 0.");
        }

        if (s.LimiteMinuto <= 0)
        {
            return new Error(CodigosError.DatosInvalidos, "El límite por minuto debe ser mayor a 0.");
        }

        return null;
    }
}

public class EditarPlan(ShapiDbContext db, IReloj reloj, IPublicadorCache publicador)
{
    public async Task<Resultado<PlanApi>> Ejecutar(Guid apiId, Guid planId, Guid organizacionId, SolicitudPlanApi solicitud, CancellationToken cancelacion = default)
    {
        var api = await db.Set<Dominio.Apis.Api>().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);
        if (api is null)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.ApiNoEncontrada, "No se encontró la API."));
        }

        var plan = await db.Set<PlanApi>().SingleOrDefaultAsync(p => p.Id == planId && p.ApiId == apiId && p.Activo, cancelacion);
        if (plan is null)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.DatosInvalidos, "No se encontró el plan activo."));
        }

        var errores = CrearPlan.ValidarPlan(solicitud);
        if (errores is not null)
        {
            return Resultado<PlanApi>.Fallo(errores);
        }

        var existeNombre = await db.Set<PlanApi>().AnyAsync(p => p.ApiId == apiId && p.Activo && p.Nombre == solicitud.Nombre && p.Id != planId, cancelacion);
        if (existeNombre)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.DatosInvalidos, "Ya existe otro plan activo con ese nombre."));
        }

        plan.Editar(
            solicitud.Nombre,
            solicitud.Descripcion,
            solicitud.Precio,
            solicitud.EsGratuito,
            solicitud.VigenciaDias,
            solicitud.CuotaLlamadas,
            solicitud.LimiteMinuto);

        db.Add(new Dominio.Bitacora.EntradaBitacora(
            reloj.Ahora,
            Dominio.Bitacora.ActorTipo.Sistema,
            null,
            "Sistema",
            organizacionId,
            "plan_api.editado",
            "plan_api",
            plan.Id,
            $"Se editó el plan {plan.Nombre}.",
            null,
            null));

        await db.SaveChangesAsync(cancelacion);

        // Publicar suscripciones activas asociadas al plan
        var suscripciones = await db.Set<SuscripcionApi>()
            .Where(s => s.PlanId == plan.Id && s.Estado == EstadoSuscripcion.Activa)
            .Select(s => s.Id)
            .ToListAsync(cancelacion);

        foreach (var subId in suscripciones)
        {
            await publicador.PublicarSuscripcion(subId, cancelacion);
        }

        return plan;
    }
}

public class DesactivarPlan(ShapiDbContext db, IReloj reloj)
{
    public async Task<Resultado> Ejecutar(Guid apiId, Guid planId, Guid organizacionId, CancellationToken cancelacion = default)
    {
        var api = await db.Set<Dominio.Apis.Api>().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);
        if (api is null)
        {
            return Resultado.Fallo(new Error(CodigosError.ApiNoEncontrada, "No se encontró la API."));
        }

        var plan = await db.Set<PlanApi>().SingleOrDefaultAsync(p => p.Id == planId && p.ApiId == apiId && p.Activo, cancelacion);
        if (plan is null)
        {
            return Resultado.Fallo(new Error(CodigosError.DatosInvalidos, "No se encontró el plan activo."));
        }

        plan.Desactivar();

        db.Add(new Dominio.Bitacora.EntradaBitacora(
            reloj.Ahora,
            Dominio.Bitacora.ActorTipo.Sistema,
            null,
            "Sistema",
            organizacionId,
            "plan_api.desactivado",
            "plan_api",
            plan.Id,
            $"Se desactivó el plan {plan.Nombre}.",
            null,
            null));

        await db.SaveChangesAsync(cancelacion);
        return Resultado.Exito();
    }
}
