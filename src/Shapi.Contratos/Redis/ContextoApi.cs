namespace Shapi.Contratos.Redis;

public sealed record ContextoApi(Guid ApiId, Guid OrganizacionId, string Estado, string UrlOrigen, string? Secreto, string? PortalHost, long Version)
{
    public const string EstadoPublicada = "publicada";
    public bool EstaPublicada => throw new NotImplementedException();
    public IReadOnlyDictionary<string, string> ACampos() => throw new NotImplementedException();
    public static ContextoApi? DesdeCampos(Guid apiId, IReadOnlyDictionary<string, string> campos) => throw new NotImplementedException();
}
