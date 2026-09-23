using Microsoft.Extensions.Logging;
using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Comun;

/// <summary>Implementación por defecto mientras no exista la tabla <c>bitacora</c> (la reemplaza EM-01).</summary>
public sealed class BitacoraNula(ILogger<BitacoraNula> registro) : IBitacora
{
    public Task Registrar(EntradaBitacora entrada, CancellationToken cancelacion = default)
    {
        registro.LogInformation("Acción no registrada (bitácora nula): {Accion}", entrada.Accion);
        return Task.CompletedTask;
    }
}
