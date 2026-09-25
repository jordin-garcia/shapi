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

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Consumidor>()
            .WithMany()
            .HasForeignKey(x => x.ConsumidorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Ambito)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.AmbitoSesion);

        builder.ToTable(t => t.HasCheckConstraint("CK_sesion_ambito",
            "ambito IN ('personal','consumidor')"));

        builder.ToTable(t => t.HasCheckConstraint("CK_sesion_actor",
            "num_nonnulls(usuario_id, consumidor_id) = 1"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_sesion_ambito", "ambito IN ('Personal','Consumidor')"));
        builder.ToTable(t => t.HasCheckConstraint("CK_sesion_usuario_consumidor", "num_nonnulls(usuario_id, consumidor_id) = 1"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.Property(x => x.HashIdentificador).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.HashIdentificador).IsUnique();

        builder.Property(x => x.Host).IsRequired();

        builder.Property(x => x.CreadaEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.UltimoUsoEn).IsRequired();
        builder.Property(x => x.ExpiraEn).IsRequired();
    }
}
