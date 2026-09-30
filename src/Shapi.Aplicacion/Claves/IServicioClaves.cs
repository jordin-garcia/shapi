using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Claves;

namespace Shapi.Aplicacion.Claves;

/// <summary>
/// Ciclo de vida de las claves de API (RF-26 a RF-28, CU-13). Guarda en PostgreSQL y, después del <i>commit</i>,
/// publica en Redis con <see cref="IPublicadorCache"/>. Todas las consultas pasan por el filtro global por
/// organización (10 §2): una clave de otra organización o de otro consumidor es <see cref="ErroresClaves.NoEncontrada"/>.
/// </summary>
public interface IServicioClaves
{
    /// <summary>
    /// Emite una clave de producción y una de pruebas al activarse una suscripción de API (RF-26). Solo emite los tipos
    /// que no tienen ya una clave activa. Guarda, publica en Redis y devuelve las claves completas: es la única vez que
    /// se ven. Se llama después de confirmar la suscripción, dentro del contexto de su organización. No se llama dos veces
    /// a la vez para la misma suscripción: la segunda chocaría con el índice único de clave activa por tipo y lanzaría
    /// <c>DbUpdateException</c> (la contratación ya lo evita, porque solo hay una suscripción vigente por consumidor).
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la suscripción no existe, es de otra organización o está finalizada.</exception>
    Task<IReadOnlyList<ClaveEmitida>> EmitirClavesParaSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default);

    /// <summary>Las claves de la suscripción sin finalizar del consumidor en la API del portal (B2.3).</summary>
    Task<IReadOnlyList<VistaClave>> ClavesDelConsumidor(ConsumidorDelPortal consumidor, CancellationToken cancelacion = default);

    /// <summary>Emite una clave del tipo pedido si la suscripción no tiene otra activa de ese tipo (CU-13, flujo alterno).</summary>
    Task<Resultado<ClaveEmitida>> Emitir(ConsumidorDelPortal consumidor, TipoClave tipo, CancellationToken cancelacion = default);

    /// <summary>
    /// Rota una clave activa del consumidor (RF-27, 06 §5.4). Si otra clave rotada del mismo tipo sigue dentro de sus
    /// 24 horas, deja de funcionar en ese momento, para que no coexistan más de dos.
    /// </summary>
    Task<Resultado<ClaveRotada>> Rotar(ConsumidorDelPortal consumidor, Guid claveId, CancellationToken cancelacion = default);

    /// <summary>El consumidor revoca una clave propia (RF-28, B2.6).</summary>
    Task<Resultado<VistaClave>> RevocarPropia(ConsumidorDelPortal consumidor, Guid claveId, CancellationToken cancelacion = default);

    /// <summary>Las claves de los consumidores de una API, agrupadas por consumidor (A4.3).</summary>
    Task<Resultado<PaginaClavesDeApi>> ClavesDeApi(Guid apiId, int pagina, int tamano, CancellationToken cancelacion = default);

    /// <summary>El proveedor revoca la clave de un consumidor de su API (RF-28, A4.3b).</summary>
    Task<Resultado<VistaClave>> RevocarDeConsumidor(MiembroDelPanel miembro, Guid apiId, Guid claveId, CancellationToken cancelacion = default);
}

/// <summary>Los errores del módulo Claves.</summary>
public static class ErroresClaves
{
    /// <summary>
    /// La clave, la API o la suscripción no existe o es de otra organización o de otro consumidor. Se responde 404 sin
    /// cuerpo, así que su código nunca sale de la API (04 §4).
    /// </summary>
    public static readonly Error NoEncontrada = new("no_encontrada", "No existe.");

    public static readonly Error NoRotable = new(Contratos.CodigosError.ClaveNoRotable,
        "Solo se puede rotar una clave activa.");

    public static readonly Error ActivaExistente = new(Contratos.CodigosError.ClaveActivaExistente,
        "Ya tiene una clave activa de ese tipo.");
}
