using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class SuscripcionPlataformaConfiguracion : IEntityTypeConfiguration<SuscripcionPlataforma>
{
    public void Configure(EntityTypeBuilder<SuscripcionPlataforma> builder)
    {
        builder.ToTable("suscripcion_plataforma");
        builder.HasKey(x => x.Id);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(x => x.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PlanPlataforma>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Shapi.Dominio.Pagos.MedioPago>()
            .WithMany()
            .HasForeignKey(x => x.MedioPagoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Estado)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.EstadoSuscripcion);

        builder.ToTable(t => t.HasCheckConstraint("CK_suscripcion_plat_estado",
            "estado IN ('activa','en_gracia','suspendida','finalizada')"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_suscripcion_plat_estado", "estado IN ('Activa','EnGracia','Suspendida','Finalizada')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)
        builder.ToTable(t => t.HasCheckConstraint("CK_suscripcion_plat_fechas", "fin > inicio"));

        // RNF-08: Una sola suscripción vigente por organización
        builder.HasIndex(x => x.OrganizacionId)
            .IsUnique()
            .HasFilter("estado <> 'finalizada'");

        builder.HasIndex(x => new { x.Estado, x.Fin });
        builder.HasIndex(x => new { x.Estado, x.GraciaHasta });

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
