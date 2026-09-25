using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Planes;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class PlanPlataformaConfiguracion : IEntityTypeConfiguration<PlanPlataforma>
{
    public void Configure(EntityTypeBuilder<PlanPlataforma> builder)
    {
        builder.ToTable("plan_plataforma");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();

        builder.Property(x => x.Descripcion).IsRequired();
        builder.Property(x => x.Precio).HasColumnType("numeric(12,2)");
        builder.ToTable(t => t.HasCheckConstraint("CK_plan_plataforma_precio", "precio >= 0"));

        builder.ToTable(t => t.HasCheckConstraint("CK_plan_plataforma_vigencia", "vigencia_dias BETWEEN 1 AND 366"));

        builder.ToTable(t => t.HasCheckConstraint("CK_plan_plataforma_cuota", "cuota_peticiones > 0"));

        builder.Property(x => x.EsPrueba).HasDefaultValue(false);

        // RNF-08: Un solo plan es_prueba
        builder.HasIndex(x => x.EsPrueba).IsUnique().HasFilter("es_prueba = true");

        builder.Property(x => x.Activo).HasDefaultValue(true);

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
