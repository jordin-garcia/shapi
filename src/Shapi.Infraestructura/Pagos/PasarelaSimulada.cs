using Microsoft.Extensions.Configuration;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;

namespace Shapi.Infraestructura.Pagos;

public class PasarelaSimulada : IPasarelaPagos
{
    private readonly int _demoraMs;
    private readonly bool _falla;
    private readonly IReloj _reloj;

    public PasarelaSimulada(IConfiguration configuracion, IReloj reloj)
    {
        _demoraMs = configuracion.GetValue<int>("Pagos:DemoraMs", 0);
        var fallaEnv = Environment.GetEnvironmentVariable("SHAPI_PASARELA_FALLA");
        _falla = fallaEnv == "true" || configuracion.GetValue<bool>("SHAPI_PASARELA_FALLA", false);
        _reloj = reloj;
    }

    private async Task SimularLatenciaYFallaAsync()
    {
        if (_demoraMs > 0)
        {
            // Simular demora aleatoria según la spec
            await Task.Delay(Random.Shared.Next(300, 800));
        }

        if (_falla)
        {
            throw new PasarelaException("pasarela_no_disponible");
        }
    }

    public async Task<ResultadoTokenizacion> TokenizarAsync(DatosTarjeta tarjeta)
    {
        try
        {
            await SimularLatenciaYFallaAsync();
        }
        catch (PasarelaException ex)
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = ex.Message };
        }

        var num = tarjeta.Numero.Replace(" ", "").Replace("-", "");

        if (num.Length < 13 || num.Length > 19 || !PasaLuhn(num))
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "numero_invalido" };
        }

        var marca = ObtenerMarca(num);
        if (marca == null)
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "marca_no_soportada" };
        }

        if (!ValidarVencimiento(tarjeta.MesVencimiento, tarjeta.AnioVencimiento))
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "tarjeta_vencida" };
        }

        int longitudCvvEsperada = marca == "American Express" ? 4 : 3;
        if (tarjeta.Cvv.Length != longitudCvvEsperada || !int.TryParse(tarjeta.Cvv, out _))
        {
            return new ResultadoTokenizacion { Exitoso = false, Error = "cvv_invalido" };
        }

        var tokenUuid = Guid.NewGuid().ToString("N");
        var tokenPrefijo = num.EndsWith("0341") ? "tok_sim_0341_" : "tok_sim_";
        var tokenCompleto = $"{tokenPrefijo}{tokenUuid}";

        if (num.EndsWith("0002"))
        {
            TokensEspeciales[tokenCompleto] = "0002";
        }

        if (num.EndsWith("0069"))
        {
            TokensEspeciales[tokenCompleto] = "0069";
        }

        return new ResultadoTokenizacion
        {
            Exitoso = true,
            Token = tokenCompleto,
            Marca = marca,
            Ultimos4 = num.Substring(num.Length - 4),
            Titular = tarjeta.Titular
        };
    }

    public async Task<ResultadoCobro> CobrarAsync(string token, decimal monto, string referencia, bool esRenovacion)
    {
        try
        {
            await SimularLatenciaYFallaAsync();
        }
        catch (PasarelaException ex)
        {
            return new ResultadoCobro { Exitoso = false, Error = ex.Message };
        }

        // Simular validaciones en base a prefijos o base en memoria si aplica,
        // pero la regla principal para tarjetas de prueba es el final del token o datos:
        // No tenemos el número completo aquí, pero la spec dice:
        // "La pasarela guarda en memoria ... el comportamiento de cada tarjeta especial. 
        // Además lo incluye en el token (tok_sim_0341_{uuid}) para no perderlo..."
        // Para 0002 y 0069 no especifica si van en el token, pero TokenizarAsync 
        // asocia el token a la tarjeta. Como es simulado y stateless, podemos
        // hacer que el token también lleve esa info o guardarlo en memoria.
        // Un diccionario estático sirve mientras el proceso está activo.

        // Buscamos si el token indica "0341"
        if (token.StartsWith("tok_sim_0341_"))
        {
            if (esRenovacion)
            {
                return new ResultadoCobro { Exitoso = false, Error = "fondos_insuficientes" }; // O cualquier error de rechazo
            }
        }
        else
        {
            // Verificamos si en memoria tenemos registrado el token
            if (TokensEspeciales.TryGetValue(token, out var sufijo))
            {
                if (sufijo == "0002")
                {
                    return new ResultadoCobro { Exitoso = false, Error = "fondos_insuficientes" };
                }

                if (sufijo == "0069")
                {
                    return new ResultadoCobro { Exitoso = false, Error = "tarjeta_vencida" };
                }
            }
        }

        return new ResultadoCobro
        {
            Exitoso = true,
            Referencia = $"ch_sim_{Guid.NewGuid():N}"
        };
    }

    public async Task<ResultadoReembolso> ReembolsarAsync(string referenciaCobro)
    {
        try
        {
            await SimularLatenciaYFallaAsync();
        }
        catch (PasarelaException ex)
        {
            return new ResultadoReembolso { Exitoso = false, Error = ex.Message };
        }

        return new ResultadoReembolso
        {
            Exitoso = true,
            Referencia = $"re_sim_{Guid.NewGuid():N}"
        };
    }

    // Memoria estática para asociar tokens a tarjetas especiales
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> TokensEspeciales = new();

    private bool PasaLuhn(string numero)
    {
        int suma = 0;
        bool alternar = false;
        for (int i = numero.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(numero[i]))
            {
                return false;
            }

            int n = numero[i] - '0';
            if (alternar)
            {
                n *= 2;
                if (n > 9)
                {
                    n -= 9;
                }
            }
            suma += n;
            alternar = !alternar;
        }
        return suma % 10 == 0;
    }

    private string? ObtenerMarca(string numero)
    {
        if (numero.StartsWith("4"))
        {
            return "Visa";
        }

        if (numero.StartsWith("34") || numero.StartsWith("37"))
        {
            return "American Express";
        }

        if (int.TryParse(numero.Substring(0, 2), out int pre2) && pre2 >= 51 && pre2 <= 55)
        {
            return "Mastercard";
        }

        if (numero.Length >= 4 && int.TryParse(numero.Substring(0, 4), out int pre4) && pre4 >= 2221 && pre4 <= 2720)
        {
            return "Mastercard";
        }

        return null;
    }

    private bool ValidarVencimiento(string mesStr, string anioStr)
    {
        if (!int.TryParse(mesStr, out int mes) || !int.TryParse(anioStr, out int anio))
        {
            return false;
        }

        if (mes < 1 || mes > 12)
        {
            return false;
        }

        if (anio < 100)
        {
            anio += 2000;
        }

        var ahora = _reloj.AhoraUtc;
        if (anio < ahora.Year)
        {
            return false;
        }

        if (anio == ahora.Year && mes < ahora.Month)
        {
            return false;
        }

        return true;
    }

    private class PasarelaException : Exception
    {
        public PasarelaException(string message) : base(message) { }
    }
}
