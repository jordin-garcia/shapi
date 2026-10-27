using System.Net;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Persistencia;
using AplicacionEntry = Shapi.Aplicacion.Comun.EntradaBitacora;
using DominioActorTipo = Shapi.Dominio.Bitacora.ActorTipo;
using DominioEntry = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Infraestructura.Bitacora;

/// <summary>Registra entradas en la tabla <c>bitacora</c>. Solo INSERT, nunca modifica.</summary>
public class BitacoraBaseDatos(ShapiDbContext db, IReloj reloj) : IBitacora
{
    public async Task Registrar(AplicacionEntry entrada, CancellationToken cancelacion = default)
    {
        var actorTipo = entrada.ActorTipo switch
        {
            TipoActor.Usuario => DominioActorTipo.Usuario,
            TipoActor.Consumidor => DominioActorTipo.Consumidor,
            TipoActor.Sistema => DominioActorTipo.Sistema,
            _ => throw new ArgumentOutOfRangeException(nameof(entrada.ActorTipo))
        };

        IPAddress? ip = entrada.Ip != null && IPAddress.TryParse(entrada.Ip, out var parsed)
            ? parsed
            : null;

        var detalle = entrada.Detalle != null
            ? System.Text.Json.JsonSerializer.Serialize(entrada.Detalle)
            : null;

        var entidad = new DominioEntry(
            reloj.Ahora,
            actorTipo,
            entrada.ActorId,
            await NombreActor(entrada, cancelacion),
            entrada.OrganizacionId,
            entrada.Accion,
            entrada.ObjetivoTipo,
            entrada.ObjetivoId,
            entrada.Descripcion.TrimEnd().TrimEnd('.') + ".",
            detalle,
            ip);

        db.Add(entidad);
        await db.SaveChangesAsync(cancelacion);
    }

    private Task<string> NombreActor(AplicacionEntry entrada, CancellationToken cancelacion) => entrada.ActorTipo switch
    {
        TipoActor.Usuario => db.Set<Usuario>().IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Id == entrada.ActorId).Select(u => u.Nombre).SingleAsync(cancelacion),
        TipoActor.Consumidor => db.Set<Consumidor>().IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.Id == entrada.ActorId).Select(c => c.Nombre).SingleAsync(cancelacion),
        TipoActor.Sistema => Task.FromResult("Sistema"),
        _ => throw new ArgumentOutOfRangeException(nameof(entrada.ActorTipo)),
    };
}
