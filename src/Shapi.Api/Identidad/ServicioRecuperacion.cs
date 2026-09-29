using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Persistencia;
using Shapi.Infraestructura.Identidad;

namespace Shapi.Api.Identidad;

public class ServicioRecuperacion : IServicioRecuperacion
{
    private readonly ShapiDbContext _db;
    private readonly IColaCorreo _colaCorreo;
    private readonly IReloj _reloj;
    private readonly IPasswordHasher<Usuario> _hasherUsuario;

    public ServicioRecuperacion(
        ShapiDbContext db,
        IColaCorreo colaCorreo,
        IReloj reloj,
        IPasswordHasher<Usuario> hasherUsuario)
    {
        _db = db;
        _colaCorreo = colaCorreo;
        _reloj = reloj;
        _hasherUsuario = hasherUsuario;
    }

    public async Task Solicitar(
        string correo, 
        AmbitoSesion ambito, 
        string? hostPortal = null, 
        string? nombrePortal = null, 
        string? colorPortal = null, 
        bool logoPortal = false,
        CancellationToken cancelacion = default)
    {
        correo = Usuario.NormalizarCorreo(correo);
        var ahora = _reloj.Ahora;
        Guid? usuarioId = null;
        Guid? consumidorId = null;
        Guid? organizacionId = null;
        string nombre;

        if (ambito == AmbitoSesion.Personal)
        {
            var usuario = await _db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correo, cancelacion);
            if (usuario is null || usuario.Estado == EstadoCuenta.Desactivado) return;
            usuarioId = usuario.Id;
            nombre = usuario.Nombre;
        }
        else
        {
            throw new NotImplementedException("Recuperación para consumidores se implementará en EM-05.");
        }

        var valorToken = SeguridadTokens.GenerarToken();
        var hash = SeguridadTokens.HashearToken(valorToken);

        var token = Token.Recuperacion(hash, usuarioId, consumidorId, organizacionId, correo, ahora);
        _db.Add(token);

        var datosCorreo = ambito == AmbitoSesion.Personal 
            ? (object)new { nombre, token = valorToken }
            : new { nombre, token = valorToken, hostPortal, nombrePortal, colorPortal, logoPortal = logoPortal ? "true" : null };

        await _colaCorreo.Encolar("recuperacion", correo, datosCorreo, cancelacion);
        await _db.SaveChangesAsync(cancelacion);
    }

    public async Task<RecuperacionExitosa?> Restablecer(
        string token, 
        string nuevaContrasena, 
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(nuevaContrasena))
        {
            return null;
        }

        var hash = SeguridadTokens.HashearToken(token);
        var ahora = _reloj.Ahora;
        
        var entidadToken = await _db.Set<Token>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.HashToken == hash && t.Tipo == TipoToken.Recuperacion, cancelacion);

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
            
            // Revocar sesiones existentes
            await _db.Set<Sesion>().IgnoreQueryFilters()
                .Where(s => s.UsuarioId == usuario.Id && s.RevocadaEn == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, ahora).SetProperty(x => x.ActualizadoEn, ahora), cancelacion);

            resultado = new RecuperacionExitosa(usuario.Id, null, null, usuario.Nombre, AmbitoSesion.Personal);
        }
        else
        {
            throw new NotImplementedException("Recuperación para consumidores se implementará en EM-05.");
        }

        await _db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return resultado;
    }
}
