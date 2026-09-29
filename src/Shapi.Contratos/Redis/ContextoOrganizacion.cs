using System.Globalization;

namespace Shapi.Contratos.Redis;

/// <summary>
/// Contenido del hash <c>org:{organizacion_id}</c> (07 §4): el estado efectivo de la organización (07 §3.1) y el ciclo
/// y la cuota de su suscripción de plataforma. Lo escriben la API de control y el trabajador, y lo lee la compuerta.
/// </summary>
/// <param name="EstadoEfectivo"><see cref="EstadoActiva"/> o <see cref="EstadoSuspendida"/>.</param>
/// <param name="CuotaPeticiones">
/// La cuota del plan de plataforma. Es <c>null</c> (y tampoco hay ciclo) si la organización no tiene una suscripción de
/// plataforma sin finalizar, como la organización de la plataforma.
/// </param>
/// <param name="CicloInicio">Inicio del ciclo de plataforma, en segundos Unix.</param>
/// <param name="CicloFin">Fin del ciclo de plataforma, en segundos Unix.</param>
public sealed record ContextoOrganizacion(
    Guid OrganizacionId,
    string EstadoEfectivo,
    long? CuotaPeticiones,
    long? CicloInicio,
    long? CicloFin)
{
    public const string EstadoActiva = "activa";
    public const string EstadoSuspendida = "suspendida";

    public const string CampoEstadoEfectivo = "estado_efectivo";
    public const string CampoCuotaPeticiones = "cuota_peticiones";
    public const string CampoCicloInicio = "ciclo_inicio";
    public const string CampoCicloFin = "ciclo_fin";

    public bool EstaActiva => EstadoEfectivo == EstadoActiva;

    /// <summary>Los campos del hash. La cuota y el ciclo solo se escriben si existen.</summary>
    public IReadOnlyDictionary<string, string> ACampos()
    {
        var campos = new Dictionary<string, string> { [CampoEstadoEfectivo] = EstadoEfectivo };
        Agregar(campos, CampoCuotaPeticiones, CuotaPeticiones);
        Agregar(campos, CampoCicloInicio, CicloInicio);
        Agregar(campos, CampoCicloFin, CicloFin);
        return campos;
    }

    /// <returns><c>null</c> si falta el estado efectivo o algún campo numérico tiene un formato inválido.</returns>
    public static ContextoOrganizacion? DesdeCampos(Guid organizacionId, IReadOnlyDictionary<string, string> campos)
    {
        if (!campos.TryGetValue(CampoEstadoEfectivo, out var estado)
            || !Opcional(campos, CampoCuotaPeticiones, out var cuota)
            || !Opcional(campos, CampoCicloInicio, out var inicio)
            || !Opcional(campos, CampoCicloFin, out var fin))
        {
            return null;
        }

        return new ContextoOrganizacion(organizacionId, estado, cuota, inicio, fin);
    }

    private static void Agregar(Dictionary<string, string> campos, string campo, long? valor)
    {
        if (valor is not null)
        {
            campos[campo] = valor.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static bool Opcional(IReadOnlyDictionary<string, string> campos, string campo, out long? valor)
    {
        valor = null;
        if (!campos.TryGetValue(campo, out var texto))
        {
            return true;
        }

        if (!long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero))
        {
            return false;
        }

        valor = numero;
        return true;
    }
}
