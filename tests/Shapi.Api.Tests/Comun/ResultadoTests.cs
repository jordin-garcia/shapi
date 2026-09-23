using Shapi.Aplicacion.Comun;
using Shapi.Contratos;

namespace Shapi.Api.Tests.Comun;

public class ResultadoTests
{
    private static readonly Error ErrorDePrueba = new(CodigosError.LimiteDelPlan, "El plan no permite más APIs");

    [Fact]
    public void Exito_ConValor_EsExitoYExponeElValor()
    {
        var resultado = Resultado<int>.Exito(42);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().Be(42);
        resultado.Error.Should().BeNull();
    }

    [Fact]
    public void Fallo_ConError_NoEsExitoYExponeElCodigo()
    {
        var resultado = Resultado<int>.Fallo(ErrorDePrueba);

        resultado.EsExito.Should().BeFalse();
        resultado.Error!.Codigo.Should().Be("limite_del_plan");
        resultado.Error.Mensaje.Should().Be("El plan no permite más APIs");
    }

    [Fact]
    public void Valor_ResultadoFallido_LanzaExcepcion()
    {
        var resultado = Resultado<int>.Fallo(ErrorDePrueba);

        var leerValor = () => resultado.Valor;

        leerValor.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConversionImplicita_DesdeValorYDesdeError_CreaElResultadoCorrespondiente()
    {
        Resultado<string> exito = "listo";
        Resultado<string> fallo = ErrorDePrueba;

        exito.EsExito.Should().BeTrue();
        exito.Valor.Should().Be("listo");
        fallo.EsExito.Should().BeFalse();
        fallo.Error.Should().Be(ErrorDePrueba);
    }

    [Fact]
    public void ResultadoSinValor_ExitoYFallo_SeDistinguen()
    {
        Resultado fallo = ErrorDePrueba;

        Resultado.Exito().EsExito.Should().BeTrue();
        fallo.EsExito.Should().BeFalse();
        fallo.Error.Should().Be(ErrorDePrueba);
    }

    [Fact]
    public void Error_ConDetalle_ConservaElDetalle()
    {
        var error = new Error(CodigosError.LimiteDelPlan, "El plan no permite más APIs", new { limite = "apis" });

        error.Detalle.Should().BeEquivalentTo(new { limite = "apis" });
    }
}
