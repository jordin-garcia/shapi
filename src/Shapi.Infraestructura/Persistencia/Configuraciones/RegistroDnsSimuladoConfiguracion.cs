using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class RegistroDnsSimuladoConfiguracion : IEntityTypeConfiguration<RegistroDnsSimulado>
{
    public void Configure(EntityTypeBuilder<RegistroDnsSimulado> builder)
    {
        builder.ToTable("registro_dns_simulado");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();

        builder.Property(x => x.Tipo).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_registro_dns_tipo", "tipo IN ('CNAME')"));

        builder.Property(x => x.Valor).IsRequired();
    }
}
