using Shapi.Contratos.Redis;

namespace Shapi.Compuerta.Tests.Redis;

// Campos de los hashes api:{id} y clave:{sha256} (07 §4).
public class ContextosRedisTests
{
    private static readonly Guid ApiId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-0c1d2e3f4a5b");
    private static readonly Guid OrganizacionId = Guid.Parse("0199a5b2-7c3d-7e4f-8a9b-000000000003");

    [Fact]
    public void CalcularHash_ClaveCompleta_Sha256EnHexMinusculasDeUtf8()
    {
        // RF-29 (criterio 4 de JG-02): valor calculado aparte con sha256sum.
        ContextoClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e")
            .Should().Be("47d162d7ace0b83b8235011dc58124d30b62dd0fde19b1f38b99ca7d3195a971");
    }

    [Fact]
    public void ContextoApi_ACamposYDesdeCampos_ConservaLosDatos()
    {
        var api = new ContextoApi(ApiId, OrganizacionId, ContextoApi.EstadoPublicada, "http://localhost:5101",
            "secreto-1", "envios.shapi.localhost", 3);

        var campos = api.ACampos();

        campos.Should().ContainKeys("organizacion_id", "estado", "url_origen", "secreto", "portal_host", "version");
        campos["organizacion_id"].Should().Be("0199a5b2-7c3d-7e4f-8a9b-000000000003");
        ContextoApi.DesdeCampos(ApiId, campos).Should().Be(api);
    }

    [Fact]
    public void ContextoApi_SinSecretoNiPortal_NoEscribeEsosCampos()
    {
        var api = new ContextoApi(ApiId, OrganizacionId, "borrador", "http://localhost:5101", null, null, 1);

        var campos = api.ACampos();

        campos.Should().NotContainKeys("secreto", "portal_host");
        ContextoApi.DesdeCampos(ApiId, campos).Should().Be(api);
    }

    [Fact]
    public void ContextoApi_DesdeCamposVacios_DevuelveNull()
    {
        ContextoApi.DesdeCampos(ApiId, new Dictionary<string, string>()).Should().BeNull();
    }

    [Fact]
    public void ContextoApi_EstaPublicada_SoloConEstadoPublicada()
    {
        var api = new ContextoApi(ApiId, OrganizacionId, "publicada", "http://o", null, null, 1);

        api.EstaPublicada.Should().BeTrue();
        (api with { Estado = "despublicada" }).EstaPublicada.Should().BeFalse();
        (api with { Estado = "borrador" }).EstaPublicada.Should().BeFalse();
    }

    [Fact]
    public void ContextoClave_ACamposYDesdeCampos_ConservaLosDatos()
    {
        var clave = new ContextoClave(Guid.NewGuid(), Guid.NewGuid(), ApiId, OrganizacionId, Guid.NewGuid(),
            ContextoClave.TipoPruebas);

        var campos = clave.ACampos();

        campos.Should().ContainKeys("clave_id", "suscripcion_id", "api_id", "organizacion_id", "consumidor_id", "tipo");
        campos["tipo"].Should().Be("pruebas");
        ContextoClave.DesdeCampos(campos).Should().Be(clave);
    }

    [Fact]
    public void ContextoClave_DesdeCamposIncompletos_DevuelveNull()
    {
        var campos = new Dictionary<string, string> { ["clave_id"] = Guid.NewGuid().ToString() };

        ContextoClave.DesdeCampos(campos).Should().BeNull();
    }

    [Fact]
    public void ContextoClave_Entorno_EsElTipoDeLaClave()
    {
        // RF-31 y RF-45: X-Shapi-Entorno es produccion o pruebas según el tipo de clave.
        var clave = new ContextoClave(Guid.NewGuid(), Guid.NewGuid(), ApiId, OrganizacionId, Guid.NewGuid(),
            ContextoClave.TipoProduccion);

        clave.Entorno.Should().Be("produccion");
        (clave with { Tipo = ContextoClave.TipoPruebas }).Entorno.Should().Be("pruebas");
    }
}
