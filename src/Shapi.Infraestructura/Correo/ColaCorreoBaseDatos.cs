using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Correo;

/// <summary>Encola un correo en <c>correo_saliente</c> para que JZ-03 lo envíe.</summary>
public class ColaCorreoBaseDatos(ShapiDbContext db, IReloj reloj) : IColaCorreo
{
    public async Task Encolar(string plantilla, string destinatario, object datos, CancellationToken cancelacion = default)
    {
        var datosJson = System.Text.Json.JsonSerializer.Serialize(datos);
        var correo = new CorreoSaliente(
            plantilla,
            destinatario,
            datosJson,
            AsuntoPara(plantilla),
            reloj.Ahora);
        db.Add(correo);
        await db.SaveChangesAsync(cancelacion);
    }

    internal static string AsuntoPara(string plantilla) => plantilla switch
    {
        "verificacion_correo" => "Verifique su correo",
        "recuperacion" => "Recupere su contraseña",
        "invitacion_miembro" => "Invitación a una organización",
        "invitacion_consumidor" => "Invitación a un portal",
        "definir_contrasena" => "Defina su contraseña",
        "pago_rechazado" => "No pudimos procesar su pago",
        "suscripcion_en_gracia" => "Su suscripción está en período de gracia",
        "suscripcion_suspendida" => "Su suscripción fue suspendida",
        "organizacion_suspendida" => "Su organización fue suspendida",
        "prueba_por_vencer" => "Su prueba está por vencer",
        "aviso_cuota_plataforma" => "Aviso de cuota de plataforma",
        "respuesta_caso" => "Nueva respuesta en su caso",
        _ => "Tiene una notificación",
    };
}
