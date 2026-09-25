using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class SuscripcionApiConfiguracion : IEntityTypeConfiguration<SuscripcionApi>
{
    public void Configure(EntityTypeBuilder<SuscripcionApi> builder)
    {
        builder.ToTable("suscripcion_api");
        builder.HasKey(x => x.Id);

        builder.HasOne<Consumidor>()
            .WithMany()
            .HasForeignKey(x => x.ConsumidorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Api>()
            .WithMany()
            .HasForeignKey(x => x.ApiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PlanApi>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MedioPago>()
            .WithMany()
            .HasForeignKey(x => x.MedioPagoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion(Conversores.EstadoSuscripcion);

        builder.ToTable(t => t.HasCheckConstraint("CK_suscripcion_api_estado",
            "estado IN ('activa','en_gracia','suspendida','finalizada')"));
        builder.ToTable(t => t.HasCheckConstraint("CK_suscripcion_api_fechas", "fin > inicio"));

        // RNF-08: Una sola suscripción vigente por consumidor en cada API
        builder.HasIndex(x => new { x.ConsumidorId, x.ApiId })
            .IsUnique()
            .HasFilter("estado <> 'finalizada'");

        builder.HasIndex(x => new { x.Estado, x.Fin });
        builder.HasIndex(x => new { x.Estado, x.GraciaHasta });

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
