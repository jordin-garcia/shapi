using Shapi.Aplicacion.Estado;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Estado;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsEstado(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/estado", async (ConsultarEstado consulta, CancellationToken cancelacion) =>
            TypedResults.Ok(await consulta.EjecutarAsync(cancelacion)))
            .RequireAuthorization(Permisos.VerEstadoComponentes);
        return app;
    }
}
