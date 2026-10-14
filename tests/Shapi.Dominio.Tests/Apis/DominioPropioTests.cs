using Shapi.Dominio.Apis;

namespace Shapi.Dominio.Tests.Apis;

public sealed class DominioPropioTests
{
    // RF-11: una coincidencia CNAME, sin importar punto final o mayúsculas, verifica el dominio.
    [Fact]
    public void RF_11_Verificar_CnameCoincide_QuedaVerificado()
    {
        var ahora = DateTimeOffset.Parse("2026-10-14T12:00:00Z");
        var dominio = new DominioPropio(Guid.NewGuid(), "api.ejemplo.localhost", "ejemplo.api.shapi.localhost", ahora);

        dominio.Verificar("EJEMPLO.api.shapi.localhost.", ahora.AddMinutes(1)).Should().BeTrue();

        dominio.Estado.Should().Be(EstadoDominio.Verificado);
        dominio.VerificadoEn.Should().Be(ahora.AddMinutes(1));
        dominio.Motivo.Should().BeNull();
    }

    // RF-11: el trabajador marca como fallido el dominio que completa 72 horas pendiente.
    [Fact]
    public void RF_11_MarcarFallido_DespuesDe72Horas_ConservaMotivo()
    {
        var ahora = DateTimeOffset.Parse("2026-10-14T12:00:00Z");
        var dominio = new DominioPropio(Guid.NewGuid(), "api.ejemplo.localhost", "ejemplo.api.shapi.localhost", ahora.AddHours(-72));

        dominio.MarcarFallido(ahora);

        dominio.Estado.Should().Be(EstadoDominio.Fallido);
        dominio.Motivo.Should().Contain("72 horas");
    }
}
