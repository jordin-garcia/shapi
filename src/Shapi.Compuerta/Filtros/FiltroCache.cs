using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Filtros;

/// <summary>RF-13: respuestas compartidas entre consumidores, después de validar y reservar su cuota.</summary>
public sealed class FiltroCache(IConnectionMultiplexer redis, ILogger<FiltroCache> registro) : IFiltroCompuerta
{
    public const int LimiteCuerpo = 1024 * 1024;

    public async ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        var http = contexto.Http;
        if (!HttpMethods.IsGet(http.Request.Method) || contexto.Ruta is not { CacheSegundos: > 0 } ruta)
        {
            return ResultadoFiltro.Continuar;
        }

        var api = contexto.Api!;
        var objetivo = http.Features.Get<IHttpRequestFeature>()?.RawTarget;
        var camino = objetivo is not null && objetivo.StartsWith('/')
            ? objetivo.Split('?', 2)[0]
            : http.Request.PathBase.ToUriComponent() + http.Request.Path.ToUriComponent();
        var llave = LlavesRedis.Cache(api.ApiId, ruta.RutaId, http.Request.Method, camino, http.Request.QueryString.Value);
        var campos = (await redis.GetDatabase().HashGetAllAsync(llave)).ToDictionary(c => c.Name.ToString(), c => c.Value);
        if (campos.TryGetValue("version", out var version) && version == api.Version
            && campos.TryGetValue("status", out var status) && status == 200
            && campos.TryGetValue("headers", out var headers) && campos.TryGetValue("body", out var body)
            && (byte[]?)body is { Length: <= LimiteCuerpo } cuerpo)
        {
            var cabeceras = JsonSerializer.Deserialize<Dictionary<string, string[]>>(headers.ToString());
            if (cabeceras is not null)
            {
                foreach (var (nombre, valores) in cabeceras.Where(c => EsCabeceraAlmacenable(c.Key)))
                {
                    http.Response.Headers[nombre] = new StringValues(valores);
                }
                Marcar(http.Response, "HIT");
                http.Response.StatusCode = StatusCodes.Status200OK;
                await http.Response.Body.WriteAsync(cuerpo, http.RequestAborted);
                return ResultadoFiltro.Responder(StatusCodes.Status200OK);
            }
        }

        Marcar(http.Response, "MISS");
        contexto.CapturarRespuesta = siguiente => CapturarAsync(contexto, llave, siguiente);
        return ResultadoFiltro.Continuar;
    }

    private static void Marcar(HttpResponse respuesta, string valor) => respuesta.OnStarting(() =>
    {
        // Reemplaza también cualquier valor inventado por el origen.
        respuesta.Headers[CabecerasCompuerta.Cache] = valor;
        return Task.CompletedTask;
    });

    private async Task CapturarAsync(ContextoPeticion contexto, string llave, Func<Task> siguiente)
    {
        var http = contexto.Http;
        var original = http.Response.Body;
        using var copia = new FlujoCaptura(original);
        http.Response.Body = copia;
        try
        {
            await siguiente();
        }
        finally
        {
            http.Response.Body = original;
        }

        if (http.Response.StatusCode != StatusCodes.Status200OK || contexto.FalloOrigen
            || http.RequestAborted.IsCancellationRequested || copia.SuperaLimite
            || http.Response.Headers.ContainsKey("Set-Cookie"))
        {
            return;
        }

        var cabeceras = http.Response.Headers.Where(c => EsCabeceraAlmacenable(c.Key))
            .ToDictionary(c => c.Key, c => c.Value.Select(v => v ?? "").ToArray());
        // Vary: Origin lo agrega el filtro CORS para la petición actual; no se acumula en los aciertos.
        if (cabeceras.TryGetValue("Vary", out var vary))
        {
            var valores = vary.SelectMany(v => v.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Where(v => !v.Equals("Origin", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (valores.Length == 0)
            {
                cabeceras.Remove("Vary");
            }
            else
            {
                cabeceras["Vary"] = valores;
            }
        }
        try
        {
            var lote = redis.GetDatabase().CreateTransaction();
            _ = lote.KeyDeleteAsync(llave);
            _ = lote.HashSetAsync(llave,
            [
                new("status", 200),
                new("headers", JsonSerializer.Serialize(cabeceras)),
                new("body", copia.Cuerpo),
                new("version", contexto.Api!.Version.ToString(CultureInfo.InvariantCulture)),
            ]);
            _ = lote.KeyExpireAsync(llave, TimeSpan.FromSeconds(contexto.Ruta!.CacheSegundos));
            await lote.ExecuteAsync();
        }
        catch (RedisException excepcion)
        {
            // La respuesta ya se transmitió: una falla al guardar la copia no la convierte en un 503.
            registro.LogWarning("No se pudo guardar la respuesta en caché: {Tipo}", excepcion.GetType().Name);
        }
    }

    private static bool EsCabeceraAlmacenable(string nombre) =>
        !CabecerasCompuerta.ExpuestasCors.Contains(nombre, StringComparer.OrdinalIgnoreCase)
        && !nombre.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase)
        && !new[] { "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization", "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Set-Cookie" }
            .Contains(nombre, StringComparer.OrdinalIgnoreCase);

    /// <summary>Transmite sin esperar el cuerpo completo y conserva como máximo 1 MB. No cierra el flujo original.</summary>
    private sealed class FlujoCaptura(Stream original) : Stream
    {
        private readonly MemoryStream _copia = new();
        public bool SuperaLimite { get; private set; }
        public byte[] Cuerpo => _copia.ToArray();
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => original.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => original.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
            original.Write(buffer, offset, count);
            Capturar(buffer.AsSpan(offset, count));
        }
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await original.WriteAsync(buffer, cancellationToken);
            Capturar(buffer.Span);
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        private void Capturar(ReadOnlySpan<byte> buffer)
        {
            if (SuperaLimite)
            {
                return;
            }
            if (_copia.Length + buffer.Length > LimiteCuerpo)
            {
                SuperaLimite = true;
                _copia.SetLength(0);
                return;
            }
            _copia.Write(buffer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _copia.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
