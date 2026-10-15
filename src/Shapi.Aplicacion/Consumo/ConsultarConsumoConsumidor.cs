using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Consumo;

namespace Shapi.Aplicacion.Consumo;

public sealed record ConsumoRutaCiclo(Guid? RutaId, string? Metodo, string Patron, int? PesoLlamadas,
    long Peticiones, long Llamadas, decimal Porcentaje);
public sealed record ConsumoCiclo(Guid SuscripcionId, DateOnly Inicio, DateOnly Fin,
    long Llamadas, long Cuota, long Restante, decimal Porcentaje, long Peticiones,
    long LlamadasConsolidadas, IReadOnlyList<ConsumoRutaCiclo> Rutas);

/// <summary>RF-37: cuota en tiempo real y detalle consolidado de producción, limitado al consumidor y al portal.</summary>
public sealed class ConsultarConsumoConsumidor(
    IConsultaConsumo consumo, ILectorCuotaConsumo cuota, IRepositorioApis apis, IReloj reloj)
{
    public async Task<Resultado<ConsumoCiclo>> Ejecutar(Guid organizacionId, Guid apiId, Guid consumidorId,
        CancellationToken cancelacion = default)
    {
        var suscripcion = await consumo.SuscripcionActualAsync(organizacionId, apiId, consumidorId, cancelacion);
        if (suscripcion is null)
        {
            return new Error(CodigosError.SuscripcionNoEncontrada, "No tiene un plan contratado.");
        }
        var usadas = await cuota.LeerAsync(suscripcion.Id, suscripcion.Inicio, cancelacion);
        if (usadas is null)
        {
            return new Error(CodigosError.ServicioNoDisponible, "No se pudo consultar la cuota. Intente de nuevo.");
        }
        static DateOnly Dia(DateTimeOffset instante) => DateOnly.FromDateTime(instante.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var inicio = Dia(suscripcion.Inicio);
        var fin = Dia(suscripcion.Fin.AddDays(-1));
        var hoy = Dia(reloj.Ahora);
        // La gracia y una caída del trabajador conservan el inicio del contador hasta que se renueva el ciclo.
        var hasta = hoy > fin ? hoy : fin;
        var periodo = await consumo.PorSuscripcionAsync(organizacionId, suscripcion.Id, inicio, hasta,
            EntornoConsumo.Produccion, cancelacion);
        var rutas = (await apis.ObtenerRutas(apiId, cancelacion)).ToDictionary(x => x.Id);
        var llamadas = periodo.Llamadas;
        var detalle = periodo.Dias.GroupBy(x => x.RutaId).Select(grupo =>
        {
            var ruta = grupo.Key is Guid id ? rutas.GetValueOrDefault(id) : null;
            var descontadas = grupo.Sum(x => x.Llamadas);
            return new ConsumoRutaCiclo(grupo.Key, ruta?.Metodo.ToString().ToUpperInvariant(),
                ruta?.Patron ?? "Sin ruta identificada", ruta?.PesoLlamadas, grupo.Sum(x => x.Peticiones),
                descontadas, llamadas == 0 ? 0 : descontadas * 100m / llamadas);
        }).OrderByDescending(x => x.Llamadas).ThenBy(x => x.Metodo).ThenBy(x => x.Patron).ToArray();
        return new ConsumoCiclo(suscripcion.Id, inicio, fin, usadas.Value, suscripcion.Cuota,
            Math.Max(0, suscripcion.Cuota - usadas.Value), suscripcion.Cuota == 0 ? 0 : usadas.Value * 100m / suscripcion.Cuota,
            periodo.Peticiones, llamadas, detalle);
    }
}
