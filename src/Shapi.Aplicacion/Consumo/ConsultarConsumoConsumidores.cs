using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Aplicacion.Consumo;

public sealed record FilaConsumoConsumidor(Guid SuscripcionId, Guid ConsumidorId, string Consumidor, string Plan,
    DateTimeOffset Inicio, DateTimeOffset Fin, long Llamadas, long Cuota, decimal Facturacion,
    EstadoPago? UltimoCobro, EstadoSuscripcion Estado);

public interface IConsultaConsumoConsumidores
{
    Task<IReadOnlyList<FilaConsumoConsumidor>> ConsultarAsync(Guid organizacionId, Guid apiId, DateOnly hoy,
        CancellationToken cancelacion = default);
}

public sealed record ConsumoConsumidor(Guid SuscripcionId, Guid ConsumidorId, string Consumidor, string Plan,
    DateOnly Inicio, DateOnly Fin, long Llamadas, long Cuota, decimal Porcentaje, decimal Facturacion,
    string? UltimoCobro, string EstadoSuscripcion);
public sealed record ConsumoConsumidores(Guid ApiId, string ApiNombre, IReadOnlyList<ConsumoConsumidor> Elementos,
    int Total, int Activas, long Llamadas, long Cuota, decimal Facturacion, string Moneda);

/// <summary>RF-36: consumo consolidado de producción y dinero efectivamente cobrado por ciclo.</summary>
public sealed class ConsultarConsumoConsumidores(IRepositorioApis apis, IConsultaConsumoConsumidores consulta, IReloj reloj)
{
    public async Task<Resultado<ConsumoConsumidores>> Ejecutar(Guid organizacionId, Guid apiId,
        CancellationToken cancelacion = default)
    {
        var api = await apis.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return new Error(CodigosError.ApiNoEncontrada, "La API no existe.");
        }
        static DateOnly Dia(DateTimeOffset instante) => DateOnly.FromDateTime(instante.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var filas = await consulta.ConsultarAsync(organizacionId, apiId, Dia(reloj.Ahora), cancelacion);
        var elementos = filas.Select(fila => new ConsumoConsumidor(fila.SuscripcionId, fila.ConsumidorId,
            fila.Consumidor, fila.Plan, Dia(fila.Inicio), Dia(fila.Fin.AddDays(-1)), fila.Llamadas, fila.Cuota,
            fila.Cuota == 0 ? 0 : fila.Llamadas * 100m / fila.Cuota, fila.Facturacion,
            fila.UltimoCobro?.ToString().ToLowerInvariant(), fila.Estado switch
            {
                EstadoSuscripcion.EnGracia => "en_gracia",
                EstadoSuscripcion.Suspendida => "suspendida",
                _ => "activa",
            })).ToArray();
        return new ConsumoConsumidores(api.Id, api.Nombre, elementos, elementos.Length,
            filas.Count(x => x.Estado == EstadoSuscripcion.Activa), filas.Sum(x => x.Llamadas),
            filas.Sum(x => x.Cuota), filas.Sum(x => x.Facturacion), "GTQ");
    }
}
