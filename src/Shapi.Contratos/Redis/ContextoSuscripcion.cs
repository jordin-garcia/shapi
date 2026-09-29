using System.Globalization;

namespace Shapi.Contratos.Redis;

/// <summary>
/// Contenido del hash <c>susc:{suscripcion_id}</c> (07 §4): la suscripción de un consumidor a una API. Lo escriben la
/// API de control y el trabajador, y lo lee la compuerta; los nombres de los campos solo se definen aquí.
/// </summary>
/// <param name="Estado"><see cref="EstadoActiva"/>, <see cref="EstadoEnGracia"/> o <see cref="EstadoSuspendida"/>.</param>
/// <param name="Inicio">Inicio del ciclo, en segundos Unix.</param>
/// <param name="Fin">Fin del ciclo, en segundos Unix.</param>
public sealed record ContextoSuscripcion(
    Guid SuscripcionId,
    Guid PlanId,
    string PlanNombre,
    string Estado,
    long Inicio,
    long Fin,
    long CuotaLlamadas,
    int LimiteMinuto)
{
    public const string EstadoActiva = "activa";
    public const string EstadoEnGracia = "en_gracia";
    public const string EstadoSuspendida = "suspendida";

    /// <summary>Una suscripción finalizada no se publica: su llave se borra (07 §4).</summary>
    public const string EstadoFinalizada = "finalizada";

    public const string CampoPlanId = "plan_id";
    public const string CampoPlanNombre = "plan_nombre";
    public const string CampoEstado = "estado";
    public const string CampoInicio = "inicio";
    public const string CampoFin = "fin";
    public const string CampoCuotaLlamadas = "cuota_llamadas";
    public const string CampoLimiteMinuto = "limite_minuto";

    /// <summary>La compuerta deja pasar el tráfico en <c>activa</c> y <c>en_gracia</c> (09 §3).</summary>
    public bool PermiteTrafico => Estado is EstadoActiva or EstadoEnGracia;

    public IReadOnlyDictionary<string, string> ACampos() => new Dictionary<string, string>
    {
        [CampoPlanId] = PlanId.ToString(),
        [CampoPlanNombre] = PlanNombre,
        [CampoEstado] = Estado,
        [CampoInicio] = Inicio.ToString(CultureInfo.InvariantCulture),
        [CampoFin] = Fin.ToString(CultureInfo.InvariantCulture),
        [CampoCuotaLlamadas] = CuotaLlamadas.ToString(CultureInfo.InvariantCulture),
        [CampoLimiteMinuto] = LimiteMinuto.ToString(CultureInfo.InvariantCulture),
    };

    /// <returns><c>null</c> si falta algún campo o tiene un formato inválido.</returns>
    public static ContextoSuscripcion? DesdeCampos(Guid suscripcionId, IReadOnlyDictionary<string, string> campos)
    {
        if (!campos.TryGetValue(CampoPlanId, out var plan) || !Guid.TryParse(plan, out var planId)
            || !campos.TryGetValue(CampoPlanNombre, out var planNombre)
            || !campos.TryGetValue(CampoEstado, out var estado)
            || !Entero(campos, CampoInicio, out var inicio)
            || !Entero(campos, CampoFin, out var fin)
            || !Entero(campos, CampoCuotaLlamadas, out var cuota)
            || !Entero(campos, CampoLimiteMinuto, out var limite) || limite is < 1 or > int.MaxValue)
        {
            return null;
        }

        return new ContextoSuscripcion(suscripcionId, planId, planNombre, estado, inicio, fin, cuota, (int)limite);
    }

    private static bool Entero(IReadOnlyDictionary<string, string> campos, string campo, out long valor)
    {
        valor = 0;
        return campos.TryGetValue(campo, out var texto)
            && long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out valor);
    }
}
