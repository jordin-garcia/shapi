using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class SesionConfiguracion : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> builder)
    {
        builder.ToTable("sesion");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Ambito)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_sesion_ambito", "ambito IN ('Personal','Consumidor')"));
        builder.ToTable(t => t.HasCheckConstraint("CK_sesion_usuario_consumidor", "num_nonnulls(usuario_id, consumidor_id) = 1"));

        builder.Property(x => x.HashIdentificador).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.HashIdentificador).IsUnique();

        builder.Property(x => x.Host).IsRequired();
        builder.Property(x => x.CreadaEn).IsRequired();
        builder.Property(x => x.UltimoUsoEn).IsRequired();
        builder.Property(x => x.ExpiraEn).IsRequired();
    }
}
