using System;
using System.Threading;
using System.Threading.Tasks;
using Shapi.Dominio.Identidad;

namespace Shapi.Aplicacion.Identidad;

public interface IServicioRecuperacion
{
    /// <summary>
    /// Genera y encola un enlace de recuperación de contraseña para el correo dado, si existe la cuenta.
    /// Para consumidores, se espera que <paramref name="hostPortal"/>, <paramref name="nombrePortal"/> y <paramref name="colorPortal"/> tengan valor.
    /// </summary>
    Task Solicitar(
        string correo,
        AmbitoSesion ambito,
        string? hostPortal = null,
        string? nombrePortal = null,
        string? colorPortal = null,
        bool logoPortal = false,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Restablece la contraseña utilizando el token provisto.
    /// Devuelve el ID de usuario o consumidor actualizado, o null si el token es inválido o venció.
    /// </summary>
    Task<RecuperacionExitosa?> Restablecer(
        string token,
        string nuevaContrasena,
        CancellationToken cancelacion = default);
}

public record RecuperacionExitosa(Guid? UsuarioId, Guid? ConsumidorId, Guid? OrganizacionId, string Nombre, AmbitoSesion Ambito);
