using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Pagos;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class PagoConfiguracion : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("pago");
        builder.HasKey(x => x.Id);

        builder.ToTable(t => t.HasCheckConstraint("CK_pago_suscripcion", "num_nonnulls(suscripcion_plataforma_id, suscripcion_api_id) = 1"));

        builder.Property(x => x.Concepto)
            .IsRequired()
            .HasConversion<string>();
            
        builder.ToTable(t => t.HasCheckConstraint("CK_pago_concepto", "concepto IN ('Contratacion','Renovacion','CambioPlan','Reactivacion')"));

        builder.Property(x => x.Descripcion).IsRequired();
        
        builder.Property(x => x.Monto).HasColumnType("numeric(12,2)");
        builder.ToTable(t => t.HasCheckConstraint("CK_pago_monto", "monto > 0"));

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<string>();
            
        builder.ToTable(t => t.HasCheckConstraint("CK_pago_estado", "estado IN ('Autorizado','Rechazado','Revertido')"));

        builder.HasIndex(x => new { x.SuscripcionPlataformaId, x.CreadoEn }).IsDescending(false, true);
        builder.HasIndex(x => new { x.SuscripcionApiId, x.CreadoEn }).IsDescending(false, true);
        builder.HasIndex(x => x.CreadoEn).IsDescending();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
