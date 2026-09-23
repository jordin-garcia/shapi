namespace Shapi.Aplicacion.Comun;

/// <summary>Encola un correo en <c>correo_saliente</c> (10 §6). La API nunca envía por SMTP.</summary>
public interface IColaCorreo
{
    /// <param name="plantilla">Plantilla de 10 §6, por ejemplo <c>verificacion_correo</c>.</param>
    /// <param name="datos">Datos de la plantilla; se guardan como <c>jsonb</c>.</param>
    Task Encolar(string plantilla, string destinatario, object datos, CancellationToken cancelacion = default);
}
