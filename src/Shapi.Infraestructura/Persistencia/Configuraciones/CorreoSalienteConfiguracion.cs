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
            .HasConversion(Conversores.EstadoCorreo);

        builder.ToTable(t => t.HasCheckConstraint("ck_correo_saliente_estado",
            "estado IN ('pendiente','enviado','fallido')"));

        // El trabajador busca cada 5 s los pendientes cuyo próximo intento ya venció (10 §6).
        builder.HasIndex(x => new { x.Estado, x.ProximoIntentoEn });

        builder.Property(x => x.Intentos).HasDefaultValue(0);
        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
