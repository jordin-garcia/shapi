using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Shapi.Infraestructura.Correo;

namespace Shapi.Api.Tests.Correo;

public class MotorPlantillasCorreoTests
{
    public static TheoryData<string, string> Plantillas => new()
    {
        { "verificacion_correo", """{"nombre":"Ana","token":"token-1"}""" },
        { "recuperacion", """{"nombre":"Ana","token":"token-1"}""" },
        { "invitacion_miembro", """{"nombre":"Ana","nombreOrganizacion":"Acme","token":"token-1"}""" },
        { "invitacion_consumidor", """{"nombre":"Ana","nombreApi":"API de envíos","enlace":"https://envios.shapi.localhost/invitacion?token=token-1"}""" },
        { "definir_contrasena", """{"nombre":"Ana","enlace":"https://shapi.localhost/definir-contrasena?token=token-1"}""" },
        { "pago_rechazado", """{"nombre":"Ana","nombrePlan":"Profesional","motivo":"Fondos insuficientes","enlace":"https://shapi.localhost/pagos"}""" },
        { "suscripcion_en_gracia", """{"nombre":"Ana","nombrePlan":"Profesional","fechaSuspension":"12 de octubre de 2026","enlace":"https://shapi.localhost/suscripcion"}""" },
        { "suscripcion_suspendida", """{"nombre":"Ana","nombrePlan":"Profesional","enlace":"https://shapi.localhost/suscripcion"}""" },
        { "organizacion_suspendida", """{"nombre":"Ana","nombreOrganizacion":"Acme","motivo":"Pago pendiente","enlace":"https://shapi.localhost/organizacion"}""" },
        { "prueba_por_vencer", """{"nombre":"Ana","fechaFin":"12 de octubre de 2026","enlace":"https://shapi.localhost/plan"}""" },
        { "aviso_cuota_plataforma", """{"nombre":"Ana","porcentaje":"80","enlace":"https://shapi.localhost/plan"}""" },
        { "respuesta_caso", """{"nombre":"Ana","numeroCaso":"CAS-104","asunto":"Error de autenticación","enlace":"https://shapi.localhost/soporte/casos/104"}""" },
    };

    // RF-46
    [Theory]
    [InlineData("verificacion_correo", "Verifique su correo")]
    [InlineData("recuperacion", "Recupere su contraseña")]
    [InlineData("invitacion_miembro", "Invitación a una organización")]
    [InlineData("invitacion_consumidor", "Invitación a un portal")]
    [InlineData("definir_contrasena", "Defina su contraseña")]
    [InlineData("pago_rechazado", "No pudimos procesar su pago")]
    [InlineData("suscripcion_en_gracia", "Su suscripción está en período de gracia")]
    [InlineData("suscripcion_suspendida", "Su suscripción fue suspendida")]
    [InlineData("organizacion_suspendida", "Su organización fue suspendida")]
    [InlineData("prueba_por_vencer", "Su prueba está por vencer")]
    [InlineData("aviso_cuota_plataforma", "Aviso de cuota de plataforma")]
    [InlineData("respuesta_caso", "Nueva respuesta en su caso")]
    public void RF_46_Plantilla_TieneUnAsuntoEspecifico(string plantilla, string asunto)
    {
        ColaCorreoBaseDatos.AsuntoPara(plantilla).Should().Be(asunto);
    }

    // RF-46
    [Theory]
    [MemberData(nameof(Plantillas))]
    public void RF_46_PlantillaDelPersonal_RenderizaCompletaConMarcaShapi(string plantilla, string datos)
    {
        var motor = CrearMotor();

        var resultado = motor.Renderizar(plantilla, datos);

        resultado.Html.Should().Contain("Shapi");
        resultado.Html.Should().Contain("#3B6FF0");
        resultado.Texto.Should().StartWith("Shapi");
        resultado.Html.Should().ContainEquivalentOf("usted");
        resultado.Texto.Should().ContainEquivalentOf("usted");
        resultado.Html.Should().NotContain("{{");
        resultado.Texto.Should().NotContain("{{");
        resultado.NombreRemitente.Should().Be("Shapi");
    }

    // RF-46
    [Theory]
    [MemberData(nameof(Plantillas))]
    public void RF_46_PlantillaDelConsumidor_UsaSoloLaMarcaDelPortal(string plantilla, string datos)
    {
        var motor = CrearMotor();
        var datosPortal = AgregarMarcaPortal(datos, incluirLogo: true);

        var resultado = motor.Renderizar(plantilla, datosPortal);

        WebUtility.HtmlDecode(resultado.Html).Should().Contain("Envíos <Xelajú>");
        resultado.Html.Should().Contain("#E11D48");
        resultado.Html.Should().Contain("https://envios.shapi.localhost/api/portal/logo");
        resultado.Html.Should().Contain("<img");
        resultado.Texto.Should().StartWith("Envíos <Xelajú>");
        resultado.Html.Should().NotContain("Shapi");
        resultado.Texto.Should().NotContain("Shapi");
        resultado.Html.Should().NotContain("{{");
        resultado.Texto.Should().NotContain("{{");
        resultado.NombreRemitente.Should().Be("Envíos <Xelajú>");
    }

    // RF-46
    [Fact]
    public void RF_46_PortalSinLogotipo_MuestraSoloElNombre()
    {
        var motor = CrearMotor();
        var datos = AgregarMarcaPortal("""{"nombre":"Ana","token":"token-1"}""", incluirLogo: false);

        var resultado = motor.Renderizar("verificacion_correo", datos);

        WebUtility.HtmlDecode(resultado.Html).Should().Contain("Envíos <Xelajú>");
        resultado.Html.Should().NotContain("<img");
        resultado.Html.Should().NotContain("/api/portal/logo");
    }

    // RF-06: el enlace para un miembro siempre apunta al dominio base y se calcula desde el token sin persistirlo como URL.
    [Fact]
    public void RF_06_InvitacionMiembro_GeneraElEnlaceDesdeElTokenEnElDominioBase()
    {
        var motor = CrearMotor();

        var resultado = motor.Renderizar("invitacion_miembro", """{"nombre":"Ana","nombreOrganizacion":"Acme","token":"token-1"}""");

        resultado.Html.Should().Contain("https://shapi.localhost/invitacion?token=token-1");
        resultado.Texto.Should().Contain("https://shapi.localhost/invitacion?token=token-1");
    }

    // RF-46
    [Theory]
    [InlineData("rojo")]
    [InlineData("#fff")]
    [InlineData("#12345678")]
    [InlineData("#GG0000")]
    [InlineData(" #E11D48")]
    public void RF_46_ColorDelPortalInvalido_RechazaLaPlantilla(string colorPortal)
    {
        var motor = CrearMotor();
        var datos = $$"""{"nombre":"Ana","nombrePortal":"Envíos Xelajú","hostPortal":"envios.shapi.localhost","colorPortal":"{{colorPortal}}"}""";

        var accion = () => motor.Renderizar("respuesta_caso", datos);

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage("*colorPortal*");
    }

    // RF-46
    [Theory]
    [InlineData("hostPortal")]
    [InlineData("colorPortal")]
    public void RF_46_MarcaDelPortalIncompleta_RechazaLaPlantilla(string datoAusente)
    {
        var motor = CrearMotor();
        var datos = new Dictionary<string, object?>
        {
            ["nombre"] = "Ana",
            ["nombrePortal"] = "Envíos Xelajú",
            ["hostPortal"] = "envios.shapi.localhost",
            ["colorPortal"] = "#E11D48",
        };
        datos.Remove(datoAusente);

        var accion = () => motor.Renderizar("respuesta_caso", JsonSerializer.Serialize(datos));

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{datoAusente}*");
    }

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
        // 10 §6: sin portal es un correo del personal, que sale con el nombre de Shapi.
        resultado.NombreRemitente.Should().Be("Shapi");
    }

    [Fact]
    public void RF_46_Recuperacion_UsaElEnlaceDeRestablecimientoYNombreDelPortal()
    {
        var motor = CrearMotor();

        var resultado = motor.Renderizar(
            "recuperacion",
            """{"nombre":"Ana","token":"token-1","nombrePortal":"Envíos Xelajú","hostPortal":"envios.shapi.localhost","colorPortal":"#E11D48"}""");

        resultado.Html.Should().Contain("https://envios.shapi.localhost/restablecer?token=token-1");
        resultado.Texto.Should().Contain("https://envios.shapi.localhost/restablecer?token=token-1");
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
            """{"nombre":"Ana","token":"token-1","nombrePortal":"Envíos Xelajú","hostPortal":"Envios.Shapi.Localhost","colorPortal":"#E11D48"}""");

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

    [Theory]
    [InlineData("api.shapi.localhost")]
    [InlineData("correo.shapi.localhost")]
    [InlineData("Admin.shapi.localhost")]
    [InlineData("interno.shapi.localhost")]
    public void RF_46_HostDelPortalConSubdominioReservado_RechazaLaPlantilla(string hostPortal)
    {
        var motor = CrearMotor();

        var accion = () => motor.Renderizar(
            "recuperacion",
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

    private static string AgregarMarcaPortal(string datosJson, bool incluirLogo)
    {
        var datos = JsonSerializer.Deserialize<Dictionary<string, object?>>(datosJson)!;
        datos["nombrePortal"] = "Envíos <Xelajú>";
        datos["hostPortal"] = "envios.shapi.localhost";
        datos["colorPortal"] = "#E11D48";
        if (incluirLogo)
        {
            datos["logoPortal"] = "true";
        }

        return JsonSerializer.Serialize(datos);
    }
}
