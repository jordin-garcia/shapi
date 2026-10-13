using Shapi.Aplicacion.Estado;

namespace Shapi.Api.Tests.Estado;

public sealed class ReglasEstadoTests
{
    // RF-39: límites exactos de 06 §8.
    [Theory]
    [InlineData("api_control", 1000, "en_servicio")]
    [InlineData("api_control", 1000.01, "degradado")]
    [InlineData("redis", 49.99, "en_servicio")]
    [InlineData("redis", 50, "degradado")]
    [InlineData("postgres", 199.99, "en_servicio")]
    [InlineData("postgres", 200, "degradado")]
    public void RF_39_Umbrales_ClasificanElLimite(string id, double ms, string esperado) =>
        Assert.Equal(esperado, ReglasEstado.PorLatencia(id, ms));

    [Theory]
    [InlineData(15d, "en_servicio")]
    [InlineData(15.01d, "degradado")]
    public void RF_39_Compuerta_ClasificaElLimiteDeQuinceMilisegundos(double ms, string esperado) =>
        Assert.Equal(esperado, ReglasEstado.Compuerta(true, (decimal)ms, false));

    [Theory]
    [InlineData(false, false, 0d, "fuera_de_servicio")]
    [InlineData(true, false, 3600d, "en_servicio")]
    [InlineData(true, true, 59d, "en_servicio")]
    [InlineData(true, true, 60d, "degradado")]
    [InlineData(true, true, null, "degradado")]
    public void RF_39_Trabajador_SoloExigeConsolidacionRecienteSiHayPendientes(
        bool latido, bool pendientes, double? atraso, string esperado) =>
        Assert.Equal(esperado, ReglasEstado.Trabajador(latido, pendientes, atraso));

    [Theory]
    [InlineData(false, false, false, "fuera_de_servicio")]
    [InlineData(true, false, false, "en_servicio")]
    [InlineData(true, true, false, "degradado")]
    [InlineData(true, false, true, "degradado")]
    public void RF_39_Correo_DistingueServidorColaYFallos(bool smtp, bool antiguos, bool fallos, string esperado) =>
        Assert.Equal(esperado, ReglasEstado.Correo(smtp, antiguos, fallos));

    [Fact]
    public void RF_39_SinMuestras_NoInventaLatenciaYDetectaCaidas()
    {
        Assert.Equal("en_servicio", ReglasEstado.Compuerta(true, null, false));
        Assert.Equal("degradado", ReglasEstado.Compuerta(true, null, true));
        Assert.Equal("fuera_de_servicio", ReglasEstado.Compuerta(false, 1, false));
        Assert.Equal("fuera_de_servicio", ReglasEstado.PorLatencia("redis", null));
        Assert.Equal("fuera_de_servicio", ReglasEstado.PorLatencia("postgres", null));
        Assert.Equal("fuera_de_servicio", ReglasEstado.Pasarela(true));
        Assert.Equal("en_servicio", ReglasEstado.Pasarela(false));
    }
}
