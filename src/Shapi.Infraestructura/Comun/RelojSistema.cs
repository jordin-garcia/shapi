using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Comun;

/// <summary>Reloj real en UTC. EM-10 le agrega el desplazamiento del modo demostración (09 §9).</summary>
public sealed class RelojSistema(TimeProvider proveedorTiempo) : IReloj
{
    public DateTimeOffset Ahora => proveedorTiempo.GetUtcNow();
}
