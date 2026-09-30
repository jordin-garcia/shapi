namespace Shapi.Dominio.Claves;

/// <summary>Una clave de API (07 §3.3). Su máquina de estados está en 07 §5.</summary>
public class Clave
{
    /// <summary>Lo que sigue funcionando una clave rotada (RF-27).</summary>
    public static readonly TimeSpan VigenciaTrasRotar = TimeSpan.FromHours(24);

    public Guid Id { get; private set; }
    public Guid SuscripcionId { get; private set; }
    public TipoClave Tipo { get; private set; }
    public string Prefijo { get; private set; } = null!;
    public string Ultimos4 { get; private set; } = null!;
    public string HashSha256 { get; private set; } = null!;
    public EstadoClave Estado { get; private set; }
    public DateTimeOffset? ExpiraEn { get; private set; }
    public DateTimeOffset? RevocadaEn { get; private set; }
    public RevocadaPor? RevocadaPor { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Clave() { }

    /// <summary>La clave como se muestra después de emitirla: <c>shp_prod_••••7c2e</c> (10 §3).</summary>
    public string Enmascarada => $"{Prefijo}••••{Ultimos4}";

    public bool PuedeRotarse => Estado == EstadoClave.Activa;

    /// <summary>Emite una clave activa (RF-26). El valor en claro se devuelve aquí y no se guarda en ningún lado.</summary>
    public static (Clave Clave, string EnClaro) Emitir(Guid suscripcionId, TipoClave tipo)
    {
        var generada = GeneradorClave.Generar(tipo);
        var clave = new Clave
        {
            Id = Guid.CreateVersion7(),
            SuscripcionId = suscripcionId,
            Tipo = tipo,
            Prefijo = generada.Prefijo,
            Ultimos4 = generada.Ultimos4,
            HashSha256 = generada.HashSha256,
            Estado = EstadoClave.Activa,
        };
        return (clave, generada.EnClaro);
    }

    /// <summary>
    /// Rota una clave activa (RF-27): esta queda <c>rotada</c> y funciona 24 horas más, y se emite otra del mismo tipo.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la clave no está activa.</exception>
    public (Clave Nueva, string EnClaro) Rotar(DateTimeOffset ahora)
    {
        if (!PuedeRotarse)
        {
            throw new InvalidOperationException($"Solo se rota una clave activa; esta está {Estado}.");
        }

        Estado = EstadoClave.Rotada;
        ExpiraEn = ahora + VigenciaTrasRotar;
        return Emitir(SuscripcionId, Tipo);
    }

    /// <summary>
    /// Adelanta el fin de una clave rotada a <paramref name="ahora"/>. Se usa al rotar otra vez antes de que pasen sus
    /// 24 horas, para que no coexistan más de dos claves del mismo tipo (RF-27).
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la clave no está rotada.</exception>
    public void TerminarRotacion(DateTimeOffset ahora)
    {
        if (Estado != EstadoClave.Rotada)
        {
            throw new InvalidOperationException($"Solo se termina la rotación de una clave rotada; esta está {Estado}.");
        }

        if (ExpiraEn > ahora)
        {
            ExpiraEn = ahora;
        }
    }

    /// <summary>Revoca una clave activa o rotada (RF-28). Devuelve <c>false</c> si ya estaba revocada.</summary>
    public bool Revocar(RevocadaPor por, DateTimeOffset ahora)
    {
        if (Estado == EstadoClave.Revocada)
        {
            return false;
        }

        Estado = EstadoClave.Revocada;
        RevocadaPor = por;
        RevocadaEn = ahora;
        return true;
    }

    /// <summary>Si la compuerta la acepta: activa, o rotada sin cumplir sus 24 horas.</summary>
    public bool EsVigente(DateTimeOffset ahora) =>
        Estado == EstadoClave.Activa || Estado == EstadoClave.Rotada && ExpiraEn > ahora;
}
