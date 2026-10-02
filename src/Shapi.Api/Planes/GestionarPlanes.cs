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

public class CrearPlan(ShapiDbContext db, IBitacora bitacora)
{
    public async Task<Resultado<PlanApi>> Ejecutar(Guid apiId, Guid organizacionId, SolicitudPlanApi solicitud, Guid usuarioId, string usuarioNombre, string? ip, CancellationToken cancelacion = default)
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

        var existeNombre = await db.Set<PlanApi>().AnyAsync(p => p.ApiId == apiId && p.Nombre == solicitud.Nombre, cancelacion);
        if (existeNombre)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.PlanDuplicado, "Ya existe un plan con ese nombre en esta API."));
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
        await bitacora.Registrar(new Shapi.Aplicacion.Comun.EntradaBitacora(
            TipoActor.Usuario,
            usuarioId,
            usuarioNombre,
            organizacionId,
            "plan_api.creado",
            $"Creó el plan {plan.Nombre} en la API {api.Nombre}.")
        {
            ObjetivoTipo = "plan_api",
            ObjetivoId = plan.Id,
            Ip = ip
        }, cancelacion);

        await db.SaveChangesAsync(cancelacion);
        return plan;
    }

    public static Error? ValidarPlan(SolicitudPlanApi s)
    {
        if (string.IsNullOrWhiteSpace(s.Nombre) || s.Nombre.Length > 100)
        {
            return new Error(CodigosError.DatosInvalidos, "El nombre del plan es obligatorio y debe tener máximo 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(s.Descripcion) || s.Descripcion.Length > 500)
        {
            return new Error(CodigosError.DatosInvalidos, "La descripción del plan es obligatoria y debe tener máximo 500 caracteres.");
        }

        if (s.Precio < 0)
        {
            return new Error(CodigosError.DatosInvalidos, "El precio no puede ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(s.Nombre) || s.Nombre.Length > 100)
        {
            return new Error(CodigosError.DatosInvalidos, "El nombre del plan es obligatorio y debe tener máximo 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(s.Descripcion) || s.Descripcion.Length > 500)
        {
            return new Error(CodigosError.DatosInvalidos, "La descripción del plan es obligatoria y debe tener máximo 500 caracteres.");
        }

        if (s.Precio < 0)
        {
            return new Error(CodigosError.DatosInvalidos, "El precio no puede ser negativo.");
        }

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

public class EditarPlan(ShapiDbContext db, IBitacora bitacora, IPublicadorCache publicador)
{
    public async Task<Resultado<PlanApi>> Ejecutar(Guid apiId, Guid planId, Guid organizacionId, SolicitudPlanApi solicitud, Guid usuarioId, string usuarioNombre, string? ip, CancellationToken cancelacion = default)
    {
        var api = await db.Set<Dominio.Apis.Api>().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);
        if (api is null)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.ApiNoEncontrada, "No se encontró la API."));
        }

        var plan = await db.Set<PlanApi>().SingleOrDefaultAsync(p => p.Id == planId && p.ApiId == apiId && p.Activo, cancelacion);
        if (plan is null)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.PlanNoEncontrado, "No se encontró el plan activo."));
        }

        var errores = CrearPlan.ValidarPlan(solicitud);
        if (errores is not null)
        {
            return Resultado<PlanApi>.Fallo(errores);
        }

        var existeNombre = await db.Set<PlanApi>().AnyAsync(p => p.ApiId == apiId && p.Nombre == solicitud.Nombre && p.Id != planId, cancelacion);
        if (existeNombre)
        {
            return Resultado<PlanApi>.Fallo(new Error(CodigosError.PlanDuplicado, "Ya existe otro plan con ese nombre en esta API."));
        }

        plan.Editar(
            solicitud.Nombre,
            solicitud.Descripcion,
            solicitud.Precio,
            solicitud.EsGratuito,
            solicitud.VigenciaDias,
            solicitud.CuotaLlamadas,
            solicitud.LimiteMinuto);

        await bitacora.Registrar(new Shapi.Aplicacion.Comun.EntradaBitacora(
            TipoActor.Usuario,
            usuarioId,
            usuarioNombre,
            organizacionId,
            "plan_api.editado",
            $"Editó el plan {plan.Nombre} de la API {api.Nombre}.")
        {
            ObjetivoTipo = "plan_api",
            ObjetivoId = plan.Id,
            Ip = ip
        }, cancelacion);

        await db.SaveChangesAsync(cancelacion);

        // Publicar suscripciones activas asociadas al plan
        var suscripciones = await db.Set<SuscripcionApi>()
            .Where(s => s.PlanId == plan.Id && s.Estado != EstadoSuscripcion.Finalizada)
            .Select(s => s.Id)
            .ToListAsync(cancelacion);

        foreach (var subId in suscripciones)
        {
            await publicador.PublicarSuscripcion(subId, cancelacion);
        }

        return plan;
    }
}

public class DesactivarPlan(ShapiDbContext db, IBitacora bitacora)
{
    public async Task<Resultado> Ejecutar(Guid apiId, Guid planId, Guid organizacionId, Guid usuarioId, string usuarioNombre, string? ip, CancellationToken cancelacion = default)
    {
        var api = await db.Set<Dominio.Apis.Api>().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, cancelacion);
        if (api is null)
        {
            return Resultado.Fallo(new Error(CodigosError.ApiNoEncontrada, "No se encontró la API."));
        }

        var plan = await db.Set<PlanApi>().SingleOrDefaultAsync(p => p.Id == planId && p.ApiId == apiId && p.Activo, cancelacion);
        if (plan is null)
        {
            return Resultado.Fallo(new Error(CodigosError.PlanNoEncontrado, "No se encontró el plan activo."));
        }

        plan.Desactivar();

        await bitacora.Registrar(new Shapi.Aplicacion.Comun.EntradaBitacora(
            TipoActor.Usuario,
            usuarioId,
            usuarioNombre,
            organizacionId,
            "plan_api.desactivado",
            $"Desactivó el plan {plan.Nombre} de la API {api.Nombre}.")
        {
            ObjetivoTipo = "plan_api",
            ObjetivoId = plan.Id,
            Ip = ip
        }, cancelacion);

        await db.SaveChangesAsync(cancelacion);
        return Resultado.Exito();
    }
}
