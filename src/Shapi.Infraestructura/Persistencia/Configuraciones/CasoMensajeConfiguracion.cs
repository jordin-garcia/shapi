using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Soporte;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class CasoMensajeConfiguracion : IEntityTypeConfiguration<CasoMensaje>
{
    public void Configure(EntityTypeBuilder<CasoMensaje> builder)
    {
        builder.ToTable("caso_mensaje");
        builder.HasKey(x => x.Id);

        builder.HasOne<Caso>()
            .WithMany()
            .HasForeignKey(x => x.CasoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.AutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Cuerpo).IsRequired();
        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
    }
}
