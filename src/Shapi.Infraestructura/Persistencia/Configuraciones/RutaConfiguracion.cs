using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Apis;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class RutaConfiguracion : IEntityTypeConfiguration<Ruta>
{
    public void Configure(EntityTypeBuilder<Ruta> builder)
    {
        builder.ToTable("ruta");
        builder.HasKey(x => x.Id);

        builder.HasOne<Api>()
            .WithMany()
            .HasForeignKey(x => x.ApiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Metodo)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.MetodoHttp);

        builder.ToTable(t => t.HasCheckConstraint("CK_ruta_metodo", "metodo IN ('GET','POST','PUT','PATCH','DELETE','HEAD','OPTIONS')"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_ruta_metodo", "metodo IN ('Get','Post','Put','Patch','Delete','Head','Options')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.Property(x => x.Patron).IsRequired();

        builder.Property(x => x.Definicion).IsRequired().HasColumnType("jsonb");

        builder.Property(x => x.Expuesta).IsRequired().HasDefaultValue(false);

        builder.ToTable(t => t.HasCheckConstraint("CK_ruta_limite_minuto", "limite_minuto IS NULL OR limite_minuto > 0"));

        builder.Property(x => x.CacheSegundos).IsRequired().HasDefaultValue(0);
        builder.ToTable(t => t.HasCheckConstraint("CK_ruta_cache_segundos", "cache_segundos BETWEEN 0 AND 86400"));

        builder.Property(x => x.PesoLlamadas).IsRequired().HasDefaultValue(1);
        builder.ToTable(t => t.HasCheckConstraint("CK_ruta_peso_llamadas", "peso_llamadas BETWEEN 1 AND 1000"));

        builder.HasIndex(x => new { x.ApiId, x.Metodo, x.Patron }).IsUnique();

        builder.Property(x => x.CreadoEn).IsRequired().HasDefaultValueSql("now()");
        builder.Property(x => x.ActualizadoEn).IsRequired();
    }
}
