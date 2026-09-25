using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class MembresiaConfiguracion : IEntityTypeConfiguration<Membresia>
{
    public void Configure(EntityTypeBuilder<Membresia> builder)
    {
        builder.ToTable("membresia");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UsuarioId).IsUnique();

        builder.Property(x => x.Rol)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_membresia_rol", "rol IN ('Administrador','Soporte','Propietario','Editor','Lector')"));

        builder.HasIndex(x => x.OrganizacionId)
            .IsUnique()
            .HasFilter("rol = 'Propietario'");
    }
}
