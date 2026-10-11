using System.Globalization;
using Shapi.Aplicacion.Apis;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;

namespace Shapi.Aplicacion.Consumo;

public sealed record ConsumoDia(DateOnly Fecha, long Peticiones);
public sealed record ConsumoRuta(Guid? RutaId, string? Metodo, string Patron, long Peticiones, decimal Porcentaje,
    Percentil95 LatenciaTotalP95, Percentil95 LatenciaCompuertaP95);
public sealed record ConsumoError(string Codigo, string Motivo, long Peticiones, decimal Porcentaje);
public sealed record ConsumoApi(Guid ApiId, string ApiNombre, DateOnly Desde, DateOnly Hasta,
    long Peticiones, long Llamadas, long BytesEntrada, long BytesSalida,
    Percentil95 LatenciaTotalP95, Percentil95 LatenciaCompuertaP95,
    IReadOnlyList<ConsumoDia> Dias, IReadOnlyList<ConsumoRuta> Rutas,
    IReadOnlyList<ConsumoError> RechazosCompuerta, IReadOnlyList<ConsumoError> ErroresOrigen);

/// <summary>RF-35: agrega contadores e histogramas consolidados; nunca promedia percentiles.</summary>
public sealed class ConsultarConsumoApi(IRepositorioApis apis, IConsultaConsumo consumo, IReloj reloj)
{
    public async Task<Resultado<ConsumoApi>> Ejecutar(Guid organizacionId, Guid apiId, string? desde, string? hasta,
        CancellationToken cancelacion = default)
    {
        var errores = new Dictionary<string, string[]>();
        var hoy = DateOnly.FromDateTime(reloj.Ahora.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var fin = LeerFecha(hasta, hoy, "hasta", errores);
        var inicio = LeerFecha(desde, DateOnly.FromDayNumber(Math.Max(0, fin.DayNumber - 9)), "desde", errores);
        if (desde is null && fin.DayNumber < 9)
        {
            errores["hasta"] = ["El fin debe permitir un periodo de diez días."];
        }
        if (inicio > fin || fin.DayNumber - inicio.DayNumber >= 366)
        {
            errores["desde"] = ["Use un inicio anterior o igual al fin y un periodo de hasta 366 días."];
        }
        if (errores.Count > 0)
        {
            return new Error(CodigosError.DatosInvalidos, "Revise las fechas del periodo.", errores);
        }
        var api = await apis.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return new Error(CodigosError.ApiNoEncontrada, "La API no existe.");
        }
        var periodo = await consumo.PorApiAsync(organizacionId, apiId, inicio, fin, cancelacion: cancelacion);
        var rutas = (await apis.ObtenerRutas(apiId, cancelacion)).ToDictionary(x => x.Id);
        var dias = periodo.Dias.GroupBy(x => x.Fecha).ToDictionary(x => x.Key, x => x.Sum(d => d.Peticiones));
        var totalPeticiones = periodo.Peticiones;
        decimal Porcentaje(long cantidad) => totalPeticiones == 0 ? 0 : cantidad * 100m / totalPeticiones;
        ConsumoError ErrorConsumo(string codigo, string motivo, long cantidad) => new(codigo, motivo, cantidad, Porcentaje(cantidad));
        var detalleRutas = periodo.Dias.GroupBy(x => x.RutaId).Select(grupo =>
        {
            var subtotal = new ConsumoPeriodo(grupo.ToArray());
            var ruta = grupo.Key is Guid id ? rutas.GetValueOrDefault(id) : null;
            return new ConsumoRuta(grupo.Key, ruta?.Metodo.ToString().ToUpperInvariant(), ruta?.Patron ?? "Sin ruta identificada",
                subtotal.Peticiones, Porcentaje(subtotal.Peticiones), subtotal.LatenciaTotalP95, subtotal.LatenciaCompuertaP95);
        }).OrderByDescending(x => x.Peticiones).ThenBy(x => x.Metodo).ThenBy(x => x.Patron).ToArray();
        return new ConsumoApi(api.Id, api.Nombre, inicio, fin, totalPeticiones, periodo.Llamadas,
            periodo.Dias.Sum(x => x.BytesEntrada), periodo.Dias.Sum(x => x.BytesSalida),
            periodo.LatenciaTotalP95, periodo.LatenciaCompuertaP95,
            Enumerable.Range(inicio.DayNumber, fin.DayNumber - inicio.DayNumber + 1)
                .Select(d => DateOnly.FromDayNumber(d)).Select(d => new ConsumoDia(d, dias.GetValueOrDefault(d))).ToArray(),
            detalleRutas,
            [ErrorConsumo("401", "Clave inválida o revocada", periodo.Dias.Sum(x => x.Rechazos401)),
             ErrorConsumo("403", "Acceso no permitido", periodo.Dias.Sum(x => x.Rechazos403)),
             ErrorConsumo("429", "Cuota agotada o límite por minuto excedido", periodo.Dias.Sum(x => x.Rechazos429))],
            [ErrorConsumo("4xx", "Respuestas 4xx de su servidor", periodo.Dias.Sum(x => x.Origen4xx)),
             ErrorConsumo("5xx", "Respuestas 5xx de su servidor", periodo.Dias.Sum(x => x.Origen5xx)),
             ErrorConsumo("fallas", "Fallas de conexión o tiempo de espera del origen", periodo.Dias.Sum(x => x.OrigenFallo))]);
    }

    private static DateOnly LeerFecha(string? valor, DateOnly predeterminada, string campo, IDictionary<string, string[]> errores)
    {
        if (valor is null)
        {
            return predeterminada;
        }
        if (DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            return fecha;
        }
        errores[campo] = ["Use una fecha válida con formato AAAA-MM-DD."];
        return predeterminada;
    }
}
