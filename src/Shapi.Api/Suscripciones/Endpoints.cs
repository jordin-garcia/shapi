using Shapi.Api.Identidad;
using Shapi.Api.Suscripciones;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Suscripciones;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsSuscripciones(this IEndpointRouteBuilder app)
    {
        var portal = app.MapGroup("/api/portal");
        portal.MapPost("/suscripciones", (ContratacionApi caso, HttpContext http, PeticionContratarPlan cuerpo, CancellationToken ct) =>
                caso.Contratar(http, cuerpo, ct))
            .RequireAuthorization(Permisos.ConsumidorContratarPlan);
        portal.MapGet("/suscripcion", (ContratacionApi caso, HttpContext http, CancellationToken ct) => caso.Consultar(http, ct))
            .RequireAuthorization(Permisos.ConsumidorVerCuenta);
        return app;
    }
}
