using Shapi.Aplicacion.Consumo;

namespace Shapi.Api.Tests.Consumo;

public sealed class HistogramaLatenciaTests
{
    // RF-34, RF-35: el percentil se calcula sobre todos los histogramas del periodo.
    [Fact]
    public void RF_35_P95_SumaElPeriodoEInterpolaDentroDelRango()
    {
        int[] primero = [50, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        int[] segundo = [40, 10, 0, 0, 0, 0, 0, 0, 0, 0];
        var p95 = HistogramaLatencia.CalcularP95([primero, segundo]);
        p95.Milisegundos.Should().Be(7.5m);
        p95.Supera2500.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 4.75)]
    [InlineData(1, 9.75)]
    [InlineData(2, 24.25)]
    [InlineData(8, 2425)]
    public void RF_35_P95_UnSoloRango_InterpolaDesdeElLimiteInferior(int rango, decimal esperado)
    {
        var histograma = new int[10];
        histograma[rango] = 100;
        HistogramaLatencia.CalcularP95([histograma]).Milisegundos.Should().Be(esperado);
    }

    [Fact]
    public void RF_35_P95_UltimoRango_ReportaMasDe2500()
    {
        int[] histograma = [0, 0, 0, 0, 0, 0, 0, 0, 90, 10];
        var resultado = HistogramaLatencia.CalcularP95([histograma]);
        resultado.Supera2500.Should().BeTrue();
        resultado.Milisegundos.Should().BeNull();
    }

    [Fact]
    public void RF_35_P95_SinPeticiones_NoInventaUnaLatencia()
    {
        HistogramaLatencia.CalcularP95([new int[10]]).Milisegundos.Should().BeNull();
    }

    [Fact]
    public void RF_35_P95_ContadoresGrandes_NoDesbordaAlSumarElPeriodo()
    {
        int[] histograma = [int.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        HistogramaLatencia.CalcularP95([histograma, histograma]).Milisegundos.Should().Be(4.75m);
    }
}
