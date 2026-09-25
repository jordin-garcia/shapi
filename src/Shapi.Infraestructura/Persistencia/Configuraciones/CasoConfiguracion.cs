using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Soporte;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class CasoConfiguracion : IEntityTypeConfiguration<Caso>
{
    public void Configure(EntityTypeBuilder<Caso> builder)
    {
        builder.ToTable("caso");
        builder.HasKey(x => x.Id);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(x => x.OrganizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Api>()
            .WithMany()
            .HasForeignKey(x => x.ApiId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.CreadoPor)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.AsignadoA)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // RF-03: secuencia de numero empieza en 100
        builder.Property(x => x.Numero)
            .UseHiLo("caso_numero_seq");
        builder.HasIndex(x => x.Numero).IsUnique();

        builder.Property(x => x.Asunto).IsRequired().HasMaxLength(120);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion(Conversores.EstadoCaso);

        builder.ToTable(t => t.HasCheckConstraint("CK_caso_estado",
            "estado IN ('abierto','cerrado')"));

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
