using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired();
        builder.Property(x => x.Correo).IsRequired();

        // RNF-08: UNIQUE lower(correo) - se aplica como SQL en la migración
        // HasIndex aquí registra el índice normal; el lower() va en migración
        builder.HasIndex(x => x.Correo).IsUnique();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion(Conversores.EstadoCuenta);

        builder.ToTable(t => t.HasCheckConstraint("CK_usuario_estado",
            "estado IN ('activo','desactivado')"));

        builder.Property(x => x.IntentosFallidos).HasDefaultValue(0);
    }
}
