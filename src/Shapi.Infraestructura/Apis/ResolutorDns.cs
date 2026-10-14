using DnsClient;
using DnsClient.Protocol;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Apis;
using Shapi.Dominio.Apis;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Apis;

public sealed class ResolutorDnsSimulado(ShapiDbContext db) : IResolutorDns
{
    public Task<string?> ResolverCname(string dominio, CancellationToken cancelacion = default) =>
        db.Set<RegistroDnsSimulado>().IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Nombre == dominio && r.Tipo == "CNAME")
            .Select(r => r.Valor).SingleOrDefaultAsync(cancelacion);
}

public sealed class ResolutorDnsReal : IResolutorDns
{
    private readonly LookupClient _cliente = new();

    public async Task<string?> ResolverCname(string dominio, CancellationToken cancelacion = default)
    {
        var respuesta = await _cliente.QueryAsync(dominio, QueryType.CNAME, cancellationToken: cancelacion);
        return respuesta.Answers.CnameRecords().FirstOrDefault()?.CanonicalName.Value.TrimEnd('.').ToLowerInvariant();
    }
}
