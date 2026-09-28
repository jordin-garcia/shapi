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
        var procesados = 0;

        while (procesados < TamanoLote)
        {
            cancelacion.ThrowIfCancellationRequested();

            // Cada correo se toma en su propia transacción con FOR UPDATE SKIP LOCKED: mientras se envía, otro
            // trabajador salta ese correo y toma el siguiente (auditoría H-61).
            await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
            var correo = await TomarSiguiente(ahora, cancelacion);
            if (correo is null)
            {
                return procesados;
            }

            await Procesar(correo, cancelacion);

            // Si el correo ya salió, el resultado se guarda aunque el trabajador se esté deteniendo, para no
            // reenviarlo (auditoría H-63).
            await db.SaveChangesAsync(CancellationToken.None);
            await transaccion.CommitAsync(CancellationToken.None);
            procesados++;
        }

        return procesados;
    }

    private async Task<CorreoSaliente?> TomarSiguiente(DateTimeOffset ahora, CancellationToken cancelacion)
    {
        var correos = await db.Set<CorreoSaliente>()
            .FromSql($"""
                SELECT * FROM correo_saliente
                WHERE estado = 'pendiente' AND (proximo_intento_en IS NULL OR proximo_intento_en <= {ahora})
                ORDER BY proximo_intento_en, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancelacion);

        return correos.SingleOrDefault();
    }

    private async Task Procesar(CorreoSaliente correo, CancellationToken cancelacion)
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
    }
}
