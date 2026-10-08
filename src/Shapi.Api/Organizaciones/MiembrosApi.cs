using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Organizaciones;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Organizaciones;

/// <summary>Endpoints del personal para administrar miembros e invitaciones (CU-04).</summary>
public static class MiembrosApi
{
    public static IEndpointRouteBuilder MapearEndpointsMiembros(this IEndpointRouteBuilder app)
    {
        var miembros = app.MapGroup("/api/miembros").RequireAuthorization(Permisos.AdministrarMiembros);
        miembros.MapGet("", Listar);
        miembros.MapPost("/invitaciones", Invitar);
        miembros.MapPut("/{id:guid}/rol", CambiarRol);
        miembros.MapDelete("/{id:guid}", Quitar);

        var invitaciones = app.MapGroup("/api/invitaciones");
        invitaciones.MapGet("/{token}", ConsultarInvitacion).AllowAnonymous();
        invitaciones.MapPost("/{token}/aceptar", AceptarInvitacion).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> Listar(
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IReloj reloj,
        ClaimsPrincipal usuario,
        CancellationToken cancelacion)
    {
        if (contexto.OrganizacionId is not { } organizacionId)
        {
            return TypedResults.NotFound();
        }

        var usuarioActualId = IdUsuario(usuario);
        var miembros = await (
            from membresia in db.Set<Membresia>().AsNoTracking()
            join cuenta in db.Set<Usuario>().AsNoTracking() on membresia.UsuarioId equals cuenta.Id
            where membresia.OrganizacionId == organizacionId
            orderby membresia.Rol, cuenta.Nombre
            select new MiembroDto(membresia.Id, cuenta.Id, cuenta.Nombre, cuenta.Correo, RolTexto(membresia.Rol), usuarioActualId != null && cuenta.Id == usuarioActualId)
        ).ToListAsync(cancelacion);

        var suscripcion = await (
            from actual in db.Set<SuscripcionPlataforma>().AsNoTracking()
            join plan in db.Set<Shapi.Dominio.Planes.PlanPlataforma>().AsNoTracking() on actual.PlanId equals plan.Id
            where actual.OrganizacionId == organizacionId && actual.Estado != EstadoSuscripcion.Finalizada
            select new { plan.Nombre, plan.MaxMiembros }
        ).SingleOrDefaultAsync(cancelacion);
        if (suscripcion is null)
        {
            return Problemas.Crear(StatusCodes.Status404NotFound, "suscripcion_no_encontrada", "No se encontró la suscripción de la organización.");
        }

        var pendientes = await db.Set<Token>().AsNoTracking()
            .CountAsync(t => t.OrganizacionId == organizacionId && t.Tipo == TipoToken.InvitacionMiembro && t.UsadoEn == null && t.ExpiraEn > reloj.Ahora, cancelacion);
        var nombreOrganizacion = await db.Set<Organizacion>().AsNoTracking().Where(o => o.Id == organizacionId).Select(o => o.Nombre).SingleAsync(cancelacion);
        return TypedResults.Ok(new
        {
            elementos = miembros,
            total = miembros.Count + pendientes,
            invitacionesPendientes = pendientes,
            organizacion = nombreOrganizacion,
            usuarioActualId = usuario.FindFirstValue(ClaimTypes.NameIdentifier),
            plan = new { nombre = suscripcion.Nombre, maxMiembros = suscripcion.MaxMiembros },
        });
    }

    private static async Task<IResult> Invitar(
        [FromBody] PeticionInvitarMiembro peticion,
        [FromServices] IValidator<PeticionInvitarMiembro> validador,
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IColaCorreo colaCorreo,
        [FromServices] IBitacora bitacora,
        [FromServices] IReloj reloj,
        HttpContext http,
        CancellationToken cancelacion)
    {
        peticion = peticion with { Correo = peticion.Correo?.Trim() };
        var validacion = await validador.ValidateAsync(peticion, cancelacion);
        if (!validacion.IsValid)
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.", validacion.ToDictionary());
        }

        if (contexto.OrganizacionId is not { } organizacionId)
        {
            return TypedResults.NotFound();
        }

        var correo = Usuario.NormalizarCorreo(peticion.Correo!);
        var existente = await (
            from membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking()
            join cuenta in db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking() on membresia.UsuarioId equals cuenta.Id
            where cuenta.Correo == correo
            select new { membresia.OrganizacionId }
        ).SingleOrDefaultAsync(cancelacion);
        if (existente is not null)
        {
            return existente.OrganizacionId == organizacionId
                ? Problemas.Crear(StatusCodes.Status409Conflict, "miembro_ya_existente", "Esta persona ya pertenece a la organización.")
                : ProblemaConDetalle(StatusCodes.Status422UnprocessableEntity, "correo_en_otra_organizacion", "El correo ya pertenece a otra organización.");
        }

        var suscripcion = await (
            from actual in db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().AsNoTracking()
            join plan in db.Set<Shapi.Dominio.Planes.PlanPlataforma>().IgnoreQueryFilters().AsNoTracking() on actual.PlanId equals plan.Id
            where actual.OrganizacionId == organizacionId && actual.Estado != EstadoSuscripcion.Finalizada
            select new { plan.MaxMiembros }
        ).SingleOrDefaultAsync(cancelacion);
        if (suscripcion is null)
        {
            return Problemas.Crear(StatusCodes.Status404NotFound, "suscripcion_no_encontrada", "No se encontró la suscripción de la organización.");
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        // Serializa las invitaciones de la organización para que dos solicitudes simultáneas no excedan el límite.
        _ = await db.Set<Organizacion>().FromSqlInterpolated($"SELECT * FROM organizacion WHERE id = {organizacionId} FOR UPDATE")
            .IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(cancelacion);

        var duplicada = await db.Set<Token>().IgnoreQueryFilters().AnyAsync(t => t.OrganizacionId == organizacionId && t.Correo == correo &&
            t.Tipo == TipoToken.InvitacionMiembro && t.UsadoEn == null && t.ExpiraEn > reloj.Ahora, cancelacion);
        if (duplicada)
        {
            return Problemas.Crear(StatusCodes.Status409Conflict, "invitacion_pendiente", "Ya hay una invitación vigente para ese correo.");
        }

        var miembrosActuales = await db.Set<Membresia>().IgnoreQueryFilters().CountAsync(m => m.OrganizacionId == organizacionId, cancelacion);
        var invitacionesActuales = await db.Set<Token>().IgnoreQueryFilters().CountAsync(t => t.OrganizacionId == organizacionId && t.Tipo == TipoToken.InvitacionMiembro &&
            t.UsadoEn == null && t.ExpiraEn > reloj.Ahora, cancelacion);
        if (suscripcion.MaxMiembros is { } limite && miembrosActuales + invitacionesActuales >= limite)
        {
            return ProblemaConDetalle(StatusCodes.Status422UnprocessableEntity, CodigosError.LimiteDelPlan, "Su plan llegó al límite de miembros.", "miembros");
        }

        var valorToken = SeguridadTokens.GenerarToken();
        var ahora = reloj.Ahora;
        var rol = peticion.Rol!;
        var token = Token.InvitacionMiembro(SeguridadTokens.HashearToken(valorToken), organizacionId, correo, rol, ahora);
        var nombreOrganizacion = await db.Set<Organizacion>().IgnoreQueryFilters().Where(o => o.Id == organizacionId).Select(o => o.Nombre).SingleAsync(cancelacion);
        db.Add(token);
        var nombreActor = await ObtenerNombreActor(db, http.User, cancelacion);
        await colaCorreo.Encolar("invitacion_miembro", correo, new
        {
            nombre = correo,
            nombreOrganizacion,
            token = valorToken,
        }, cancelacion);
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario,
            IdUsuario(http.User),
            nombreActor,
            organizacionId,
            "miembro.invitado",
            $"Invitó a {correo} con el rol de {rol}")
        {
            ObjetivoTipo = "invitacion_miembro",
            ObjetivoId = token.Id,
            Detalle = new { correo, rol },
            Ip = http.Connection.RemoteIpAddress?.ToString(),
        }, cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return TypedResults.StatusCode(StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> ConsultarInvitacion(
        string token,
        [FromServices] ShapiDbContext db,
        [FromServices] IReloj reloj,
        CancellationToken cancelacion)
    {
        var hash = SeguridadTokens.HashearToken(token);
        var invitacion = await db.Set<Token>().IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.HashToken == hash && t.Tipo == TipoToken.InvitacionMiembro && t.OrganizacionId != null && t.UsadoEn == null && t.ExpiraEn > reloj.Ahora)
            .Select(t => new { t.Id, t.OrganizacionId, t.Correo, t.Rol, t.ExpiraEn })
            .SingleOrDefaultAsync(cancelacion);
        if (invitacion?.OrganizacionId is not { } organizacionId)
        {
            return InvitacionInvalida();
        }

        var org = await db.Set<Organizacion>().IgnoreQueryFilters().Where(o => o.Id == organizacionId).Select(o => o.Nombre).SingleOrDefaultAsync(cancelacion);
        if (org is null)
        {
            return InvitacionInvalida();
        }

        var propietario = await (
            from membresia in db.Set<Membresia>().IgnoreQueryFilters().AsNoTracking()
            join cuenta in db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking() on membresia.UsuarioId equals cuenta.Id
            where membresia.OrganizacionId == organizacionId && membresia.Rol == Rol.Propietario
            select cuenta.Nombre
        ).SingleOrDefaultAsync(cancelacion);
        var cuentaInvitada = await db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking()
            .Where(cuenta => cuenta.Correo == invitacion.Correo)
            .Select(cuenta => new { cuenta.Id })
            .SingleOrDefaultAsync(cancelacion);
        var cuentaExistente = cuentaInvitada is not null
            && !await db.Set<Membresia>().IgnoreQueryFilters().AnyAsync(membresia => membresia.UsuarioId == cuentaInvitada.Id, cancelacion);
        return TypedResults.Ok(new
        {
            organizacion = org,
            nombrePropietario = propietario ?? "El propietario",
            correo = invitacion.Correo,
            rol = invitacion.Rol,
            expiraEn = invitacion.ExpiraEn,
            cuentaExistente,
        });
    }

    private static async Task<IResult> AceptarInvitacion(
        string token,
        [FromBody] PeticionAceptarInvitacionMiembro peticion,
        [FromServices] IValidator<PeticionAceptarInvitacionMiembro> validador,
        [FromServices] ShapiDbContext db,
        [FromServices] IPasswordHasher<Usuario> hasher,
        [FromServices] IReloj reloj,
        CancellationToken cancelacion)
    {
        var ahora = reloj.Ahora;
        var hashToken = SeguridadTokens.HashearToken(token);
        var invitacion = await db.Set<Token>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(t => t.HashToken == hashToken && t.Tipo == TipoToken.InvitacionMiembro && t.UsadoEn == null && t.ExpiraEn > ahora, cancelacion);
        if (invitacion?.OrganizacionId is not { } organizacionId || invitacion.Rol is not ("editor" or "lector"))
        {
            return InvitacionInvalida();
        }

        var existente = await db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(cuenta => cuenta.Correo == invitacion.Correo, cancelacion);
        if (existente is null)
        {
            var validacion = await validador.ValidateAsync(peticion, cancelacion);
            if (!validacion.IsValid)
            {
                return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.", validacion.ToDictionary());
            }

            if (string.Equals(peticion.Contrasena?.Trim(), invitacion.Correo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.",
                    new Dictionary<string, string[]> { ["contrasena"] = ["La contraseña no puede ser igual al correo."] });
            }
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var marcada = await db.Set<Token>().IgnoreQueryFilters().Where(t => t.Id == invitacion.Id && t.UsadoEn == null && t.ExpiraEn > ahora)
            .ExecuteUpdateAsync(c => c.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), cancelacion);
        if (marcada == 0)
        {
            return InvitacionInvalida();
        }

        Usuario usuario;
        if (existente is not null)
        {
            var bloqueada = await db.Set<Usuario>().FromSqlInterpolated($"SELECT * FROM usuario WHERE id = {existente.Id} FOR UPDATE")
                .IgnoreQueryFilters().SingleOrDefaultAsync(cancelacion);
            if (bloqueada is null)
            {
                return InvitacionInvalida();
            }

            usuario = bloqueada;
            if (await db.Set<Membresia>().IgnoreQueryFilters().AnyAsync(membresia => membresia.UsuarioId == usuario.Id, cancelacion))
            {
                return ProblemaConDetalle(StatusCodes.Status422UnprocessableEntity, "correo_en_otra_organizacion", "El correo ya pertenece a otra organización.");
            }

            db.Add(new Membresia(usuario.Id, organizacionId, invitacion.Rol == "editor" ? Rol.Editor : Rol.Lector));
        }
        else
        {
            usuario = new Usuario(peticion.Nombre!, invitacion.Correo);
            usuario.DefinirHashContrasena(hasher.HashPassword(usuario, peticion.Contrasena!));
            usuario.VerificarCorreo(ahora);
            db.AddRange(usuario, new Membresia(usuario.Id, organizacionId, invitacion.Rol == "editor" ? Rol.Editor : Rol.Lector));
        }
        try
        {
            await db.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation, TableName: "usuario" })
        {
            return ProblemaConDetalle(StatusCodes.Status422UnprocessableEntity, "correo_en_otra_organizacion", "El correo ya pertenece a otra organización.");
        }

        return TypedResults.Created("/api/auth/entrar", new { usuarioId = usuario.Id });
    }

    private static async Task<IResult> CambiarRol(
        Guid id,
        [FromBody] PeticionCambiarRolMiembro peticion,
        [FromServices] IValidator<PeticionCambiarRolMiembro> validador,
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IBitacora bitacora,
        [FromServices] IReloj reloj,
        HttpContext http,
        CancellationToken cancelacion)
    {
        var validacion = await validador.ValidateAsync(peticion, cancelacion);
        if (!validacion.IsValid)
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los datos del formulario.", validacion.ToDictionary());
        }

        if (contexto.OrganizacionId is not { } organizacionId)
        {
            return TypedResults.NotFound();
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var membresia = await db.Set<Membresia>().SingleOrDefaultAsync(m => m.Id == id && m.OrganizacionId == organizacionId, cancelacion);
        if (membresia is null)
        {
            return TypedResults.NotFound();
        }

        if (membresia.Rol == Rol.Propietario)
        {
            return Problemas.Crear(StatusCodes.Status422UnprocessableEntity, "propietario_inmutable", "No puede cambiar el rol del propietario.");
        }

        var rolNuevo = peticion.Rol == "editor" ? Rol.Editor : Rol.Lector;
        if (membresia.Rol == rolNuevo)
        {
            return TypedResults.Ok();
        }

        var rolAnterior = membresia.Rol;
        membresia.CambiarRol(rolNuevo);
        await db.SaveChangesAsync(cancelacion);
        await RegistrarAccion(db, bitacora, http, organizacionId, "miembro.rol_cambiado", membresia.Id, $"Cambió el rol de {rolAnterior} a {rolNuevo}", new { rolAnterior = RolTexto(rolAnterior), rol = peticion.Rol }, cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return TypedResults.Ok();
    }

    private static async Task<IResult> Quitar(
        Guid id,
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IBitacora bitacora,
        HttpContext http,
        CancellationToken cancelacion)
    {
        if (contexto.OrganizacionId is not { } organizacionId)
        {
            return TypedResults.NotFound();
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        var membresia = await db.Set<Membresia>().SingleOrDefaultAsync(m => m.Id == id && m.OrganizacionId == organizacionId, cancelacion);
        if (membresia is null)
        {
            return TypedResults.NotFound();
        }

        if (membresia.Rol == Rol.Propietario)
        {
            return Problemas.Crear(StatusCodes.Status422UnprocessableEntity, "propietario_inmutable", "No puede quitar al propietario.");
        }

        var cuenta = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == membresia.UsuarioId, cancelacion);
        var correo = cuenta.Correo;
        db.Remove(membresia);
        await db.SaveChangesAsync(cancelacion);
        await RegistrarAccion(db, bitacora, http, organizacionId, "miembro.quitado", membresia.Id, $"Quitó a {correo}", new { correo }, cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return TypedResults.NoContent();
    }

    private static async Task RegistrarAccion(ShapiDbContext db, IBitacora bitacora, HttpContext http, Guid organizacionId, string accion, Guid objetivoId, string descripcion, object detalle, CancellationToken cancelacion)
    {
        var actorId = IdUsuario(http.User);
        var nombreActor = await ObtenerNombreActor(db, http.User, cancelacion);
        await bitacora.Registrar(new EntradaBitacora(TipoActor.Usuario, actorId, nombreActor, organizacionId, accion, descripcion)
        {
            ObjetivoTipo = "membresia",
            ObjetivoId = objetivoId,
            Detalle = detalle,
            Ip = http.Connection.RemoteIpAddress?.ToString(),
        }, cancelacion);
    }

    private static async Task<string> ObtenerNombreActor(ShapiDbContext db, ClaimsPrincipal usuario, CancellationToken cancelacion)
    {
        var actorId = IdUsuario(usuario);
        var nombre = actorId is { } id
            ? await db.Set<Usuario>().IgnoreQueryFilters().Where(cuenta => cuenta.Id == id).Select(cuenta => cuenta.Nombre).SingleOrDefaultAsync(cancelacion)
            : null;
        return nombre ?? usuario.FindFirstValue(ClaimTypes.Name) ?? "Usuario";
    }

    private static IResult ProblemaConDetalle(int estado, string codigo, string titulo, string? limite = null)
    {
        object detalle = limite is null ? new Dictionary<string, string>() : new { limite };
        return TypedResults.Problem(statusCode: estado, title: titulo, type: "about:blank", extensions: new Dictionary<string, object?>
        {
            ["codigo"] = codigo,
            ["detalle"] = detalle,
        });
    }

    private static IResult InvitacionInvalida() => Problemas.Crear(StatusCodes.Status404NotFound, "invitacion_no_encontrada", "La invitación venció, ya se usó o no existe.");
    private static Guid? IdUsuario(ClaimsPrincipal usuario) => Guid.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static string RolTexto(Rol rol) => rol.ToString().ToLowerInvariant();

    private sealed record MiembroDto(Guid Id, Guid UsuarioId, string Nombre, string Correo, string Rol, bool EsActual);
}
