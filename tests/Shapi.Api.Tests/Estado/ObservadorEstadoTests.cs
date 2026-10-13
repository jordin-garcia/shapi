using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Persistencia;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Correo;
using Shapi.Infraestructura.Estado;
using Shapi.Infraestructura.Persistencia;
using StackExchange.Redis;
using ServidorHttp = Microsoft.AspNetCore.Hosting.Server.IServer;

namespace Shapi.Api.Tests.Estado;

[Collection(nameof(RedisCache))]
public sealed class ObservadorEstadoTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis)
{
    // RF-39: se consulta PostgreSQL real y SMTP real; no se simula la persistencia de la cola ni los lotes.
    [Fact]
    public async Task RF_39_Sondas_LeenColaFallosConsolidacionYConfiguracion()
    {
        await using var smtp = new SmtpDePrueba();
        await using var db = CrearDb();
        var observador = Crear(db, smtp.Puerto);
        var normal = await observador.ObservarAsync(CancellationToken.None);
        Assert.NotNull(normal.PostgresMs);
        Assert.NotNull(normal.ApiMs);
        Assert.True(normal.SmtpResponde);
        Assert.False(normal.CorreosAntiguos);
        Assert.False(normal.FallosCorreoRecientes);
        Assert.False(normal.MetricasPendientes);
        Assert.True(normal.PasarelaFalla);
        // La cola justo en el límite de dos minutos no degrada; un segundo después sí.
        var correo = new CorreoSaliente("verificacion", "persona@ejemplo.test", "{}", "Asunto", Reloj.Ahora);
        db.Add(correo);
        await db.SaveChangesAsync();
        Reloj.Ahora = Reloj.Ahora.AddMinutes(2);
        Assert.False((await observador.ObservarAsync(CancellationToken.None)).CorreosAntiguos);
        Reloj.Ahora = Reloj.Ahora.AddSeconds(1);
        var antiguo = await observador.ObservarAsync(CancellationToken.None);
        Assert.True(antiguo.CorreosAntiguos);
        Assert.Equal(correo.CreadoEn, antiguo.IncidenciaCorreoDesde);
        correo.MarcarEnviado(Reloj.Ahora);
        await db.SaveChangesAsync();
        var fallido = new CorreoSaliente("verificacion", "otra@ejemplo.test", "{}", "Asunto", Reloj.Ahora);
        fallido.RegistrarFallo("Fallo simulado", Reloj.Ahora);
        db.Add(fallido);
        await db.SaveChangesAsync();
        Assert.True((await observador.ObservarAsync(CancellationToken.None)).FallosCorreoRecientes);
        Reloj.Ahora = Reloj.Ahora.AddSeconds(121);
        // Un fallo antiguo no es reciente, aunque el correo todavía puede seguir pendiente.
        Assert.False((await observador.ObservarAsync(CancellationToken.None)).FallosCorreoRecientes);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO lote_consolidado (lote_id, procesado_en) VALUES ({Guid.NewGuid()}, {Reloj.Ahora.AddSeconds(-60)})");
        Assert.Equal(60, (await observador.ObservarAsync(CancellationToken.None)).AtrasoConsolidacionSegundos);
    }

    [Fact]
    public async Task RF_39_ComponentesCaidos_NoImpidenConsultarLosOtros()
    {
        // Un puerto local cerrado para SMTP y PostgreSQL: no toca ningún servicio del usuario.
        using var escucha = new TcpListener(IPAddress.Loopback, 0);
        escucha.Start();
        var puerto = ((IPEndPoint)escucha.LocalEndpoint).Port;
        escucha.Stop();
        var opciones = new DbContextOptionsBuilder<ShapiDbContext>()
            .UseNpgsql($"Host=127.0.0.1;Port={puerto};Database=no_existe;Username=prueba;Password=prueba;Timeout=1").Options;
        await using var db = new ShapiDbContext(opciones, Contexto);
        var caido = await Crear(db, puerto).ObservarAsync(CancellationToken.None);
        Assert.Null(caido.PostgresMs);
        Assert.False(caido.SmtpResponde);
        Assert.NotNull(caido.RedisMs);
        Assert.NotNull(caido.ApiMs);
    }

    [Fact]
    public async Task RF_39_RedisCaido_DevuelveEstadoSinLatidosNiLatencia()
    {
        var opciones = ConfigurationOptions.Parse("127.0.0.1:1,abortConnect=false,connectTimeout=100,asyncTimeout=100");
        opciones.BacklogPolicy = BacklogPolicy.FailFast;
        using var conexion = await ConnectionMultiplexer.ConnectAsync(opciones);
        var salud = await new LectorEstadoRedis(conexion, TimeProvider.System).LeerAsync(CancellationToken.None);
        Assert.Null(salud.Milisegundos);
        Assert.Null(salud.Latencia.Milisegundos);
        Assert.False(salud.Compuerta);
        Assert.False(salud.Trabajador);
    }

    private ObservadorEstado Crear(ShapiDbContext db, int puerto)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_SMTP_HOST"] = "127.0.0.1",
            ["SHAPI_SMTP_PUERTO"] = puerto.ToString(),
            ["SHAPI_PASARELA_FALLA"] = "true",
        }).Build();
        var servidor = Substitute.For<ServidorHttp>();
        var caracteristicas = new FeatureCollection();
        var direcciones = new ServerAddressesFeature();
        direcciones.Addresses.Add("http://+:5080");
        caracteristicas.Set<IServerAddressesFeature>(direcciones);
        servidor.Features.Returns(caracteristicas);
        var clientes = Substitute.For<IHttpClientFactory>();
        clientes.CreateClient("salud-api").Returns(_ => new HttpClient(new SaludApi()));
        return new(db, RedisCache.Conexion, Reloj, TimeProvider.System, configuracion, clientes, servidor,
            NullLogger<ObservadorEstado>.Instance);
    }

    private sealed class SaludApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("http://localhost:5080/salud", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class SmtpDePrueba : IAsyncDisposable
    {
        private readonly TcpListener _escucha = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cancelacion = new();
        private readonly Task _trabajo;
        public int Puerto => ((IPEndPoint)_escucha.LocalEndpoint).Port;
        public SmtpDePrueba()
        {
            _escucha.Start();
            _trabajo = AtenderAsync();
        }
        private async Task AtenderAsync()
        {
            try
            {
                while (!_cancelacion.IsCancellationRequested)
                {
                    using var conexion = await _escucha.AcceptTcpClientAsync(_cancelacion.Token);
                    using var lector = new StreamReader(conexion.GetStream(), Encoding.ASCII);
                    await using var escritor = new StreamWriter(conexion.GetStream(), Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
                    await escritor.WriteLineAsync("220 prueba ESMTP");
                    while (await lector.ReadLineAsync(_cancelacion.Token) is { } linea)
                    {
                        if (linea == "QUIT") { await escritor.WriteLineAsync("221 adios"); break; }
                        await escritor.WriteLineAsync("250 correcto");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
        public async ValueTask DisposeAsync()
        {
            await _cancelacion.CancelAsync();
            await _trabajo;
            _escucha.Stop();
            _cancelacion.Dispose();
        }
    }
}
