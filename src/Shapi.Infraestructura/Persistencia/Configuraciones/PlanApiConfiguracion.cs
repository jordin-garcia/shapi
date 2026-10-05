using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Planes;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class PlanApiConfiguracion : IEntityTypeConfiguration<PlanApi>
{
    public void Configure(EntityTypeBuilder<PlanApi> builder)
    {
        builder.ToTable("plan_api");
        builder.HasKey(x => x.Id);

        builder.HasOne<Api>()
            .WithMany()
            .HasForeignKey(x => x.ApiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Nombre).IsRequired();
        builder.HasIndex(x => new { x.ApiId, x.Nombre }).IsUnique();

        builder.Property(x => x.Descripcion).IsRequired();
        builder.Property(x => x.Precio).HasColumnType("numeric(12,2)");
        builder.ToTable(t => t.HasCheckConstraint("ck_plan_api_precio", "precio >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_plan_api_gratuito", "NOT es_gratuito OR precio = 0"));
        // Un plan de pago con precio 0 fallaría al contratarlo, porque ck_pago_monto exige monto > 0 (auditoría del 3 oct, H-60).
        builder.ToTable(t => t.HasCheckConstraint("ck_plan_api_pago", "es_gratuito OR precio > 0"));

        builder.ToTable(t => t.HasCheckConstraint("ck_plan_api_vigencia", "vigencia_dias BETWEEN 1 AND 366"));
        builder.ToTable(t => t.HasCheckConstraint("ck_plan_api_cuota", "cuota_llamadas > 0"));
        builder.ToTable(t => t.HasCheckConstraint("ck_plan_api_limite", "limite_minuto > 0"));

        // ValueGeneratedNever: sin él, EF toma false como "sin valor" y la base guardaría el DEFAULT true
        builder.Property(x => x.Activo).HasDefaultValue(true).ValueGeneratedNever();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
