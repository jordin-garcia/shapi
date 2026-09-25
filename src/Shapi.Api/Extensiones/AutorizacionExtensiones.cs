using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Api.Extensiones;

public static class AutorizacionExtensiones
{
    public static IServiceCollection AgregarPoliticasShapi(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Proveedor
            .AddPolicy(Permisos.VerApis, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString(), Rol.Lector.ToString()))
            .AddPolicy(Permisos.ConfigurarApis, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString()))
            .AddPolicy(Permisos.PersonalizarPortal, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString()))
            .AddPolicy(Permisos.ConfigurarDominio, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString()))
            .AddPolicy(Permisos.VerSecretoOrigen, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString()))
            .AddPolicy(Permisos.AdministrarPlanesApi, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString()))
            .AddPolicy(Permisos.VerClaves, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString(), Rol.Lector.ToString()))
            .AddPolicy(Permisos.RevocarClaves, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString()))
            .AddPolicy(Permisos.VerConsumo, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString(), Rol.Lector.ToString()))
            .AddPolicy(Permisos.InvitarConsumidores, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString()))
            .AddPolicy(Permisos.AdministrarMiembros, p => p.RequireRole(Rol.Propietario.ToString()))
            .AddPolicy(Permisos.ContratarSuscripcion, p => p.RequireRole(Rol.Propietario.ToString()))
            .AddPolicy(Permisos.VerSuscripcion, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Lector.ToString()))
            .AddPolicy(Permisos.AdministrarCasos, p => p.RequireRole(Rol.Propietario.ToString(), Rol.Editor.ToString(), Rol.Lector.ToString()))

            // Administrador y Soporte
            .AddPolicy(Permisos.GestionarPlanesPlataforma, p => p.RequireRole(Rol.Administrador.ToString()))
            .AddPolicy(Permisos.VerOrganizaciones, p => p.RequireRole(Rol.Administrador.ToString(), Rol.Soporte.ToString()))
            .AddPolicy(Permisos.AdministrarOrganizaciones, p => p.RequireRole(Rol.Administrador.ToString()))
            .AddPolicy(Permisos.VerPagos, p => p.RequireRole(Rol.Administrador.ToString()))
            .AddPolicy(Permisos.RevertirPagos, p => p.RequireRole(Rol.Administrador.ToString()))
            .AddPolicy(Permisos.AdministrarCasosSoporte, p => p.RequireRole(Rol.Administrador.ToString(), Rol.Soporte.ToString()))
            .AddPolicy(Permisos.VerDatosOrganizacionSoporte, p => p.RequireRole(Rol.Administrador.ToString(), Rol.Soporte.ToString()))
            .AddPolicy(Permisos.VerEstadoComponentes, p => p.RequireRole(Rol.Administrador.ToString(), Rol.Soporte.ToString()))
            .AddPolicy(Permisos.VerBitacora, p => p.RequireRole(Rol.Administrador.ToString(), Rol.Soporte.ToString()))
            .AddPolicy(Permisos.AdministrarCuentas, p => p.RequireRole(Rol.Administrador.ToString()));

        return services;
    }
}
