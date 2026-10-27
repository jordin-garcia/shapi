using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Apis;

/// <summary>Desconecta dentro de la transacción del cambio de plan; publica únicamente después del commit.</summary>
public static class DominiosDelPlan
{
    public static async Task<List<DominioPropio>> Desconectar(ShapiDbContext db, Guid organizacionId, CancellationToken ct)
    {
        var dominios = await (from dominio in db.Set<DominioPropio>().IgnoreQueryFilters()
                              join api in db.Set<Api>().IgnoreQueryFilters() on dominio.ApiId equals api.Id
                              where api.OrganizacionId == organizacionId
                              select dominio).ToListAsync(ct);
        db.RemoveRange(dominios);
        return dominios;
    }

    public static async Task Publicar(IPublicadorCache publicador, IEnumerable<DominioPropio> dominios, CancellationToken ct)
    {
        foreach (var dominio in dominios)
        {
            await publicador.EliminarHost(dominio.Dominio, dominio.ApiId, ct);
            await publicador.PublicarApi(dominio.ApiId, ct);
        }
    }
}
