using FluentAssertions;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Dominio.Tests.Suscripciones;

public sealed class ProrrateoCambioPlanTests
{
    // RF-25 · ejemplo de A2.5.
    [Fact]
    public void Calcular_LanzamientoAProducto_DevuelveMontoDelMockup()
    {
        ProrrateoCambioPlan.Calcular(199m, 30, 599m, 30, 13)
            .Should().Be(new ResultadoProrrateo(86.23m, 259.57m, 173.34m));
    }

    // RF-25 · otra vigencia cobra el precio completo y abre un ciclo nuevo.
    [Fact]
    public void Calcular_CambiaVigencia_CobraPlanNuevoCompleto()
    {
        ProrrateoCambioPlan.Calcular(199m, 30, 999m, 365, 13)
            .Should().Be(new ResultadoProrrateo(86.23m, 999m, 912.77m));
    }

    // RF-25 · una bajada nunca genera reembolso.
    [Fact]
    public void Calcular_CargoMenorQueCredito_NoGeneraPagoNegativo()
    {
        ProrrateoCambioPlan.Calcular(599m, 30, 199m, 30, 13)
            .Should().Be(new ResultadoProrrateo(259.57m, 86.23m, 0m));
    }
}
