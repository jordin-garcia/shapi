using Shapi.Aplicacion.Comun;

namespace Shapi.Aplicacion.Estado;

public sealed record EstadoComponente(string Id, string Estado, DateTimeOffset? IncidenciaDesde = null);
public sealed record EstadoPlataforma(DateTimeOffset ComprobadoEn, decimal? LatenciaP95Ms,
    bool LatenciaSupera2500, IReadOnlyList<EstadoComponente> Componentes);

public sealed record ObservacionEstado(bool LatidoCompuerta, bool LatidoTrabajador,
    decimal? LatenciaP95Ms, bool LatenciaSupera2500, double? ApiMs, double? RedisMs, double? PostgresMs,
    bool MetricasPendientes, double? AtrasoConsolidacionSegundos, bool PasarelaFalla,
    bool SmtpResponde, bool CorreosAntiguos, bool FallosCorreoRecientes, DateTimeOffset? IncidenciaCorreoDesde);

public interface IObservadorEstado
{
    Task<ObservacionEstado> ObservarAsync(CancellationToken cancelacion);
}

/// <summary>Caché por proceso, con exclusión mutua; no depende del Redis cuya salud se comprueba.</summary>
public sealed class CacheEstado(TimeProvider tiempo) : IDisposable
{
    private readonly SemaphoreSlim _candado = new(1, 1);
    private EstadoPlataforma? _valor;
    private long _guardado;

    public async Task<EstadoPlataforma> ObtenerAsync(Func<Task<EstadoPlataforma>> comprobar, CancellationToken cancelacion)
    {
        await _candado.WaitAsync(cancelacion);
        try
        {
            if (_valor is not null && tiempo.GetElapsedTime(_guardado) < TimeSpan.FromSeconds(30))
            {
                return _valor;
            }
            _valor = await comprobar();
            _guardado = tiempo.GetTimestamp();
            return _valor;
        }
        finally
        {
            _candado.Release();
        }
    }

    public void Dispose() => _candado.Dispose();
}

public sealed class ConsultarEstado(IObservadorEstado observador, CacheEstado cache, IReloj reloj)
{
    public Task<EstadoPlataforma> EjecutarAsync(CancellationToken cancelacion) => cache.ObtenerAsync(async () =>
    {
        var o = await observador.ObservarAsync(cancelacion);
        return new EstadoPlataforma(reloj.Ahora, o.LatenciaP95Ms, o.LatenciaSupera2500,
        [
            new("compuerta", ReglasEstado.Compuerta(o.LatidoCompuerta, o.LatenciaP95Ms, o.LatenciaSupera2500)),
            new("api_control", ReglasEstado.PorLatencia("api_control", o.ApiMs)),
            new("trabajador", ReglasEstado.Trabajador(o.LatidoTrabajador, o.MetricasPendientes, o.AtrasoConsolidacionSegundos)),
            new("redis", ReglasEstado.PorLatencia("redis", o.RedisMs)),
            new("postgres", ReglasEstado.PorLatencia("postgres", o.PostgresMs)),
            new("pasarela", ReglasEstado.Pasarela(o.PasarelaFalla)),
            new("correo", ReglasEstado.Correo(o.SmtpResponde, o.CorreosAntiguos, o.FallosCorreoRecientes), o.IncidenciaCorreoDesde),
        ]);
    }, cancelacion);
}

public static class ReglasEstado
{
    public static string PorLatencia(string id, double? ms) => id switch
    {
        "api_control" => ms is null or > 1000 ? "degradado" : "en_servicio",
        "redis" => ms is null ? "fuera_de_servicio" : ms >= 50 ? "degradado" : "en_servicio",
        "postgres" => ms is null ? "fuera_de_servicio" : ms >= 200 ? "degradado" : "en_servicio",
        "compuerta" => ms > 15 ? "degradado" : "en_servicio",
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };

    public static string Compuerta(bool latido, decimal? ms, bool supera2500) => !latido ? "fuera_de_servicio"
        : supera2500 || ms > 15 ? "degradado" : "en_servicio";
    public static string Trabajador(bool latido, bool pendientes, double? atraso) => !latido ? "fuera_de_servicio"
        : pendientes && (atraso is null or >= 60) ? "degradado" : "en_servicio";
    public static string Pasarela(bool falla) => falla ? "fuera_de_servicio" : "en_servicio";
    public static string Correo(bool smtp, bool antiguos, bool fallos) => !smtp ? "fuera_de_servicio"
        : antiguos || fallos ? "degradado" : "en_servicio";
}
