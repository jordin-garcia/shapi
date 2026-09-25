using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Pagos;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class MedioPagoConfiguracion : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> builder)
    {
        builder.ToTable("medio_pago");
        builder.HasKey(x => x.Id);

        builder.ToTable(t => t.HasCheckConstraint("CK_medio_pago_org_cons", "num_nonnulls(organizacion_id, consumidor_id) = 1"));

        builder.Property(x => x.TokenPasarela).IsRequired();

        builder.Property(x => x.Marca)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_medio_pago_marca", "marca IN ('Visa','Mastercard','AmericanExpress')"));

        builder.Property(x => x.Ultimos4).IsRequired().HasMaxLength(4).IsFixedLength();
        builder.Property(x => x.Titular).IsRequired();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
    }
}
