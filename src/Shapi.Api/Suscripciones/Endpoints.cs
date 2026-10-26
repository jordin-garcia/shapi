using Shapi.Api.Identidad;
using Shapi.Api.Suscripciones;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Suscripciones;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsSuscripciones(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/demo/reloj", (RelojDemoApi caso) => caso.Consultar())
            .RequireAuthorization(Permisos.GestionarPlanesPlataforma);
        app.MapGet("/api/reloj", (Shapi.Aplicacion.Comun.IReloj reloj) => RelojDemoApi.ConsultarFecha(reloj))
            .RequireAuthorization();
        app.MapGet("/api/portal/reloj", (Shapi.Aplicacion.Comun.IReloj reloj) => RelojDemoApi.ConsultarFecha(reloj))
            .RequireAuthorization(Permisos.ConsumidorVerCuenta);
        app.MapPost("/api/admin/demo/reloj", (RelojDemoApi caso, PeticionRelojDemo peticion, CancellationToken ct) => caso.Adelantar(peticion, ct))
            .RequireAuthorization(Permisos.GestionarPlanesPlataforma);
        var portal = app.MapGroup("/api/portal");
        portal.MapPost("/suscripciones", (ContratacionApi caso, HttpContext http, PeticionContratarPlan cuerpo, CancellationToken ct) =>
                caso.Contratar(http, cuerpo, ct))
            .RequireAuthorization(Permisos.ConsumidorContratarPlan);
        portal.MapGet("/suscripcion", (ContratacionApi caso, HttpContext http, CancellationToken ct) => caso.Consultar(http, ct))
            .RequireAuthorization(Permisos.ConsumidorVerCuenta);
        portal.MapDelete("/suscripcion/cambio-programado", (ContratacionApi caso, HttpContext http, CancellationToken ct) => caso.CancelarCambio(http, ct))
            .RequireAuthorization(Permisos.ConsumidorContratarPlan);
        portal.MapPost("/suscripcion/pagar", (ContratacionApi caso, HttpContext http, PeticionPagarSuscripcionApi cuerpo, CancellationToken ct) => caso.Pagar(http, cuerpo, ct))
            .RequireAuthorization(Permisos.ConsumidorContratarPlan);
        portal.MapPost("/suscripcion/cambiar", (ContratacionApi caso, HttpContext http, PeticionCambiarPlanApi cuerpo, CancellationToken ct) => caso.CambiarPlan(http, cuerpo, ct))
            .RequireAuthorization(Permisos.ConsumidorContratarPlan);

        app.MapGet("/api/planes-plataforma", (PlataformaSuscripcionesApi caso, CancellationToken ct) => caso.ListarPlanes(ct))
            .AllowAnonymous();
        var panel = app.MapGroup("/api/suscripcion");
        panel.MapGet("", (PlataformaSuscripcionesApi caso, HttpContext http, CancellationToken ct) => caso.Consultar(http, ct))
            .RequireAuthorization(Permisos.VerSuscripcion);
        panel.MapGet("/uso", (PlataformaSuscripcionesApi caso, HttpContext http, CancellationToken ct) => caso.ConsultarUso(http, ct))
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
