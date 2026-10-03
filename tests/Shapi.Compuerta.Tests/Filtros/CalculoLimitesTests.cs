using Shapi.Compuerta.Filtros;

namespace Shapi.Compuerta.Tests.Filtros;

// JG-06: cálculo de Retry-After, X-RateLimit-Reset y del día de Guatemala de la clave de pruebas (08 §3 a §5).
public class CalculoLimitesTests
{
    [Theory]
    [InlineData(0, 60)]
    [InlineData(1, 59)]
    [InlineData(30, 30)]
    [InlineData(59, 1)]
    public void RF_30_SegundosParaSiguienteMinuto_VentanaAlineadaAlMinuto(int segundo, long esperado)
    {
        // Criterio 1: Retry-After hasta el siguiente minuto (minuto_epoch = floor(unix / 60)).
        var ahora = new DateTimeOffset(2026, 10, 15, 12, 7, segundo, 400, TimeSpan.Zero);

        FiltroLimitesYCuotas.SegundosParaSiguienteMinuto(ahora).Should().Be(esperado);
    }

    [Fact]
    public void RF_30_SegundosHasta_FinDelCiclo_RedondeaHaciaArriba()
    {
        // Criterio 3: Retry-After hasta el fin del ciclo; nunca invita a reintentar antes de tiempo.
        var ahora = new DateTimeOffset(2026, 10, 15, 12, 0, 30, 250, TimeSpan.Zero);
        var fin = new DateTimeOffset(2026, 10, 31, 6, 0, 0, TimeSpan.Zero);

        FiltroLimitesYCuotas.SegundosHasta(fin, ahora).Should().Be(1_360_770);
    }

    [Fact]
    public void RF_30_SegundosHasta_MomentoQueYaPaso_UnSegundo()
    {
        // Un ciclo en gracia ya terminó: Retry-After no puede ser 0 ni negativo.
        var ahora = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        FiltroLimitesYCuotas.SegundosHasta(ahora.AddDays(-3), ahora).Should().Be(1);
        FiltroLimitesYCuotas.SegundosHasta(ahora, ahora).Should().Be(1);
    }

    [Theory]
    [InlineData("2026-10-15T12:00:30Z", "2026-10-15", "2026-10-16T06:00:00Z")]
    [InlineData("2026-10-16T05:59:59Z", "2026-10-15", "2026-10-16T06:00:00Z")]
    [InlineData("2026-10-16T06:00:00Z", "2026-10-16", "2026-10-17T06:00:00Z")]
    [InlineData("2026-12-31T23:00:00Z", "2026-12-31", "2027-01-01T06:00:00Z")]
    public void RF_45_DiaDePruebas_EsElDiaDeGuatemala(string ahora, string dia, string finDelDia)
    {
        // Criterio 6: el límite diario de la clave de pruebas se cuenta por día de Guatemala (UTC−6, 07 §4).
        var momento = DateTimeOffset.Parse(ahora, System.Globalization.CultureInfo.InvariantCulture);

        FiltroLimitesYCuotas.DiaGuatemala(momento).Should().Be(DateOnly.Parse(dia, System.Globalization.CultureInfo.InvariantCulture));
        FiltroLimitesYCuotas.FinDelDiaGuatemala(momento).Should().Be(
            DateTimeOffset.Parse(finDelDia, System.Globalization.CultureInfo.InvariantCulture));
    }
}
