using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Portal;

namespace Shapi.Api.Tests.Portal;

/// <summary>Cada prueba usa su propia base de datos en el PostgreSQL compartido (JG-18).</summary>
public sealed class ContenedorPostgresPortal : PostgresDePrueba;

public sealed class PortalTests(ContenedorPostgresPortal postgres)
    : IClassFixture<ContenedorPostgresPortal>, IAsyncLifetime
{
    private string _cadena = null!;
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;

    public Task InitializeAsync()
    {
        _cadena = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
        {
            Database = $"prueba_{Guid.NewGuid():N}",
        }.ConnectionString;

        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _cadena);
            builder.UseSetting("SHAPI_DOMINIO_BASE", "shapi.localhost");
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    [Fact]
    public async Task RF_15_IResolutorPortal_HostPublicado_DevuelveApiYOrganizacion()
    {
        // RF-15 · CA1: el portal se resuelve antes de conocer la organización.
        var apiId = await InsertarApi("envios", "publicada");

        using var alcance = _fabrica.Services.CreateScope();
        var resolutor = alcance.ServiceProvider.GetRequiredService<IResolutorPortal>();
        var portal = await resolutor.Resolver("envios.shapi.localhost");

        Assert.NotNull(portal);
        Assert.Equal(apiId, portal.ApiId);
        Assert.NotEqual(Guid.Empty, portal.OrganizacionId);
        Assert.Equal("Envíos Xelajú, S.A.", portal.NombreOrganizacion);
        Assert.Equal("envios", portal.Subdominio);
    }

    [Theory]
    [InlineData("despublicada", "envios.shapi.localhost")]
    [InlineData("publicada", "inexistente.shapi.localhost")]
    [InlineData("publicada", "envios.otro.localhost")]
    public async Task RF_14_IResolutorPortal_ApiNoDisponible_DevuelveVacio(string estado, string host)
    {
        // RF-14 · CA: una API despublicada o un host ajeno no tiene portal.
        await InsertarApi("envios", estado);

        using var alcance = _fabrica.Services.CreateScope();
        var resolutor = alcance.ServiceProvider.GetRequiredService<IResolutorPortal>();

        Assert.Null(await resolutor.Resolver(host));
    }

    [Fact]
    public async Task RF_15_Configuracion_Publicada_DevuelveMarcaApiYHosts()
    {
        // RF-15 y RF-16 · CA2: la configuración pública contiene la marca y los hosts canónicos.
        await InsertarApi("envios", "publicada", personalizar: true);

        using var respuesta = await Enviar("/api/portal/configuracion", "envios.shapi.localhost");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Envíos Xelajú", json.GetProperty("nombrePortal").GetString());
        Assert.Equal("#B8322A", json.GetProperty("colorPrincipal").GetString());
        Assert.Equal("/api/portal/logo", json.GetProperty("urlLogo").GetString());
        Assert.Equal("Cotice envíos a todo el país.", json.GetProperty("bienvenida").GetString());
        Assert.Equal("API de Cotización de Envíos", json.GetProperty("nombreApi").GetString());
        Assert.Equal("Cotizaciones, guías y rastreo.", json.GetProperty("descripcionApi").GetString());
        Assert.Equal("envios.shapi.localhost", json.GetProperty("hostPortal").GetString());
        Assert.Equal("envios.api.shapi.localhost", json.GetProperty("hostApi").GetString());
    }

    [Fact]
    public async Task RF_15_Configuracion_SinPersonalizacion_UsaNombreApiYSinLogo()
    {
        // RF-15 · CA2: el nombre de la API es el valor de respaldo y el logo es opcional.
        await InsertarApi("basica", "publicada");

        using var configuracion = await Enviar("/api/portal/configuracion", "basica.shapi.localhost");
        using var logo = await Enviar("/api/portal/logo", "basica.shapi.localhost");

        var json = await configuracion.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("API de Cotización de Envíos", json.GetProperty("nombrePortal").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("urlLogo").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, logo.StatusCode);
    }

    [Fact]
    public async Task RF_15_LogoSvg_ConservaTipoYAislaContenido()
    {
        // RF-15 · CA2: un SVG no puede ejecutar contenido en el origen del portal.
        var contenido = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");
        await InsertarApi("envios", "publicada", personalizar: true, logo: contenido, tipoLogo: "image/svg+xml");

        using var respuesta = await Enviar("/api/portal/logo", "envios.shapi.localhost");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("image/svg+xml", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(contenido, await respuesta.Content.ReadAsByteArrayAsync());
        Assert.Equal("sandbox", respuesta.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task RF_15_LogoPng_ConservaTipoContenido()
    {
        // RF-15 · CA2: el tipo almacenado gobierna la respuesta binaria.
        byte[] contenido = [0x89, 0x50, 0x4E, 0x47];
        await InsertarApi("envios", "publicada", personalizar: true, logo: contenido, tipoLogo: "image/png");

        using var respuesta = await Enviar("/api/portal/logo", "envios.shapi.localhost");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("image/png", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(contenido, await respuesta.Content.ReadAsByteArrayAsync());
        Assert.False(respuesta.Headers.Contains("Content-Security-Policy"));
        Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Theory]
    [InlineData("despublicada", "envios.shapi.localhost")]
    [InlineData("publicada", "inexistente.shapi.localhost")]
    public async Task RF_14_Endpoints_ApiNoDisponible_Responden404(string estado, string host)
    {
        // RF-14 · CA: ni la configuración ni el logo revelan una API despublicada o inexistente.
        await InsertarApi("envios", estado, personalizar: true);

        Assert.Equal(HttpStatusCode.NotFound, (await Enviar("/api/portal/configuracion", host)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar("/api/portal/logo", host)).StatusCode);
    }

    private async Task<Guid> InsertarApi(
        string subdominio,
        string estado,
        bool personalizar = false,
        byte[]? logo = null,
        string? tipoLogo = null)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            WITH nueva_organizacion AS (
                INSERT INTO organizacion (id, nombre, tipo, estado_admin)
                VALUES (gen_random_uuid(), 'Envíos Xelajú, S.A.', 'proveedor', 'activa')
                RETURNING id
            )
            INSERT INTO api (
                id, organizacion_id, nombre, subdominio, url_origen, estado,
                especificacion_descripcion, portal_nombre, portal_color, portal_logo,
                portal_logo_tipo, portal_bienvenida, secreto_origen_cifrado)
            SELECT
                gen_random_uuid(), id, 'API de Cotización de Envíos', @subdominio,
                'https://origen.ejemplo.com', @estado, 'Cotizaciones, guías y rastreo.',
                @nombrePortal, @color, @logo, @tipoLogo, @bienvenida, 'secreto'
            FROM nueva_organizacion
            RETURNING api.id
            """;
        comando.Parameters.AddWithValue("subdominio", subdominio);
        comando.Parameters.AddWithValue("estado", estado);
        comando.Parameters.AddWithValue("nombrePortal", personalizar ? "Envíos Xelajú" : DBNull.Value);
        comando.Parameters.AddWithValue("color", personalizar ? "#B8322A" : "#3B6FF0");
        object valorLogo = logo is not null
            ? logo
            : personalizar ? new byte[] { 0x89, 0x50, 0x4E, 0x47 } : DBNull.Value;
        object valorTipoLogo = tipoLogo is not null
            ? tipoLogo
            : personalizar ? "image/png" : DBNull.Value;
        comando.Parameters.AddWithValue("logo", valorLogo);
        comando.Parameters.AddWithValue("tipoLogo", valorTipoLogo);
        comando.Parameters.AddWithValue("bienvenida", personalizar ? "Cotice envíos a todo el país." : DBNull.Value);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private Task<HttpResponseMessage> Enviar(string ruta, string host)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        peticion.Headers.Host = host;
        return _cliente.SendAsync(peticion);
    }
}
