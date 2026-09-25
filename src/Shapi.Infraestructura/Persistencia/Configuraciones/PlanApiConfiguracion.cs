using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Planes;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class PlanApiConfiguracion : IEntityTypeConfiguration<PlanApi>
{
    public void Configure(EntityTypeBuilder<PlanApi> builder)
    {
        builder.ToTable("plan_api");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired();
        builder.HasIndex(x => new { x.ApiId, x.Nombre }).IsUnique();

        builder.Property(x => x.Descripcion).IsRequired();
        builder.Property(x => x.Precio).HasColumnType("numeric(12,2)");
        builder.ToTable(t => t.HasCheckConstraint("CK_plan_api_precio", "precio >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_plan_api_gratuito", "NOT es_gratuito OR precio = 0"));

        builder.ToTable(t => t.HasCheckConstraint("CK_plan_api_vigencia", "vigencia_dias BETWEEN 1 AND 366"));
        builder.ToTable(t => t.HasCheckConstraint("CK_plan_api_cuota", "cuota_llamadas > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_plan_api_limite", "limite_minuto > 0"));

        builder.Property(x => x.Activo).HasDefaultValue(true);

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
