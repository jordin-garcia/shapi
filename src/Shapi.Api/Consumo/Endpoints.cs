using System.Security.Claims;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Consumo;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;

namespace Shapi.Api.Consumo;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsConsumo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/apis/{apiId:guid}/consumo", Consultar)
            .RequireAuthorization(Permisos.VerConsumo);
        return app;
    }

    private static async Task<IResult> Consultar(Guid apiId, HttpContext http, ConsultarConsumoApi casoUso,
        CancellationToken cancelacion)
    {
        var organizacion = Guid.Parse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion)!);
        var resultado = await casoUso.Ejecutar(organizacion, apiId,
            http.Request.Query.ContainsKey("desde") ? http.Request.Query["desde"].ToString() : null,
            http.Request.Query.ContainsKey("hasta") ? http.Request.Query["hasta"].ToString() : null,
            cancelacion);
        if (resultado.EsExito)
        {
            return TypedResults.Ok(resultado.Valor);
        }
        var error = resultado.Error;
        return Problemas.Crear(error.Codigo == CodigosError.ApiNoEncontrada ? 404 : 400,
            error.Codigo, error.Mensaje, error.Detalle as IDictionary<string, string[]>);
    }
}
