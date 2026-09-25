using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class SuscripcionApiConfiguracion : IEntityTypeConfiguration<SuscripcionApi>
{
    public void Configure(EntityTypeBuilder<SuscripcionApi> builder)
    {
        builder.ToTable("suscripcion_api");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<string>();
            
        builder.ToTable(t => t.HasCheckConstraint("CK_suscripcion_api_estado", "estado IN ('Activa','EnGracia','Suspendida','Finalizada')"));
        builder.ToTable(t => t.HasCheckConstraint("CK_suscripcion_api_fechas", "fin > inicio"));

        builder.HasIndex(x => new { x.ConsumidorId, x.ApiId })
            .IsUnique()
            .HasFilter("estado <> 'Finalizada'");

        builder.HasIndex(x => new { x.Estado, x.Fin });
        builder.HasIndex(x => new { x.Estado, x.GraciaHasta });

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
