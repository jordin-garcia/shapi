using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Consumo;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ConsumoDiarioConfiguracion : IEntityTypeConfiguration<ConsumoDiario>
{
    public void Configure(EntityTypeBuilder<ConsumoDiario> builder)
    {
        builder.ToTable("consumo_diario");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();

        builder.HasOne<Api>()
            .WithMany()
            .HasForeignKey(x => x.ApiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ruta>()
            .WithMany()
            .HasForeignKey(x => x.RutaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SuscripcionApi>()
            .WithMany()
            .HasForeignKey(x => x.SuscripcionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Fecha).IsRequired();

        builder.Property(x => x.Entorno)
            .IsRequired()
            .HasConversion(Conversores.EntornoConsumo);

        builder.ToTable(t => t.HasCheckConstraint("ck_consumo_diario_entorno",
            "entorno IN ('produccion','pruebas')"));

        builder.Property(x => x.Peticiones).HasDefaultValue(0);
        builder.Property(x => x.Llamadas).HasDefaultValue(0);
        builder.Property(x => x.BytesEntrada).HasDefaultValue(0);
        builder.Property(x => x.BytesSalida).HasDefaultValue(0);

        // 07 §3.5: la convención snake_case daría rechazos401 y origen2xx
        builder.Property(x => x.Rechazos401).HasColumnName("rechazos_401").HasDefaultValue(0);
        builder.Property(x => x.Rechazos403).HasColumnName("rechazos_403").HasDefaultValue(0);
        builder.Property(x => x.Rechazos404).HasColumnName("rechazos_404").HasDefaultValue(0);
        builder.Property(x => x.Rechazos429).HasColumnName("rechazos_429").HasDefaultValue(0);

        builder.Property(x => x.Origen2xx).HasColumnName("origen_2xx").HasDefaultValue(0);
        builder.Property(x => x.Origen3xx).HasColumnName("origen_3xx").HasDefaultValue(0);
        builder.Property(x => x.Origen4xx).HasColumnName("origen_4xx").HasDefaultValue(0);
        builder.Property(x => x.Origen5xx).HasColumnName("origen_5xx").HasDefaultValue(0);
        builder.Property(x => x.OrigenFallo).HasDefaultValue(0);
        // 10 rangos de latencia (07 §3.5)
        builder.ToTable(t => t.HasCheckConstraint("ck_consumo_diario_hist_latencia_total", "cardinality(hist_latencia_total) = 10"));
        builder.ToTable(t => t.HasCheckConstraint("ck_consumo_diario_hist_latencia_compuerta", "cardinality(hist_latencia_compuerta) = 10"));
        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();

        // RNF-08: UNIQUE NULLS NOT DISTINCT
        builder.HasIndex(x => new { x.Fecha, x.ApiId, x.RutaId, x.SuscripcionId, x.Entorno })
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasIndex(x => new { x.ApiId, x.Fecha });
        builder.HasIndex(x => new { x.SuscripcionId, x.Fecha });
    }
}
