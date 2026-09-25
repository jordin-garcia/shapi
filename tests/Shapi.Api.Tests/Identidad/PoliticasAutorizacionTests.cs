using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Identidad;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Xunit;

namespace Shapi.Api.Tests.Identidad;

/// <summary>Las políticas de <see cref="PoliticasAutorizacion"/> contra la matriz de 04 §3, sin levantar la API.</summary>
public class PoliticasAutorizacionTests
{
    private static readonly IServiceProvider Servicios = new ServiceCollection()
        .AddLogging()
        .AgregarPoliticasShapi()
        .BuildServiceProvider();

    // ---------- RF-07 · Políticas de 04 §3 ----------

    public static TheoryData<string, Rol, bool> MatrizDePermisos()
    {
        Rol[] proveedor = [Rol.Propietario, Rol.Editor, Rol.Lector];
        Rol[] propietarioYEditor = [Rol.Propietario, Rol.Editor];
        Rol[] administracion = [Rol.Administrador, Rol.Soporte];
        var matriz = new Dictionary<string, Rol[]>
        {
            [Permisos.VerApis] = proveedor,
            [Permisos.ConfigurarApis] = propietarioYEditor,
            [Permisos.PersonalizarPortal] = propietarioYEditor,
            [Permisos.ConfigurarDominio] = propietarioYEditor,
            [Permisos.VerSecretoOrigen] = propietarioYEditor,
            [Permisos.AdministrarPlanesApi] = propietarioYEditor,
            [Permisos.VerClaves] = proveedor,
            [Permisos.RevocarClaves] = propietarioYEditor,
            [Permisos.VerConsumo] = proveedor,
            [Permisos.InvitarConsumidores] = propietarioYEditor,
            [Permisos.AdministrarMiembros] = [Rol.Propietario],
            [Permisos.ContratarSuscripcion] = [Rol.Propietario],
            [Permisos.VerSuscripcion] = [Rol.Propietario, Rol.Lector],
            [Permisos.AdministrarCasos] = proveedor,
            [Permisos.EditarPerfil] = Enum.GetValues<Rol>(),
            [Permisos.GestionarPlanesPlataforma] = [Rol.Administrador],
            [Permisos.VerOrganizaciones] = administracion,
            [Permisos.AdministrarOrganizaciones] = [Rol.Administrador],
            [Permisos.VerPagos] = [Rol.Administrador],
            [Permisos.RevertirPagos] = [Rol.Administrador],
            [Permisos.AdministrarCasosSoporte] = administracion,
            [Permisos.VerDatosOrganizacionSoporte] = administracion,
            [Permisos.VerEstadoComponentes] = administracion,
            [Permisos.VerBitacora] = administracion,
            [Permisos.AdministrarCuentas] = [Rol.Administrador],
        };
        var datos = new TheoryData<string, Rol, bool>();
        foreach (var (permiso, permitidos) in matriz)
        {
            foreach (var rol in Enum.GetValues<Rol>())
            {
                datos.Add(permiso, rol, permitidos.Contains(rol));
            }
        }
        return datos;
    }

    [Theory]
    [MemberData(nameof(MatrizDePermisos))]
    public async Task RF_07_Politicas_SiguenLaMatrizDe04Seccion3(string permiso, Rol rol, bool permitido)
    {
        var autorizacion = Servicios.GetRequiredService<IAuthorizationService>();

        var resultado = await autorizacion.AuthorizeAsync(Personal(rol), permiso);

        Assert.Equal(permitido, resultado.Succeeded);
    }

    [Theory]
    [InlineData(Permisos.ConsumidorContratarPlan)]
    [InlineData(Permisos.ConsumidorVerCuenta)]
    [InlineData(Permisos.ConsumidorAdministrarClaves)]
    public async Task RF_07_PoliticasDelConsumidor_SoloPermitenElAmbitoConsumidor(string permiso)
    {
        var autorizacion = Servicios.GetRequiredService<IAuthorizationService>();
        var consumidor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(PoliticasAutorizacion.ClaimAmbito, nameof(AmbitoSesion.Consumidor))], "Consumidor"));

        Assert.True((await autorizacion.AuthorizeAsync(consumidor, permiso)).Succeeded);
        Assert.False((await autorizacion.AuthorizeAsync(Personal(Rol.Propietario), permiso)).Succeeded);
    }

    [Fact]
    public async Task RF_07_Politicas_CadaPermisoTieneSuPolitica()
    {
        var proveedor = Servicios.GetRequiredService<IAuthorizationPolicyProvider>();
        var permisos = typeof(Permisos).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!).ToArray();

        Assert.Equal(28, permisos.Length);
        foreach (var permiso in permisos)
        {
            Assert.NotNull(await proveedor.GetPolicyAsync(permiso));
        }
    }

    private static ClaimsPrincipal Personal(Rol rol) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.Role, rol.ToString()), new Claim(PoliticasAutorizacion.ClaimAmbito, nameof(AmbitoSesion.Personal))],
        PersonalAutenticacionOpciones.Esquema));
}
