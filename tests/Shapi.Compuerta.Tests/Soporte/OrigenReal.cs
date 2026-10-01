using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Shapi.Compuerta.Tests.Soporte;

/// <summary>
/// Un origen con Kestrel en <c>127.0.0.1</c> y un puerto libre, para probar el cliente real de la compuerta
/// (<c>ConnectCallback</c>, redirecciones) en vez del origen en memoria.
/// </summary>
public sealed class OrigenReal : IAsyncDisposable
{
    private readonly WebApplication _app;

    private OrigenReal(WebApplication app, int puerto)
    {
        _app = app;
        Puerto = puerto;
    }

    public int Puerto { get; }

    public int Peticiones { get; private set; }

    public static async Task<OrigenReal> IniciarAsync(RequestDelegate responder)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        OrigenReal? origen = null;
        app.Run(http =>
        {
            origen!.Peticiones++;
            return responder(http);
        });
        await app.StartAsync();
        origen = new OrigenReal(app, new Uri(app.Urls.First()).Port);
        return origen;
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
