using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Infraestructura.Pagos;

namespace Shapi.Api.Tests.Pagos;

// RF-20: pasarela simulada de 09 §2.
public class PasarelaSimuladaTests
{
    private static readonly DateTimeOffset Hoy = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private const string FormatoUuid = "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";

    // --- Tabla de tarjetas de prueba (09 §2) ---

    [Theory]
    [InlineData("4242 4242 4242 4242", "Visa")]
    [InlineData("5555 5555 5555 4444", "Mastercard")]
    [InlineData("4024 0071 2284 4821", "Visa")]
    [InlineData("5412 7534 1203 3057", "Mastercard")]
    public async Task RF_20_TarjetaQueSiempreSeAprueba_CobraAlContratarYAlRenovar(string numero, string marca)
    {
        var pasarela = CrearPasarela();

        var tokenizacion = await pasarela.TokenizarAsync(Tarjeta(numero));
        var alContratar = await pasarela.CobrarAsync(tokenizacion.Token!, 100m, "ref-1", esRenovacion: false);
        var alRenovar = await pasarela.CobrarAsync(tokenizacion.Token!, 100m, "ref-2", esRenovacion: true);

        tokenizacion.Exitoso.Should().BeTrue();
        tokenizacion.Marca.Should().Be(marca);
        tokenizacion.Token.Should().MatchRegex($"^tok_sim_{FormatoUuid}$");
        alContratar.Exitoso.Should().BeTrue();
        alRenovar.Exitoso.Should().BeTrue();
    }

    [Theory]
    [InlineData("4000 0000 0000 0002", "fondos_insuficientes")]
    [InlineData("4000 0000 0000 0069", "tarjeta_vencida")]
    public async Task RF_20_TarjetaQueSiempreSeRechaza_SeTokenizaYElCobroFallaConSuMotivo(string numero, string motivo)
    {
        var pasarela = CrearPasarela();

        var tokenizacion = await pasarela.TokenizarAsync(Tarjeta(numero));
        var alContratar = await pasarela.CobrarAsync(tokenizacion.Token!, 100m, "ref-1", esRenovacion: false);
        var alRenovar = await pasarela.CobrarAsync(tokenizacion.Token!, 100m, "ref-2", esRenovacion: true);

        tokenizacion.Exitoso.Should().BeTrue();
        tokenizacion.Marca.Should().Be("Visa");
        alContratar.Exitoso.Should().BeFalse();
        alContratar.Error.Should().Be(motivo);
        alContratar.Referencia.Should().BeNull();
        alRenovar.Exitoso.Should().BeFalse();
        alRenovar.Error.Should().Be(motivo);
    }

    [Fact]
    public async Task RF_20_Tarjeta0341_SeApruebaAlContratarYSeRechazaEnLasRenovaciones()
    {
        var pasarela = CrearPasarela();

        var tokenizacion = await pasarela.TokenizarAsync(Tarjeta("4000 0000 0000 0341"));
        var alContratar = await pasarela.CobrarAsync(tokenizacion.Token!, 100m, "ref-1", esRenovacion: false);
        var alRenovar = await pasarela.CobrarAsync(tokenizacion.Token!, 100m, "ref-2", esRenovacion: true);

        tokenizacion.Token.Should().MatchRegex($"^tok_sim_0341_{FormatoUuid}$");
        alContratar.Exitoso.Should().BeTrue();
        alRenovar.Exitoso.Should().BeFalse();
        alRenovar.Error.Should().Be("fondos_insuficientes");
    }

    [Theory]
    [InlineData("4000 0000 0000 0002", "0002", "fondos_insuficientes")]
    [InlineData("4000 0000 0000 0069", "0069", "tarjeta_vencida")]
    [InlineData("4000 0000 0000 0341", "0341", "fondos_insuficientes")]
    public async Task RF_20_TarjetaEspecial_ElTokenLlevaSuComportamientoYOtroProcesoLoRespeta(string numero, string sufijo, string motivoAlRenovar)
    {
        var token = (await CrearPasarela().TokenizarAsync(Tarjeta(numero))).Token!;

        // Las renovaciones las cobra el Trabajador, que es otro proceso con otra instancia de la pasarela.
        var alRenovar = await CrearPasarela().CobrarAsync(token, 100m, "ref-1", esRenovacion: true);

        token.Should().MatchRegex($"^tok_sim_{sufijo}_{FormatoUuid}$");
        alRenovar.Exitoso.Should().BeFalse();
        alRenovar.Error.Should().Be(motivoAlRenovar);
    }

    [Theory]
    [InlineData("4000 0000 0018 0002")]
    [InlineData("4000 0000 0018 0069")]
    [InlineData("4000 0000 0018 0341")]
    public async Task RF_20_OtraTarjetaConLosMismosUltimos4_NoEsEspecialYSeAprueba(string numero)
    {
        var pasarela = CrearPasarela();

        var tokenizacion = await pasarela.TokenizarAsync(Tarjeta(numero));
        var alRenovar = await pasarela.CobrarAsync(tokenizacion.Token!, 100m, "ref-1", esRenovacion: true);

        tokenizacion.Token.Should().MatchRegex($"^tok_sim_{FormatoUuid}$");
        alRenovar.Exitoso.Should().BeTrue();
    }

    // --- Número y Luhn ---

    [Theory]
    [InlineData("4242 4242 4242 4241")]   // falla Luhn
    [InlineData("4024 0071 2244 4821")]   // la tarjeta anterior del mockup A2, que no pasa Luhn
    [InlineData("4242 4242 4242 424a")]   // no son dígitos
    [InlineData("4242 4242 4242 424٢")]   // dígito que no es ASCII
    [InlineData("")]
    public async Task RF_20_NumeroQueNoPasaLuhn_EsNumeroInvalido(string numero)
    {
        var resultado = await CrearPasarela().TokenizarAsync(Tarjeta(numero));

        resultado.Exitoso.Should().BeFalse();
        resultado.Error.Should().Be("numero_invalido");
        resultado.Token.Should().BeNull();
    }

    [Theory]
    [InlineData("4000000000006")]          // 13 dígitos
    [InlineData("4000000000000000006")]    // 19 dígitos
    [InlineData("4242-4242-4242-4242")]    // con guiones
    public async Task RF_20_NumeroDe13a19DigitosQuePasaLuhn_SeAcepta(string numero)
    {
        var resultado = await CrearPasarela().TokenizarAsync(Tarjeta(numero));

        resultado.Exitoso.Should().BeTrue();
        resultado.Marca.Should().Be("Visa");
    }

    [Theory]
    [InlineData("400000000002")]           // 12 dígitos, pasa Luhn
    [InlineData("40000000000000000002")]   // 20 dígitos, pasa Luhn
    public async Task RF_20_NumeroFueraDe13a19Digitos_EsNumeroInvalido(string numero)
    {
        var resultado = await CrearPasarela().TokenizarAsync(Tarjeta(numero));

        resultado.Exitoso.Should().BeFalse();
        resultado.Error.Should().Be("numero_invalido");
    }

    // --- Marcas según el BIN ---

    [Theory]
    [InlineData("4242424242424242", "Visa", "123")]
    [InlineData("5555555555554444", "Mastercard", "123")]
    [InlineData("5000000000000009", null, "123")]              // 50: no es Mastercard
    [InlineData("5600000000000003", null, "123")]              // 56: no es Mastercard
    [InlineData("2221000000000009", "Mastercard", "123")]
    [InlineData("2720990000000007", "Mastercard", "123")]
    [InlineData("2220000000000000", null, "123")]              // 2220: fuera del rango
    [InlineData("2721000000000004", null, "123")]              // 2721: fuera del rango
    [InlineData("378282246310005", "American Express", "1234")]
    [InlineData("371449635398431", "American Express", "1234")]
    [InlineData("6011111111111117", null, "123")]              // Discover
    public async Task RF_20_Marca_SeDetectaPorElBin(string numero, string? marca, string cvv)
    {
        var resultado = await CrearPasarela().TokenizarAsync(Tarjeta(numero, cvv: cvv));

        if (marca is null)
        {
            resultado.Exitoso.Should().BeFalse();
            resultado.Error.Should().Be("marca_no_soportada");
        }
        else
        {
            resultado.Exitoso.Should().BeTrue();
            resultado.Marca.Should().Be(marca);
        }
    }

    // --- Vencimiento ---

    [Theory]
    [InlineData("09", "2026", true)]    // el mes actual
    [InlineData("10", "2026", true)]
    [InlineData("01", "2027", true)]
    [InlineData("9", "26", true)]       // año de 2 dígitos
    [InlineData("08", "2026", false)]   // el mes anterior
    [InlineData("12", "2025", false)]
    [InlineData("13", "2027", false)]
    [InlineData("00", "2027", false)]
    [InlineData("ab", "2027", false)]
    public async Task RF_20_Vencimiento_TieneQueSerElMesActualOPosterior(string mes, string anio, bool valido)
    {
        var resultado = await CrearPasarela().TokenizarAsync(Tarjeta("4242424242424242", mes: mes, anio: anio));

        resultado.Exitoso.Should().Be(valido);
        if (!valido)
        {
            resultado.Error.Should().Be("tarjeta_vencida");
        }
    }

    [Theory]
    [InlineData("2026-10-01T03:00:00Z", true)]    // en Guatemala todavía es 30 de septiembre
    [InlineData("2026-10-01T06:00:00Z", false)]   // en Guatemala ya es 1 de octubre
    public async Task RF_20_Vencimiento_ElMesActualEsElDeGuatemala(string ahora, bool valido)
    {
        var pasarela = CrearPasarela(ahora: DateTimeOffset.Parse(ahora, System.Globalization.CultureInfo.InvariantCulture));

        var resultado = await pasarela.TokenizarAsync(Tarjeta("4242424242424242", mes: "09", anio: "2026"));

        resultado.Exitoso.Should().Be(valido);
    }

    // --- CVV ---

    [Theory]
    [InlineData("4242424242424242", "123", true)]
    [InlineData("4242424242424242", "1234", false)]
    [InlineData("4242424242424242", "12", false)]
    [InlineData("4242424242424242", "+12", false)]
    [InlineData("4242424242424242", " 12", false)]
    [InlineData("378282246310005", "1234", true)]
    [InlineData("378282246310005", "123", false)]
    public async Task RF_20_Cvv_Tiene3DigitosO4SiEsAmericanExpress(string numero, string cvv, bool valido)
    {
        var resultado = await CrearPasarela().TokenizarAsync(Tarjeta(numero, cvv: cvv));

        resultado.Exitoso.Should().Be(valido);
        if (!valido)
        {
            resultado.Error.Should().Be("cvv_invalido");
        }
    }

    // --- Resultado de la tokenización y formatos ---

    [Fact]
    public async Task RF_20_Tokenizar_DevuelveLaMarcaLosUltimos4YElTitularSinElNumeroNiElCvv()
    {
        var tarjeta = Tarjeta("4242 4242 4242 4242", cvv: "987");

        var resultado = await CrearPasarela().TokenizarAsync(tarjeta);

        resultado.Marca.Should().Be("Visa");
        resultado.Ultimos4.Should().Be("4242");
        resultado.Titular.Should().Be("Ana López");
        var serializado = JsonSerializer.Serialize(resultado);
        serializado.Should().NotContain("4242424242424242");
        serializado.Should().NotContain("987");
        tarjeta.ToString().Should().NotContain("4242").And.NotContain("987");
    }

    [Fact]
    public async Task RF_20_CobrarYReembolsar_DevuelvenReferenciasChSimYReSim()
    {
        var pasarela = CrearPasarela();
        var token = (await pasarela.TokenizarAsync(Tarjeta("4242424242424242"))).Token!;

        var cobro = await pasarela.CobrarAsync(token, 100m, "ref-1", esRenovacion: false);
        var reembolso = await pasarela.ReembolsarAsync(cobro.Referencia!);

        cobro.Referencia.Should().MatchRegex($"^ch_sim_{FormatoUuid}$");
        reembolso.Exitoso.Should().BeTrue();
        reembolso.Referencia.Should().MatchRegex($"^re_sim_{FormatoUuid}$");
    }

    // --- Falla y demora simuladas ---

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    public async Task RF_20_ConLaPasarelaEnFalla_TodoFallaConPasarelaNoDisponible(string valor)
    {
        var pasarela = CrearPasarela(configuracion: new() { ["SHAPI_PASARELA_FALLA"] = valor });

        var tokenizacion = await pasarela.TokenizarAsync(Tarjeta("4242424242424242"));
        var cobro = await pasarela.CobrarAsync("tok_sim_00000000-0000-0000-0000-000000000000", 100m, "ref-1", esRenovacion: false);
        var reembolso = await pasarela.ReembolsarAsync("ch_sim_00000000-0000-0000-0000-000000000000");

        tokenizacion.Exitoso.Should().BeFalse();
        tokenizacion.Error.Should().Be("pasarela_no_disponible");
        cobro.Exitoso.Should().BeFalse();
        cobro.Error.Should().Be("pasarela_no_disponible");
        reembolso.Exitoso.Should().BeFalse();
        reembolso.Error.Should().Be("pasarela_no_disponible");
    }

    [Fact]
    public async Task RF_20_SinConfigurarLaDemora_TardaEntre300y800Ms()
    {
        var pasarela = CrearPasarela(configuracion: []);

        var cronometro = Stopwatch.StartNew();
        await pasarela.ReembolsarAsync("ch_sim_00000000-0000-0000-0000-000000000000");
        cronometro.Stop();

        cronometro.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(290);
    }

    [Fact]
    public async Task RF_20_ConLaDemoraConfigurada_TardaEseTiempo()
    {
        var pasarela = CrearPasarela(configuracion: new() { ["Pagos:DemoraMs"] = "50" });

        var cronometro = Stopwatch.StartNew();
        await pasarela.ReembolsarAsync("ch_sim_00000000-0000-0000-0000-000000000000");
        cronometro.Stop();

        cronometro.ElapsedMilliseconds.Should().BeInRange(45, 290);
    }

    [Fact]
    public async Task RF_20_ConLaDemoraEnCero_RespondeSinEsperar()
    {
        var pasarela = CrearPasarela();

        var cronometro = Stopwatch.StartNew();
        await pasarela.ReembolsarAsync("ch_sim_00000000-0000-0000-0000-000000000000");
        cronometro.Stop();

        cronometro.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    // --- Utilidades ---

    private static PasarelaSimulada CrearPasarela(Dictionary<string, string?>? configuracion = null, DateTimeOffset? ahora = null)
    {
        var reloj = Substitute.For<IReloj>();
        reloj.Ahora.Returns(ahora ?? Hoy);
        var datos = configuracion ?? new() { ["Pagos:DemoraMs"] = "0" };
        return new PasarelaSimulada(new ConfigurationBuilder().AddInMemoryCollection(datos).Build(), reloj);
    }

    private static DatosTarjeta Tarjeta(string numero, string mes = "12", string anio = "2028", string cvv = "123") => new()
    {
        Numero = numero,
        MesVencimiento = mes,
        AnioVencimiento = anio,
        Cvv = cvv,
        Titular = "Ana López",
    };
}
