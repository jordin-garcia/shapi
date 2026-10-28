using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Bitacora;
using Shapi.Infraestructura.Correo;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Persistencia;

/// <summary>Criterio 6 de EM-01: la cola de correo y la bitácora escriben en la base de datos.</summary>
[Collection(nameof(PostgresPersistencia))]
public sealed class ServiciosBaseDatosTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    [Fact]
    public async Task RF_41_TodoElCatalogo_ResuelveNombreDeCuentaYConservaLaCopiaConPuntoFinal()
    {
        var usuario = new Usuario("Persona de la bitácora", $"{Guid.NewGuid():N}@ejemplo.test");
        await using (var db = CrearDb())
        {
            db.Add(usuario);
            await db.SaveChangesAsync();
            var bitacora = new BitacoraBaseDatos(db, Reloj);
            foreach (var accion in AccionesBitacora.Todas)
            {
                await bitacora.Registrar(new EntradaBitacora(TipoActor.Usuario, usuario.Id, "Administrador", null,
                    accion, "Acción sobre una organización S.A."));
            }
            await bitacora.Registrar(new EntradaBitacora(TipoActor.Sistema, null, "Trabajador", null,
                AccionesBitacora.SuscripcionSuspendida, "Suspendió por falta de pago"));
            var cambiadas = await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Id == usuario.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Nombre, "Nombre cambiado"));
            Assert.Equal(1, cambiadas);
        }

        await using var lectura = CrearDb();
        var entradas = await lectura.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().ToListAsync();
        var personales = entradas.Where(e => e.ActorId == usuario.Id).ToArray();
        Assert.Equal(AccionesBitacora.Todas.Order(), personales.Select(e => e.Accion).Order());
        Assert.All(personales, e =>
        {
            Assert.Equal("Persona de la bitácora", e.ActorNombre);
            Assert.Equal("Acción sobre una organización S.A.", e.Descripcion);
        });
        var sistema = Assert.Single(entradas, e => e.ActorTipo == Shapi.Dominio.Bitacora.ActorTipo.Sistema);
        Assert.Equal("Sistema", sistema.ActorNombre);
        Assert.Equal("Suspendió por falta de pago.", sistema.Descripcion);
    }

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
        var usuario = new Usuario("Ana López", $"{Guid.NewGuid():N}@ejemplo.test");
        var actor = usuario.Id;
        var objetivo = Guid.CreateVersion7();
        var entrada = new EntradaBitacora(TipoActor.Usuario, actor, "Propietario", organizacion,
            AccionesBitacora.SuscripcionSuspendida, "Suspendió la suscripción de Mercadito")
        {
            ObjetivoTipo = "suscripcion_api",
            ObjetivoId = objetivo,
            Detalle = new { motivo = "falta de pago" },
            Ip = "190.56.1.2",
        };

        await using (var db = CrearDb())
        {
            db.Add(usuario);
            await db.SaveChangesAsync();
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
        Assert.Equal("Suspendió la suscripción de Mercadito.", guardada.Descripcion);
        Assert.Contains("falta de pago", guardada.Detalle);
        Assert.Equal("190.56.1.2", guardada.Ip?.ToString());
    }
}
