using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ApiConfiguracion : IEntityTypeConfiguration<Api>
{
    public void Configure(EntityTypeBuilder<Api> builder)
    {
        builder.ToTable("api");
        builder.HasKey(x => x.Id);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(x => x.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Nombre).IsRequired();
        builder.Property(x => x.Subdominio).IsRequired();
        builder.HasIndex(x => x.Subdominio).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_api_subdominio",
            "subdominio ~ '^[a-z0-9][a-z0-9-]{1,28}[a-z0-9]$'"));

        builder.Property(x => x.UrlOrigen).IsRequired();

        builder.Property(x => x.Estado)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.EstadoApi);
=======
            .HasDefaultValue(EstadoApi.Borrador)
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_api_estado", "estado IN ('Borrador','Publicada','Despublicada')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.ToTable(t => t.HasCheckConstraint("CK_api_estado",
            "estado IN ('borrador','publicada','despublicada')"));

        builder.Property(x => x.EspecificacionFormato)
            .HasConversion(Conversores.EspecificacionFormato);

        builder.ToTable(t => t.HasCheckConstraint("CK_api_especificacion_formato",
            "especificacion_formato IS NULL OR especificacion_formato IN ('json','yaml')"));

        builder.Property(x => x.PortalColor).IsRequired().HasMaxLength(7).IsFixedLength().HasDefaultValue("#3B6FF0");
        builder.ToTable(t => t.HasCheckConstraint("CK_api_portal_color",
            "portal_color ~ '^#[0-9A-Fa-f]{6}$'"));

        builder.Property(x => x.PortalLogoTipo)
            .HasConversion(Conversores.LogoTipo);

        builder.ToTable(t => t.HasCheckConstraint("CK_api_portal_logo_tipo",
            "portal_logo_tipo IS NULL OR portal_logo_tipo IN ('image/png','image/svg+xml')"));

        builder.Property(x => x.PortalBienvenida).HasMaxLength(280);
        builder.Property(x => x.SecretoOrigenCifrado).IsRequired();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
