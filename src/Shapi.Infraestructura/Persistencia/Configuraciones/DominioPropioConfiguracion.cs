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

        builder.HasOne<Api>()
            .WithMany()
            .HasForeignKey(x => x.ApiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApiId).IsUnique();
        builder.Property(x => x.Dominio).IsRequired();
        builder.HasIndex(x => x.Dominio).IsUnique();

        builder.Property(x => x.DestinoCname).IsRequired();

        builder.Property(x => x.Estado)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.EstadoDominio);

        builder.ToTable(t => t.HasCheckConstraint("CK_dominio_propio_estado",
            "estado IN ('pendiente','verificado','fallido')"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_dominio_propio_estado", "estado IN ('Pendiente','Verificado','Fallido')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
