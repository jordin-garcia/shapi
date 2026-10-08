using Shapi.Dominio.Soporte;

namespace Shapi.Dominio.Tests.Soporte;

public class CasoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 7, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RF_40_CasoNuevo_QuedaAbiertoConPrimerMensaje()
    {
        var organizacionId = Guid.NewGuid();
        var autorId = Guid.NewGuid();

        var caso = Caso.Abrir(organizacionId, null, autorId, "No puedo publicar", "La API no publica.", Ahora);

        Assert.Equal(EstadoCaso.Abierto, caso.Estado);
        Assert.Equal(organizacionId, caso.OrganizacionId);
        Assert.Equal("No puedo publicar", caso.Asunto);
        Assert.Single(caso.ObtenerMensajes());
        Assert.Equal("La API no publica.", caso.ObtenerMensajes().Single().Cuerpo);
        Assert.Equal(autorId, caso.ObtenerMensajes().Single().AutorId);
    }

    [Fact]
    public void RF_40_CasoCerrado_NoAdmiteMasMensajes()
    {
        var caso = Caso.Abrir(Guid.NewGuid(), null, Guid.NewGuid(), "Asunto", "Descripcion", Ahora);
        caso.Cerrar(Ahora.AddMinutes(1));

        var excepcion = Assert.Throws<InvalidOperationException>(() =>
            caso.Responder(Guid.NewGuid(), "Otro mensaje", Ahora.AddMinutes(2)));

        Assert.Equal("caso_cerrado", excepcion.Message);
        Assert.Single(caso.ObtenerMensajes());
    }

    [Fact]
    public void RF_40_AsignarYCerrar_ActualizaElCaso()
    {
        var soporteId = Guid.NewGuid();
        var caso = Caso.Abrir(Guid.NewGuid(), null, Guid.NewGuid(), "Asunto", "Descripcion", Ahora);

        caso.Asignar(soporteId, Ahora.AddMinutes(1));
        caso.Cerrar(Ahora.AddMinutes(2));

        Assert.Equal(soporteId, caso.AsignadoA);
        Assert.Equal(EstadoCaso.Cerrado, caso.Estado);
        Assert.Equal(Ahora.AddMinutes(2), caso.CerradoEn);
        Assert.Equal(Ahora.AddMinutes(2), caso.ActualizadoEn);
    }
}
