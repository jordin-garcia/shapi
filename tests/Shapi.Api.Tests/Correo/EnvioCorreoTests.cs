using System.Net;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Correo;
using Shapi.Infraestructura.Persistencia;
using Shapi.Trabajador.Correo;
using Testcontainers.PostgreSql;

namespace Shapi.Api.Tests.Correo;

[CollectionDefinition(nameof(EntornoCorreo), DisableParallelization = true)]
public sealed class ColeccionCorreo : ICollectionFixture<EntornoCorreo>;

public sealed class EntornoCorreo : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public IContainer Mailpit { get; } = new ContainerBuilder("axllent/mailpit:v1.27")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(peticion => peticion
            .ForPort(8025)
            .ForPath("/api/v1/messages")))
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(Postgres.StartAsync(), Mailpit.StartAsync());
        await using var db = CrearDb();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(Postgres.DisposeAsync().AsTask(), Mailpit.DisposeAsync().AsTask());
    }

    public ShapiDbContext CrearDb()
    {
        var opciones = new DbContextOptionsBuilder<ShapiDbContext>()
            .UseNpgsql(Postgres.GetConnectionString())
            .Options;
        return new ShapiDbContext(opciones, new ContextoOrganizacionNulo());
    }

    public IConfiguration CrearConfiguracion(int? puertoSmtp = null) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_DOMINIO_BASE"] = "shapi.localhost",
            ["SHAPI_SMTP_HOST"] = "127.0.0.1",
            ["SHAPI_SMTP_PUERTO"] = (puertoSmtp ?? Mailpit.GetMappedPublicPort(1025)).ToString(),
            ["SHAPI_SMTP_USUARIO"] = string.Empty,
            ["SHAPI_SMTP_CONTRASENA"] = string.Empty,
            ["SHAPI_SMTP_TLS"] = "false",
        })
        .Build();
}

[Collection(nameof(EntornoCorreo))]
public sealed class EnvioCorreoTests(EntornoCorreo entorno) : IAsyncLifetime
{
    private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await using var db = entorno.CrearDb();
        await db.Set<CorreoSaliente>().ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RF_46_ProcesarPendiente_EntregaEnMailpitYLoMarcaEnviado()
    {
        await using var db = entorno.CrearDb();
        var correo = new CorreoSaliente(
            "verificacion_correo",
            "ana@ejemplo.com",
            """{"nombre":"Ana","token":"token-1","nombrePortal":"Envíos Xelajú"}""",
            "Verifique su correo",
            _reloj.Ahora);
        db.Add(correo);
        await db.SaveChangesAsync();
        var procesador = CrearProcesador(db, entorno.CrearConfiguracion());

        var procesados = await procesador.ProcesarPendientes();

        procesados.Should().Be(1);
        await db.Entry(correo).ReloadAsync();
        correo.Estado.Should().Be(EstadoCorreo.Enviado);
        correo.EnviadoEn.Should().Be(_reloj.Ahora);

        using var cliente = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{entorno.Mailpit.GetMappedPublicPort(8025)}"),
        };
        using var respuesta = await cliente.GetAsync("/api/v1/messages");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
        json.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
        var ultimo = json.GetProperty("messages").EnumerateArray().First();
        ultimo.GetProperty("To").ToString().Should().Contain("ana@ejemplo.com");
        ultimo.GetProperty("From").ToString().Should().Contain("no-responder@shapi.localhost");
        ultimo.GetProperty("From").ToString().Should().Contain("Envíos Xelajú");
    }

    [Fact]
    public async Task RF_46_SmtpCaido_RegistraElFalloYProgramaElReintento()
    {
        await using var db = entorno.CrearDb();
        var correo = new CorreoSaliente(
            "recuperacion",
            "ana@ejemplo.com",
            """{"nombre":"Ana","token":"token-1"}""",
            "Recupere su contraseña",
            _reloj.Ahora);
        db.Add(correo);
        await db.SaveChangesAsync();
        var procesador = CrearProcesador(db, entorno.CrearConfiguracion(1));

        var procesados = await procesador.ProcesarPendientes();

        procesados.Should().Be(1);
        await db.Entry(correo).ReloadAsync();
        correo.Estado.Should().Be(EstadoCorreo.Pendiente);
        correo.Intentos.Should().Be(1);
        correo.UltimoError.Should().NotBeNullOrWhiteSpace();
        correo.ProximoIntentoEn.Should().Be(_reloj.Ahora.AddSeconds(5));
    }

    [Fact]
    public void RF_46_Despachador_EjecutaCadaCincoSegundos()
    {
        DespachadorCorreos.Intervalo.Should().Be(TimeSpan.FromSeconds(5));
    }

    private ProcesadorCorreos CrearProcesador(ShapiDbContext db, IConfiguration configuracion) => new(
        db,
        new MotorPlantillasCorreo(configuracion),
        new EnviadorSmtp(configuracion),
        _reloj,
        NullLogger<ProcesadorCorreos>.Instance);

    private sealed class RelojFalso(DateTimeOffset ahora) : Shapi.Aplicacion.Comun.IReloj
    {
        public DateTimeOffset Ahora { get; set; } = ahora;
    }
}
