using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class MembresiaConfiguracion : IEntityTypeConfiguration<Membresia>
{
    public void Configure(EntityTypeBuilder<Membresia> builder)
    {
        builder.ToTable("membresia");
        builder.HasKey(x => x.Id);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(x => x.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UsuarioId).IsUnique();

        builder.Property(x => x.Rol)
            .IsRequired()
            .HasConversion(Conversores.Rol);

        builder.ToTable(t => t.HasCheckConstraint("CK_membresia_rol",
            "rol IN ('administrador','soporte','propietario','editor','lector')"));

        // RNF-08: Un solo propietario por organización
        builder.HasIndex(x => x.OrganizacionId)
            .IsUnique()
            .HasFilter("rol = 'propietario'");
    }
}
