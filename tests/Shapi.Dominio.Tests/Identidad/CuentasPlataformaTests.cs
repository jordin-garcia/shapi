using Shapi.Dominio.Identidad;

namespace Shapi.Dominio.Tests.Identidad;

public sealed class CuentasPlataformaTests
{
    [Fact]
    public void RF_42_Cuenta_SePuedeDesactivarYActivar()
    {
        var usuario = new Usuario("Lucía", "lucia@shapi.test");
        usuario.Desactivar();
        Assert.Equal(EstadoCuenta.Desactivado, usuario.Estado);
        usuario.Activar();
        Assert.Equal(EstadoCuenta.Activo, usuario.Estado);
    }

    [Fact]
    public void RF_42_TokenDefinirContrasena_VenceExactamenteALosSieteDias()
    {
        var ahora = DateTimeOffset.Parse("2026-10-12T18:00:00Z");
        var usuario = new Usuario("Lucía", "lucia@shapi.test");
        var token = Token.DefinirContrasena("hash", usuario, ahora);
        Assert.Equal(TipoToken.DefinirContrasena, token.Tipo);
        Assert.Equal(usuario.Id, token.UsuarioId);
        Assert.Equal(usuario.Correo, token.Correo);
        Assert.Equal(ahora.AddDays(7), token.ExpiraEn);
        Assert.True(token.EsValido(ahora.AddDays(7).AddTicks(-1)));
        Assert.False(token.EsValido(ahora.AddDays(7)));
    }
}
