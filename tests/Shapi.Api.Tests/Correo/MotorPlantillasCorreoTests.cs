using Microsoft.Extensions.Configuration;
using Shapi.Infraestructura.Correo;

namespace Shapi.Api.Tests.Correo;

public class MotorPlantillasCorreoTests
{
    [Fact]
    public void RF_46_Verificacion_RenderizaEnEspanolConEnlaceYEscapaElHtml()
    {
        var motor = CrearMotor();

        var resultado = motor.Renderizar(
            "verificacion_correo",
            """{"nombre":"Ana <López>","token":"a+b&c"}""");

        resultado.Html.Should().Contain("Ana &lt;L&#243;pez&gt;");
        resultado.Html.Should().Contain("https://shapi.localhost/verificar-correo?token=a%2Bb%26c");
        resultado.Html.Should().NotContain("Ana <López>");
        resultado.Texto.Should().Contain("Ana <López>");
        resultado.Texto.Should().Contain("https://shapi.localhost/verificar-correo?token=a%2Bb%26c");
        resultado.Texto.Should().Contain("usted");
        resultado.Html.Should().NotContain("{{");
        resultado.Texto.Should().NotContain("{{");
    }

    [Fact]
    public void RF_46_Recuperacion_UsaElEnlaceDeRestablecimientoYNombreDelPortal()
    {
        var motor = CrearMotor();

        var resultado = motor.Renderizar(
            "recuperacion",
            """{"nombre":"Ana","token":"token-1","nombrePortal":"Envíos Xelajú"}""");

        resultado.Html.Should().Contain("https://shapi.localhost/restablecer?token=token-1");
        resultado.Texto.Should().Contain("https://shapi.localhost/restablecer?token=token-1");
        resultado.Texto.Should().Contain("Usted");
        resultado.NombreRemitente.Should().Be("Envíos Xelajú");
    }

    [Theory]
    [InlineData("verificacion_correo", "https://envios.shapi.localhost/verificar-correo?token=token-1")]
    [InlineData("recuperacion", "https://envios.shapi.localhost/restablecer?token=token-1")]
    public void RF_46_CorreoDeUnPortal_LlevaElEnlaceAlHostDelPortal(string plantilla, string enlace)
    {
        var motor = CrearMotor();

        var resultado = motor.Renderizar(
            plantilla,
            """{"nombre":"Ana","token":"token-1","nombrePortal":"Envíos Xelajú","hostPortal":"Envios.Shapi.Localhost"}""");

        resultado.Html.Should().Contain(enlace);
        resultado.Texto.Should().Contain(enlace);
        resultado.Html.Should().NotContain("https://shapi.localhost/");
        resultado.NombreRemitente.Should().Be("Envíos Xelajú");
    }

    [Theory]
    [InlineData("evil.com")]
    [InlineData("evil.com/ruta")]
    [InlineData("shapi.localhost")]
    [InlineData("envios.shapi.localhost.evil.com")]
    [InlineData("a.b.shapi.localhost")]
    [InlineData("evil.com／x.shapi.localhost")]
    [InlineData("evil.com℀.shapi.localhost")]
    [InlineData("bücher.shapi.localhost")]
    [InlineData("a_b.shapi.localhost")]
    [InlineData("envios.shapi.localhost:8443")]
    [InlineData("usuario@envios.shapi.localhost")]
    [InlineData("  ")]
    public void RF_46_HostDelPortalInvalido_RechazaLaPlantilla(string hostPortal)
    {
        var motor = CrearMotor();

        var accion = () => motor.Renderizar(
            "verificacion_correo",
            $$"""{"nombre":"Ana","token":"token-1","hostPortal":"{{hostPortal}}"}""");

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage("*hostPortal*");
    }

    [Fact]
    public void RF_46_DatoRequeridoAusente_RechazaLaPlantilla()
    {
        var motor = CrearMotor();

        var accion = () => motor.Renderizar("verificacion_correo", """{"nombre":"Ana"}""");

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage("*token*");
    }

    private static MotorPlantillasCorreo CrearMotor()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SHAPI_DOMINIO_BASE"] = "shapi.localhost",
            })
            .Build();

        return new MotorPlantillasCorreo(configuracion);
    }
}
