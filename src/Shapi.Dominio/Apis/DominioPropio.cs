namespace Shapi.Dominio.Apis;

public class DominioPropio
{
    public Guid Id { get; private set; }
    public Guid ApiId { get; private set; }
    public string Dominio { get; private set; } = null!;
    public string DestinoCname { get; private set; } = null!;
    public EstadoDominio Estado { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset? VerificadoEn { get; private set; }
    public DateTimeOffset? UltimoIntentoEn { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected DominioPropio() { }

    public DominioPropio(Guid apiId, string dominio, string destinoCname, DateTimeOffset ahora)
    {
        Id = Guid.CreateVersion7();
        ApiId = apiId;
        Dominio = dominio;
        DestinoCname = destinoCname;
        Estado = EstadoDominio.Pendiente;
        CreadoEn = ahora;
        ActualizadoEn = ahora;
    }

    public void Reemplazar(string dominio, string destinoCname, DateTimeOffset ahora)
    {
        Dominio = dominio;
        DestinoCname = destinoCname;
        Estado = EstadoDominio.Pendiente;
        Motivo = null;
        VerificadoEn = null;
        UltimoIntentoEn = null;
        CreadoEn = ahora;
        ActualizadoEn = ahora;
    }

    public bool Verificar(string? cname, DateTimeOffset ahora)
    {
        UltimoIntentoEn = ahora;
        ActualizadoEn = ahora;
        if (string.Equals(cname?.TrimEnd('.'), DestinoCname.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
        {
            Estado = EstadoDominio.Verificado;
            Motivo = null;
            VerificadoEn = ahora;
            return true;
        }

        Estado = EstadoDominio.Pendiente;
        Motivo = cname is null
            ? "No se encontró el registro CNAME."
            : $"El CNAME apunta a {cname.TrimEnd('.')} y debe apuntar a {DestinoCname}.";
        VerificadoEn = null;
        return false;
    }

    public void MarcarFallido(DateTimeOffset ahora)
    {
        Estado = EstadoDominio.Fallido;
        Motivo = "No se verificó el registro DNS durante 72 horas.";
        UltimoIntentoEn = ahora;
        ActualizadoEn = ahora;
    }
}
