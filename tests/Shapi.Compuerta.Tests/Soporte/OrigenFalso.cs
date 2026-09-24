using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Shapi.Compuerta.Tests.Soporte;

/// <summary>
/// Origen del proveedor en memoria (TestServer): guarda la última petición que recibe y responde
/// siempre 201 con una cabecera y un cuerpo reconocibles.
/// </summary>
public sealed class OrigenFalso : IAsyncDisposable
{
    public const string CuerpoRespuesta = "{\"cotizacion\":42.5,\"moneda\":\"GTQ\"}";

    private WebApplication? _app;

    public PeticionRecibida? Ultima { get; private set; }

    public async Task IniciarAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        _app = builder.Build();
        _app.Run(async http =>
        {
            using var lector = new StreamReader(http.Request.Body);
            var cuerpo = await lector.ReadToEndAsync();
            Ultima = new PeticionRecibida(
                http.Request.Method,
                http.Request.Path.Value ?? "",
                http.Request.QueryString.Value ?? "",
                http.Request.Headers.ToDictionary(c => c.Key, c => c.Value.ToString(), StringComparer.OrdinalIgnoreCase),
                cuerpo);

            http.Response.StatusCode = StatusCodes.Status201Created;
            http.Response.Headers["X-Origen"] = "falso";
            http.Response.ContentType = "application/json";
            await http.Response.WriteAsync(CuerpoRespuesta);
        });
        await _app.StartAsync();
    }

    public HttpMessageHandler CrearManejador() => _app!.GetTestServer().CreateHandler();

    public void Olvidar() => Ultima = null;

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

public sealed record PeticionRecibida(
    string Metodo,
    string Ruta,
    string Query,
    IReadOnlyDictionary<string, string> Cabeceras,
    string Cuerpo);
