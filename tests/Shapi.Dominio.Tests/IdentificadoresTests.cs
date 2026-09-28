using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;

namespace Shapi.Dominio.Tests;

// 07 §3: las llaves primarias son UUID v7, generados en la aplicación.
public class IdentificadoresTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 26, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructores_CrearEntidades_GeneranUuidV7()
    {
        var organizacion = new Organizacion("Envíos Xelajú", TipoOrganizacion.Proveedor);
        var usuario = new Usuario("Ana", "ana@ejemplo.com");
        var planPrueba = (PlanPlataforma)Activator.CreateInstance(typeof(PlanPlataforma), nonPublic: true)!;
        typeof(PlanPlataforma).GetProperty(nameof(PlanPlataforma.EsPrueba))!.SetValue(planPrueba, true);
        typeof(PlanPlataforma).GetProperty(nameof(PlanPlataforma.VigenciaDias))!.SetValue(planPrueba, 30);

        Guid[] identificadores =
        [
            organizacion.Id,
            usuario.Id,
            new Membresia(usuario.Id, organizacion.Id, Rol.Propietario).Id,
            Token.VerificacionCorreo(new string('a', 64), usuario, Ahora).Id,
            Sesion.IniciarPersonal(new string('b', 64), usuario.Id, "shapi.localhost", null, null, Ahora).Id,
            new CorreoSaliente("verificacion_correo", "ana@ejemplo.com", "{}", "Verifique su correo", Ahora).Id,
            SuscripcionPlataforma.IniciarPrueba(organizacion.Id, planPrueba, Ahora).Id,
        ];

        identificadores.Should().AllSatisfy(id => id.Version.Should().Be(7));
    }
}
