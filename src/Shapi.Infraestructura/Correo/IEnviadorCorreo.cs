using Shapi.Dominio.Correo;

namespace Shapi.Infraestructura.Correo;

public interface IEnviadorCorreo
{
    Task EnviarAsync(
        CorreoSaliente correo,
        CorreoRenderizado contenido,
        CancellationToken cancelacion = default);
}
