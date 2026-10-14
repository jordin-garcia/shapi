using Shapi.Dominio.Pagos;

namespace Shapi.Aplicacion.Pagos;

public sealed record PagoHistorial(
    Guid Id,
    DateTimeOffset Fecha,
    ConceptoPago Concepto,
    string Descripcion,
    DateTimeOffset? PeriodoInicio,
    DateTimeOffset? PeriodoFin,
    decimal Monto,
    EstadoPago Estado,
    MarcaTarjeta? Marca,
    string? Ultimos4);

public sealed record PaginaPagos(IReadOnlyList<PagoHistorial> Elementos, int Total);

public interface IConsultaPagos
{
    Task<PaginaPagos> DePlataforma(Guid organizacionId, int pagina, int tamano, CancellationToken cancelacion);
    Task<PaginaPagos> DeConsumidor(Guid consumidorId, Guid apiId, int pagina, int tamano, CancellationToken cancelacion);
}

/// <summary>Consulta el historial de plataforma o de un consumidor en la API del portal actual (RF-21).</summary>
public sealed class ConsultarPagos(IConsultaPagos consulta)
{
    public Task<PaginaPagos> DePlataforma(Guid organizacionId, int pagina, int tamano, CancellationToken cancelacion) =>
        consulta.DePlataforma(organizacionId, pagina, tamano, cancelacion);

    public Task<PaginaPagos> DeConsumidor(Guid consumidorId, Guid apiId, int pagina, int tamano, CancellationToken cancelacion) =>
        consulta.DeConsumidor(consumidorId, apiId, pagina, tamano, cancelacion);
}
