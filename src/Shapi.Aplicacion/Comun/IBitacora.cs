namespace Shapi.Aplicacion.Comun;

/// <summary>Registra las acciones sensibles de 10 §7 (RF-41). Solo agrega: nunca modifica ni borra.</summary>
public interface IBitacora
{
    Task Registrar(EntradaBitacora entrada, CancellationToken cancelacion = default);
}
