namespace Shapi.Contratos.Redis;

public sealed record ContextoClave(Guid ClaveId, Guid SuscripcionId, Guid ApiId, Guid OrganizacionId, Guid ConsumidorId, string Tipo)
{
    public const string TipoProduccion = "produccion";
    public const string TipoPruebas = "pruebas";
    public string Entorno => throw new NotImplementedException();
    public static string CalcularHash(string clave) => throw new NotImplementedException();
    public IReadOnlyDictionary<string, string> ACampos() => throw new NotImplementedException();
    public static ContextoClave? DesdeCampos(IReadOnlyDictionary<string, string> campos) => throw new NotImplementedException();
}
