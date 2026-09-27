using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Bitacora;
using Shapi.Infraestructura.Correo;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Persistencia;

/// <summary>Criterio 6 de EM-01: la cola de correo y la bitácora escriben en la base de datos.</summary>
[Collection(nameof(PostgresPersistencia))]
public sealed class ServiciosBaseDatosTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    [Fact]
    public async Task RF_46_Encolar_InsertaUnCorreoPendiente()
    {
        await using (var db = CrearDb())
        {
            await new ColaCorreoBaseDatos(db, Reloj).Encolar("verificacion_correo", "ana@ejemplo.com", new { enlace = "https://x" });
        }

        await using var lectura = CrearDb();
        var correo = await lectura.Set<CorreoSaliente>().SingleAsync();
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
        Assert.Equal("verificacion_correo", correo.Plantilla);
        Assert.Equal("ana@ejemplo.com", correo.Destinatario);
        Assert.Equal("Verifique su correo", correo.Asunto);
        Assert.Contains("\"enlace\": \"https://x\"", correo.Datos);
        Assert.Equal(0, correo.Intentos);
        Assert.Equal(Reloj.Ahora, correo.ProximoIntentoEn);
    }

    [Fact]
    public async Task RF_41_Registrar_InsertaLaEntradaConLaHoraDelReloj()
    {
        var organizacion = await NuevaOrganizacion();
        var actor = Guid.CreateVersion7();
        var objetivo = Guid.CreateVersion7();
        var entrada = new EntradaBitacora(TipoActor.Usuario, actor, "Ana López", organizacion,
            AccionesBitacora.SuscripcionSuspendida, "Suspendió la suscripción de Mercadito")
        {
            ObjetivoTipo = "suscripcion_api",
            ObjetivoId = objetivo,
            Detalle = new { motivo = "falta de pago" },
            Ip = "190.56.1.2",
        };

        await using (var db = CrearDb())
        {
            await new BitacoraBaseDatos(db, Reloj).Registrar(entrada);
        }

        Contexto.OrganizacionId = organizacion;
        await using var lectura = CrearDb();
        var guardada = await lectura.Set<EntradaBitacoraDominio>().SingleAsync();
        Assert.Equal(Reloj.Ahora, guardada.Fecha);
        Assert.Equal(Shapi.Dominio.Bitacora.ActorTipo.Usuario, guardada.ActorTipo);
        Assert.Equal(actor, guardada.ActorId);
        Assert.Equal("Ana López", guardada.ActorNombre);
        Assert.Equal(organizacion, guardada.OrganizacionId);
        Assert.Equal(AccionesBitacora.SuscripcionSuspendida, guardada.Accion);
        Assert.Equal("suscripcion_api", guardada.ObjetivoTipo);
        Assert.Equal(objetivo, guardada.ObjetivoId);
        Assert.Equal("Suspendió la suscripción de Mercadito", guardada.Descripcion);
        Assert.Contains("falta de pago", guardada.Detalle);
        Assert.Equal("190.56.1.2", guardada.Ip?.ToString());
    }
}
