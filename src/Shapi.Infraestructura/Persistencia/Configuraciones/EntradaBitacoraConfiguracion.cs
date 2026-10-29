using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Bitacora;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class EntradaBitacoraConfiguracion : IEntityTypeConfiguration<EntradaBitacora>
{
    public void Configure(EntityTypeBuilder<EntradaBitacora> builder)
    {
        builder.ToTable("bitacora");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).UseIdentityByDefaultColumn();

        // RF-41: referencia histórica, también después de borrar organizaciones de ensayos (JG-27).
        builder.HasIndex(x => x.OrganizacionId);

        builder.Property(x => x.Fecha).IsRequired().HasDefaultValueSql("now()");

        builder.Property(x => x.ActorTipo)
            .IsRequired()
            .HasConversion(Conversores.ActorTipo);

        builder.ToTable(t => t.HasCheckConstraint("ck_bitacora_actor_tipo",
            "actor_tipo IN ('usuario','consumidor','sistema')"));

        builder.Property(x => x.ActorNombre).IsRequired();
        builder.Property(x => x.Accion).IsRequired();
        builder.Property(x => x.Descripcion).IsRequired();

        builder.Property(x => x.Detalle).HasColumnType("jsonb");
    }
}
