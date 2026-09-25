using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class ClaveConfiguracion : IEntityTypeConfiguration<Clave>
{
    public void Configure(EntityTypeBuilder<Clave> builder)
    {
        builder.ToTable("clave");
        builder.HasKey(x => x.Id);

        builder.HasOne<SuscripcionApi>()
            .WithMany()
            .HasForeignKey(x => x.SuscripcionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Tipo)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.TipoClave);
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_clave_tipo", "tipo IN ('Produccion','Pruebas')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.ToTable(t => t.HasCheckConstraint("CK_clave_tipo",
            "tipo IN ('produccion','pruebas')"));

        builder.Property(x => x.Prefijo).IsRequired();
        builder.Property(x => x.Ultimos4).IsRequired().HasMaxLength(4).IsFixedLength();
<<<<<<< HEAD
=======

>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)
        builder.Property(x => x.HashSha256).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.HashSha256).IsUnique();

        builder.Property(x => x.Estado)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.EstadoClave);
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_clave_estado", "estado IN ('Activa','Rotada','Revocada')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.ToTable(t => t.HasCheckConstraint("CK_clave_estado",
            "estado IN ('activa','rotada','revocada')"));

        builder.Property(x => x.RevocadaPor)
            .HasConversion(Conversores.RevocadaPor);

        builder.ToTable(t => t.HasCheckConstraint("CK_clave_revocada_por",
            "revocada_por IS NULL OR revocada_por IN ('consumidor','proveedor')"));

        // RNF-08: Una sola clave activa por tipo por suscripción
        builder.HasIndex(x => new { x.SuscripcionId, x.Tipo })
            .IsUnique()
            .HasFilter("estado = 'activa'");

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
