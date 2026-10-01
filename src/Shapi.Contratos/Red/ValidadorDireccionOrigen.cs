using System.Net;
using System.Net.Sockets;

namespace Shapi.Contratos.Red;

public enum ErrorDireccionOrigen
{
    FormatoInvalido,
    ResolucionFallida,
    DireccionNoPermitida,
}

public sealed record DireccionOrigenValidada(Uri Direccion, IReadOnlyList<IPAddress> Direcciones);

public sealed record ResultadoValidacionDireccionOrigen(
    DireccionOrigenValidada? Origen,
    ErrorDireccionOrigen? Error,
    string? Detalle)
{
    public bool EsValida => Origen is not null;

    public Uri? Direccion => Origen?.Direccion;

    public IReadOnlyList<IPAddress> Direcciones => Origen?.Direcciones ?? [];

    public static ResultadoValidacionDireccionOrigen Valida(Uri direccion, IReadOnlyList<IPAddress> direcciones) =>
        new(new DireccionOrigenValidada(direccion, direcciones), null, null);

    public static ResultadoValidacionDireccionOrigen Fallo(ErrorDireccionOrigen error, string detalle) =>
        new(null, error, detalle);
}

/// <summary>
/// Valida una URL de origen y resuelve sus direcciones antes de abrir una conexión (RNF-10).
/// La compuerta reutiliza el resultado para conectarse a una de las direcciones ya comprobadas y evitar DNS rebinding.
/// </summary>
public sealed class ValidadorDireccionOrigen
{
    /// <summary>
    /// <c>SHAPI_ORIGENES_PERMITIDOS</c> por defecto (10 §4, punto 3): los orígenes de demostración en desarrollo y en
    /// el ambiente productivo simulado. La usan la API de control y la compuerta.
    /// </summary>
    public const string OrigenesPermitidosPorDefecto = "localhost:5101,localhost:5102,origen-envios:8080,origen-agro:8080";

    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolver;

    public ValidadorDireccionOrigen()
        : this((host, cancelacion) => Dns.GetHostAddressesAsync(host, cancelacion))
    {
    }

    public ValidadorDireccionOrigen(Func<string, CancellationToken, Task<IPAddress[]>> resolver) =>
        _resolver = resolver;

    public async Task<ResultadoValidacionDireccionOrigen> Validar(
        string? valor,
        bool modoDemo,
        IEnumerable<string> origenesPermitidos,
        CancellationToken cancelacion = default)
    {
        if (!Uri.TryCreate(valor, UriKind.Absolute, out var direccion)
            || (direccion.Scheme != Uri.UriSchemeHttp && direccion.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(direccion.UserInfo)
            || string.IsNullOrWhiteSpace(direccion.Host)
            || direccion.Port is < 1 or > 65535)
        {
            return ResultadoValidacionDireccionOrigen.Fallo(
                ErrorDireccionOrigen.FormatoInvalido,
                "Use una URL http o https, sin credenciales y con un puerto válido.");
        }

        IPAddress[] direcciones;
        try
        {
            direcciones = await _resolver(direccion.DnsSafeHost, cancelacion);
        }
        catch (OperationCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            return ResultadoValidacionDireccionOrigen.Fallo(
                ErrorDireccionOrigen.ResolucionFallida,
                "No se pudo resolver el host del origen.");
        }
        catch (Exception ex) when (ex is SocketException or HttpRequestException or ArgumentException)
        {
            return ResultadoValidacionDireccionOrigen.Fallo(
                ErrorDireccionOrigen.ResolucionFallida,
                "No se pudo resolver el host del origen.");
        }

        if (direcciones.Length == 0)
        {
            return ResultadoValidacionDireccionOrigen.Fallo(
                ErrorDireccionOrigen.ResolucionFallida,
                "El host del origen no devolvió ninguna dirección.");
        }

        var origenDemo = $"{direccion.IdnHost}:{direccion.Port}";
        var permitidoEnDemo = modoDemo && origenesPermitidos.Any(x =>
            string.Equals(x.Trim(), origenDemo, StringComparison.OrdinalIgnoreCase));
        if (!permitidoEnDemo && direcciones.Any(EsDireccionNoPermitida))
        {
            return ResultadoValidacionDireccionOrigen.Fallo(
                ErrorDireccionOrigen.DireccionNoPermitida,
                "El origen resuelve a una dirección interna o reservada.");
        }

        return ResultadoValidacionDireccionOrigen.Valida(direccion, direcciones);
    }

    public static bool EsDireccionNoPermitida(IPAddress direccion)
    {
        if (direccion.IsIPv4MappedToIPv6)
        {
            direccion = direccion.MapToIPv4();
        }

        if (IPAddress.IsLoopback(direccion)
            || direccion.Equals(IPAddress.Any)
            || direccion.Equals(IPAddress.IPv6Any)
            || direccion.IsIPv6LinkLocal
            || direccion.IsIPv6Multicast
            || direccion.IsIPv6SiteLocal)
        {
            return true;
        }

        var bytes = direccion.GetAddressBytes();
        if (direccion.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 0
                || bytes[0] == 10
                || bytes[0] == 127
                || (bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
                || (bytes[0] == 169 && bytes[1] == 254)
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || bytes[0] is >= 224 and <= 239;
        }

        return direccion.AddressFamily == AddressFamily.InterNetworkV6
            && (bytes[0] & 0xFE) == 0xFC;
    }
}
