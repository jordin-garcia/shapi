using Microsoft.Extensions.Configuration;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;

namespace Shapi.Infraestructura.Pagos;

/// <summary>
/// Pasarela simulada de 09 §2. Nunca registra ni devuelve el número completo ni el CVV (ADR-13).
/// </summary>
public class PasarelaSimulada : IPasarelaPagos
{
    // Tarjetas de prueba de 09 §2, comparadas por el número completo. Su comportamiento va en el token
    // (tok_sim_{sufijo}_{uuid}) para que lo respete cualquier proceso que cobre, como el Trabajador en las renovaciones.
    private static readonly Dictionary<string, string> TarjetasEspeciales = new()
    {
        ["4000000000000002"] = "0002",
        ["4000000000000069"] = "0069",
        ["4000000000000341"] = "0341",
    };

    // Guatemala no tiene horario de verano (UTC−6).
    private static readonly TimeSpan DesfaseGuatemala = TimeSpan.FromHours(-6);

    private readonly int? _demoraMs;
    private readonly bool _falla;
    private readonly IReloj _reloj;

    public PasarelaSimulada(IConfiguration configuracion, IReloj reloj)
    {
        // Sin Pagos:DemoraMs, la demora es al azar entre 300 y 800 ms; con un valor, es ese valor (0 en las pruebas).
        _demoraMs = configuracion.GetValue<int?>("Pagos:DemoraMs");
        _falla = configuracion.GetValue<bool>("SHAPI_PASARELA_FALLA");
        _reloj = reloj;
    }

    public async Task<ResultadoTokenizacion> TokenizarAsync(DatosTarjeta tarjeta)
    {
        await SimularDemoraAsync();
        if (_falla)
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "pasarela_no_disponible" };
        }

        var numero = tarjeta.Numero.Replace(" ", "").Replace("-", "");
        if (numero.Length is < 13 or > 19 || !numero.All(char.IsAsciiDigit) || !PasaLuhn(numero))
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "numero_invalido" };
        }

        var marca = ObtenerMarca(numero);
        if (marca is null)
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "marca_no_soportada" };
        }

        if (!EstaVigente(tarjeta.MesVencimiento, tarjeta.AnioVencimiento))
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "tarjeta_vencida" };
        }

        var longitudCvv = marca == "American Express" ? 4 : 3;
        if (tarjeta.Cvv.Length != longitudCvv || !tarjeta.Cvv.All(char.IsAsciiDigit))
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "cvv_invalido" };
        }

        var token = TarjetasEspeciales.TryGetValue(numero, out var sufijo)
            ? $"tok_sim_{sufijo}_{Guid.NewGuid()}"
            : $"tok_sim_{Guid.NewGuid()}";

        return new ResultadoTokenizacion
        {
            Exitoso = true,
            Token = token,
            Marca = marca,
            Ultimos4 = numero[^4..],
            Titular = tarjeta.Titular,
        };
    }

    public async Task<ResultadoCobro> CobrarAsync(string token, decimal monto, string referencia, bool esRenovacion)
    {
        await SimularDemoraAsync();
        if (_falla)
        {
            return new ResultadoCobro { Exitoso = false, Error = "pasarela_no_disponible" };
        }

        // 0341 se aprueba al contratar y se rechaza en las renovaciones.
        var motivoRechazo = token switch
        {
            _ when token.StartsWith("tok_sim_0002_", StringComparison.Ordinal) => "fondos_insuficientes",
            _ when token.StartsWith("tok_sim_0069_", StringComparison.Ordinal) => "tarjeta_vencida",
            _ when token.StartsWith("tok_sim_0341_", StringComparison.Ordinal) && esRenovacion => "fondos_insuficientes",
            _ => null,
        };

        return motivoRechazo is null
            ? new ResultadoCobro { Exitoso = true, Referencia = $"ch_sim_{Guid.NewGuid()}" }
            : new ResultadoCobro { Exitoso = false, Error = motivoRechazo };
    }

    public async Task<ResultadoReembolso> ReembolsarAsync(string referenciaCobro)
    {
        await SimularDemoraAsync();
        if (_falla)
        {
            return new ResultadoReembolso { Exitoso = false, Error = "pasarela_no_disponible" };
        }

        return new ResultadoReembolso { Exitoso = true, Referencia = $"re_sim_{Guid.NewGuid()}" };
    }

    private Task SimularDemoraAsync()
    {
        var demora = _demoraMs ?? Random.Shared.Next(300, 801);
        return demora > 0 ? Task.Delay(demora) : Task.CompletedTask;
    }

    private static bool PasaLuhn(string numero)
    {
        var suma = 0;
        var duplicar = false;
        for (var i = numero.Length - 1; i >= 0; i--)
        {
            var digito = numero[i] - '0';
            if (duplicar)
            {
                digito *= 2;
                if (digito > 9)
                {
                    digito -= 9;
                }
            }

            suma += digito;
            duplicar = !duplicar;
        }

        return suma % 10 == 0;
    }

    private static string? ObtenerMarca(string numero)
    {
        var prefijo2 = int.Parse(numero[..2]);
        var prefijo4 = int.Parse(numero[..4]);

        if (numero[0] == '4')
        {
            return "Visa";
        }

        if (prefijo2 is >= 51 and <= 55 || prefijo4 is >= 2221 and <= 2720)
        {
            return "Mastercard";
        }

        if (prefijo2 is 34 or 37)
        {
            return "American Express";
        }

        return null;
    }

    // El vencimiento tiene que ser el mes actual o uno posterior, en la zona de Guatemala.
    private bool EstaVigente(string mesTexto, string anioTexto)
    {
        if (!int.TryParse(mesTexto, out var mes) || !int.TryParse(anioTexto, out var anio) || mes is < 1 or > 12)
        {
            return false;
        }

        if (anio < 100)
        {
            anio += 2000;
        }

        var hoy = _reloj.Ahora.ToOffset(DesfaseGuatemala);
        return anio > hoy.Year || (anio == hoy.Year && mes >= hoy.Month);
    }
}
