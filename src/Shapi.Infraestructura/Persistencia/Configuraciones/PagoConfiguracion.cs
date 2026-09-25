using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class PagoConfiguracion : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("pago");
        builder.HasKey(x => x.Id);

        builder.HasOne<SuscripcionPlataforma>()
            .WithMany()
            .HasForeignKey(x => x.SuscripcionPlataformaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SuscripcionApi>()
            .WithMany()
            .HasForeignKey(x => x.SuscripcionApiId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MedioPago>()
            .WithMany()
            .HasForeignKey(x => x.MedioPagoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.RevertidoPor)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint("CK_pago_suscripcion",
            "num_nonnulls(suscripcion_plataforma_id, suscripcion_api_id) = 1"));

        builder.Property(x => x.Concepto)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.ConceptoPago);

        builder.ToTable(t => t.HasCheckConstraint("CK_pago_concepto",
            "concepto IN ('contratacion','renovacion','cambio_plan','reactivacion')"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_pago_concepto", "concepto IN ('Contratacion','Renovacion','CambioPlan','Reactivacion')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.Property(x => x.Descripcion).IsRequired();

        builder.Property(x => x.Monto).HasColumnType("numeric(12,2)");
        builder.ToTable(t => t.HasCheckConstraint("CK_pago_monto", "monto > 0"));

        builder.Property(x => x.Estado)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.EstadoPago);

        builder.ToTable(t => t.HasCheckConstraint("CK_pago_estado",
            "estado IN ('autorizado','rechazado','revertido')"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_pago_estado", "estado IN ('Autorizado','Rechazado','Revertido')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.HasIndex(x => new { x.SuscripcionPlataformaId, x.CreadoEn }).IsDescending(false, true);
        builder.HasIndex(x => new { x.SuscripcionApiId, x.CreadoEn }).IsDescending(false, true);
        builder.HasIndex(x => x.CreadoEn).IsDescending();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
