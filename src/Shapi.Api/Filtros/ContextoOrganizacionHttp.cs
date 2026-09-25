using Microsoft.AspNetCore.Http;
using Shapi.Aplicacion.Comun;

namespace Shapi.Api.Filtros;

public class ContextoOrganizacionHttp : IContextoOrganizacion
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContextoOrganizacionHttp(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OrganizacionId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user != null)
            {
                var orgIdStr = user.FindFirst("OrganizacionId")?.Value;
                if (Guid.TryParse(orgIdStr, out var orgId) && orgId != Guid.Empty)
                {
                    return orgId;
                }
            }
            return null;
        }
    }
}
