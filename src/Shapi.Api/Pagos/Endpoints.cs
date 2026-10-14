using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Pagos;
using Shapi.Aplicacion.Portal;
using Shapi.Contratos;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Pagos;

namespace Shapi.Api.Pagos;

/// <summary>Historial de pagos del proveedor y del consumidor (RF-21).</summary>
public static class Endpoints
{
    public static IEndpointRouteBuilder MapearEndpointsPagos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/pagos", PagosPlataforma).RequireAuthorization(Permisos.VerSuscripcion);
        app.MapGet("/api/portal/pagos", PagosPortal).RequireAuthorization(Permisos.ConsumidorVerCuenta);
        return app;
    }

    private static async Task<IResult> PagosPlataforma(
        HttpContext http, [FromQuery] string? pagina, [FromQuery] string? tamano, ConsultarPagos consulta, CancellationToken ct)
    {
        if (!Paginacion.TryLeer(pagina, tamano, out var p, out var t, out var errores))
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los parámetros de paginación.", errores);
        }

        var organizacionId = Guid.Parse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion)!);
        return TypedResults.Ok(Respuesta(await consulta.DePlataforma(organizacionId, p, t, ct)));
    }

    private static async Task<IResult> PagosPortal(
        HttpContext http, [FromQuery] string? pagina, [FromQuery] string? tamano,
        IResolutorPortal resolutor, ConsultarPagos consulta, CancellationToken ct)
    {
        if (!Paginacion.TryLeer(pagina, tamano, out var p, out var t, out var errores))
        {
            return Problemas.Crear(StatusCodes.Status400BadRequest, CodigosError.DatosInvalidos, "Revise los parámetros de paginación.", errores);
        }

        var portal = await resolutor.Resolver(http.Request.Host.Host, ct);
        if (portal is null
            || !Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var consumidorId)
            || !Guid.TryParse(http.User.FindFirstValue(PoliticasAutorizacion.ClaimOrganizacion), out var organizacionId)
            || organizacionId != portal.OrganizacionId)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(Respuesta(await consulta.DeConsumidor(consumidorId, portal.ApiId, p, t, ct)));
    }

    private static object Respuesta(PaginaPagos pagina)
    {
        var elementos = pagina.Elementos.Select(pago => new
        {
            id = pago.Id,
            fecha = pago.Fecha,
            concepto = Concepto(pago.Concepto),
            descripcion = pago.Descripcion,
            periodoInicio = pago.PeriodoInicio,
            periodoFin = pago.PeriodoFin,
            monto = pago.Monto,
            moneda = "GTQ",
            medioPago = pago.Marca is MarcaTarjeta marca && pago.Ultimos4 is not null
                ? new { marca = Marca(marca), ultimos4 = pago.Ultimos4 }
                : null,
            estado = Estado(pago.Estado),
        }).ToArray();

        return new { elementos, total = pagina.Total };
    }

    private static string Concepto(ConceptoPago concepto) => concepto switch
    {
        ConceptoPago.Contratacion => "contratacion",
        ConceptoPago.Renovacion => "renovacion",
        ConceptoPago.CambioPlan => "cambioPlan",
        ConceptoPago.Reactivacion => "reactivacion",
        _ => throw new ArgumentOutOfRangeException(nameof(concepto)),
    };

    private static string Estado(EstadoPago estado) => estado switch
    {
        EstadoPago.Autorizado => "autorizado",
        EstadoPago.Rechazado => "rechazado",
        EstadoPago.Revertido => "revertido",
        _ => throw new ArgumentOutOfRangeException(nameof(estado)),
    };

    private static string Marca(MarcaTarjeta marca) => marca switch
    {
        MarcaTarjeta.Visa => "visa",
        MarcaTarjeta.Mastercard => "mastercard",
        MarcaTarjeta.AmericanExpress => "americanExpress",
        _ => throw new ArgumentOutOfRangeException(nameof(marca)),
    };

    private static class Paginacion
    {
        public static bool TryLeer(string? paginaTexto, string? tamanoTexto, out int pagina, out int tamano,
            out IDictionary<string, string[]> errores)
        {
            pagina = 1;
            tamano = 20;
            var fallos = new Dictionary<string, string[]>();
            if (!Leer(paginaTexto, 1, int.MaxValue / 100, 1, out pagina))
            {
                fallos["pagina"] = ["Use un entero mayor que cero."];
            }
            if (!Leer(tamanoTexto, 1, 100, 20, out tamano))
            {
                fallos["tamano"] = ["Use un entero entre 1 y 100."];
            }
            errores = fallos;
            return fallos.Count == 0;
        }

        private static bool Leer(string? texto, int minimo, int maximo, int predeterminado, out int valor)
        {
            if (string.IsNullOrEmpty(texto))
            {
                valor = predeterminado;
                return true;
            }

            return int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out valor)
                && valor >= minimo && valor <= maximo;
        }
    }
}
