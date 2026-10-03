using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Api.Tests.Persistencia;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Correo;
using Shapi.Infraestructura.Persistencia;
using Shapi.Trabajador.Correo;

namespace Shapi.Api.Tests.Correo;

[CollectionDefinition(nameof(EntornoCorreo), DisableParallelization = true)]
public sealed class ColeccionCorreo : ICollectionFixture<EntornoCorreo>;

public sealed class EntornoCorreo : IAsyncLifetime
{
    public const string UsuarioSmtp = "shapi";
    public const string ContrasenaSmtp = "clave-de-prueba";

    public EntornoCorreo()
    {
        // Certificado autofirmado para el Mailpit que exige STARTTLS y autenticación.
        using var llave = RSA.Create(2048);
        var solicitud = new CertificateRequest("CN=localhost", llave, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var nombres = new SubjectAlternativeNameBuilder();
        nombres.AddDnsName("localhost");
        nombres.AddIpAddress(IPAddress.Loopback);
        solicitud.CertificateExtensions.Add(nombres.Build());
        using var certificado = solicitud.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        HuellaCertificado = certificado.GetCertHashString();

        MailpitSeguro = new ContainerBuilder("axllent/mailpit:v1.27")
            .WithResourceMapping(Encoding.ASCII.GetBytes(certificado.ExportCertificatePem()), "/certificados/cert.pem")
            .WithResourceMapping(Encoding.ASCII.GetBytes(llave.ExportPkcs8PrivateKeyPem()), "/certificados/llave.pem")
            .WithEnvironment("MP_SMTP_TLS_CERT", "/certificados/cert.pem")
            .WithEnvironment("MP_SMTP_TLS_KEY", "/certificados/llave.pem")
            .WithEnvironment("MP_SMTP_REQUIRE_STARTTLS", "true")
            .WithEnvironment("MP_SMTP_AUTH", $"{UsuarioSmtp}:{ContrasenaSmtp}")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(peticion => peticion
                .ForPort(8025)
                .ForPath("/api/v1/messages")))
            .Build();
    }

    /// <summary>La base de la colección, en el PostgreSQL compartido (JG-18). Se conoce al arrancar el contenedor.</summary>
    private string _cadena = null!;

    public IContainer Mailpit { get; } = new ContainerBuilder("axllent/mailpit:v1.27")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(peticion => peticion
            .ForPort(8025)
            .ForPath("/api/v1/messages")))
        .Build();

    /// <summary>Mailpit que exige STARTTLS y usuario y contraseña.</summary>
    public IContainer MailpitSeguro { get; }

    public string HuellaCertificado { get; }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(PostgresCompartido.IniciarAsync(), Mailpit.StartAsync(), MailpitSeguro.StartAsync());
        _cadena = PostgresCompartido.NuevaCadena("correo");
        await using var db = CrearDb();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            Mailpit.DisposeAsync().AsTask(),
            MailpitSeguro.DisposeAsync().AsTask());
    }

    public ShapiDbContext CrearDb()
    {
        var opciones = new DbContextOptionsBuilder<ShapiDbContext>()
            .UseNpgsql(_cadena)
            .Options;
        return new ShapiDbContext(opciones, new ContextoOrganizacionNulo());
    }

    public IConfiguration CrearConfiguracion(
        int? puertoSmtp = null,
        string usuario = "",
        string contrasena = "",
        bool tls = false) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_DOMINIO_BASE"] = "shapi.localhost",
            ["SHAPI_SMTP_HOST"] = "127.0.0.1",
            ["SHAPI_SMTP_PUERTO"] = (puertoSmtp ?? Mailpit.GetMappedPublicPort(1025)).ToString(),
            ["SHAPI_SMTP_USUARIO"] = usuario,
            ["SHAPI_SMTP_CONTRASENA"] = contrasena,
            ["SHAPI_SMTP_TLS"] = tls ? "true" : "false",
        })
        .Build();

    /// <summary>Busca en Mailpit el último mensaje para el destinatario y devuelve su detalle (HTML y texto).</summary>
    public static async Task<JsonElement> LeerMensaje(IContainer mailpit, string destinatario)
    {
        using var cliente = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{mailpit.GetMappedPublicPort(8025)}"),
        };
        var busqueda = JsonDocument.Parse(
            await cliente.GetStringAsync($"/api/v1/search?query={Uri.EscapeDataString($"to:{destinatario}")}")).RootElement;
        var mensajes = busqueda.GetProperty("messages").EnumerateArray().ToList();
        mensajes.Should().NotBeEmpty($"Mailpit debería tener un mensaje para {destinatario}");
        var id = mensajes[0].GetProperty("ID").GetString();
        return JsonDocument.Parse(await cliente.GetStringAsync($"/api/v1/message/{id}")).RootElement;
    }
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
        LeerDatos(correo).Should().NotContainKey("token");

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
    public async Task RF_46_CorreoEntregado_LlevaElEnlaceElHtmlEscapadoYLaParteDeTexto()
    {
        await using var db = entorno.CrearDb();
        db.Add(new CorreoSaliente(
            "verificacion_correo",
            "cuerpo@ejemplo.com",
            """{"nombre":"Ana <López>","token":"a+b&c","hostPortal":"envios.shapi.localhost"}""",
            "Verifique su correo",
            _reloj.Ahora));
        await db.SaveChangesAsync();

        await CrearProcesador(db, entorno.CrearConfiguracion()).ProcesarPendientes();

        var mensaje = await EntornoCorreo.LeerMensaje(entorno.Mailpit, "cuerpo@ejemplo.com");
        var html = mensaje.GetProperty("HTML").GetString();
        var texto = mensaje.GetProperty("Text").GetString();
        html.Should().Contain("https://envios.shapi.localhost/verificar-correo?token=a%2Bb%26c");
        html.Should().Contain("Ana &lt;L");
        html.Should().NotContain("Ana <López>");
        texto.Should().Contain("https://envios.shapi.localhost/verificar-correo?token=a%2Bb%26c");
        texto.Should().Contain("Ana <López>");
        texto.Should().Contain("usted");
    }

    [Fact]
    public async Task RF_46_SmtpConAutenticacionYStartTls_EntregaElCorreo()
    {
        await using var db = entorno.CrearDb();
        var correo = new CorreoSaliente(
            "recuperacion",
            "seguro@ejemplo.com",
            """{"nombre":"Ana","token":"token-1"}""",
            "Recupere su contraseña",
            _reloj.Ahora);
        db.Add(correo);
        await db.SaveChangesAsync();
        var configuracion = entorno.CrearConfiguracion(
            entorno.MailpitSeguro.GetMappedPublicPort(1025),
            EntornoCorreo.UsuarioSmtp,
            EntornoCorreo.ContrasenaSmtp,
            tls: true);

        await CrearProcesador(db, configuracion, CrearEnviadorQueConfiaEnMailpitSeguro(configuracion))
            .ProcesarPendientes();

        await db.Entry(correo).ReloadAsync();
        correo.Estado.Should().Be(EstadoCorreo.Enviado, correo.UltimoError);
        var mensaje = await EntornoCorreo.LeerMensaje(entorno.MailpitSeguro, "seguro@ejemplo.com");
        mensaje.GetProperty("Text").GetString().Should().Contain("https://shapi.localhost/restablecer?token=token-1");
    }

    [Theory]
    [InlineData("otra-clave", true)]
    [InlineData(EntornoCorreo.ContrasenaSmtp, false)]
    public async Task RF_46_SmtpConCredencialesInvalidasOSinTls_RegistraElFallo(string contrasena, bool tls)
    {
        await using var db = entorno.CrearDb();
        var correo = new CorreoSaliente(
            "recuperacion",
            "rechazado@ejemplo.com",
            """{"nombre":"Ana","token":"token-1"}""",
            "Recupere su contraseña",
            _reloj.Ahora);
        db.Add(correo);
        await db.SaveChangesAsync();
        var configuracion = entorno.CrearConfiguracion(
            entorno.MailpitSeguro.GetMappedPublicPort(1025),
            EntornoCorreo.UsuarioSmtp,
            contrasena,
            tls);

        await CrearProcesador(db, configuracion, CrearEnviadorQueConfiaEnMailpitSeguro(configuracion))
            .ProcesarPendientes();

        await db.Entry(correo).ReloadAsync();
        correo.Estado.Should().Be(EstadoCorreo.Pendiente);
        correo.Intentos.Should().Be(1);
        correo.UltimoError.Should().NotBeNullOrWhiteSpace();
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
        LeerDatos(correo).Should().ContainKey("token");
    }

    [Fact]
    public async Task RF_46_SeisIntentosFallidos_ElProcesadorLoDejaFallidoSinToken()
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
        var enviador = new EnviadorFalso(_ => throw new InvalidOperationException("SMTP caído"));
        var procesador = CrearProcesador(db, entorno.CrearConfiguracion(), enviador);
        var esperas = new[] { 5, 30, 120, 600, 3600 };

        for (var intento = 1; intento <= 6; intento++)
        {
            (await procesador.ProcesarPendientes()).Should().Be(1);
            await db.Entry(correo).ReloadAsync();
            correo.Intentos.Should().Be(intento);
            if (intento <= esperas.Length)
            {
                correo.Estado.Should().Be(EstadoCorreo.Pendiente);
                correo.ProximoIntentoEn.Should().Be(_reloj.Ahora.AddSeconds(esperas[intento - 1]));
                _reloj.Ahora = correo.ProximoIntentoEn!.Value;
            }
        }

        correo.Estado.Should().Be(EstadoCorreo.Fallido);
        correo.ProximoIntentoEn.Should().BeNull();
        correo.UltimoError.Should().Be("SMTP caído");
        LeerDatos(correo).Should().NotContainKey("token");
        _reloj.Ahora = _reloj.Ahora.AddDays(1);
        (await procesador.ProcesarPendientes()).Should().Be(0);
        enviador.Enviados.Should().BeEmpty();
    }

    [Theory]
    [InlineData("evil.com")]
    [InlineData("api.shapi.localhost")]
    public async Task RF_46_HostDelPortalInvalido_CuentaComoIntentoYNoSeEnvia(string hostPortal)
    {
        await using var db = entorno.CrearDb();
        var correo = new CorreoSaliente(
            "verificacion_correo",
            "ana@ejemplo.com",
            $$"""{"nombre":"Ana","token":"token-1","hostPortal":"{{hostPortal}}"}""",
            "Verifique su correo",
            _reloj.Ahora);
        db.Add(correo);
        await db.SaveChangesAsync();
        var enviador = new EnviadorFalso();

        await CrearProcesador(db, entorno.CrearConfiguracion(), enviador).ProcesarPendientes();

        await db.Entry(correo).ReloadAsync();
        enviador.Enviados.Should().BeEmpty();
        correo.Estado.Should().Be(EstadoCorreo.Pendiente);
        correo.Intentos.Should().Be(1);
        correo.UltimoError.Should().Contain("hostPortal");
    }

    [Fact]
    public async Task RF_46_DosProcesadoresALaVez_NoEnvianDosVecesElMismoCorreo()
    {
        await using (var db = entorno.CrearDb())
        {
            for (var indice = 0; indice < 6; indice++)
            {
                db.Add(new CorreoSaliente(
                    "recuperacion",
                    $"persona{indice}@ejemplo.com",
                    """{"nombre":"Ana","token":"token-1"}""",
                    "Recupere su contraseña",
                    _reloj.Ahora));
            }

            await db.SaveChangesAsync();
        }

        var enviador = new EnviadorFalso(_ => Task.Delay(TimeSpan.FromMilliseconds(200)));
        await using var db1 = entorno.CrearDb();
        await using var db2 = entorno.CrearDb();

        var resultados = await Task.WhenAll(
            CrearProcesador(db1, entorno.CrearConfiguracion(), enviador).ProcesarPendientes(),
            CrearProcesador(db2, entorno.CrearConfiguracion(), enviador).ProcesarPendientes());

        resultados.Sum().Should().Be(6);
        enviador.Enviados.Should().HaveCount(6).And.OnlyHaveUniqueItems();
        await using var verificacion = entorno.CrearDb();
        (await verificacion.Set<CorreoSaliente>().CountAsync(c => c.Estado == EstadoCorreo.Enviado)).Should().Be(6);
    }

    [Fact]
    public async Task RF_46_TrabajadorDetenidoDespuesDeEnviar_ElCorreoQuedaEnviado()
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
        using var detener = new CancellationTokenSource();
        var enviador = new EnviadorFalso(_ =>
        {
            detener.Cancel();
            return Task.CompletedTask;
        });

        try
        {
            await CrearProcesador(db, entorno.CrearConfiguracion(), enviador).ProcesarPendientes(detener.Token);
        }
        catch (OperationCanceledException)
        {
            // El trabajador se detiene; lo que importa es lo que quedó guardado.
        }

        await using var verificacion = entorno.CrearDb();
        var guardado = await verificacion.Set<CorreoSaliente>().SingleAsync(c => c.Id == correo.Id);
        guardado.Estado.Should().Be(EstadoCorreo.Enviado);
        enviador.Enviados.Should().ContainSingle();
    }

    [Fact]
    public async Task RF_46_ProximoIntentoFuturo_NoSeProcesaAntesDeTiempo()
    {
        await using var db = entorno.CrearDb();
        var correo = new CorreoSaliente(
            "recuperacion",
            "ana@ejemplo.com",
            """{"nombre":"Ana","token":"token-1"}""",
            "Recupere su contraseña",
            _reloj.Ahora.AddMinutes(1));
        db.Add(correo);
        await db.SaveChangesAsync();
        var procesador = CrearProcesador(db, entorno.CrearConfiguracion(1));

        var procesados = await procesador.ProcesarPendientes();

        procesados.Should().Be(0);
        await db.Entry(correo).ReloadAsync();
        correo.Estado.Should().Be(EstadoCorreo.Pendiente);
        correo.Intentos.Should().Be(0);
    }

    [Fact]
    public async Task RF_46_CorreoSaliente_TieneIndicePorEstadoYProximoIntento()
    {
        await using var db = entorno.CrearDb();

        var indices = await db.Database
            .SqlQuery<string>($"SELECT indexdef AS \"Value\" FROM pg_indexes WHERE tablename = 'correo_saliente'")
            .ToListAsync();

        indices.Should().Contain(definicion => definicion.Contains("(estado, proximo_intento_en)"));
    }

    [Fact]
    public void RF_46_Despachador_EjecutaCadaCincoSegundos()
    {
        DespachadorCorreos.Intervalo.Should().Be(TimeSpan.FromSeconds(5));
    }

    private ProcesadorCorreos CrearProcesador(
        ShapiDbContext db,
        IConfiguration configuracion,
        IEnviadorCorreo? enviador = null) => new(
        db,
        new MotorPlantillasCorreo(configuracion),
        enviador ?? new EnviadorSmtp(configuracion),
        _reloj,
        NullLogger<ProcesadorCorreos>.Instance);

    /// <summary>El certificado de <see cref="EntornoCorreo.MailpitSeguro"/> es autofirmado: solo se acepta ese.</summary>
    private EnviadorSmtp CrearEnviadorQueConfiaEnMailpitSeguro(IConfiguration configuracion) => new(
        configuracion,
        (_, certificado, _, _) => certificado?.GetCertHashString() == entorno.HuellaCertificado);

    private static Dictionary<string, JsonElement> LeerDatos(CorreoSaliente correo) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(correo.Datos)!;

    private sealed class EnviadorFalso(Func<CorreoSaliente, Task>? accion = null) : IEnviadorCorreo
    {
        public ConcurrentQueue<Guid> Enviados { get; } = new();

        public async Task EnviarAsync(
            CorreoSaliente correo,
            CorreoRenderizado contenido,
            CancellationToken cancelacion = default)
        {
            if (accion is not null)
            {
                await accion(correo);
            }

            Enviados.Enqueue(correo.Id);
        }
    }

    private sealed class RelojFalso(DateTimeOffset ahora) : Shapi.Aplicacion.Comun.IReloj
    {
        public DateTimeOffset Ahora { get; set; } = ahora;
    }
}
