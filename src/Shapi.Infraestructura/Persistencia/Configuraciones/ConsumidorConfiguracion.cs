using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ConsumidorConfiguracion : IEntityTypeConfiguration<Consumidor>
{
    public void Configure(EntityTypeBuilder<Consumidor> builder)
    {
        builder.ToTable("consumidor");
        builder.HasKey(x => x.Id);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(x => x.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Nombre).IsRequired();
        builder.Property(x => x.NombreEmpresa).IsRequired();
        builder.Property(x => x.Correo).IsRequired();
        builder.Property(x => x.HashContrasena).IsRequired();
<<<<<<< HEAD

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion(Conversores.EstadoCuenta);

        builder.ToTable(t => t.HasCheckConstraint("CK_consumidor_estado",
            "estado IN ('activo','desactivado')"));

=======

        builder.HasIndex(x => new { x.OrganizacionId, x.Correo }).IsUnique();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasDefaultValue(EstadoCuenta.Activo)
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_consumidor_estado", "estado IN ('Activo','Desactivado')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)
        builder.Property(x => x.IntentosFallidos).HasDefaultValue(0);

        // RNF-08: UNIQUE (organizacion_id, lower(correo)) - el lower se aplica como SQL en la migración
        builder.HasIndex(x => new { x.OrganizacionId, x.Correo }).IsUnique();
    }
}
