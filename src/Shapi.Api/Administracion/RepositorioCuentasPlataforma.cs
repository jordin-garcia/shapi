using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Aplicacion.Administracion;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Administracion;

/// <summary>Administración entre organizaciones: los filtros se omiten solo al buscar membresías de plataforma.</summary>
public sealed class RepositorioCuentasPlataforma(
    ShapiDbContext db, IReloj reloj, IColaCorreo correo, IBitacora bitacora,
    IPasswordHasher<Usuario> hasher) : IRepositorioCuentasPlataforma
{
    private IQueryable<Membresia> Membresias =>
        from m in db.Set<Membresia>().IgnoreQueryFilters()
        join o in db.Set<Organizacion>() on m.OrganizacionId equals o.Id
        where o.Tipo == TipoOrganizacion.Plataforma && (m.Rol == Rol.Administrador || m.Rol == Rol.Soporte)
        select m;

    private IQueryable<Usuario> Usuarios => db.Set<Usuario>().IgnoreQueryFilters()
        .Where(u => Membresias.Any(m => m.UsuarioId == u.Id));

    public async Task<IReadOnlyList<CuentaPlataforma>> Listar(CancellationToken cancelacion)
    {
        var filas = await (from u in Usuarios.AsNoTracking()
                           join m in Membresias.AsNoTracking() on u.Id equals m.UsuarioId
                           orderby u.CreadoEn, u.Id
                           select new { Usuario = u, m.Rol }).ToListAsync(cancelacion);
        return filas.Select(x => Dto(x.Usuario, x.Rol)).ToArray();
    }

    private static CuentaPlataforma Dto(Usuario u, Rol rol) => new(u.Id, u.Nombre, u.Correo,
        rol == Rol.Administrador ? "administrador" : "soporte", u.Estado == EstadoCuenta.Activo ? "activa" : "desactivada");

    public async Task<Resultado<CuentaPlataforma>> Crear(NuevaCuentaPlataforma solicitud, ActorCuenta actor, CancellationToken cancelacion)
    {
        var direccion = Usuario.NormalizarCorreo(solicitud.Correo!);
        if (await db.Set<Usuario>().IgnoreQueryFilters().AnyAsync(u => u.Correo == direccion, cancelacion))
        {
            return new Error(CodigosError.CorreoYaRegistrado, "El correo ya está registrado.");
        }

        var plataforma = await db.Set<Organizacion>().SingleAsync(o => o.Tipo == TipoOrganizacion.Plataforma, cancelacion);
        var usuario = new Usuario(solicitud.Nombre!, direccion);
        var rol = solicitud.Rol == "administrador" ? Rol.Administrador : Rol.Soporte;
        var valor = SeguridadTokens.GenerarToken();
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        db.AddRange(usuario, new Membresia(usuario.Id, plataforma.Id, rol),
            Token.DefinirContrasena(SeguridadTokens.HashearToken(valor), usuario, reloj.Ahora));
        try
        {
            await correo.Encolar("definir_contrasena", direccion, new
            {
                nombre = usuario.Nombre,
                token = valor,
            }, cancelacion);
            await Registrar(usuario, plataforma.Id, actor, AccionesBitacora.CuentaPlataformaCreada,
                $"Creó la cuenta de plataforma de {usuario.Nombre} con el rol de {solicitud.Rol}.", cancelacion);
            await db.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, TableName: "usuario" })
        {
            return new Error(CodigosError.CorreoYaRegistrado, "El correo ya está registrado.");
        }
        return Dto(usuario, rol);
    }

    public async Task<Resultado> CambiarEstado(Guid id, bool activar, ActorCuenta actor, CancellationToken cancelacion)
    {
        if (!activar && id == actor.Id)
        {
            return new Error(CodigosError.DatosInvalidos, "No puede desactivar su propia cuenta.");
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        // Serializa con el uso del enlace inicial y los cambios de estado de la misma cuenta.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM usuario WHERE id = {id} FOR UPDATE", cancelacion);
        var usuario = await Usuarios.SingleOrDefaultAsync(u => u.Id == id, cancelacion);
        if (usuario is null)
        {
            return new Error(CodigosError.DatosInvalidos, "No se encontró la cuenta.");
        }

        var membresia = await Membresias.SingleAsync(m => m.UsuarioId == id, cancelacion);
        if (activar)
        {
            usuario.Activar();
        }
        else
        {
            usuario.Desactivar();
            await db.Set<Sesion>().Where(s => s.UsuarioId == id && s.RevocadaEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, reloj.Ahora).SetProperty(x => x.ActualizadoEn, reloj.Ahora), cancelacion);
        }
        await Registrar(usuario, membresia.OrganizacionId, actor,
            activar ? AccionesBitacora.CuentaPlataformaActivada : AccionesBitacora.CuentaPlataformaDesactivada,
            $"{(activar ? "Activó" : "Desactivó")} la cuenta de plataforma de {usuario.Nombre}.", cancelacion);
        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return Resultado.Exito();
    }

    public async Task<Resultado<string>> DefinirContrasena(DefinicionContrasena solicitud, string host, string? ip, string? agente, CancellationToken cancelacion)
    {
        var invalido = new Error(CodigosError.TokenInvalido, "El enlace venció o ya se usó.");
        if (string.IsNullOrWhiteSpace(solicitud.Token))
        {
            return invalido;
        }

        var hash = SeguridadTokens.HashearToken(solicitud.Token);
        var token = await db.Set<Token>().AsNoTracking().SingleOrDefaultAsync(t => t.HashToken == hash
            && t.Tipo == TipoToken.DefinirContrasena && t.UsuarioId != null && t.ConsumidorId == null, cancelacion);
        if (token is null || !token.EsValido(reloj.Ahora))
        {
            return invalido;
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM usuario WHERE id = {token.UsuarioId} FOR UPDATE", cancelacion);
        var usuario = await Usuarios.SingleOrDefaultAsync(u => u.Id == token.UsuarioId, cancelacion);
        if (usuario is null || usuario.Estado != EstadoCuenta.Activo || usuario.HashContrasena is not null)
        {
            return invalido;
        }

        if (string.IsNullOrWhiteSpace(solicitud.Contrasena) || solicitud.Contrasena.Length is < 10 or > 128
            || string.Equals(solicitud.Contrasena.Trim(), usuario.Correo, StringComparison.OrdinalIgnoreCase))
        {
            return new Error(CodigosError.DatosInvalidos, "La contraseña debe tener entre 10 y 128 caracteres y ser distinta del correo.");
        }

        var ahora = reloj.Ahora;
        var usados = await db.Set<Token>().Where(t => t.Id == token.Id && t.UsadoEn == null && t.ExpiraEn > ahora)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), cancelacion);
        if (usados != 1)
        {
            return invalido;
        }

        usuario.DefinirHashContrasena(hasher.HashPassword(usuario, solicitud.Contrasena));
        usuario.VerificarCorreo(ahora);
        usuario.RegistrarInicioExitoso();
        var cookie = SeguridadTokens.GenerarToken();
        db.Add(Sesion.IniciarPersonal(SeguridadTokens.HashearToken(cookie), usuario.Id, host,
            IPAddress.TryParse(ip, out var direccion) ? direccion : null, agente, ahora));
        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return cookie;
    }

    private async Task Registrar(Usuario usuario, Guid organizacion, ActorCuenta actor, string accion, string descripcion, CancellationToken cancelacion)
    {
        var nombreActor = await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Id == actor.Id)
            .Select(u => u.Nombre).SingleAsync(cancelacion);
        await bitacora.Registrar(new EntradaBitacora(TipoActor.Usuario, actor.Id, nombreActor, organizacion, accion, descripcion)
        {
            ObjetivoTipo = "usuario",
            ObjetivoId = usuario.Id,
            Ip = actor.Ip,
        }, cancelacion);
    }
}
