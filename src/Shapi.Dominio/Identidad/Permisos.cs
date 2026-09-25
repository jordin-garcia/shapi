namespace Shapi.Dominio.Identidad;

/// <summary>
/// Define los permisos granulares del sistema. Estos permisos se evalúan
/// contra el rol del usuario en la sesión actual.
/// </summary>
public static class Permisos
{
    // Panel del proveedor
    public const string VerApis = "VerApis";
    public const string ConfigurarApis = "ConfigurarApis";
    public const string PersonalizarPortal = "PersonalizarPortal";
    public const string ConfigurarDominio = "ConfigurarDominio";
    public const string VerSecretoOrigen = "VerSecretoOrigen";
    public const string AdministrarPlanesApi = "AdministrarPlanesApi";
    public const string VerClaves = "VerClaves";
    public const string RevocarClaves = "RevocarClaves";
    public const string VerConsumo = "VerConsumo";
    public const string InvitarConsumidores = "InvitarConsumidores";
    public const string AdministrarMiembros = "AdministrarMiembros";
    public const string ContratarSuscripcion = "ContratarSuscripcion";
    public const string VerSuscripcion = "VerSuscripcion";
    public const string AdministrarCasos = "AdministrarCasos";
    
    // Panel de administración
    public const string GestionarPlanesPlataforma = "GestionarPlanesPlataforma";
    public const string VerOrganizaciones = "VerOrganizaciones";
    public const string AdministrarOrganizaciones = "AdministrarOrganizaciones";
    public const string VerPagos = "VerPagos";
    public const string RevertirPagos = "RevertirPagos";
    public const string AdministrarCasosSoporte = "AdministrarCasosSoporte";
    public const string VerDatosOrganizacionSoporte = "VerDatosOrganizacionSoporte";
    public const string VerEstadoComponentes = "VerEstadoComponentes";
    public const string VerBitacora = "VerBitacora";
    public const string AdministrarCuentas = "AdministrarCuentas";
}
