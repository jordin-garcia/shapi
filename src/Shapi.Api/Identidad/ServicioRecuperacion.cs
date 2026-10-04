using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Identidad;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Identidad;

public class ServicioRecuperacion : IServicioRecuperacion
{
    private readonly ShapiDbContext _db;
    private readonly IColaCorreo _colaCorreo;
    private readonly IReloj _reloj;
    private readonly IPasswordHasher<Usuario> _hasherUsuario;
    private readonly IPasswordHasher<Consumidor> _hasherConsumidor;

    public ServicioRecuperacion(
        ShapiDbContext db,
        IColaCorreo colaCorreo,
        IReloj reloj,
        IPasswordHasher<Usuario> hasherUsuario,
        IPasswordHasher<Consumidor> hasherConsumidor)
    {
        _db = db;
        _colaCorreo = colaCorreo;
        _reloj = reloj;
        _hasherUsuario = hasherUsuario;
        _hasherConsumidor = hasherConsumidor;
    }

    public async Task Solicitar(
        string correo,
        AmbitoSesion ambito,
        string? hostPortal = null,
        string? nombrePortal = null,
        string? colorPortal = null,
        bool logoPortal = false,
        CancellationToken cancelacion = default,
        Guid? organizacionPortalId = null)
    {
        correo = Usuario.NormalizarCorreo(correo);
        var ahora = _reloj.Ahora;
        Guid? usuarioId = null;
        Guid? consumidorId = null;
        Guid? organizacionId = null;
        string? nombre = null;

        if (ambito == AmbitoSesion.Personal)
        {
            var usuario = await _db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correo, cancelacion);
            if (usuario is not null && usuario.Estado != EstadoCuenta.Desactivado)
            {
                usuarioId = usuario.Id;
                nombre = usuario.Nombre;
            }
        }
        else
        {
            var consumidor = await _db.Set<Consumidor>().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Correo == correo && (organizacionPortalId == null || c.OrganizacionId == organizacionPortalId), cancelacion);
            if (consumidor is not null && consumidor.Estado == EstadoCuenta.Activo)
            {
                consumidorId = consumidor.Id;
                organizacionId = consumidor.OrganizacionId;
                nombre = consumidor.Nombre;
            }
        }

        // 10 §1: como máximo 3 solicitudes por hora por cuenta. La consulta se hace también si la cuenta no existe,
        // sobre un ID que no existe, para que las dos rutas hagan el mismo trabajo (10 §8).
        var cuentaId = usuarioId ?? consumidorId ?? Guid.Empty;
        var desde = ahora - TimeSpan.FromHours(1);
        var recientes = await _db.Set<Token>().IgnoreQueryFilters()
            .CountAsync(t => t.Tipo == TipoToken.Recuperacion && (t.UsuarioId == cuentaId || t.ConsumidorId == cuentaId)
                && t.CreadoEn > desde, cancelacion);

        var valorToken = SeguridadTokens.GenerarToken();
        var hash = SeguridadTokens.HashearToken(valorToken);

        // Las dos ramas escriben dentro de una transacción, para hacer los mismos viajes a la base, incluido el COMMIT.
        await using var transaccion = await _db.Database.BeginTransactionAsync(cancelacion);
        if (nombre is null || recientes >= SolicitudesPorHora)
        {
            // 10 §8: el mismo tiempo de respuesta exista o no la cuenta. En vez de guardar el token y el correo, una
            // escritura sobre una fila que no existe, con el mismo número de comandos.
            await _db.Set<Token>().IgnoreQueryFilters()
                .Where(t => t.Id == Guid.Empty)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ActualizadoEn, ahora), cancelacion);
            await transaccion.CommitAsync(cancelacion);
            return;
        }

        var token = Token.Recuperacion(hash, usuarioId, consumidorId, organizacionId, correo, ahora);
        _db.Add(token);

        var datosCorreo = ambito == AmbitoSesion.Personal
            ? (object)new { nombre, token = valorToken }
            : new { nombre, token = valorToken, hostPortal, nombrePortal, colorPortal, logoPortal = logoPortal ? "true" : null };

        // Encolar guarda el token y el correo en el mismo SaveChanges.
        await _colaCorreo.Encolar("recuperacion", correo, datosCorreo, cancelacion);
        await transaccion.CommitAsync(cancelacion);
    }

    /// <summary>10 §1: como máximo 3 solicitudes de recuperación por hora por cuenta; las demás responden igual sin enviar.</summary>
    public const int SolicitudesPorHora = 3;

    public async Task<RecuperacionExitosa?> Restablecer(
        string token,
        string nuevaContrasena,
        CancellationToken cancelacion = default,
        Guid? organizacionPortalId = null)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(nuevaContrasena))
        {
            return null;
        }

        var hash = SeguridadTokens.HashearToken(token);
        var ahora = _reloj.Ahora;

        var entidadToken = await _db.Set<Token>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.HashToken == hash && t.Tipo == TipoToken.Recuperacion
                && (organizacionPortalId == null
                    ? t.UsuarioId != null && t.ConsumidorId == null
                    : t.ConsumidorId != null && t.UsuarioId == null && t.OrganizacionId == organizacionPortalId), cancelacion);

        if (entidadToken is null || !entidadToken.EsValido(ahora))
        {
            return null;
        }

        await using var transaccion = await _db.Database.BeginTransactionAsync(cancelacion);

        var marcados = await _db.Set<Token>().IgnoreQueryFilters()
            .Where(t => t.Id == entidadToken.Id && t.UsadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), cancelacion);

        if (marcados == 0)
        {
            return null;
        }

        // 10 §1 (auditoría 2026-10-03): los demás enlaces de recuperación pendientes de la cuenta dejan de servir.
        await _db.Set<Token>().IgnoreQueryFilters()
            .Where(t => t.Tipo == TipoToken.Recuperacion && t.UsadoEn == null && t.Id != entidadToken.Id
                && (entidadToken.UsuarioId != null ? t.UsuarioId == entidadToken.UsuarioId : t.ConsumidorId == entidadToken.ConsumidorId))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora).SetProperty(t => t.ActualizadoEn, ahora), cancelacion);

        RecuperacionExitosa resultado;

        if (entidadToken.UsuarioId.HasValue)
        {
            var usuario = await _db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == entidadToken.UsuarioId.Value, cancelacion);
            if (usuario.Estado == EstadoCuenta.Desactivado)
            {
                await transaccion.CommitAsync(cancelacion);
                return null;
            }

            var hashContrasena = _hasherUsuario.HashPassword(usuario, nuevaContrasena);
            usuario.DefinirHashContrasena(hashContrasena);

            // Quien restablece la contraseña recupera el acceso: se reinicia el bloqueo por intentos fallidos.
            usuario.RegistrarInicioExitoso();

            // Revocar sesiones existentes
            await _db.Set<Sesion>().IgnoreQueryFilters()
                .Where(s => s.UsuarioId == usuario.Id && s.RevocadaEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, ahora).SetProperty(x => x.ActualizadoEn, ahora), cancelacion);

            resultado = new RecuperacionExitosa(usuario.Id, null, null, usuario.Nombre, AmbitoSesion.Personal);
        }
        else
        {
            var consumidor = await _db.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(c => c.Id == entidadToken.ConsumidorId, cancelacion);
            if (consumidor.Estado != EstadoCuenta.Activo)
            {
                await transaccion.CommitAsync(cancelacion);
                return null;
            }
            consumidor.DefinirHashContrasena(_hasherConsumidor.HashPassword(consumidor, nuevaContrasena));
            consumidor.RegistrarInicioExitoso();
            await _db.Set<Sesion>().IgnoreQueryFilters()
                .Where(s => s.ConsumidorId == consumidor.Id && s.RevocadaEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, ahora).SetProperty(x => x.ActualizadoEn, ahora), cancelacion);
            resultado = new RecuperacionExitosa(null, consumidor.Id, consumidor.OrganizacionId, consumidor.Nombre, AmbitoSesion.Consumidor);
        }

        await _db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return resultado;
    }
}
