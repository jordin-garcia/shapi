using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Trabajador.VerificacionDominios;

public sealed class VerificarDominiosPendientes(
    ShapiDbContext db, IResolutorDns resolutor, IReloj reloj, IBitacora bitacora, IPublicadorCache publicador)
{
    public async Task EjecutarAsync(CancellationToken ct = default)
    {
        var pendientes = await db.Set<DominioPropio>().IgnoreQueryFilters()
            .Where(d => d.Estado == EstadoDominio.Pendiente).ToListAsync(ct);
        foreach (var dominio in pendientes)
        {
            var ahora = reloj.Ahora;
            if (ahora - dominio.CreadoEn >= TimeSpan.FromHours(72))
            {
                dominio.MarcarFallido(ahora);
                continue;
            }

            if (!dominio.Verificar(await resolutor.ResolverCname(dominio.Dominio, ct), ahora))
            {
                continue;
            }

            var api = await db.Set<Api>().IgnoreQueryFilters().AsNoTracking().SingleAsync(a => a.Id == dominio.ApiId, ct);
            await bitacora.Registrar(new EntradaBitacora(TipoActor.Sistema, null, "Sistema", api.OrganizacionId,
                AccionesBitacora.DominioVerificado, $"Verificó el dominio {dominio.Dominio}.")
            { ObjetivoTipo = "api", ObjetivoId = api.Id }, ct);
        }
        await db.SaveChangesAsync(ct);
        foreach (var apiId in pendientes.Where(d => d.Estado == EstadoDominio.Verificado).Select(d => d.ApiId).Distinct())
        {
            await publicador.PublicarApi(apiId, ct);
        }
    }
}
