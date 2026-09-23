using System.Reflection;
using System.Text.RegularExpressions;
using Shapi.Aplicacion.Comun;

namespace Shapi.Api.Tests.Comun;

// RF-41: el catálogo de acciones es exactamente el de docs/specs/10-identidad-y-seguridad.md §7.
public partial class AccionesBitacoraTests
{
    [Fact]
    public void AccionesBitacora_Constantes_CoincidenConElCatalogoDe10Seccion7()
    {
        var declaradas = typeof(AccionesBitacora)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(campo => campo.IsLiteral && campo.FieldType == typeof(string))
            .Select(campo => (string)campo.GetRawConstantValue()!)
            .ToArray();

        declaradas.Should().OnlyHaveUniqueItems();
        declaradas.Should().BeEquivalentTo(AccionesDeLaEspecificacion());
    }

    [Fact]
    public void AccionesBitacora_Todas_ContieneCadaConstante()
    {
        AccionesBitacora.Todas.Should().BeEquivalentTo(AccionesDeLaEspecificacion());
    }

    /// <summary>
    /// Lee la primera columna de la tabla. Las formas abreviadas (`.editado`) toman la entidad
    /// de la acción anterior de la misma fila (`plan_plataforma.creado` → `plan_plataforma.editado`).
    /// </summary>
    private static List<string> AccionesDeLaEspecificacion()
    {
        var especificacion = File.ReadAllText(RaizRepositorio.Ruta("docs", "specs", "10-identidad-y-seguridad.md"));
        var inicio = especificacion.IndexOf("## 7. Bitácora", StringComparison.Ordinal);
        var fin = especificacion.IndexOf("## 8. Amenazas", StringComparison.Ordinal);
        var seccion = especificacion[inicio..fin];
        var acciones = new List<string>();

        foreach (var fila in seccion.Split('\n').Where(linea => linea.StartsWith("| `", StringComparison.Ordinal)))
        {
            var primeraColumna = fila.Split('|')[1];
            var entidad = string.Empty;
            foreach (Match accion in AccionEnComillas().Matches(primeraColumna))
            {
                var valor = accion.Groups["accion"].Value;
                if (valor.StartsWith('.'))
                {
                    valor = entidad + valor;
                }
                else
                {
                    entidad = valor[..valor.IndexOf('.', StringComparison.Ordinal)];
                }

                acciones.Add(valor);
            }
        }

        acciones.Should().HaveCountGreaterThan(20, "la tabla de 10 §7 debe poder leerse");
        return acciones;
    }

    [GeneratedRegex("`(?<accion>[a-z_]*\\.[a-z_]+)`")]
    private static partial Regex AccionEnComillas();
}
