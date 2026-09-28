namespace Shapi.Dominio.Identidad;

/// <summary>
/// Políticas de autorización de la matriz de 04 §3. Cada endpoint declara la suya con
/// <c>RequireAuthorization(Permisos.X)</c> (convenciones §5).
/// </summary>
public static class Permisos
{
    // 04 §3.1 Panel del proveedor
    public const string VerApis = "Permiso.VerApis";
    public const string ConfigurarApis = "Permiso.ConfigurarApis";
    public const string PersonalizarPortal = "Permiso.PersonalizarPortal";
    public const string ConfigurarDominio = "Permiso.ConfigurarDominio";
    public const string VerSecretoOrigen = "Permiso.VerSecretoOrigen";
    public const string AdministrarPlanesApi = "Permiso.AdministrarPlanesApi";
    public const string VerClaves = "Permiso.VerClaves";
    public const string RevocarClaves = "Permiso.RevocarClaves";
    public const string VerConsumo = "Permiso.VerConsumo";
    public const string InvitarConsumidores = "Permiso.InvitarConsumidores";
    public const string AdministrarMiembros = "Permiso.AdministrarMiembros";
    public const string ContratarSuscripcion = "Permiso.ContratarSuscripcion";
    public const string VerSuscripcion = "Permiso.VerSuscripcion";
    public const string AdministrarCasos = "Permiso.AdministrarCasos";
    public const string EditarPerfil = "Permiso.EditarPerfil";

    // 04 §3.2 Panel de administración
    public const string GestionarPlanesPlataforma = "Permiso.GestionarPlanesPlataforma";
    public const string VerOrganizaciones = "Permiso.VerOrganizaciones";
    public const string AdministrarOrganizaciones = "Permiso.AdministrarOrganizaciones";
    public const string VerPagos = "Permiso.VerPagos";
    public const string RevertirPagos = "Permiso.RevertirPagos";
    public const string AdministrarCasosSoporte = "Permiso.AdministrarCasosSoporte";
    public const string VerDatosOrganizacionSoporte = "Permiso.VerDatosOrganizacionSoporte";
    public const string VerEstadoComponentes = "Permiso.VerEstadoComponentes";
    public const string VerBitacora = "Permiso.VerBitacora";
    public const string AdministrarCuentas = "Permiso.AdministrarCuentas";

    // 04 §3.3 Portal del consumidor (la sesión del consumidor llega con EM-05).
    // Contratar exige además el correo verificado, que es una regla de negocio (422 correo_no_verificado).
    public const string ConsumidorContratarPlan = "Permiso.ConsumidorContratarPlan";
    public const string ConsumidorVerCuenta = "Permiso.ConsumidorVerCuenta";
    public const string ConsumidorAdministrarClaves = "Permiso.ConsumidorAdministrarClaves";
}
