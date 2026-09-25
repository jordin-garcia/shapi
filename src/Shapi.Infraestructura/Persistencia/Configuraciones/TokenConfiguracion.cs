using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Infraestructura.Persistencia.Configuraciones;

public class TokenConfiguracion : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.ToTable("token");
        builder.HasKey(x => x.Id);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Consumidor>()
            .WithMany()
            .HasForeignKey(x => x.ConsumidorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organizacion>()
            .WithMany()
            .HasForeignKey(x => x.OrganizacionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Tipo)
            .IsRequired()
<<<<<<< HEAD
            .HasConversion(Conversores.TipoToken);

        builder.ToTable(t => t.HasCheckConstraint("CK_token_tipo",
            "tipo IN ('verificacion_correo','recuperacion','invitacion_miembro','invitacion_consumidor','definir_contrasena')"));
=======
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_token_tipo", "tipo IN ('VerificacionCorreo','Recuperacion','InvitacionMiembro','InvitacionConsumidor','DefinirContrasena')"));
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)

        builder.Property(x => x.HashToken).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.HashToken).IsUnique();

        builder.Property(x => x.Correo).IsRequired();
        builder.Property(x => x.ExpiraEn).IsRequired();
    }
}
