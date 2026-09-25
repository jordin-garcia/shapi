using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Consumo;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ConsumoDiarioConfiguracion : IEntityTypeConfiguration<ConsumoDiario>
{
    public void Configure(EntityTypeBuilder<ConsumoDiario> builder)
    {
        builder.ToTable("consumo_diario");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Fecha).IsRequired();

        builder.Property(x => x.Entorno)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_consumo_entorno", "entorno IN ('Produccion','Pruebas')"));

        builder.Property(x => x.Peticiones).HasDefaultValue(0);
        builder.Property(x => x.Llamadas).HasDefaultValue(0);
        builder.Property(x => x.BytesEntrada).HasDefaultValue(0);
        builder.Property(x => x.BytesSalida).HasDefaultValue(0);

        builder.Property(x => x.Rechazos401).HasDefaultValue(0);
        builder.Property(x => x.Rechazos403).HasDefaultValue(0);
        builder.Property(x => x.Rechazos404).HasDefaultValue(0);
        builder.Property(x => x.Rechazos429).HasDefaultValue(0);

        builder.Property(x => x.Origen2xx).HasDefaultValue(0);
        builder.Property(x => x.Origen3xx).HasDefaultValue(0);
        builder.Property(x => x.Origen4xx).HasDefaultValue(0);
        builder.Property(x => x.Origen5xx).HasDefaultValue(0);
        builder.Property(x => x.OrigenFallo).HasDefaultValue(0);

        builder.HasIndex(x => new { x.Fecha, x.ApiId, x.RutaId, x.SuscripcionId, x.Entorno })
            .IsUnique()
            .AreNullsDistinct(false); // NULLS NOT DISTINCT

        builder.HasIndex(x => new { x.ApiId, x.Fecha });
        builder.HasIndex(x => new { x.SuscripcionId, x.Fecha });
    }
}
