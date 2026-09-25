using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Claves;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ClaveConfiguracion : IEntityTypeConfiguration<Clave>
{
    public void Configure(EntityTypeBuilder<Clave> builder)
    {
        builder.ToTable("clave");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_clave_tipo", "tipo IN ('Produccion','Pruebas')"));

        builder.Property(x => x.Ultimos4).IsRequired().HasMaxLength(4).IsFixedLength();

        builder.Property(x => x.HashSha256).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.HashSha256).IsUnique();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_clave_estado", "estado IN ('Activa','Rotada','Revocada')"));

        builder.Property(x => x.RevocadaPor).HasConversion<string>();
        builder.ToTable(t => t.HasCheckConstraint("CK_clave_revocada_por", "revocada_por IS NULL OR revocada_por IN ('Consumidor','Proveedor')"));

        builder.HasIndex(x => new { x.SuscripcionId, x.Tipo })
            .IsUnique()
            .HasFilter("estado = 'Activa'");

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
