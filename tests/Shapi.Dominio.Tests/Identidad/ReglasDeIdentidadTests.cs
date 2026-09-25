using Shapi.Dominio.Identidad;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Dominio.Tests.Identidad;

public class ReglasDeIdentidadTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 24, 21, 30, 0, TimeSpan.Zero);

    // RF-04: tras 5 intentos fallidos seguidos, la cuenta se bloquea 15 minutos.
    [Fact]
    public void RF_04_Usuario_QuintoIntentoFallido_BloqueaQuinceMinutosYReiniciaElContador()
    {
        var usuario = new Usuario("Ana López", "  Ana@EnviosXelaju.com ");
        for (var i = 0; i < 4; i++)
        {
            usuario.RegistrarIntentoFallido(Ahora);
        }
        usuario.EstaBloqueado(Ahora).Should().BeFalse();

        usuario.RegistrarIntentoFallido(Ahora);

        usuario.Correo.Should().Be("ana@enviosxelaju.com");
        usuario.EstaBloqueado(Ahora.AddMinutes(14)).Should().BeTrue();
        usuario.EstaBloqueado(Ahora.AddMinutes(15)).Should().BeFalse();
        usuario.IntentosFallidos.Should().Be(0);
    }

    // RF-04: la sesión vence a las 8 h de inactividad o a los 7 días; el último uso se actualiza como máximo cada minuto.
    [Fact]
    public void RF_04_Sesion_VencePorInactividadOPorDuracionMaxima()
    {
        var sesion = Sesion.IniciarPersonal(new string('a', 64), Guid.NewGuid(), "shapi.localhost", null, null, Ahora);
        var inactividad = TimeSpan.FromHours(8);

        sesion.RegistrarUso(Ahora.AddSeconds(30)).Should().BeFalse();
        sesion.EstaVigente(Ahora.AddHours(8).AddMinutes(-1), inactividad).Should().BeTrue();
        sesion.EstaVigente(Ahora.AddHours(8), inactividad).Should().BeFalse();
        sesion.RegistrarUso(Ahora.AddHours(7)).Should().BeTrue();
        sesion.EstaVigente(Ahora.AddHours(14), inactividad).Should().BeTrue();
        sesion.EstaVigente(Ahora.AddDays(7), TimeSpan.FromDays(30)).Should().BeFalse();
        sesion.Revocar(Ahora);
        sesion.EstaVigente(Ahora.AddMinutes(1), inactividad).Should().BeFalse();
    }

    // RF-02: el enlace de verificación vence a las 24 horas.
    [Fact]
    public void RF_02_Token_VerificacionCorreo_VenceALas24Horas()
    {
        var token = Token.VerificacionCorreo(new string('b', 64), new Usuario("Ana", "ana@enviosxelaju.com"), Ahora);

        token.EsValido(Ahora.AddHours(24).AddSeconds(-1)).Should().BeTrue();
        token.EsValido(Ahora.AddHours(24)).Should().BeFalse();
    }

    // 09 §4: el inicio del ciclo es el inicio del día en America/Guatemala (UTC−6).
    [Theory]
    [InlineData("2026-08-24T21:30:00Z", "2026-08-24T06:00:00Z")]
    [InlineData("2026-08-24T06:00:00Z", "2026-08-24T06:00:00Z")]
    [InlineData("2026-08-25T05:59:59Z", "2026-08-24T06:00:00Z")] // todavía es 24 de agosto en Guatemala
    [InlineData("2026-08-25T06:00:00Z", "2026-08-25T06:00:00Z")]
    public void Ciclo_InicioDeCiclo_RedondeaAlInicioDelDiaEnGuatemala(string momento, string inicio)
    {
        Suscripcion.InicioDeCiclo(DateTimeOffset.Parse(momento)).Should().Be(DateTimeOffset.Parse(inicio));
    }
}
