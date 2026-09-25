using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class TokenConfiguracion : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.ToTable("token");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo)
            .IsRequired()
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_token_tipo", "tipo IN ('VerificacionCorreo','Recuperacion','InvitacionMiembro','InvitacionConsumidor','DefinirContrasena')"));

        builder.Property(x => x.HashToken).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.HashToken).IsUnique();

        builder.Property(x => x.Correo).IsRequired();
        builder.Property(x => x.ExpiraEn).IsRequired();
    }
}
