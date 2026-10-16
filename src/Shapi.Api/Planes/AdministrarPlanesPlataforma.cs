using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Planes;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Planes;

public sealed record SolicitudPlanPlataforma(
    string? Nombre,
    string? Descripcion,
    decimal Precio,
    int VigenciaDias,
    int? MaxApis,
    int? MaxMiembros,
    long CuotaPeticiones,
    bool DominioPropio);

public sealed record RespuestaPlanPlataforma(
    Guid Id,
    string Nombre,
    string Descripcion,
    decimal Precio,
    string Moneda,
    int VigenciaDias,
    int? MaxApis,
    int? MaxMiembros,
    long CuotaPeticiones,
    bool DominioPropio,
    bool EsPrueba,
    bool Activo,
    int Orden);

public sealed class AdministrarPlanesPlataforma(ShapiDbContext db, IBitacora bitacora, IReloj reloj)
{
    public async Task<IReadOnlyList<RespuestaPlanPlataforma>> Listar(CancellationToken ct)
    {
        return await db.Set<PlanPlataforma>().IgnoreQueryFilters().AsNoTracking()
            .OrderBy(p => p.Orden).ThenBy(p => p.Nombre)
            .Select(p => new RespuestaPlanPlataforma(p.Id, p.Nombre, p.Descripcion, p.Precio, "GTQ", p.VigenciaDias,
                p.MaxApis, p.MaxMiembros, p.CuotaPeticiones, p.DominioPropio, p.EsPrueba, p.Activo, p.Orden))
            .ToListAsync(ct);
    }

    public async Task<Resultado<PlanPlataforma>> Crear(SolicitudPlanPlataforma solicitud, Guid actorId,
        string actorNombre, Guid organizacionPlataforma, string? ip, CancellationToken ct)
    {
        var error = Validar(solicitud);
        if (error is not null)
        {
            return error;
        }

        if (await db.Set<PlanPlataforma>().IgnoreQueryFilters().AnyAsync(p => p.Nombre == solicitud.Nombre, ct))
        {
            return new Error(CodigosError.PlanDuplicado, "Ya existe un plan de plataforma con ese nombre.");
        }

        var orden = (await db.Set<PlanPlataforma>().IgnoreQueryFilters().Select(p => (int?)p.Orden).MaxAsync(ct) ?? 0) + 1;
        var plan = PlanPlataforma.Crear(Guid.CreateVersion7(), solicitud.Nombre!, solicitud.Descripcion!, solicitud.Precio,
            solicitud.VigenciaDias, solicitud.MaxApis, solicitud.MaxMiembros, solicitud.CuotaPeticiones,
            solicitud.DominioPropio, orden, reloj.Ahora);
        db.Add(plan);
        try
        {
            await Registrar(actorId, actorNombre, organizacionPlataforma, AccionesBitacora.PlanPlataformaCreado,
                $"Creó el plan de plataforma {plan.Nombre}.", "plan_plataforma", plan.Id, ip, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (EsNombreDuplicado(e))
        {
            return new Error(CodigosError.PlanDuplicado, "Ya existe un plan de plataforma con ese nombre.");
        }

        return plan;
    }

    public async Task<Resultado<PlanPlataforma>> Editar(Guid id, SolicitudPlanPlataforma solicitud, Guid actorId,
        string actorNombre, Guid organizacionPlataforma, string? ip, CancellationToken ct)
    {
        var error = Validar(solicitud);
        if (error is not null)
        {
            return error;
        }

        var plan = await db.Set<PlanPlataforma>().IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
        {
            return new Error(CodigosError.PlanNoEncontrado, "No se encontró el plan de plataforma.");
        }

        if (await db.Set<PlanPlataforma>().IgnoreQueryFilters().AnyAsync(p => p.Id != id && p.Nombre == solicitud.Nombre, ct))
        {
            return new Error(CodigosError.PlanDuplicado, "Ya existe un plan de plataforma con ese nombre.");
        }

        plan.Editar(solicitud.Nombre!, solicitud.Descripcion!, solicitud.Precio, solicitud.VigenciaDias,
            solicitud.MaxApis, solicitud.MaxMiembros, solicitud.CuotaPeticiones, solicitud.DominioPropio, reloj.Ahora);
        try
        {
            await Registrar(actorId, actorNombre, organizacionPlataforma, AccionesBitacora.PlanPlataformaEditado,
                $"Editó el plan de plataforma {plan.Nombre}.", "plan_plataforma", plan.Id, ip, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (EsNombreDuplicado(e))
        {
            return new Error(CodigosError.PlanDuplicado, "Ya existe un plan de plataforma con ese nombre.");
        }

        return plan;
    }

    public async Task<Resultado> Desactivar(Guid id, Guid actorId, string actorNombre, Guid organizacionPlataforma,
        string? ip, CancellationToken ct)
    {
        var plan = await db.Set<PlanPlataforma>().IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null)
        {
            return Resultado.Fallo(new Error(CodigosError.PlanNoEncontrado, "No se encontró el plan de plataforma."));
        }

        plan.Desactivar(reloj.Ahora);
        await Registrar(actorId, actorNombre, organizacionPlataforma, AccionesBitacora.PlanPlataformaDesactivado,
            $"Desactivó el plan de plataforma {plan.Nombre}.", "plan_plataforma", plan.Id, ip, ct);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito();
    }

    private static Error? Validar(SolicitudPlanPlataforma s)
    {
        var errores = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(s.Nombre) || s.Nombre.Length > 100)
        {
            errores["nombre"] = ["Escriba un nombre de hasta 100 caracteres."];
        }
        if (string.IsNullOrWhiteSpace(s.Descripcion) || s.Descripcion.Length > 500)
        {
            errores["descripcion"] = ["Escriba una descripción de hasta 500 caracteres."];
        }
        if (s.Precio < 0 || s.Precio > 9_999_999_999.99m || decimal.Round(s.Precio, 2) != s.Precio)
        {
            errores["precio"] = ["El precio debe ser no negativo y tener máximo dos decimales."];
        }
        if (s.VigenciaDias is < 1 or > 366)
        {
            errores["vigenciaDias"] = ["La vigencia debe estar entre 1 y 366 días."];
        }
        if (s.MaxApis is <= 0)
        {
            errores["maxApis"] = ["El máximo de APIs debe ser mayor que cero o nulo para ilimitado."];
        }
        if (s.MaxMiembros is <= 0)
        {
            errores["maxMiembros"] = ["El máximo de miembros debe ser mayor que cero o nulo para ilimitado."];
        }
        if (s.CuotaPeticiones <= 0)
        {
            errores["cuotaPeticiones"] = ["La cuota debe ser mayor que cero."];
        }

        return errores.Count == 0
            ? null
            : new Error(CodigosError.DatosInvalidos, "Revise los datos del plan.", errores);
    }

    private async Task Registrar(Guid actorId, string actorNombre, Guid organizacion, string accion, string descripcion,
        string tipoObjetivo, Guid objetivo, string? ip, CancellationToken ct) =>
        await bitacora.Registrar(new Shapi.Aplicacion.Comun.EntradaBitacora(TipoActor.Usuario, actorId, actorNombre, organizacion, accion, descripcion)
        {
            ObjetivoTipo = tipoObjetivo,
            ObjetivoId = objetivo,
            Ip = ip,
        }, ct);

    private static bool EsNombreDuplicado(DbUpdateException e) =>
        e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, TableName: "plan_plataforma" };
}
