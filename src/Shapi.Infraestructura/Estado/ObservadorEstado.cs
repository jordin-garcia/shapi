using System.Diagnostics;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Estado;
using Shapi.Dominio.Consumo;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Persistencia;
using StackExchange.Redis;
using ServidorHttp = Microsoft.AspNetCore.Hosting.Server.IServer;

namespace Shapi.Infraestructura.Estado;

public sealed class ObservadorEstado(ShapiDbContext db, IConnectionMultiplexer redis, IReloj reloj,
    TimeProvider tiempo, IConfiguration configuracion, IHttpClientFactory clientes, ServidorHttp servidor,
    ILogger<ObservadorEstado> registro) : IObservadorEstado
{
    public async Task<ObservacionEstado> ObservarAsync(CancellationToken cancelacion)
    {
        // Las sondas tienen plazos propios: la caída de un componente no impide ver los otros.
        var datos = BaseDatosAsync(cancelacion);
        var cache = RedisAsync(cancelacion);
        var api = ApiAsync(cancelacion);
        var smtp = SmtpAsync(cancelacion);
        await Task.WhenAll(datos, cache, api, smtp);
        var d = await datos;
        var r = await cache;
        var atraso = d.UltimoLote is { } ultimo ? Math.Max(0, (reloj.Ahora - ultimo).TotalSeconds) : (double?)null;
        return new(r.Compuerta, r.Trabajador, r.Latencia.Milisegundos, r.Latencia.Supera2500,
            await api, r.Milisegundos, d.Milisegundos, r.Pendientes, atraso,
            bool.TryParse(configuracion["SHAPI_PASARELA_FALLA"], out var falla) && falla,
            await smtp, d.CorreoAntiguo is not null, d.FalloReciente || d.Milisegundos is null,
            d.CorreoAntiguo ?? d.PrimerFallo);
    }

    private async Task<SaludRedis> RedisAsync(CancellationToken cancelacion)
    {
        using var plazo = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        plazo.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            return await new LectorEstadoRedis(redis, tiempo).LeerAsync(plazo.Token);
        }
        catch (OperationCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            return new(null, false, false, false, new(null));
        }
    }

    private async Task<DatosSalud> BaseDatosAsync(CancellationToken cancelacion)
    {
        using var plazo = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        plazo.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            var inicio = Stopwatch.GetTimestamp();
            await db.Database.ExecuteSqlRawAsync("SELECT 1", plazo.Token);
            var ms = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
            var ultimo = await db.Set<LoteConsolidado>().AsNoTracking()
                .MaxAsync(x => (DateTimeOffset?)x.ProcesadoEn, plazo.Token);
            var limite = reloj.Ahora.AddMinutes(-2);
            var cola = db.Set<CorreoSaliente>().AsNoTracking();
            var antiguo = await cola.Where(x => x.Estado == EstadoCorreo.Pendiente && x.CreadoEn < limite)
                .MinAsync(x => (DateTimeOffset?)x.CreadoEn, plazo.Token);
            var fallo = await cola.Where(x => x.Estado != EstadoCorreo.Enviado && x.UltimoError != null && x.ActualizadoEn >= limite)
                .MinAsync(x => (DateTimeOffset?)x.ActualizadoEn, plazo.Token);
            return new(ms, ultimo, antiguo, fallo is not null, fallo);
        }
        catch (Exception ex) when (!cancelacion.IsCancellationRequested)
        {
            registro.LogWarning("No se pudo comprobar PostgreSQL: {Tipo}", ex.GetType().Name);
            return new(null, null, null, false, null);
        }
    }

    private async Task<double?> ApiAsync(CancellationToken cancelacion)
    {
        try
        {
            // La dirección proviene del servidor local, nunca de Host ni de una entrada del usuario.
            var direccion = servidor.Features.Get<IServerAddressesFeature>()?.Addresses
                .FirstOrDefault(x => x.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
            if (direccion is null)
            {
                return null;
            }
            var uri = new UriBuilder(direccion.Replace("+", "localhost", StringComparison.Ordinal)
                .Replace("*", "localhost", StringComparison.Ordinal))
            { Host = "localhost", Path = "/salud" }.Uri;
            var inicio = Stopwatch.GetTimestamp();
            using var respuesta = await clientes.CreateClient("salud-api").GetAsync(uri, cancelacion);
            return respuesta.IsSuccessStatusCode ? Stopwatch.GetElapsedTime(inicio).TotalMilliseconds : null;
        }
        catch (Exception) when (!cancelacion.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task<bool> SmtpAsync(CancellationToken cancelacion)
    {
        using var plazo = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        plazo.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            using var cliente = new SmtpClient { Timeout = 2000 };
            var host = configuracion["SHAPI_SMTP_HOST"]?.Trim() ?? "localhost";
            var puerto = int.TryParse(configuracion["SHAPI_SMTP_PUERTO"], out var p) && p is > 0 and <= 65535 ? p : 1025;
            var tls = bool.TryParse(configuracion["SHAPI_SMTP_TLS"], out var t) && t;
            var seguridad = !tls ? SecureSocketOptions.None : puerto == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await cliente.ConnectAsync(host, puerto, seguridad, plazo.Token);
            await cliente.NoOpAsync(plazo.Token);
            await cliente.DisconnectAsync(true, plazo.Token);
            return true;
        }
        catch (Exception) when (!cancelacion.IsCancellationRequested)
        {
            return false;
        }
    }

    private sealed record DatosSalud(double? Milisegundos, DateTimeOffset? UltimoLote,
        DateTimeOffset? CorreoAntiguo, bool FalloReciente, DateTimeOffset? PrimerFallo);
}
