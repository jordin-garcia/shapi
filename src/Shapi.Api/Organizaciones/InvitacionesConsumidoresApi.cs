using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using ApiDominio = Shapi.Dominio.Apis.Api;

namespace Shapi.Api.Organizaciones;

/// <summary>Invitaciones a cuentas consumidoras desde el panel del proveedor (CU-23, B1.5).</summary>
public static class InvitacionesConsumidoresApi
{
    public static IEndpointRouteBuilder MapearEndpointsInvitacionesConsumidores(this IEndpointRouteBuilder app)
    {
        var invitaciones = app.MapGroup("/api/apis/{apiId:guid}/invitaciones")
            .RequireAuthorization(Permisos.InvitarConsumidores);
        invitaciones.MapGet("", Listar);
        invitaciones.MapPost("", Crear);
        invitaciones.MapPost("/{invitacionId:guid}/reenviar", Reenviar);
        invitaciones.MapDelete("/{invitacionId:guid}", Cancelar);
        return app;
    }

    private static async Task<IResult> Listar(
        Guid apiId,
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IReloj reloj,
        CancellationToken ct)
    {
        if (contexto.OrganizacionId is not { } organizacionId)
        {
            return TypedResults.NotFound();
        }

        var api = await ObtenerApi(db, apiId, organizacionId, ct);
        if (api is null)
        {
            return TypedResults.NotFound();
        }

        var elementos = await db.Set<Token>().IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.OrganizacionId == organizacionId && t.Tipo == TipoToken.InvitacionConsumidor
                && t.ConsumidorId == null && t.UsadoEn == null && t.ExpiraEn > reloj.Ahora)
            .OrderBy(t => t.CreadoEn)
            .Select(t => new InvitacionConsumidorDto(t.Id, t.Correo, t.CreadoEn, t.ExpiraEn))
            .ToListAsync(ct);
        return TypedResults.Ok(new ListaInvitacionesConsumidoresDto(api.Nombre, api.PortalNombre ?? api.Nombre, elementos));
    }

    private static async Task<IResult> Crear(
        Guid apiId,
        [FromBody] PeticionInvitarConsumidor peticion,
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IColaCorreo colaCorreo,
        [FromServices] IReloj reloj,
        [FromServices] IConfiguration configuracion,
        CancellationToken ct)
    {
        var correoRecibido = peticion.Correo?.Trim();
        if (!CorreoValido(correoRecibido))
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise el correo.", new Dictionary<string, string[]> { ["correo"] = ["Escriba un correo válido."] });
        }

        if (contexto.OrganizacionId is not { } organizacionId)
        {
            return TypedResults.NotFound();
        }

        var api = await ObtenerApi(db, apiId, organizacionId, ct);
        if (api is null)
        {
            return TypedResults.NotFound();
        }

        var correo = Usuario.NormalizarCorreo(correoRecibido!);
        if (await db.Set<Consumidor>().IgnoreQueryFilters().AnyAsync(c => c.OrganizacionId == organizacionId && c.Correo == correo, ct))
        {
            return Problemas.Crear(StatusCodes.Status409Conflict, "consumidor_existente", "Ya existe una cuenta de consumidor con este correo.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        _ = await db.Set<Organizacion>().FromSqlInterpolated($"SELECT * FROM organizacion WHERE id = {organizacionId} FOR UPDATE")
            .IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(ct);
        if (await db.Set<Consumidor>().IgnoreQueryFilters().AnyAsync(c => c.OrganizacionId == organizacionId && c.Correo == correo, ct))
        {
            return Problemas.Crear(StatusCodes.Status409Conflict, "consumidor_existente", "Ya existe una cuenta de consumidor con este correo.");
        }

        if (await InvitacionPendiente(db, organizacionId, correo, reloj.Ahora, ct))
        {
            return Problemas.Crear(StatusCodes.Status409Conflict, "invitacion_pendiente", "Ya existe una invitación pendiente para este correo.");
        }

        var token = SeguridadTokens.GenerarToken();
        db.Add(Token.InvitacionConsumidor(SeguridadTokens.HashearToken(token), organizacionId, correo, reloj.Ahora));
        await EncolarCorreo(colaCorreo, api, correo, token, configuracion, ct);
        await tx.CommitAsync(ct);
        return TypedResults.StatusCode(StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> Reenviar(
        Guid apiId,
        Guid invitacionId,
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IColaCorreo colaCorreo,
        [FromServices] IReloj reloj,
        [FromServices] IConfiguration configuracion,
        CancellationToken ct)
    {
        if (contexto.OrganizacionId is not { } organizacionId)
        {
            return TypedResults.NotFound();
        }

        var api = await ObtenerApi(db, apiId, organizacionId, ct);
        if (api is null)
        {
            return TypedResults.NotFound();
        }

        var ahora = reloj.Ahora;
        var invitacion = await db.Set<Token>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(t => t.Id == invitacionId
            && t.OrganizacionId == organizacionId && t.Tipo == TipoToken.InvitacionConsumidor && t.ConsumidorId == null
            && t.UsadoEn == null && t.ExpiraEn > ahora, ct);
        if (invitacion is null)
        {
            return TypedResults.NotFound();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actualizada = await db.Set<Token>().IgnoreQueryFilters().Where(t => t.Id == invitacionId && t.UsadoEn == null && t.ExpiraEn > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), ct);
        if (actualizada == 0)
        {
            return TypedResults.NotFound();
        }

        var token = SeguridadTokens.GenerarToken();
        db.Add(Token.InvitacionConsumidor(SeguridadTokens.HashearToken(token), organizacionId, invitacion.Correo, ahora));
        await EncolarCorreo(colaCorreo, api, invitacion.Correo, token, configuracion, ct);
        await tx.CommitAsync(ct);
        return TypedResults.StatusCode(StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> Cancelar(
        Guid apiId,
        Guid invitacionId,
        [FromServices] ShapiDbContext db,
        [FromServices] IContextoOrganizacion contexto,
        [FromServices] IReloj reloj,
        CancellationToken ct)
    {
        if (contexto.OrganizacionId is not { } organizacionId || !await ExisteApi(db, apiId, organizacionId, ct))
        {
            return TypedResults.NotFound();
        }

        var ahora = reloj.Ahora;
        var filas = await db.Set<Token>().IgnoreQueryFilters().Where(t => t.Id == invitacionId && t.OrganizacionId == organizacionId
            && t.Tipo == TipoToken.InvitacionConsumidor && t.ConsumidorId == null && t.UsadoEn == null && t.ExpiraEn > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), ct);
        return filas == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static async Task<ApiDominio?> ObtenerApi(ShapiDbContext db, Guid apiId, Guid organizacionId, CancellationToken ct) =>
        await db.Set<ApiDominio>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, ct);

    private static async Task<bool> ExisteApi(ShapiDbContext db, Guid apiId, Guid organizacionId, CancellationToken ct) =>
        await db.Set<ApiDominio>().IgnoreQueryFilters().AnyAsync(a => a.Id == apiId && a.OrganizacionId == organizacionId, ct);

    private static Task<bool> InvitacionPendiente(ShapiDbContext db, Guid organizacionId, string correo, DateTimeOffset ahora, CancellationToken ct) =>
        db.Set<Token>().IgnoreQueryFilters().AnyAsync(t => t.OrganizacionId == organizacionId && t.Correo == correo
            && t.Tipo == TipoToken.InvitacionConsumidor && t.ConsumidorId == null && t.UsadoEn == null && t.ExpiraEn > ahora, ct);

    private static async Task EncolarCorreo(IColaCorreo colaCorreo, ApiDominio api, string correo, string token, IConfiguration configuracion, CancellationToken ct)
    {
        var dominioBase = configuracion["SHAPI_DOMINIO_BASE"]?.Trim().TrimEnd('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(dominioBase))
        {
            dominioBase = "shapi.localhost";
        }

        await colaCorreo.Encolar("invitacion_consumidor", correo, new
        {
            nombre = correo,
            nombreApi = api.Nombre,
            token,
            nombrePortal = api.PortalNombre ?? api.Nombre,
            hostPortal = $"{api.Subdominio}.{dominioBase}",
            colorPortal = api.PortalColor,
            logoPortal = api.PortalLogo is null ? null : "true",
        }, ct);
    }

    private static bool CorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo) || correo.Length > 320)
        {
            return false;
        }

        try { return string.Equals(new MailAddress(correo).Address, correo, StringComparison.OrdinalIgnoreCase); }
        catch (FormatException) { return false; }
    }

    public sealed record PeticionInvitarConsumidor(string? Correo);
    private sealed record ListaInvitacionesConsumidoresDto(string NombreApi, string NombrePortal, IReadOnlyList<InvitacionConsumidorDto> Elementos);
    private sealed record InvitacionConsumidorDto(Guid Id, string Correo, DateTimeOffset EnviadaEn, DateTimeOffset VenceEn);
}
