using Microsoft.Extensions.Configuration;
using Shapi.Infraestructura.Siembra.Demo;

namespace Shapi.Trabajador.Siembra;

/// <summary>
/// El comando <c>sembrar-demo [--reiniciar]</c> (JZ-05, RNF-14): lee <c>SHAPI_MODO_DEMO</c>, las URL y los secretos de
/// los orígenes, ejecuta la siembra y, al terminar, la resincronización de Redis para que la compuerta vea los datos.
/// La siembra y la resincronización llegan como funciones para poder probar el comando sin levantar el host.
/// </summary>
public static class ComandoSembrarDemo
{
    public const string Nombre = "sembrar-demo";

    public static bool EsElComando(string[] args) =>
        args.FirstOrDefault()?.Equals(Nombre, StringComparison.OrdinalIgnoreCase) == true;

    public static async Task EjecutarAsync(
        string[] args,
        IConfiguration configuracion,
        bool esProduccion,
        Func<bool, bool, string, string, string?, string?, Task> sembrar,
        Func<Task> resincronizar)
    {
        var modoDemo = bool.TryParse(configuracion["SHAPI_MODO_DEMO"], out var habilitado) && habilitado;
        var reiniciar = args.Any(a => a.Equals("--reiniciar", StringComparison.OrdinalIgnoreCase));
        var urls = ConfiguracionSiembraDemo.ResolverUrls(
            configuracion["SHAPI_URL_ORIGEN_ENVIOS"],
            configuracion["SHAPI_URL_ORIGEN_AGRO"],
            esProduccion);
        await sembrar(modoDemo, reiniciar, urls.Envios, urls.Agro,
            configuracion["SHAPI_SECRETO_ORIGEN_ENVIOS"], configuracion["SHAPI_SECRETO_ORIGEN_AGRO"]);
        await resincronizar();
    }
}
