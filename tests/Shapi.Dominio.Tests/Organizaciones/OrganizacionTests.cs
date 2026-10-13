using Shapi.Dominio.Organizaciones;

namespace Shapi.Dominio.Tests.Organizaciones;

public class OrganizacionTests
{
    [Fact]
    public void RF_38_Suspender_GuardaElMotivoAdministrativo()
    {
        var organizacion = new Organizacion("Transportes Peten, S.A.", TipoOrganizacion.Proveedor);

        organizacion.Suspender("Incumplimiento de los terminos de servicio");

        organizacion.EstadoAdmin.Should().Be(EstadoAdmin.Suspendida);
        organizacion.MotivoSuspension.Should().Be("Incumplimiento de los terminos de servicio");
    }

    [Fact]
    public void RF_38_Reactivar_QuitaSoloLaSuspensionAdministrativa()
    {
        var organizacion = new Organizacion("Transportes Peten, S.A.", TipoOrganizacion.Proveedor);
        organizacion.Suspender("Revision administrativa");

        organizacion.Reactivar();

        organizacion.EstadoAdmin.Should().Be(EstadoAdmin.Activa);
        organizacion.MotivoSuspension.Should().BeNull();
    }
}
