using System.Reflection;
using System.Text.RegularExpressions;
using Shapi.Contratos;

namespace Shapi.Compuerta.Tests.Contratos;

public partial class CodigosErrorTests
{
    private static readonly string[] Codigos = typeof(CodigosError)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(campo => campo.IsLiteral && campo.FieldType == typeof(string))
        .Select(campo => (string)campo.GetRawConstantValue()!)
        .ToArray();

    [Fact]
    public void CodigosError_ContratoDeErroresDeLaCompuerta_ContieneCadaCodigoDe08Seccion4()
    {
        // RF-29, RF-30: los códigos de la tabla de docs/specs/08-compuerta.md §4
        var especificacion = File.ReadAllText(RaizRepositorio.Ruta("docs", "specs", "08-compuerta.md"));
        var inicio = especificacion.IndexOf("## 4. Contrato de errores", StringComparison.Ordinal);
        var fin = especificacion.IndexOf("## 5. Cabeceras", StringComparison.Ordinal);
        var seccion = especificacion[inicio..fin];
        var codigosDeLaTabla = FilaDeError().Matches(seccion).Select(m => m.Groups["codigo"].Value).ToArray();

        codigosDeLaTabla.Should().HaveCount(12);
        Codigos.Should().Contain(codigosDeLaTabla);
    }

    [Theory]
    // 03 · Requisitos
    [InlineData("correo_no_verificado")]
    [InlineData("correo_en_otra_organizacion")]
    // 09 · Cobros y suscripciones
    [InlineData("excede_limites_del_plan")]
    [InlineData("limite_del_plan")]
    [InlineData("plan_sin_dominio_propio")]
    [InlineData("numero_invalido")]
    [InlineData("marca_no_soportada")]
    [InlineData("tarjeta_vencida")]
    [InlineData("cvv_invalido")]
    [InlineData("fondos_insuficientes")]
    [InlineData("pasarela_no_disponible")]
    // convenciones §5
    [InlineData("pago_rechazado")]
    [InlineData("origen_inaccesible")]
    // Criterios de aceptación de las tareas del plan
    [InlineData("correo_ya_registrado")]
    [InlineData("credenciales_invalidas")]
    [InlineData("cuenta_bloqueada")]
    [InlineData("token_invalido")]
    [InlineData("consumidor_existente")]
    [InlineData("suscripcion_existente")]
    [InlineData("origen_no_permitido")]
    [InlineData("subdominio_ocupado")]
    [InlineData("especificacion_invalida")]
    [InlineData("publicacion_incompleta")]
    [InlineData("logo_invalido")]
    [InlineData("clave_no_rotable")]
    [InlineData("csrf")]
    [InlineData("caso_cerrado")]
    public void CodigosError_CodigoDeLaApiDeControl_EstaDeclarado(string codigo)
    {
        Codigos.Should().Contain(codigo);
    }

    [Fact]
    public void CodigosError_Todos_SonUnicosYEnSnakeCase()
    {
        Codigos.Should().OnlyHaveUniqueItems();
        Codigos.Should().OnlyContain(codigo => SnakeCase().IsMatch(codigo));
    }

    [GeneratedRegex(@"^\| \d{3} \| `(?<codigo>[a-z_]+)`", RegexOptions.Multiline)]
    private static partial Regex FilaDeError();

    [GeneratedRegex("^[a-z0-9]+(_[a-z0-9]+)*$")]
    private static partial Regex SnakeCase();
}
