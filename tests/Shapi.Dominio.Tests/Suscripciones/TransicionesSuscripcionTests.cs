using Shapi.Dominio.Suscripciones;

namespace Shapi.Dominio.Tests.Suscripciones;

public sealed class TransicionesSuscripcionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 18, 0, 0, TimeSpan.Zero);

    // RF-22: una renovación autorizada conserva el fin previo como inicio del ciclo nuevo.
    [Fact]
    public void Renovar_CicloNuevoEmpiezaAlFinAnterior()
    {
        var finAnterior = Ahora.AddDays(-2);
        var suscripcion = SuscripcionApi.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), finAnterior.AddDays(-30), finAnterior);

        suscripcion.Renovar(30, Ahora);

        suscripcion.Estado.Should().Be(EstadoSuscripcion.Activa);
        suscripcion.Inicio.Should().Be(finAnterior);
        suscripcion.Fin.Should().Be(finAnterior.AddDays(30));
        suscripcion.GraciaHasta.Should().BeNull();
    }

    // RF-22: un rechazo y el fin de una Prueba abren siete días de gracia desde el fin del ciclo.
    [Fact]
    public void EntrarEnGracia_FijaSuspensionSieteDiasDespuesDelFin()
    {
        var finAnterior = Ahora.AddDays(-1);
        var suscripcion = SuscripcionApi.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), finAnterior.AddDays(-30), finAnterior);

        suscripcion.EntrarEnGracia(TimeSpan.FromDays(7), Ahora);

        suscripcion.Estado.Should().Be(EstadoSuscripcion.EnGracia);
        suscripcion.Fin.Should().Be(finAnterior);
        suscripcion.GraciaHasta.Should().Be(finAnterior.AddDays(7));
    }

    // RF-23: la suscripción en gracia se suspende y deja de tener fecha de gracia pendiente.
    [Fact]
    public void Suspender_CambiaEstadoYActualizaFecha()
    {
        var suscripcion = SuscripcionApi.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Ahora.AddDays(-37), Ahora.AddDays(-7));
        suscripcion.EntrarEnGracia(TimeSpan.FromDays(7), Ahora.AddDays(-7));

        suscripcion.Suspender(Ahora);

        suscripcion.Estado.Should().Be(EstadoSuscripcion.Suspendida);
        suscripcion.GraciaHasta.Should().Be(Ahora);
        suscripcion.ActualizadoEn.Should().Be(Ahora);
    }

    // RF-22: pagar una suscripción en gracia o suspendida inicia un ciclo nuevo desde la fecha actual.
    [Theory]
    [InlineData(EstadoSuscripcion.EnGracia)]
    [InlineData(EstadoSuscripcion.Suspendida)]
    public void Reactivar_AbreCicloDesdeHoyYGuardaMedio(EstadoSuscripcion estado)
    {
        var suscripcion = SuscripcionApi.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Ahora.AddDays(-37), Ahora.AddDays(-7));
        if (estado == EstadoSuscripcion.EnGracia)
        {
            suscripcion.EntrarEnGracia(TimeSpan.FromDays(7), Ahora.AddDays(-7));
        }
        else
        {
            suscripcion.EntrarEnGracia(TimeSpan.FromDays(7), Ahora.AddDays(-7));
            suscripcion.Suspender(Ahora);
        }
        var medioPagoId = Guid.NewGuid();

        suscripcion.Reactivar(30, medioPagoId, Ahora);

        suscripcion.Estado.Should().Be(EstadoSuscripcion.Activa);
        suscripcion.Inicio.Should().Be(Suscripcion.InicioDeCiclo(Ahora));
        suscripcion.Fin.Should().Be(Suscripcion.InicioDeCiclo(Ahora).AddDays(30));
        suscripcion.GraciaHasta.Should().BeNull();
        suscripcion.MedioPagoId.Should().Be(medioPagoId);
    }
}
