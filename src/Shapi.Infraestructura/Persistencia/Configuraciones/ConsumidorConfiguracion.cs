using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ConsumidorConfiguracion : IEntityTypeConfiguration<Consumidor>
{
    public void Configure(EntityTypeBuilder<Consumidor> builder)
    {
        builder.ToTable("consumidor");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired();
        builder.Property(x => x.NombreEmpresa).IsRequired();
        builder.Property(x => x.Correo).IsRequired();
        builder.Property(x => x.HashContrasena).IsRequired();

        builder.HasIndex(x => new { x.OrganizacionId, x.Correo }).IsUnique();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasDefaultValue(EstadoCuenta.Activo)
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_consumidor_estado", "estado IN ('Activo','Desactivado')"));
        builder.Property(x => x.IntentosFallidos).HasDefaultValue(0);
    }
}
