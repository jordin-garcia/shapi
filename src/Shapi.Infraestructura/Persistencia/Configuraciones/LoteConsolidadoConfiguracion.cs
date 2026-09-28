using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Consumo;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class LoteConsolidadoConfiguracion : IEntityTypeConfiguration<LoteConsolidado>
{
    public void Configure(EntityTypeBuilder<LoteConsolidado> builder)
    {
        builder.ToTable("lote_consolidado");
        builder.HasKey(x => x.LoteId);
        // 07 §3: procesado_en hace de creado_en.
        builder.Property(x => x.ProcesadoEn).IsRequired().HasDefaultValueSql("now()");
    }
}
