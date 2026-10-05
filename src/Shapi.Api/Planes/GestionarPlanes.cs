using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
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

        // A4.1 y A5.4 muestran los planes del más barato al más caro (Básico, Comercio y Volumen).
        return await db.Set<PlanApi>().IgnoreQueryFilters()
            .Where(p => p.ApiId == apiId && p.Activo)
            .OrderBy(p => p.Precio)
            .ThenBy(p => p.Nombre)
            .ToListAsync(cancelacion);
    }
}

public class CrearPlan(ShapiDbContext db, IBitacora bitacora)
{
    /// <summary>El mayor precio que cabe en <c>numeric(12,2)</c> (07 §3.3).</summary>
    public const decimal PrecioMaximo = 9_999_999_999.99m;

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
            return Resultado<PlanApi>.Fallo(PlanDuplicado());
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
            AccionesBitacora.PlanApiCreado,
            $"Creó el plan {plan.Nombre} en {TextoBitacora.LaApi(api.Nombre)}.")
        {
            ObjetivoTipo = "plan_api",
            ObjetivoId = plan.Id,
            Ip = ip
        }, cancelacion);

        try
        {
            await db.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (EsNombreDuplicado(ex))
        {
            return Resultado<PlanApi>.Fallo(PlanDuplicado());
        }
        return plan;
    }

    /// <summary>Validaciones de 07 §3.3, con el error de cada campo en <c>errores</c> (convenciones §5).</summary>
    public static Error? ValidarPlan(SolicitudPlanApi s)
    {
        var errores = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(s.Nombre) || s.Nombre.Length > 100)
        {
            errores["nombre"] = ["El nombre del plan es obligatorio y debe tener máximo 100 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(s.Descripcion) || s.Descripcion.Length > 500)
        {
            errores["descripcion"] = ["La descripción del plan es obligatoria y debe tener máximo 500 caracteres."];
        }

        if (s.Precio < 0)
        {
            errores["precio"] = ["El precio no puede ser negativo."];
        }
        else if (s.Precio > PrecioMaximo)
        {
            errores["precio"] = ["El precio no puede superar Q 9,999,999,999.99."];
        }
        else if (decimal.Round(s.Precio, 2) != s.Precio)
        {
            errores["precio"] = ["El precio puede tener como máximo 2 decimales."];
        }
        else if (s.EsGratuito && s.Precio != 0)
        {
            errores["precio"] = ["El plan gratuito debe tener precio 0."];
        }
        else if (!s.EsGratuito && s.Precio == 0)
        {
            // Un plan de pago con precio 0 fallaría al contratarlo: ck_pago_monto exige monto > 0 (07 §3.3).
            errores["precio"] = ["Un plan de pago debe tener un precio mayor que 0. Si no cobra, márquelo como gratuito."];
        }

        if (s.VigenciaDias < 1 || s.VigenciaDias > 366)
        {
            errores["vigenciaDias"] = ["La vigencia debe estar entre 1 y 366 días."];
        }

        if (s.CuotaLlamadas <= 0)
        {
            errores["cuotaLlamadas"] = ["La cuota debe ser mayor a 0."];
        }

        if (s.LimiteMinuto <= 0)
        {
            errores["limiteMinuto"] = ["El límite por minuto debe ser mayor a 0."];
        }

        return errores.Count == 0
            ? null
            : new Error(CodigosError.DatosInvalidos, "Revise los datos del plan.", errores);
    }

    internal static Error PlanDuplicado() =>
        new(CodigosError.PlanDuplicado, "Ya existe un plan con ese nombre en esta API.");

    /// <summary>Solo el UNIQUE (api_id, nombre) es un nombre repetido; cualquier otro error de la base no lo es.</summary>
    internal static bool EsNombreDuplicado(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, TableName: "plan_api" };
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

        var suscripciones = await db.Set<SuscripcionApi>()
            .Where(s => s.PlanId == plan.Id && s.Estado != EstadoSuscripcion.Finalizada)
            .Select(s => s.Id)
            .ToListAsync(cancelacion);

        // 09 §5: pasar de gratuito a pago (o al revés) dejaría renovaciones sin tarjeta o tarjetas sin cobro.
        if (plan.EsGratuito != solicitud.EsGratuito && suscripciones.Count > 0)
        {
            return Resultado<PlanApi>.Fallo(new Error(
                CodigosError.PlanConSuscripciones,
                "No se puede cambiar entre gratuito y de pago mientras el plan tenga suscripciones vigentes."));
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
            AccionesBitacora.PlanApiEditado,
            $"Editó el plan {plan.Nombre} de {TextoBitacora.LaApi(api.Nombre)}.")
        {
            ObjetivoTipo = "plan_api",
            ObjetivoId = plan.Id,
            Ip = ip
        }, cancelacion);

        try
        {
            await db.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (CrearPlan.EsNombreDuplicado(ex))
        {
            return Resultado<PlanApi>.Fallo(CrearPlan.PlanDuplicado());
        }

        // Criterio 2 de EM-07: la cuota y el límite nuevos se publican de inmediato en las suscripciones vigentes.
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
            AccionesBitacora.PlanApiDesactivado,
            $"Desactivó el plan {plan.Nombre} de {TextoBitacora.LaApi(api.Nombre)}.")
        {
            ObjetivoTipo = "plan_api",
            ObjetivoId = plan.Id,
            Ip = ip
        }, cancelacion);

        await db.SaveChangesAsync(cancelacion);
        return Resultado.Exito();
    }
}
