using Shapi.Dominio.Apis;

namespace Shapi.Dominio.Tests.Apis;

public class SubdominiosReservadosTests
{
    [Theory]
    [InlineData("api")]
    [InlineData("app")]
    [InlineData("www")]
    [InlineData("admin")]
    [InlineData("panel")]
    [InlineData("correo")]
    [InlineData("mail")]
    [InlineData("soporte")]
    [InlineData("docs")]
    [InlineData("estado")]
    [InlineData("status")]
    [InlineData("shapi")]
    [InlineData("static")]
    [InlineData("cdn")]
    [InlineData("interno")]
    [InlineData("API")]
    public void RF_08_SubdominioReservado_EstaEnLaLista(string subdominio)
    {
        SubdominiosReservados.Contiene(subdominio).Should().BeTrue();
    }

    [Theory]
    [InlineData("envios")]
    [InlineData("agro")]
    [InlineData("apis")]
    public void RF_08_SubdominioLibre_NoEstaEnLaLista(string subdominio)
    {
        SubdominiosReservados.Contiene(subdominio).Should().BeFalse();
    }

    [Fact]
    public void RF_08_Lista_TieneLosQuinceDeLaEspecificacion()
    {
        SubdominiosReservados.Todos.Should().HaveCount(15);
    }
}
