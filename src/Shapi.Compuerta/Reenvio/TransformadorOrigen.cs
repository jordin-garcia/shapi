using Shapi.Contratos.Redis;
using Yarp.ReverseProxy.Forwarder;

namespace Shapi.Compuerta.Reenvio;

/// <summary>Las cabeceras hacia el origen (08 §5, RF-31).</summary>
internal sealed class TransformadorOrigen(ContextoClave clave) : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest,
        string destinationPrefix, CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        // El origen nunca recibe la clave del consumidor.
        proxyRequest.Headers.Remove(CabecerasCompuerta.ApiKey);
        proxyRequest.Content?.Headers.Remove(CabecerasCompuerta.ApiKey);

        // Se reemplazan las que haya enviado el cliente, para que no pueda hacerse pasar por otro consumidor.
        Reemplazar(proxyRequest, CabecerasCompuerta.Consumidor, clave.ConsumidorId.ToString());
        Reemplazar(proxyRequest, CabecerasCompuerta.Entorno, clave.Entorno);
    }

    private static void Reemplazar(HttpRequestMessage peticion, string nombre, string valor)
    {
        peticion.Headers.Remove(nombre);
        peticion.Content?.Headers.Remove(nombre);
        peticion.Headers.TryAddWithoutValidation(nombre, valor);
    }
}
