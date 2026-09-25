using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Infraestructura.Pagos;
using Xunit;

namespace Shapi.Dominio.Tests.Pagos;

public class PasarelaSimuladaTests
{
    private readonly IConfiguration _config;
    private readonly Mock<IReloj> _relojMock;
    private readonly PasarelaSimulada _pasarela;

    public PasarelaSimuladaTests()
    {
        var inMemorySettings = new Dictionary<string, string?> {
            {"Pagos:DemoraMs", "0"},
            {"SHAPI_PASARELA_FALLA", "false"}
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _relojMock = new Mock<IReloj>();
        _relojMock.Setup(r => r.AhoraUtc).Returns(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        _pasarela = new PasarelaSimulada(_config, _relojMock.Object);
    }

    [Fact]
    public async Task Tokenizar_DeberiaFallar_SiLuhnEsInvalido()
    {
        var tarjeta = new DatosTarjeta { Numero = "4242424242424243", MesVencimiento = "12", AnioVencimiento = "2028", Cvv = "123", Titular = "Prueba" };
        var res = await _pasarela.TokenizarAsync(tarjeta);
        Assert.False(res.Exitoso);
        Assert.Equal("numero_invalido", res.Error);
    }

    [Fact]
    public async Task Tokenizar_DeberiaPasar_SiLuhnEsValido()
    {
        var tarjeta = new DatosTarjeta { Numero = "4242424242424242", MesVencimiento = "12", AnioVencimiento = "2028", Cvv = "123", Titular = "Prueba" };
        var res = await _pasarela.TokenizarAsync(tarjeta);
        Assert.True(res.Exitoso);
        Assert.StartsWith("tok_sim_", res.Token);
        Assert.Equal("4242", res.Ultimos4);
    }

    [Theory]
    [InlineData("4242424242424242", "Visa")]
    [InlineData("5555555555554444", "Mastercard")]
    [InlineData("341234567890123", "American Express")]
    public async Task Tokenizar_DeberiaDetectarMarca(string numero, string marcaEsperada)
    {
        var cvv = marcaEsperada == "American Express" ? "1234" : "123";
        var tarjeta = new DatosTarjeta { Numero = numero, MesVencimiento = "12", AnioVencimiento = "2028", Cvv = cvv };
        var res = await _pasarela.TokenizarAsync(tarjeta);
        Assert.True(res.Exitoso);
        Assert.Equal(marcaEsperada, res.Marca);
    }

    [Fact]
    public async Task Tokenizar_DeberiaFallar_SiVencida()
    {
        var tarjeta = new DatosTarjeta { Numero = "4242424242424242", MesVencimiento = "07", AnioVencimiento = "2026", Cvv = "123" };
        var res = await _pasarela.TokenizarAsync(tarjeta);
        Assert.False(res.Exitoso);
        Assert.Equal("tarjeta_vencida", res.Error);
    }

    [Fact]
    public async Task Cobrar_Tarjeta0002_DeberiaFallar()
    {
        var tarjeta = new DatosTarjeta { Numero = "4000000000000002", MesVencimiento = "12", AnioVencimiento = "2028", Cvv = "123" };
        var res = await _pasarela.TokenizarAsync(tarjeta);
        var cobro = await _pasarela.CobrarAsync(res.Token!, 100m, "ref1", false);
        Assert.False(cobro.Exitoso);
        Assert.Equal("fondos_insuficientes", cobro.Error);
    }

    [Fact]
    public async Task Cobrar_Tarjeta0069_DeberiaFallar()
    {
        var tarjeta = new DatosTarjeta { Numero = "4000000000000069", MesVencimiento = "12", AnioVencimiento = "2028", Cvv = "123" };
        var res = await _pasarela.TokenizarAsync(tarjeta);
        var cobro = await _pasarela.CobrarAsync(res.Token!, 100m, "ref2", false);
        Assert.False(cobro.Exitoso);
        Assert.Equal("tarjeta_vencida", cobro.Error);
    }

    [Fact]
    public async Task Cobrar_Tarjeta0341_ExitoContratacion_FallaRenovacion()
    {
        var tarjeta = new DatosTarjeta { Numero = "4000000000000341", MesVencimiento = "12", AnioVencimiento = "2028", Cvv = "123" };
        var res = await _pasarela.TokenizarAsync(tarjeta);
        Assert.Contains("tok_sim_0341_", res.Token);

        var cobro1 = await _pasarela.CobrarAsync(res.Token!, 100m, "ref3", false);
        Assert.True(cobro1.Exitoso);
        Assert.StartsWith("ch_sim_", cobro1.Referencia);

        var cobro2 = await _pasarela.CobrarAsync(res.Token!, 100m, "ref4", true);
        Assert.False(cobro2.Exitoso);
    }

    [Fact]
    public async Task Falla_Si_SHAPI_PASARELA_FALLA_EsTrue()
    {
        var inMemorySettings = new Dictionary<string, string?> {
            {"Pagos:DemoraMs", "0"},
            {"SHAPI_PASARELA_FALLA", "true"}
        };
        var configFalla = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var pasarela = new PasarelaSimulada(configFalla, _relojMock.Object);

        var res = await pasarela.TokenizarAsync(new DatosTarjeta());
        Assert.False(res.Exitoso);
        Assert.Equal("pasarela_no_disponible", res.Error);
    }
}
