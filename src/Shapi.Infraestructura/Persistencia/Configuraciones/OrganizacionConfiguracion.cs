using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class OrganizacionConfiguracion : IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> builder)
    {
        builder.ToTable("organizacion");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(120);

        builder.Property(x => x.Tipo)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_organizacion_tipo", "tipo IN ('Plataforma','Proveedor')"));

        builder.Property(x => x.EstadoAdmin)
            .IsRequired()
            .HasDefaultValue(EstadoAdmin.Activa)
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_organizacion_estado_admin", "estado_admin IN ('Activa','Suspendida')"));

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();

        builder.HasIndex(x => x.Tipo)
            .IsUnique()
            .HasFilter("tipo = 'Plataforma'");
    }
}
