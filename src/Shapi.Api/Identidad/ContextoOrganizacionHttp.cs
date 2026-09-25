using Microsoft.AspNetCore.Http;
using Shapi.Aplicacion.Comun;

namespace Shapi.Api.Identidad;

/// <summary>
/// Organización de la petición actual (10 §2): la del claim que pone <see cref="PersonalAutenticacionHandler"/>
/// a partir de la membresía del usuario. Sin sesión es <c>null</c> y el filtro global no deja ver ninguna fila.
/// </summary>
public class ContextoOrganizacionHttp(IHttpContextAccessor httpContextAccessor) : IContextoOrganizacion
{
    public Guid? OrganizacionId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(PoliticasAutorizacion.ClaimOrganizacion)?.Value, out var id) && id != Guid.Empty
            ? id
            : null;
}
