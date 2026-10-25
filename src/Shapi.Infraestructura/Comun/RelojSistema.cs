using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Infraestructura.Comun;

/// <summary>La lectura de la hora nunca espera a Redis, tampoco en demostración (09 §9, RNF-06).</summary>
public sealed class RelojSistema(
    TimeProvider proveedorTiempo, IConfiguration configuracion, IConnectionMultiplexer? redis = null,
    ILogger<RelojSistema>? registro = null) : BackgroundService, IReloj
{
    private readonly bool _modoDemo = redis is not null
        && string.Equals(configuracion["SHAPI_MODO_DEMO"], "true", StringComparison.OrdinalIgnoreCase);
    private readonly ILogger<RelojSistema> _registro = registro ?? NullLogger<RelojSistema>.Instance;
    private readonly object _puerta = new();
    private long _segundos;
    private long _version;
    private bool _suscrito;

    public DateTimeOffset Ahora => proveedorTiempo.GetUtcNow().AddSeconds(Interlocked.Read(ref _segundos));

    // Los suscriptores solo encolan trabajo; la hora ya está actualizada cuando reciben el aviso.
    public event Action? DesplazamientoActualizado;

    /// <summary>Carga la hora antes de la siembra, que también se ejecuta sin arrancar el host.</summary>
    public Task InicializarAsync(CancellationToken cancelacion = default) => _modoDemo
        ? SincronizarSeguroAsync(cancelacion, suscribir: false)
        : Task.CompletedTask;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_modoDemo)
        {
            await SincronizarSeguroAsync(cancellationToken);
        }
        await base.StartAsync(cancellationToken);
    }

    /// <summary>El mensaje lleva el total en segundos. Es idempotente y tolera mensajes fuera de orden.</summary>
    public void AplicarDesplazamiento(long segundos)
    {
        if (!_modoDemo || !EsValido(segundos))
        {
            return;
        }
        bool cambio;
        lock (_puerta)
        {
            _version++;
            cambio = segundos > _segundos;
            if (cambio)
            {
                Interlocked.Exchange(ref _segundos, segundos);
            }
        }
        if (cambio)
        {
            DesplazamientoActualizado?.Invoke();
        }
    }

    private void Recibir(RedisChannel canal, RedisValue mensaje)
    {
        if (mensaje.TryParse(out long segundos))
        {
            AplicarDesplazamiento(segundos);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_modoDemo)
        {
            return;
        }
        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(5), proveedorTiempo);
        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                await SincronizarSeguroAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            try
            {
                await redis!.GetSubscriber().UnsubscribeAsync(
                    RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), Recibir);
            }
            catch (Exception excepcion)
            {
                _registro.LogWarning("No se pudo retirar la suscripción del reloj: {Tipo}", excepcion.GetType().Name);
            }
        }
    }

    private async Task SincronizarSeguroAsync(CancellationToken ct, bool suscribir = true)
    {
        try
        {
            if (suscribir && !_suscrito)
            {
                await redis!.GetSubscriber().SubscribeAsync(
                    RedisChannel.Literal(LlavesRedis.DemoRelojDesplazamiento), Recibir).WaitAsync(ct);
                _suscrito = true;
            }
            long version;
            lock (_puerta)
            {
                version = _version;
            }
            var valor = await redis!.GetDatabase().StringGetAsync(LlavesRedis.DemoRelojDesplazamiento).WaitAsync(ct);
            var segundos = valor.IsNull ? 0 : valor.TryParse(out long total) ? total : -1;
            bool cambio;
            lock (_puerta)
            {
                // Una lectura iniciada antes de un mensaje no puede sobrescribir la hora nueva.
                if (version != _version || !EsValido(segundos))
                {
                    return;
                }
                cambio = segundos != _segundos;
                Interlocked.Exchange(ref _segundos, segundos);
            }
            if (cambio)
            {
                DesplazamientoActualizado?.Invoke();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            _registro.LogWarning("No se pudo sincronizar el reloj; se conserva el desplazamiento conocido: {Tipo}",
                excepcion.GetType().Name);
        }
    }

    private bool EsValido(long segundos) => segundos >= 0
        && segundos <= (DateTimeOffset.MaxValue - proveedorTiempo.GetUtcNow()).TotalSeconds;
}
