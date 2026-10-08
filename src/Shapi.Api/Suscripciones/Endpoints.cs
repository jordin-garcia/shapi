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

        app.MapGet("/api/planes-plataforma", (PlataformaSuscripcionesApi caso, CancellationToken ct) => caso.ListarPlanes(ct))
            .AllowAnonymous();
        var panel = app.MapGroup("/api/suscripcion");
        panel.MapGet("", (PlataformaSuscripcionesApi caso, HttpContext http, CancellationToken ct) => caso.Consultar(http, ct))
            .RequireAuthorization(Permisos.VerSuscripcion);
        panel.MapPost("/contratar", (PlataformaSuscripcionesApi caso, HttpContext http, PeticionSuscripcionPlataforma p, CancellationToken ct) => caso.Contratar(http, p, ct))
            .RequireAuthorization(Permisos.ContratarSuscripcion);
        panel.MapPost("/cambiar", (PlataformaSuscripcionesApi caso, HttpContext http, PeticionSuscripcionPlataforma p, CancellationToken ct) => caso.Cambiar(http, p, ct))
            .RequireAuthorization(Permisos.ContratarSuscripcion);
        panel.MapDelete("/cambio-programado", (PlataformaSuscripcionesApi caso, HttpContext http, CancellationToken ct) => caso.CancelarCambio(http, ct))
            .RequireAuthorization(Permisos.ContratarSuscripcion);
        panel.MapPost("/pagar", (PlataformaSuscripcionesApi caso, HttpContext http, PeticionPagoPlataforma p, CancellationToken ct) => caso.Pagar(http, p, ct))
            .RequireAuthorization(Permisos.ContratarSuscripcion);
        return app;
    }
}
