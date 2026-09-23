using Microsoft.Extensions.Logging;
using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Comun;

/// <summary>
/// Implementación por defecto mientras no exista <c>correo_saliente</c> (la reemplaza EM-01).
/// Solo registra la plantilla: los datos pueden traer tokens y nunca se escriben en el log.
/// </summary>
public sealed class ColaCorreoNula(ILogger<ColaCorreoNula> registro) : IColaCorreo
{
    public Task Encolar(string plantilla, string destinatario, object datos, CancellationToken cancelacion = default)
    {
        registro.LogInformation("Correo no encolado (cola nula): plantilla {Plantilla}", plantilla);
        return Task.CompletedTask;
    }
}
