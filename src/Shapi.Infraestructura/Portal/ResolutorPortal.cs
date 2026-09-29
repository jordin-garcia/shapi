using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shapi.Aplicacion.Portal;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Portal;

/// <summary>Resolución del portal antes de que exista un contexto de organización.</summary>
public sealed class ResolutorPortal : IResolutorPortal
{
    private readonly ShapiDbContext _db;
    private readonly string _dominioBase;

    public ResolutorPortal(ShapiDbContext db, IConfiguration configuracion)
    {
        _db = db;
        _dominioBase = (configuracion["SHAPI_DOMINIO_BASE"] ?? "shapi.localhost")
            .Trim()
            .TrimEnd('.')
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(_dominioBase))
        {
            throw new InvalidOperationException("SHAPI_DOMINIO_BASE no puede estar vacío.");
        }
    }

    public Task<PortalResuelto?> Resolver(string host, CancellationToken cancelacion = default)
    {
        var subdominio = ExtraerSubdominio(host);
        if (subdominio is null)
        {
            return Task.FromResult<PortalResuelto?>(null);
        }

        return (
            from api in _db.Set<Api>().IgnoreQueryFilters().AsNoTracking()
            join organizacion in _db.Set<Organizacion>().IgnoreQueryFilters().AsNoTracking()
                on api.OrganizacionId equals organizacion.Id
            where api.Subdominio == subdominio && api.Estado == EstadoApi.Publicada
            select new PortalResuelto(
                api.Id,
                organizacion.Id,
                organizacion.Nombre,
                api.Subdominio,
                api.Nombre,
                api.EspecificacionDescripcion,
                api.PortalNombre ?? api.Nombre,
                api.PortalColor,
                api.PortalBienvenida,
                api.PortalLogo,
                api.PortalLogoTipo,
                api.Subdominio + "." + _dominioBase,
                api.Subdominio + ".api." + _dominioBase))
            .SingleOrDefaultAsync(cancelacion);
    }

    private string? ExtraerSubdominio(string host)
    {
        var normalizado = host.Trim().TrimEnd('.').ToLowerInvariant();
        var sufijo = "." + _dominioBase;
        if (!normalizado.EndsWith(sufijo, StringComparison.Ordinal))
        {
            return null;
        }

        var subdominio = normalizado[..^sufijo.Length];
        return string.IsNullOrEmpty(subdominio) || subdominio.Contains('.', StringComparison.Ordinal)
            ? null
            : subdominio;
    }
}
