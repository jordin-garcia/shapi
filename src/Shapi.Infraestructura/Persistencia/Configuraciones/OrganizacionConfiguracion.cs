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
            .HasConversion(Conversores.TipoOrganizacion);

        builder.ToTable(t => t.HasCheckConstraint("CK_organizacion_tipo",
            "tipo IN ('plataforma','proveedor')"));

        builder.Property(x => x.EstadoAdmin)
            .IsRequired()
            .HasConversion(Conversores.EstadoAdmin);

        builder.ToTable(t => t.HasCheckConstraint("CK_organizacion_estado_admin",
            "estado_admin IN ('activa','suspendida')"));

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();

        // Una sola organización de tipo plataforma
        builder.HasIndex(x => x.Tipo)
            .IsUnique()
            .HasFilter("tipo = 'plataforma'");
    }
}
