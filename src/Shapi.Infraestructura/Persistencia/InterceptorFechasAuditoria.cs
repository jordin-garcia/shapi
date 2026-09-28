using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Persistencia;

/// <summary>
/// Llena <c>creado_en</c> y <c>actualizado_en</c> (07 §3) con la hora de <see cref="IReloj"/>, para que el modo
/// demostración también las adelante. Al insertar respeta un <c>creado_en</c> que la entidad ya traiga.
/// </summary>
public sealed class InterceptorFechasAuditoria(IReloj reloj) : SaveChangesInterceptor
{
    private const string CreadoEn = "CreadoEn";
    private const string ActualizadoEn = "ActualizadoEn";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        MarcarFechas(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        MarcarFechas(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void MarcarFechas(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        var ahora = reloj.Ahora;
        foreach (var entrada in contexto.ChangeTracker.Entries())
        {
            if (entrada.State == EntityState.Added)
            {
                if (entrada.Metadata.FindProperty(CreadoEn) is not null
                    && entrada.Property(CreadoEn).CurrentValue is DateTimeOffset creado && creado == default)
                {
                    entrada.Property(CreadoEn).CurrentValue = ahora;
                }

                if (entrada.Metadata.FindProperty(ActualizadoEn) is not null)
                {
                    entrada.Property(ActualizadoEn).CurrentValue = ahora;
                }
            }
            else if (entrada.State == EntityState.Modified && entrada.Metadata.FindProperty(ActualizadoEn) is not null)
            {
                entrada.Property(ActualizadoEn).CurrentValue = ahora;
            }
        }
    }
}
