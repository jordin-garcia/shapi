using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Redis;

// Formatos de docs/specs/07-modelo-de-datos.md §4 (y demo:reloj de 09 §9).
public class LlavesRedisTests
{
    private static readonly Guid ApiId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b");
    private static readonly Guid RutaId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-000000000001");
    private static readonly Guid SuscripcionId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-000000000002");
    private static readonly Guid OrganizacionId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-000000000003");
    private static readonly Guid ClaveId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-000000000004");
    private static readonly Guid LoteId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-000000000005");

    [Fact]
    public void ApiPorHost_HostDeLaApi_FormatoApiHost()
    {
        LlavesRedis.ApiPorHost("envios.api.shapi.localhost").Should().Be("api:host:envios.api.shapi.localhost");
    }

    [Fact]
    public void ApiPorHost_HostConMayusculas_SeNormalizaAMinusculas()
    {
        LlavesRedis.ApiPorHost("Envios.API.shapi.localhost").Should().Be("api:host:envios.api.shapi.localhost");
    }

    [Fact]
    public void Api_IdDeLaApi_FormatoApi()
    {
        LlavesRedis.Api(ApiId).Should().Be("api:0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b");
    }

    [Fact]
    public void RutasApi_IdDeLaApi_FormatoApiRutas()
    {
        LlavesRedis.RutasApi(ApiId).Should().Be("api:0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b:rutas");
    }

    [Fact]
    public void Clave_HashSha256_FormatoClaveEnMinusculas()
    {
        const string hash = "E6DBA9DE64F42A003EAF40574E5EF3A8E9724B888EF3614F9681463CCCB62B65";

        LlavesRedis.Clave(hash).Should().Be("clave:e6dba9de64f42a003eaf40574e5ef3a8e9724b888ef3614f9681463cccb62b65");
    }

    [Fact]
    public void Suscripcion_IdDeLaSuscripcion_FormatoSusc()
    {
        LlavesRedis.Suscripcion(SuscripcionId).Should().Be("susc:0199a5b2-7c3d-7e4f-8a9b-000000000002");
    }

    [Fact]
    public void Organizacion_IdDeLaOrganizacion_FormatoOrg()
    {
        LlavesRedis.Organizacion(OrganizacionId).Should().Be("org:0199a5b2-7c3d-7e4f-8a9b-000000000003");
    }

    [Fact]
    public void CuotaSuscripcion_InicioDelCiclo_FormatoCuotaSusc()
    {
        LlavesRedis.CuotaSuscripcion(SuscripcionId, 1790812800)
            .Should().Be("cuota:susc:0199a5b2-7c3d-7e4f-8a9b-000000000002:1790812800");
    }

    [Fact]
    public void CuotaOrganizacion_InicioDelCiclo_FormatoCuotaOrg()
    {
        LlavesRedis.CuotaOrganizacion(OrganizacionId, 1790812800)
            .Should().Be("cuota:org:0199a5b2-7c3d-7e4f-8a9b-000000000003:1790812800");
    }

    [Fact]
    public void LimiteMinutoSuscripcion_MinutoEpoch_FormatoRlS()
    {
        LlavesRedis.LimiteMinutoSuscripcion(SuscripcionId, 29846880)
            .Should().Be("rl:s:0199a5b2-7c3d-7e4f-8a9b-000000000002:29846880");
    }

    [Fact]
    public void LimiteMinutoRuta_MinutoEpoch_FormatoRlR()
    {
        LlavesRedis.LimiteMinutoRuta(SuscripcionId, RutaId, 29846880)
            .Should().Be("rl:r:0199a5b2-7c3d-7e4f-8a9b-000000000002:0199a5b2-7c3d-7e4f-8a9b-000000000001:29846880");
    }

    [Fact]
    public void LimiteMinutoPruebas_MinutoEpoch_FormatoRlP()
    {
        LlavesRedis.LimiteMinutoPruebas(ClaveId, 29846880)
            .Should().Be("rl:p:0199a5b2-7c3d-7e4f-8a9b-000000000004:29846880");
    }

    [Fact]
    public void LimiteDiaPruebas_Fecha_FormatoDiaPConAaaammdd()
    {
        LlavesRedis.LimiteDiaPruebas(ClaveId, new DateOnly(2026, 9, 3))
            .Should().Be("dia:p:0199a5b2-7c3d-7e4f-8a9b-000000000004:20260903");
    }

    [Fact]
    public void Cache_MetodoRutaYQuery_FormatoCacheConSha256()
    {
        // sha256("GET/rastreo/GT123?detalle=1") en hex minúsculas
        LlavesRedis.Cache(ApiId, RutaId, "GET", "/rastreo/GT123", "?detalle=1")
            .Should().Be("cache:0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b:0199a5b2-7c3d-7e4f-8a9b-000000000001:"
                + "e6dba9de64f42a003eaf40574e5ef3a8e9724b888ef3614f9681463cccb62b65");
    }

    [Fact]
    public void Cache_MetodoEnMinusculas_MismaLlaveQueEnMayusculas()
    {
        LlavesRedis.Cache(ApiId, RutaId, "get", "/rastreo/GT123", "?detalle=1")
            .Should().Be(LlavesRedis.Cache(ApiId, RutaId, "GET", "/rastreo/GT123", "?detalle=1"));
    }

    [Fact]
    public void Cache_QueryDistinta_LlaveDistinta()
    {
        LlavesRedis.Cache(ApiId, RutaId, "GET", "/rastreo/GT123", "?detalle=2")
            .Should().NotBe(LlavesRedis.Cache(ApiId, RutaId, "GET", "/rastreo/GT123", "?detalle=1"));
    }

    [Fact]
    public void Metricas_ConRutaYSuscripcion_FormatoMet()
    {
        LlavesRedis.Metricas(new DateOnly(2026, 9, 23), ApiId, RutaId, SuscripcionId, "produccion")
            .Should().Be("met:20260923:0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b:0199a5b2-7c3d-7e4f-8a9b-000000000001:"
                + "0199a5b2-7c3d-7e4f-8a9b-000000000002:produccion");
    }

    [Fact]
    public void Metricas_SinRutaNiSuscripcion_UsaGuion()
    {
        LlavesRedis.Metricas(new DateOnly(2026, 9, 23), ApiId, null, null, "pruebas")
            .Should().Be("met:20260923:0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b:-:-:pruebas");
    }

    [Fact]
    public void MetricasPendientes_FormatoMetPendientes()
    {
        LlavesRedis.MetricasPendientes.Should().Be("met:pendientes");
    }

    [Fact]
    public void LoteMetricas_LoteYLlave_FormatoMetLote()
    {
        LlavesRedis.LoteMetricas(LoteId, "met:20260923:a:-:-:produccion")
            .Should().Be("met:lote:0199a5b2-7c3d-7e4f-8a9b-000000000005:met:20260923:a:-:-:produccion");
    }

    [Fact]
    public void PatronLote_Lote_PatronDeSusLlaves()
    {
        LlavesRedis.PatronLote(LoteId).Should().Be("met:lote:0199a5b2-7c3d-7e4f-8a9b-000000000005:*");
        LlavesRedis.PatronLotes.Should().Be("met:lote:*");
    }

    [Fact]
    public void SaludCompuerta_Instancia_FormatoSaludCompuerta()
    {
        LlavesRedis.SaludCompuerta("compuerta-1").Should().Be("salud:compuerta:compuerta-1");
    }

    [Fact]
    public void SaludTrabajador_FormatoSaludTrabajador()
    {
        LlavesRedis.SaludTrabajador.Should().Be("salud:trabajador");
    }

    [Fact]
    public void DemoRelojDesplazamiento_FormatoDemoReloj()
    {
        // 09 §9
        LlavesRedis.DemoRelojDesplazamiento.Should().Be("demo:reloj:desplazamiento");
    }
}
