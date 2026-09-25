using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Correo;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class CorreoSalienteConfiguracion : IEntityTypeConfiguration<CorreoSaliente>
{
    public void Configure(EntityTypeBuilder<CorreoSaliente> builder)
    {
        builder.ToTable("correo_saliente");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Destinatario).IsRequired();
        builder.Property(x => x.Asunto).IsRequired();
        builder.Property(x => x.Plantilla).IsRequired();
        builder.Property(x => x.Datos).IsRequired().HasColumnType("jsonb");

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_correo_saliente_estado", "estado IN ('Pendiente','Enviado','Fallido')"));
    }
}
