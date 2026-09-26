using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Correo;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Trabajador.Correo;

public sealed class ProcesadorCorreos(
    ShapiDbContext db,
    MotorPlantillasCorreo motorPlantillas,
    IEnviadorCorreo enviador,
    IReloj reloj,
    ILogger<ProcesadorCorreos> registro)
{
    private const int TamanoLote = 50;

    public async Task<int> ProcesarPendientes(CancellationToken cancelacion = default)
    {
        var ahora = reloj.Ahora;
        var correos = await db.Set<CorreoSaliente>()
            .Where(correo => correo.Estado == EstadoCorreo.Pendiente
                && (correo.ProximoIntentoEn == null || correo.ProximoIntentoEn <= ahora))
            .OrderBy(correo => correo.ProximoIntentoEn)
            .ThenBy(correo => correo.Id)
            .Take(TamanoLote)
            .ToListAsync(cancelacion);

        foreach (var correo in correos)
        {
            try
            {
                var contenido = motorPlantillas.Renderizar(correo.Plantilla, correo.Datos);
                await enviador.EnviarAsync(correo, contenido, cancelacion);
                correo.MarcarEnviado(reloj.Ahora);
                registro.LogInformation(
                    "Correo {CorreoId} enviado con la plantilla {Plantilla}",
                    correo.Id,
                    correo.Plantilla);
            }
            catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception excepcion)
            {
                correo.RegistrarFallo(excepcion.Message, reloj.Ahora);
                registro.LogWarning(
                    excepcion,
                    "Falló el intento {Intento} del correo {CorreoId} con la plantilla {Plantilla}",
                    correo.Intentos,
                    correo.Id,
                    correo.Plantilla);
            }

            await db.SaveChangesAsync(cancelacion);
        }

        return correos.Count;
    }
}
