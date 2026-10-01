using System.Text;
using System.Text.RegularExpressions;

namespace Shapi.Compuerta.Rutas;

/// <summary>
/// Un patrón de ruta en sintaxis OpenAPI (<c>/guias/{numero}</c>), comparado segmento por segmento (08 §1). Un
/// parámetro coincide con un segmento no vacío; un segmento puede mezclar texto y parámetros
/// (<c>{nombre}.json</c>). Los literales distinguen mayúsculas, como los caminos de HTTP.
/// </summary>
public sealed class PatronRuta
{
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromMilliseconds(100);

    private readonly Segmento[] _segmentos;

    private PatronRuta(Segmento[] segmentos)
    {
        _segmentos = segmentos;
        Literales = segmentos.Count(s => s.Literal is not null);
        Parametros = segmentos.Sum(s => s.Parametros);
    }

    /// <summary>Segmentos sin parámetros. Gana el patrón que tiene más (08 §1).</summary>
    public int Literales { get; }

    /// <summary>Parámetros del patrón. Si empatan en literales, gana el que tiene menos (08 §1).</summary>
    public int Parametros { get; }

    /// <returns><c>null</c> si el patrón no empieza con <c>/</c> o tiene llaves sin cerrar.</returns>
    public static PatronRuta? Crear(string patron)
    {
        if (!patron.StartsWith('/'))
        {
            return null;
        }

        var segmentos = new List<Segmento>();
        foreach (var texto in patron.Split('/'))
        {
            var segmento = Segmento.Crear(texto);
            if (segmento is null)
            {
                return null;
            }

            segmentos.Add(segmento);
        }

        return new PatronRuta([.. segmentos]);
    }

    public bool Coincide(string camino)
    {
        var partes = camino.Split('/');
        if (partes.Length != _segmentos.Length)
        {
            return false;
        }

        for (var i = 0; i < partes.Length; i++)
        {
            if (!_segmentos[i].Coincide(partes[i]))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class Segmento
    {
        private readonly Regex? _expresion;

        private Segmento(string? literal, Regex? expresion, int parametros)
        {
            Literal = literal;
            _expresion = expresion;
            Parametros = parametros;
        }

        public string? Literal { get; }

        public int Parametros { get; }

        public static Segmento? Crear(string texto)
        {
            if (!texto.Contains('{') && !texto.Contains('}'))
            {
                return new Segmento(texto, null, 0);
            }

            // Cada {nombre} es un parámetro: uno o más caracteres de un mismo segmento.
            var expresion = new StringBuilder("^");
            var parametros = 0;
            var posicion = 0;
            while (posicion < texto.Length)
            {
                var abre = texto.IndexOf('{', posicion);
                var cierraSuelta = texto.IndexOf('}', posicion);
                if (abre < 0)
                {
                    if (cierraSuelta >= 0)
                    {
                        return null;
                    }

                    expresion.Append(Regex.Escape(texto[posicion..]));
                    break;
                }

                var cierra = texto.IndexOf('}', abre);
                if ((cierraSuelta >= 0 && cierraSuelta < abre) || cierra < 0 || cierra == abre + 1
                    || texto.IndexOf('{', abre + 1, cierra - abre - 1) >= 0)
                {
                    return null;
                }

                expresion.Append(Regex.Escape(texto[posicion..abre])).Append("(.+?)");
                parametros++;
                posicion = cierra + 1;
            }

            expresion.Append('$');
            return new Segmento(null,
                new Regex(expresion.ToString(), RegexOptions.CultureInvariant | RegexOptions.Singleline, TiempoMaximo),
                parametros);
        }

        public bool Coincide(string parte)
        {
            if (Literal is not null)
            {
                return string.Equals(parte, Literal, StringComparison.Ordinal);
            }

            try
            {
                return _expresion!.IsMatch(parte);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
