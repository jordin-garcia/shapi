using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Correo;

/// <summary>Encola un correo en <c>correo_saliente</c> para que JZ-03 lo envíe.</summary>
public class ColaCorreoBaseDatos(ShapiDbContext db) : IColaCorreo
{
    public async Task Encolar(string plantilla, string destinatario, object datos, CancellationToken cancelacion = default)
    {
        var datosJson = System.Text.Json.JsonSerializer.Serialize(datos);
        var correo = new CorreoSaliente(
            EstadoCorreo.Pendiente,
            plantilla,
            destinatario,
            datosJson,
            $"Notificación Shapi - {plantilla}");
        db.Add(correo);
        await db.SaveChangesAsync(cancelacion);
    }
}
