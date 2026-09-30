using System.Net;
using Shapi.Contratos.Red;

namespace Shapi.Api.Tests.Apis;

public class ValidadorDireccionOrigenTests
{
    [Theory]
    [InlineData("ftp://8.8.8.8")]
    [InlineData("https://usuario:secreto@8.8.8.8")]
    [InlineData("no-es-una-url")]
    public async Task RNF_10_Validar_FormatoNoPermitido_Rechaza(string valor)
    {
        var validador = Crear("8.8.8.8");

        var resultado = await validador.Validar(valor, false, []);

        resultado.Error.Should().Be(ErrorDireccionOrigen.FormatoInvalido);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.2.3.4")]
    [InlineData("172.30.0.8")]
    [InlineData("192.168.1.20")]
    [InlineData("169.254.10.1")]
    [InlineData("100.64.0.1")]
    [InlineData("0.1.2.3")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("ff02::1")]
    public async Task RNF_10_Validar_DireccionInterna_Rechaza(string direccion)
    {
        var validador = Crear(direccion);

        var resultado = await validador.Validar("https://origen.ejemplo", false, []);

        resultado.Error.Should().Be(ErrorDireccionOrigen.DireccionNoPermitida);
    }

    [Fact]
    public async Task RNF_10_Validar_UnaDireccionInternaEntreVarias_RechazaTodas()
    {
        var validador = Crear("8.8.8.8", "10.0.0.5");

        var resultado = await validador.Validar("https://origen.ejemplo", false, []);

        resultado.Error.Should().Be(ErrorDireccionOrigen.DireccionNoPermitida);
    }

    [Fact]
    public async Task RF_08_Validar_DireccionPublica_DevuelveLaUrlYDirecciones()
    {
        var validador = Crear("8.8.8.8", "2001:4860:4860::8888");

        var resultado = await validador.Validar("https://origen.ejemplo:8443/v1", false, []);

        resultado.EsValida.Should().BeTrue();
        resultado.Direccion.Should().Be(new Uri("https://origen.ejemplo:8443/v1"));
        resultado.Direcciones.Should().HaveCount(2);
    }

    [Fact]
    public async Task RNF_10_Validar_OrigenDemoExacto_PermiteDireccionInterna()
    {
        var validador = Crear("127.0.0.1");

        var resultado = await validador.Validar(
            "http://localhost:5101/cotizar",
            true,
            ["localhost:5101", "origen-agro:8080"]);

        resultado.EsValida.Should().BeTrue();
    }

    [Fact]
    public async Task RNF_10_Validar_OrigenDemoConPuertoDistinto_Rechaza()
    {
        var validador = Crear("127.0.0.1");

        var resultado = await validador.Validar("http://localhost:5102", true, ["localhost:5101"]);

        resultado.Error.Should().Be(ErrorDireccionOrigen.DireccionNoPermitida);
    }

    [Fact]
    public async Task RF_08_Validar_DnsNoResuelve_IndicaOrigenInaccesible()
    {
        var validador = new ValidadorDireccionOrigen((_, _) => throw new HttpRequestException("DNS caído"));

        var resultado = await validador.Validar("https://origen.ejemplo", false, []);

        resultado.Error.Should().Be(ErrorDireccionOrigen.ResolucionFallida);
    }

    private static ValidadorDireccionOrigen Crear(params string[] direcciones) =>
        new((_, _) => Task.FromResult(direcciones.Select(IPAddress.Parse).ToArray()));
}
