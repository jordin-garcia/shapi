using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class DominioPropioConfiguracion : IEntityTypeConfiguration<DominioPropio>
{
    public void Configure(EntityTypeBuilder<DominioPropio> builder)
    {
        builder.ToTable("dominio_propio");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ApiId).IsUnique();
        builder.Property(x => x.Dominio).IsRequired();
        builder.HasIndex(x => x.Dominio).IsUnique();

        builder.Property(x => x.DestinoCname).IsRequired();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_dominio_propio_estado", "estado IN ('Pendiente','Verificado','Fallido')"));

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
