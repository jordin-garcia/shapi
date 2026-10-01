using Shapi.Contratos.Red;

namespace Shapi.Compuerta.Reenvio;

/// <summary>
/// La excepción del modo demostración a la protección contra SSRF (10 §4, punto 3): con <c>SHAPI_MODO_DEMO=true</c>,
/// los orígenes de <c>SHAPI_ORIGENES_PERMITIDOS</c> (entradas <c>host:puerto</c>) se permiten aunque sean internos.
/// </summary>
public sealed record ProteccionOrigen(bool ModoDemo, IReadOnlyList<string> OrigenesPermitidos)
{
    public const string VariableModoDemo = "SHAPI_MODO_DEMO";
    public const string VariableOrigenesPermitidos = "SHAPI_ORIGENES_PERMITIDOS";

    public static ProteccionOrigen DesdeConfiguracion(IConfiguration configuracion) => new(
        bool.TryParse(configuracion[VariableModoDemo], out var modoDemo) && modoDemo,
        (configuracion[VariableOrigenesPermitidos] ?? ValidadorDireccionOrigen.OrigenesPermitidosPorDefecto)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
