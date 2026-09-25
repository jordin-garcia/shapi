using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ApiConfiguracion : IEntityTypeConfiguration<Api>
{
    public void Configure(EntityTypeBuilder<Api> builder)
    {
        builder.ToTable("api");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired();
        builder.Property(x => x.Subdominio).IsRequired();
        builder.HasIndex(x => x.Subdominio).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_api_subdominio", "subdominio ~ '^[a-z0-9][a-z0-9-]{1,28}[a-z0-9]$'"));

        builder.Property(x => x.UrlOrigen).IsRequired();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasDefaultValue(EstadoApi.Borrador)
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_api_estado", "estado IN ('Borrador','Publicada','Despublicada')"));

        builder.Property(x => x.EspecificacionFormato).HasConversion<string>();
        builder.ToTable(t => t.HasCheckConstraint("CK_api_especificacion_formato", "especificacion_formato IS NULL OR especificacion_formato IN ('Json','Yaml')"));

        builder.Property(x => x.PortalColor).IsRequired().HasMaxLength(7).IsFixedLength().HasDefaultValue("#3B6FF0");
        builder.ToTable(t => t.HasCheckConstraint("CK_api_portal_color", "portal_color ~ '^#[0-9A-Fa-f]{6}$'"));

        builder.Property(x => x.PortalLogoTipo).HasConversion<string>();
        builder.ToTable(t => t.HasCheckConstraint("CK_api_portal_logo_tipo", "portal_logo_tipo IS NULL OR portal_logo_tipo IN ('Png','Svg')"));

        builder.Property(x => x.SecretoOrigenCifrado).IsRequired();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
