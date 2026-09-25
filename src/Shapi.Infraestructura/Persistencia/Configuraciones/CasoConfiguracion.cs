using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Soporte;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class CasoConfiguracion : IEntityTypeConfiguration<Caso>
{
    public void Configure(EntityTypeBuilder<Caso> builder)
    {
        builder.ToTable("caso");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Numero)
            .ValueGeneratedOnAdd()
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: 100);

        builder.HasIndex(x => x.Numero).IsUnique();

        builder.Property(x => x.Asunto).IsRequired().HasMaxLength(120);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_caso_estado", "estado IN ('Abierto','Cerrado')"));

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
