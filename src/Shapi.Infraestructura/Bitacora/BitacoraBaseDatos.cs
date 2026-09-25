using System.Net;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Persistencia;
using AplicacionEntry = Shapi.Aplicacion.Comun.EntradaBitacora;
using DominioActorTipo = Shapi.Dominio.Bitacora.ActorTipo;
using DominioEntry = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Infraestructura.Bitacora;

/// <summary>Registra entradas en la tabla <c>bitacora</c>. Solo INSERT, nunca modifica.</summary>
public class BitacoraBaseDatos(ShapiDbContext db) : IBitacora
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
            actorTipo,
            entrada.ActorId,
            entrada.ActorNombre,
            entrada.OrganizacionId,
            entrada.Accion,
            entrada.ObjetivoTipo,
            entrada.ObjetivoId,
            entrada.Descripcion,
            detalle,
            ip);

        db.Add(entidad);
        await db.SaveChangesAsync(cancelacion);
    }
}
