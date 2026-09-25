using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Api.Identidad;

/// <summary>Registra una política por cada permiso de la matriz de 04 §3.</summary>
public static class PoliticasAutorizacion
{
    public const string ClaimOrganizacion = "OrganizacionId";
    public const string ClaimAmbito = "Ambito";

    private static readonly string Propietario = Rol.Propietario.ToString();
    private static readonly string Editor = Rol.Editor.ToString();
    private static readonly string Lector = Rol.Lector.ToString();
    private static readonly string Administrador = Rol.Administrador.ToString();
    private static readonly string Soporte = Rol.Soporte.ToString();

    public static IServiceCollection AgregarPoliticasShapi(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // 04 §3.1 Panel del proveedor
            .AddPolicy(Permisos.VerApis, p => p.RequireRole(Propietario, Editor, Lector))
            .AddPolicy(Permisos.ConfigurarApis, p => p.RequireRole(Propietario, Editor))
            .AddPolicy(Permisos.PersonalizarPortal, p => p.RequireRole(Propietario, Editor))
            .AddPolicy(Permisos.ConfigurarDominio, p => p.RequireRole(Propietario, Editor))
            .AddPolicy(Permisos.VerSecretoOrigen, p => p.RequireRole(Propietario, Editor))
            .AddPolicy(Permisos.AdministrarPlanesApi, p => p.RequireRole(Propietario, Editor))
            .AddPolicy(Permisos.VerClaves, p => p.RequireRole(Propietario, Editor, Lector))
            .AddPolicy(Permisos.RevocarClaves, p => p.RequireRole(Propietario, Editor))
            .AddPolicy(Permisos.VerConsumo, p => p.RequireRole(Propietario, Editor, Lector))
            .AddPolicy(Permisos.InvitarConsumidores, p => p.RequireRole(Propietario, Editor))
            .AddPolicy(Permisos.AdministrarMiembros, p => p.RequireRole(Propietario))
            .AddPolicy(Permisos.ContratarSuscripcion, p => p.RequireRole(Propietario))
            .AddPolicy(Permisos.VerSuscripcion, p => p.RequireRole(Propietario, Lector))
            .AddPolicy(Permisos.AdministrarCasos, p => p.RequireRole(Propietario, Editor, Lector))
            .AddPolicy(Permisos.EditarPerfil, p => p.RequireRole(Propietario, Editor, Lector, Administrador, Soporte))

            // 04 §3.2 Panel de administración
            .AddPolicy(Permisos.GestionarPlanesPlataforma, p => p.RequireRole(Administrador))
            .AddPolicy(Permisos.VerOrganizaciones, p => p.RequireRole(Administrador, Soporte))
            .AddPolicy(Permisos.AdministrarOrganizaciones, p => p.RequireRole(Administrador))
            .AddPolicy(Permisos.VerPagos, p => p.RequireRole(Administrador))
            .AddPolicy(Permisos.RevertirPagos, p => p.RequireRole(Administrador))
            .AddPolicy(Permisos.AdministrarCasosSoporte, p => p.RequireRole(Administrador, Soporte))
            .AddPolicy(Permisos.VerDatosOrganizacionSoporte, p => p.RequireRole(Administrador, Soporte))
            .AddPolicy(Permisos.VerEstadoComponentes, p => p.RequireRole(Administrador, Soporte))
            .AddPolicy(Permisos.VerBitacora, p => p.RequireRole(Administrador, Soporte))
            .AddPolicy(Permisos.AdministrarCuentas, p => p.RequireRole(Administrador))

            // 04 §3.3 Portal del consumidor: toda sesión del ámbito consumidor
            .AddPolicy(Permisos.ConsumidorContratarPlan, p => p.RequireClaim(ClaimAmbito, AmbitoSesion.Consumidor.ToString()))
            .AddPolicy(Permisos.ConsumidorVerCuenta, p => p.RequireClaim(ClaimAmbito, AmbitoSesion.Consumidor.ToString()))
            .AddPolicy(Permisos.ConsumidorAdministrarClaves, p => p.RequireClaim(ClaimAmbito, AmbitoSesion.Consumidor.ToString()));

        return services;
    }
}
