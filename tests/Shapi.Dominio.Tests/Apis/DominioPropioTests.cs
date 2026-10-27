using Shapi.Dominio.Apis;

namespace Shapi.Dominio.Tests.Apis;

public sealed class DominioPropioTests
{
    // RF-12: un CNAME existente que apunta a otro destino sigue pendiente.
    [Fact]
    public void RF_12_Verificar_CnameDistinto_ConservaPendienteYMotivo()
    {
        var ahora = DateTimeOffset.Parse("2026-10-26T12:00:00Z");
        var dominio = new DominioPropio(Guid.NewGuid(), "api.ejemplo.localhost", "ejemplo.api.shapi.localhost", ahora);
        dominio.Verificar("otra.api.shapi.localhost", ahora).Should().BeFalse();
        dominio.Estado.Should().Be(EstadoDominio.Pendiente);
        dominio.VerificadoEn.Should().BeNull();
        dominio.Motivo.Should().Contain("otra.api.shapi.localhost").And.Contain("ejemplo.api.shapi.localhost");
    }
    // RF-12: una coincidencia CNAME, sin importar punto final o mayúsculas, verifica el dominio.
    [Fact]
    public void RF_12_Verificar_CnameCoincide_QuedaVerificado()
    {
        var ahora = DateTimeOffset.Parse("2026-10-14T12:00:00Z");
        var dominio = new DominioPropio(Guid.NewGuid(), "api.ejemplo.localhost", "ejemplo.api.shapi.localhost", ahora);

        dominio.Verificar("EJEMPLO.api.shapi.localhost.", ahora.AddMinutes(1)).Should().BeTrue();

        dominio.Estado.Should().Be(EstadoDominio.Verificado);
        dominio.VerificadoEn.Should().Be(ahora.AddMinutes(1));
        dominio.Motivo.Should().BeNull();
    }

    // RF-12: el trabajador marca como fallido el dominio que completa 72 horas pendiente.
    [Fact]
    public void RF_12_MarcarFallido_DespuesDe72Horas_ConservaMotivo()
    {
        var ahora = DateTimeOffset.Parse("2026-10-14T12:00:00Z");
        var dominio = new DominioPropio(Guid.NewGuid(), "api.ejemplo.localhost", "ejemplo.api.shapi.localhost", ahora.AddHours(-72));

        dominio.MarcarFallido(ahora);

        dominio.Estado.Should().Be(EstadoDominio.Fallido);
        dominio.Motivo.Should().Contain("72 horas");
    }
}
