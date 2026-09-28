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

        // 07 §3.6: la secuencia empieza en 100 y avanza de 1 en 1 (CAS-100, CAS-101…)
        builder.Property(x => x.Numero)
            .HasDefaultValueSql($"nextval('{ShapiDbContext.SecuenciaNumeroCaso}')");
        builder.HasIndex(x => x.Numero).IsUnique();

        builder.Property(x => x.Asunto).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("ck_caso_asunto", "char_length(asunto) <= 120"));

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion(Conversores.EstadoCaso);

        builder.ToTable(t => t.HasCheckConstraint("ck_caso_estado",
            "estado IN ('abierto','cerrado')"));

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
