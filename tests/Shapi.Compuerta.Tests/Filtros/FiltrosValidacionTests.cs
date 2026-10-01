using Microsoft.AspNetCore.Http;
using Shapi.Compuerta.Filtros;
using Shapi.Compuerta.Rutas;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Filtros;

// JG-05: filtros 0, 3, 4 y 5 de 08 §3, sin Redis (los datos los deja el lector en el contexto).
public class FiltrosValidacionTests
{
    private static readonly Guid OrganizacionId = Guid.NewGuid();

    [Fact]
    public async Task RF_29_FiltroOrganizacion_Activa_Continua()
    {
        var contexto = Contexto(organizacion: Organizacion(ContextoOrganizacion.EstadoActiva));

        var resultado = await new FiltroOrganizacion().EvaluarAsync(contexto);

        resultado.Continua.Should().BeTrue();
    }

    [Fact]
    public async Task RF_29_FiltroOrganizacion_Suspendida_Rechaza403ApiNoDisponible()
    {
        // Criterio 1
        var contexto = Contexto(organizacion: Organizacion(ContextoOrganizacion.EstadoSuspendida));

        var resultado = await new FiltroOrganizacion().EvaluarAsync(contexto);

        VerificarRechazo(resultado, 403, "api_no_disponible");
    }

    [Fact]
    public async Task RF_29_FiltroOrganizacion_SinDatosEnRedis_Rechaza403ApiNoDisponible()
    {
        // Sin org:{id} no se puede comprobar el estado efectivo: no se deja pasar.
        var resultado = await new FiltroOrganizacion().EvaluarAsync(Contexto(organizacion: null));

        VerificarRechazo(resultado, 403, "api_no_disponible");
    }

    [Theory]
    [InlineData(ContextoSuscripcion.EstadoActiva)]
    [InlineData(ContextoSuscripcion.EstadoEnGracia)]
    public async Task RF_29_FiltroSuscripcion_ActivaOEnGracia_Continua(string estado)
    {
        var resultado = await new FiltroSuscripcion().EvaluarAsync(Contexto(suscripcion: Suscripcion(estado)));

        resultado.Continua.Should().BeTrue();
    }

    [Fact]
    public async Task RF_29_FiltroSuscripcion_Suspendida_Rechaza403SuscripcionInactiva()
    {
        // Criterio 2
        var contexto = Contexto(suscripcion: Suscripcion(ContextoSuscripcion.EstadoSuspendida));

        var resultado = await new FiltroSuscripcion().EvaluarAsync(contexto);

        VerificarRechazo(resultado, 403, "suscripcion_inactiva");
    }

    [Fact]
    public async Task RF_29_FiltroSuscripcion_Finalizada_Rechaza403SuscripcionInactiva()
    {
        // Criterio 2: una suscripción finalizada no tiene susc:{id} (07 §4).
        var resultado = await new FiltroSuscripcion().EvaluarAsync(Contexto(suscripcion: null));

        VerificarRechazo(resultado, 403, "suscripcion_inactiva");
    }

    [Fact]
    public async Task RF_29_FiltroSuscripcion_ActivaConElCicloVencido_ContinuaPorqueNoComparaFechas()
    {
        // Criterio 2 y 08 §3, filtro 4: los cambios de estado los hace el trabajador (CU-16), no la compuerta.
        var vencida = Suscripcion(ContextoSuscripcion.EstadoActiva) with { Inicio = 1_000_000, Fin = 1_000_100 };

        var resultado = await new FiltroSuscripcion().EvaluarAsync(Contexto(suscripcion: vencida));

        resultado.Continua.Should().BeTrue();
    }

    [Theory]
    [InlineData("GET", "/guias/GT123")]
    [InlineData("POST", "/cotizaciones")]
    public async Task RF_29_FiltroRuta_MetodoYPatronExpuestos_ContinuaYGuardaLaRuta(string metodo, string camino)
    {
        // Criterio 3
        var contexto = Contexto(metodo: metodo, camino: camino, rutas:
        [
            EntornoRuta("GET", "/guias/{numero}"),
            EntornoRuta("POST", "/cotizaciones"),
        ]);

        var resultado = await new FiltroRuta().EvaluarAsync(contexto);

        resultado.Continua.Should().BeTrue();
        contexto.Ruta.Should().NotBeNull();
        contexto.Ruta!.Metodo.Should().Be(metodo);
    }

    [Theory]
    [InlineData("GET", "/cotizaciones")]
    [InlineData("DELETE", "/guias/GT123")]
    [InlineData("GET", "/tarifas")]
    [InlineData("GET", "/no-existe")]
    public async Task RF_29_FiltroRuta_MetodoORutaNoExpuestos_Rechaza403RutaNoPermitida(string metodo, string camino)
    {
        // Criterio 3 y RF-10: solo las rutas expuestas pasan por la compuerta.
        var contexto = Contexto(metodo: metodo, camino: camino, rutas:
        [
            EntornoRuta("GET", "/guias/{numero}"),
            EntornoRuta("POST", "/cotizaciones"),
            EntornoRuta("GET", "/tarifas", expuesta: false),
        ]);

        var resultado = await new FiltroRuta().EvaluarAsync(contexto);

        VerificarRechazo(resultado, 403, "ruta_no_permitida");
        contexto.Ruta.Should().BeNull();
    }

    [Fact]
    public async Task RF_29_FiltroCors_PreflightDeUnaApiPublicada_Responde204SinPedirClave()
    {
        // Criterio 7
        var contexto = Contexto(metodo: "OPTIONS", camino: "/cotizaciones", portalHost: "tienda.shapi.localhost");
        contexto.Http.Request.Headers.Origin = "https://tienda.shapi.localhost";
        contexto.Http.Request.Headers.AccessControlRequestMethod = "POST";

        var resultado = await new FiltroCors().EvaluarAsync(contexto);

        resultado.Continua.Should().BeFalse();
        resultado.EsError.Should().BeFalse();
        resultado.Estado.Should().Be(204);
    }

    [Fact]
    public async Task RF_29_FiltroCors_PreflightDeOtroOrigen_Responde204()
    {
        // Criterio 7: el preflight se contesta, pero sin Access-Control-Allow-Origin (lo verifica la integración).
        var contexto = Contexto(metodo: "OPTIONS", camino: "/cotizaciones", portalHost: "tienda.shapi.localhost");
        contexto.Http.Request.Headers.Origin = "https://otro.ejemplo.com";
        contexto.Http.Request.Headers.AccessControlRequestMethod = "POST";

        var resultado = await new FiltroCors().EvaluarAsync(contexto);

        resultado.Estado.Should().Be(204);
        resultado.EsError.Should().BeFalse();
    }

    [Theory]
    [InlineData("GET", "https://tienda.shapi.localhost", null)]
    [InlineData("OPTIONS", null, "POST")]
    [InlineData("OPTIONS", "https://tienda.shapi.localhost", null)]
    public async Task RF_29_FiltroCors_NoEsPreflight_Continua(string metodo, string? origen, string? metodoSolicitado)
    {
        // Criterio 7: las peticiones sin Origin, y los OPTIONS que no son preflight, siguen la tubería normal.
        var contexto = Contexto(metodo: metodo, camino: "/cotizaciones", portalHost: "tienda.shapi.localhost");
        if (origen is not null)
        {
            contexto.Http.Request.Headers.Origin = origen;
        }

        if (metodoSolicitado is not null)
        {
            contexto.Http.Request.Headers.AccessControlRequestMethod = metodoSolicitado;
        }

        var resultado = await new FiltroCors().EvaluarAsync(contexto);

        resultado.Continua.Should().BeTrue();
    }

    [Fact]
    public async Task RF_29_FiltroCors_PreflightDeUnaApiNoPublicada_ContinuaParaQueFiltroApiResponda404()
    {
        var contexto = Contexto(metodo: "OPTIONS", camino: "/cotizaciones", portalHost: "tienda.shapi.localhost",
            estadoApi: ContextoApi.EstadoDespublicada);
        contexto.Http.Request.Headers.Origin = "https://tienda.shapi.localhost";
        contexto.Http.Request.Headers.AccessControlRequestMethod = "POST";

        var resultado = await new FiltroCors().EvaluarAsync(contexto);

        resultado.Continua.Should().BeTrue();
    }

    private static RutaCache EntornoRuta(string metodo, string patron, bool expuesta = true) =>
        Soporte.EntornoCompuerta.Ruta(metodo, patron, expuesta);

    private static ContextoOrganizacion Organizacion(string estado) => new(OrganizacionId, estado, null, null, null);

    private static ContextoSuscripcion Suscripcion(string estado) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Comercio", estado, 1_790_000_000, 1_792_600_000, 50_000, 60);

    private static ContextoPeticion Contexto(
        ContextoOrganizacion? organizacion = null, ContextoSuscripcion? suscripcion = null, string metodo = "GET",
        string camino = "/", IReadOnlyList<RutaCache>? rutas = null, string? portalHost = null,
        string estadoApi = ContextoApi.EstadoPublicada)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = metodo;
        http.Request.Path = camino;
        return new ContextoPeticion(http)
        {
            Api = new ContextoApi(Guid.NewGuid(), OrganizacionId, estadoApi, "http://origen.prueba", null, portalHost, 1),
            Rutas = TablaRutas.Crear(rutas ?? []),
            Organizacion = organizacion,
            Suscripcion = suscripcion,
        };
    }

    private static void VerificarRechazo(ResultadoFiltro resultado, int estado, string codigo)
    {
        resultado.Continua.Should().BeFalse();
        resultado.EsError.Should().BeTrue();
        resultado.Estado.Should().Be(estado);
        resultado.Codigo.Should().Be(codigo);
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }
}
