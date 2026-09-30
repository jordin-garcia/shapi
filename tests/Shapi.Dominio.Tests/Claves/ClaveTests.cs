using System.Security.Cryptography;
using System.Text;
using Shapi.Dominio.Claves;

namespace Shapi.Dominio.Tests.Claves;

public class ClaveTests
{
    private const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private static readonly DateTimeOffset Ahora = new(2026, 9, 18, 16, 42, 0, TimeSpan.Zero);

    // ---------- Formato, entropía y hash (08 §2, ADR-01) ----------

    [Theory]
    [InlineData(TipoClave.Produccion, "shp_prod_")]
    [InlineData(TipoClave.Pruebas, "shp_prueba_")]
    public void RF_26_Generar_PrefijoDelTipoYVeintiseisCaracteresBase62(TipoClave tipo, string prefijo)
    {
        // RF-26: {prefijo}{26 caracteres base62}.
        var generada = GeneradorClave.Generar(tipo);

        generada.EnClaro.Should().StartWith(prefijo);
        generada.EnClaro.Should().HaveLength(prefijo.Length + 26);
        generada.EnClaro[prefijo.Length..].ToCharArray().Should().OnlyContain(c => Base62.Contains(c));
        generada.Prefijo.Should().Be(prefijo);
    }

    [Fact]
    public void RNF_07_Generar_GuardaSoloPrefijoUltimosCuatroYSha256EnHexMinusculas()
    {
        // RNF-07: en la base de datos solo van el prefijo, los últimos 4 y el SHA-256 de la clave completa.
        var generada = GeneradorClave.Generar(TipoClave.Produccion);

        var esperado = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(generada.EnClaro)));
        generada.HashSha256.Should().Be(esperado).And.HaveLength(64);
        generada.Ultimos4.Should().Be(generada.EnClaro[^4..]);
        GeneradorClave.CalcularHash(generada.EnClaro).Should().Be(esperado);
    }

    [Fact]
    public void RF_26_Generar_NoRepiteClavesYUsaTodoElAlfabeto()
    {
        // RF-26: 26 caracteres de 62 posibles (unos 154 bits) sacados de RandomNumberGenerator.
        var claves = Enumerable.Range(0, 2000).Select(_ => GeneradorClave.Generar(TipoClave.Pruebas).EnClaro).ToList();

        claves.Should().OnlyHaveUniqueItems();
        var caracteres = claves.SelectMany(c => c["shp_prueba_".Length..]).ToHashSet();
        caracteres.Should().BeEquivalentTo(Base62.ToHashSet(), "con 52 000 caracteres al azar aparece todo el alfabeto");
    }

    [Fact]
    public void RF_26_Emitir_CreaUnaClaveActivaConLosDatosDeLaGenerada()
    {
        var suscripcionId = Guid.NewGuid();

        var (clave, enClaro) = Clave.Emitir(suscripcionId, TipoClave.Produccion);

        clave.Id.Should().NotBeEmpty();
        clave.SuscripcionId.Should().Be(suscripcionId);
        clave.Tipo.Should().Be(TipoClave.Produccion);
        clave.Estado.Should().Be(EstadoClave.Activa);
        clave.HashSha256.Should().Be(GeneradorClave.CalcularHash(enClaro));
        clave.Ultimos4.Should().Be(enClaro[^4..]);
        clave.ExpiraEn.Should().BeNull();
        clave.RevocadaEn.Should().BeNull();
    }

    [Fact]
    public void RF_26_Enmascarada_MuestraElPrefijoYLosUltimosCuatro()
    {
        // RF-26: después de mostrarla, solo se ve enmascarada (shp_prod_••••7c2e).
        var (clave, enClaro) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);

        clave.Enmascarada.Should().Be($"shp_prod_••••{enClaro[^4..]}");
    }

    // ---------- Rotación (RF-27) ----------

    [Fact]
    public void RF_27_Rotar_LaAnteriorQuedaRotadaVeinticuatroHorasYLaNuevaActiva()
    {
        var (anterior, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Pruebas);

        var (nueva, enClaro) = anterior.Rotar(Ahora);

        anterior.Estado.Should().Be(EstadoClave.Rotada);
        anterior.ExpiraEn.Should().Be(Ahora.AddHours(24));
        nueva.Estado.Should().Be(EstadoClave.Activa);
        nueva.Tipo.Should().Be(TipoClave.Pruebas);
        nueva.SuscripcionId.Should().Be(anterior.SuscripcionId);
        nueva.Id.Should().NotBe(anterior.Id);
        nueva.HashSha256.Should().Be(GeneradorClave.CalcularHash(enClaro)).And.NotBe(anterior.HashSha256);
    }

    [Fact]
    public void RF_27_Rotar_ClaveRotadaORevocada_NoSePuede()
    {
        // RF-27: una clave ya rotada no se puede volver a rotar.
        var (rotada, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);
        rotada.Rotar(Ahora);
        var (revocada, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);
        revocada.Revocar(RevocadaPor.Consumidor, Ahora);

        rotada.PuedeRotarse.Should().BeFalse();
        revocada.PuedeRotarse.Should().BeFalse();
        rotada.Invoking(c => c.Rotar(Ahora)).Should().Throw<InvalidOperationException>();
        revocada.Invoking(c => c.Rotar(Ahora)).Should().Throw<InvalidOperationException>();
    }

    // ---------- Revocación (RF-28) ----------

    [Theory]
    [InlineData(RevocadaPor.Consumidor)]
    [InlineData(RevocadaPor.Proveedor)]
    public void RF_28_Revocar_ClaveActiva_GuardaQuienYCuando(RevocadaPor por)
    {
        var (clave, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);

        clave.Revocar(por, Ahora).Should().BeTrue();

        clave.Estado.Should().Be(EstadoClave.Revocada);
        clave.RevocadaPor.Should().Be(por);
        clave.RevocadaEn.Should().Be(Ahora);
    }

    [Fact]
    public void RF_28_Revocar_ClaveRotada_TambienSeRevoca()
    {
        // 07 §5: rotada → revocada.
        var (clave, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);
        clave.Rotar(Ahora);

        clave.Revocar(RevocadaPor.Proveedor, Ahora.AddHours(1)).Should().BeTrue();

        clave.Estado.Should().Be(EstadoClave.Revocada);
        clave.RevocadaEn.Should().Be(Ahora.AddHours(1));
    }

    [Fact]
    public void RF_28_Revocar_ClaveYaRevocada_NoCambiaNada()
    {
        var (clave, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);
        clave.Revocar(RevocadaPor.Consumidor, Ahora);

        clave.Revocar(RevocadaPor.Proveedor, Ahora.AddHours(1)).Should().BeFalse();

        clave.RevocadaPor.Should().Be(RevocadaPor.Consumidor);
        clave.RevocadaEn.Should().Be(Ahora);
    }

    [Fact]
    public void RF_27_EsVigente_ActivaYRotadaDentroDeLas24Horas()
    {
        var (activa, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);
        var (rotada, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);
        rotada.Rotar(Ahora);
        var (revocada, _) = Clave.Emitir(Guid.NewGuid(), TipoClave.Produccion);
        revocada.Revocar(RevocadaPor.Consumidor, Ahora);

        activa.EsVigente(Ahora).Should().BeTrue();
        rotada.EsVigente(Ahora.AddHours(23)).Should().BeTrue();
        rotada.EsVigente(Ahora.AddHours(24)).Should().BeFalse();
        revocada.EsVigente(Ahora).Should().BeFalse();
    }
}
