using Shapi.Dominio.Correo;

namespace Shapi.Dominio.Tests.Correo;

public class CorreoSalienteTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RF_46_RegistrarFallo_ProgramaLosCincoIntentosYTerminaFallido()
    {
        var correo = CrearCorreo();
        var esperas = new[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromHours(1),
        };

        for (var indice = 0; indice < esperas.Length; indice++)
        {
            var instante = Ahora.AddMinutes(indice);

            correo.RegistrarFallo($"fallo {indice + 1}", instante);

            correo.Intentos.Should().Be(indice + 1);
            correo.UltimoError.Should().Be($"fallo {indice + 1}");
            correo.ProximoIntentoEn.Should().Be(instante + esperas[indice]);
            correo.Estado.Should().Be(indice == 4 ? EstadoCorreo.Fallido : EstadoCorreo.Pendiente);
        }
    }

    [Fact]
    public void RF_46_MarcarEnviado_RegistraLaFechaYDejaDeEstarPendiente()
    {
        var correo = CrearCorreo();

        correo.MarcarEnviado(Ahora);

        correo.Estado.Should().Be(EstadoCorreo.Enviado);
        correo.EnviadoEn.Should().Be(Ahora);
        correo.ProximoIntentoEn.Should().BeNull();
        correo.UltimoError.Should().BeNull();
    }

    private static CorreoSaliente CrearCorreo() => new(
        "verificacion_correo",
        "ana@ejemplo.com",
        "{\"nombre\":\"Ana\",\"token\":\"secreto\"}",
        "Verifique su correo",
        Ahora);
}
