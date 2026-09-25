using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class MedioPagoConfiguracion : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> builder)
    {
        builder.ToTable("medio_pago");
        builder.HasKey(x => x.Id);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(x => x.OrganizacionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Consumidor>()
            .WithMany()
            .HasForeignKey(x => x.ConsumidorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint("CK_medio_pago_org_cons",
            "num_nonnulls(organizacion_id, consumidor_id) = 1"));

        builder.Property(x => x.TokenPasarela).IsRequired();

        builder.Property(x => x.Marca)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.MarcaTarjeta);

        builder.ToTable(t => t.HasCheckConstraint("CK_medio_pago_marca",
            "marca IN ('Visa','Mastercard','American Express')"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_medio_pago_marca", "marca IN ('Visa','Mastercard','AmericanExpress')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.Property(x => x.Ultimos4).IsRequired().HasMaxLength(4).IsFixedLength();
        builder.Property(x => x.Titular).IsRequired();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
    }
}
