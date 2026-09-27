using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Infraestructura.Siembra.Base;

namespace Shapi.Api.Tests.Persistencia;

/// <summary>Criterio 5 de EM-01: datos base idempotentes (01 §6 y 07 §6).</summary>
[Collection(nameof(PostgresPersistencia))]
public sealed class SiembraBaseTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    [Fact]
    public async Task RF_17_Siembra_CreaLosCincoPlanesDePlataformaDe01Seccion6()
    {
        await using var db = CrearDb();
        await SiembraBase.EjecutarAsync(db, null, null, null, Reloj, _hasher, NullLogger.Instance);

        var planes = await db.Set<PlanPlataforma>().OrderBy(p => p.Orden).ToListAsync();

        // Nombre, descripción, precio, vigencia, APIs, peticiones por ciclo, miembros y dominio propio de 01 §6.
        var esperados = new (string Nombre, string Descripcion, decimal Precio, int Vigencia, int? Apis, long Cuota, int? Miembros, bool Dominio)[]
        {
            ("Prueba", "Publicar una primera API sin costo", 0m, 30, 1, 10_000, 1, false),
            ("Lanzamiento", "Empezar a cobrar por una API existente", 199m, 30, 3, 250_000, 3, false),
            ("Producto", "Proveedores con clientes establecidos", 599m, 30, 10, 2_000_000, 10, true),
            ("Escala mensual", "Varias APIs y alto volumen", 1_500m, 30, null, 10_000_000, null, true),
            ("Escala anual", "Alto volumen, con pago anual", 15_000m, 365, null, 120_000_000, null, true),
        };
        Assert.Equal(esperados.Length, planes.Count);
        for (var i = 0; i < esperados.Length; i++)
        {
            var (plan, esperado) = (planes[i], esperados[i]);
            Assert.Equal(esperado.Nombre, plan.Nombre);
            Assert.Equal(esperado.Descripcion, plan.Descripcion);
            Assert.Equal(esperado.Precio, plan.Precio);
            Assert.Equal(esperado.Vigencia, plan.VigenciaDias);
            Assert.Equal(esperado.Apis, plan.MaxApis);
            Assert.Equal(esperado.Cuota, plan.CuotaPeticiones);
            Assert.Equal(esperado.Miembros, plan.MaxMiembros);
            Assert.Equal(esperado.Dominio, plan.DominioPropio);
            Assert.Equal(i == 0, plan.EsPrueba);
            Assert.True(plan.Activo);
        }
    }

    [Fact]
    public async Task Siembra_EjecutadaDosVeces_NoDuplicaNada()
    {
        await using var db = CrearDb();

        await SiembraBase.EjecutarAsync(db, "admin@shapi.test", "Admin", "Contra123", Reloj, _hasher, NullLogger.Instance);
        await SiembraBase.EjecutarAsync(db, "admin@shapi.test", "Admin", "Contra123", Reloj, _hasher, NullLogger.Instance);

        Assert.Equal(5, await db.Set<PlanPlataforma>().CountAsync());
        Assert.Equal(1, await db.Set<Organizacion>().CountAsync(o => o.Tipo == TipoOrganizacion.Plataforma));
        var admin = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal("admin@shapi.test", admin.Correo);
        Assert.Equal(PasswordVerificationResult.Success, _hasher.VerifyHashedPassword(admin, admin.HashContrasena!, "Contra123"));
        var membresia = await db.Set<Membresia>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal(Rol.Administrador, membresia.Rol);
    }

    [Theory]
    [InlineData(null, "Admin", "Contra123")]
    [InlineData("admin@shapi.test", null, "Contra123")]
    [InlineData("admin@shapi.test", "Admin", "")]
    public async Task Siembra_SinVariablesDelAdministrador_NoLoCreaYRegistraUnAviso(string? correo, string? nombre, string? contrasena)
    {
        var registro = new RegistroCapturado();
        await using var db = CrearDb();

        await SiembraBase.EjecutarAsync(db, correo, nombre, contrasena, Reloj, _hasher, registro);

        Assert.Empty(await db.Set<Usuario>().IgnoreQueryFilters().ToListAsync());
        var aviso = Assert.Single(registro.Entradas);
        Assert.Equal(LogLevel.Warning, aviso.Nivel);
        Assert.Contains("SHAPI_ADMIN_", aviso.Mensaje);
    }

    private sealed class RegistroCapturado : ILogger
    {
        public List<(LogLevel Nivel, string Mensaje)> Entradas { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entradas.Add((logLevel, formatter(state, exception)));
    }
}
