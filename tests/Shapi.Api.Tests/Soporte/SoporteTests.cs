using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Soporte;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using ApiDominio = Shapi.Dominio.Apis.Api;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Soporte;

public sealed class ContenedorPostgresSoporte : PostgresDePrueba;

public sealed class RelojSoporte : IReloj
{
    public DateTimeOffset Ahora { get; set; } = new(2026, 10, 7, 15, 0, 0, TimeSpan.Zero);
}

public class SoporteTests(ContenedorPostgresSoporte postgres) : IClassFixture<ContenedorPostgresSoporte>, IAsyncLifetime
{
    private const string Contrasena = "SuperSecreto123!";
    private readonly RelojSoporte _reloj = new();
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private string _cadena = null!;

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
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", Contrasena);
            builder.UseSetting("SHAPI_DOMINIO_BASE", "shapi.localhost");
            builder.ConfigureTestServices(services => services.AddSingleton<IReloj>(_reloj));
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    [Theory]
    [InlineData(Rol.Propietario)]
    [InlineData(Rol.Editor)]
    [InlineData(Rol.Lector)]
    public async Task RF_40_Proveedor_PuedeAbrirListarYConsultarSusCasos(Rol rol)
    {
        var (usuario, organizacion) = await CrearUsuario($"Persona {rol}", rol, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario($"Soporte {rol}", Rol.Soporte, TipoOrganizacion.Plataforma);
        var api = await CrearApi(organizacion.Id, $"api-{Guid.NewGuid():N}"[..20]);
        var cookie = await CrearSesion(usuario.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);

        var abrir = await Enviar(HttpMethod.Post, "/api/casos", cookie,
            new { asunto = "El dominio propio no verifica", apiId = api.Id, descripcion = "Sigue pendiente." });

        Assert.Equal(HttpStatusCode.Created, abrir.StatusCode);
        var creado = await abrir.Content.ReadFromJsonAsync<JsonElement>();
        var numero = creado.GetProperty("numero").GetInt32();
        Assert.True(numero >= 100);
        Assert.Single(creado.GetProperty("mensajes").EnumerateArray());

        var responder = await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/mensajes", cookieSoporte,
            new { cuerpo = "Estamos revisando el caso." });
        Assert.Equal(HttpStatusCode.Created, responder.StatusCode);

        var lista = await Enviar(HttpMethod.Get, "/api/casos", cookie);
        var pagina = await lista.Content.ReadFromJsonAsync<JsonElement>();
        var elementos = pagina.GetProperty("elementos");
        Assert.Single(elementos.EnumerateArray());
        Assert.Equal(1, pagina.GetProperty("total").GetInt32());
        Assert.Equal(api.Nombre, elementos[0].GetProperty("apiNombre").GetString());

        var otro = await CrearUsuario("Persona ajena", Rol.Propietario, TipoOrganizacion.Proveedor);
        var otroCookie = await CrearSesion(otro.Usuario.Id);
        var aislado = await Enviar(HttpMethod.Get, $"/api/casos/{numero}", otroCookie);
        await AfirmarProblema(aislado, HttpStatusCode.NotFound, "caso_no_encontrado");
        Assert.Equal("application/problem+json", aislado.Content.Headers.ContentType?.MediaType);

        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.True(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().AnyAsync(e => e.Accion == "caso.abierto"));
        var correos = await db.Set<CorreoSaliente>()
            .Where(c => c.Plantilla == "respuesta_caso" && c.Destinatario == usuario.Correo)
            .Select(c => c.Datos)
            .ToListAsync();
        Assert.Contains(correos, datos => datos.Contains($"CAS-{numero}", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Rol.Soporte)]
    [InlineData(Rol.Administrador)]
    public async Task RF_40_Soporte_AsignaRespondeYCierra_EncolaCorreoYRegistraBitacora(Rol rol)
    {
        var (propietaria, organizacion) = await CrearUsuario("Ana Lucia Morales", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Sofía Menchú", rol, TipoOrganizacion.Plataforma);
        var cookiePropietaria = await CrearSesion(propietaria.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);
        var abierto = await Enviar(HttpMethod.Post, "/api/casos", cookiePropietaria,
            new { asunto = "El dominio propio no verifica", descripcion = "Sigue pendiente." });
        var numero = (await abierto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/asignar", cookieSoporte)).StatusCode);
        var respuesta = await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/mensajes", cookieSoporte,
            new { cuerpo = "Cambie el destino CNAME." });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/cerrar", cookieSoporte)).StatusCode);

        var cerrado = await Enviar(HttpMethod.Post, $"/api/casos/{numero}/mensajes", cookiePropietaria,
            new { cuerpo = "Gracias." });
        var problemaCerrado = await AfirmarProblema(cerrado, HttpStatusCode.UnprocessableEntity, "caso_cerrado");
        Assert.Equal("El caso está cerrado.", problemaCerrado.GetProperty("title").GetString());
        await AfirmarProblema(await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/asignar", cookieSoporte),
            HttpStatusCode.UnprocessableEntity, "caso_cerrado");

        await using var db = Db(out var scope);
        using var _ = scope;
        var correo = await db.Set<CorreoSaliente>().SingleAsync(c =>
            c.Plantilla == "respuesta_caso" && c.Destinatario == propietaria.Correo);
        Assert.Equal(propietaria.Correo, correo.Destinatario);
        Assert.Contains($"CAS-{numero}", correo.Datos);
        Assert.True(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().AnyAsync(e => e.Accion == "caso.cerrado" && e.OrganizacionId == organizacion.Id));
        var entradas = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters()
            .Where(e => e.ObjetivoId == null || e.OrganizacionId == organizacion.Id).ToListAsync();
        var apertura = Assert.Single(entradas, e => e.Accion == "caso.abierto");
        Assert.Equal(propietaria.Nombre, apertura.ActorNombre);
        Assert.Equal($"Abrió el caso CAS-{numero} de {organizacion.Nombre}.", apertura.Descripcion);
        var cierre = Assert.Single(entradas, e => e.Accion == "caso.cerrado");
        Assert.Equal(soporte.Nombre, cierre.ActorNombre);
        Assert.Equal($"Cerró el caso CAS-{numero} de {organizacion.Nombre}.", cierre.Descripcion);
    }

    [Fact]
    public async Task RF_40_RF_42_Apertura_NotificaSoloATodoElSoporteActivoConNuevoCaso()
    {
        var (propietaria, _) = await CrearUsuario("Ana Lucía", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte1, _) = await CrearUsuario("Soporte uno", Rol.Soporte, TipoOrganizacion.Plataforma);
        var (soporte2, _) = await CrearUsuario("Soporte dos", Rol.Soporte, TipoOrganizacion.Plataforma);
        var (inactivo, _) = await CrearUsuario("Soporte inactivo", Rol.Soporte, TipoOrganizacion.Plataforma);
        await CrearUsuario("Administrador de casos", Rol.Administrador, TipoOrganizacion.Plataforma);
        await Desactivar(inactivo.Id);

        var respuesta = await Enviar(HttpMethod.Post, "/api/casos", await CrearSesion(propietaria.Id),
            new { asunto = "¿Qué ocurrió?", descripcion = "Descripción del caso." });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var numero = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();

        await using var db = Db(out var scope);
        using var _ = scope;
        var correos = await db.Set<CorreoSaliente>().Where(c => c.Plantilla == "nuevo_caso").ToListAsync();
        Assert.Equal(new[] { soporte1.Correo, soporte2.Correo }.Order(), correos.Select(c => c.Destinatario).Order());
        Assert.All(correos, c =>
        {
            Assert.Equal("Nuevo caso", c.Asunto);
            Assert.Contains($"CAS-{numero}", c.Datos);
            Assert.Contains($"https://shapi.localhost/admin/casos/{numero}", c.Datos);
        });
        Assert.False(await db.Set<CorreoSaliente>().AnyAsync(c => c.Plantilla == "respuesta_caso"));
    }

    [Theory]
    [InlineData(Rol.Soporte)]
    [InlineData(Rol.Administrador)]
    public async Task RF_40_RespuestaProveedor_NotificaSoloAlAsignadoActivo(Rol rol)
    {
        var (propietaria, _) = await CrearUsuario("Propietaria respuesta", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (asignado, _) = await CrearUsuario("Asignado", rol, TipoOrganizacion.Plataforma);
        await CrearUsuario("Otro soporte", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookie = await CrearSesion(propietaria.Id);
        var abierto = await Enviar(HttpMethod.Post, "/api/casos", cookie, new { asunto = "Respuesta", descripcion = "Inicio." });
        var numero = (await abierto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/asignar", await CrearSesion(asignado.Id))).StatusCode);

        var respuesta = await Enviar(HttpMethod.Post, $"/api/casos/{numero}/mensajes", cookie, new { cuerpo = "Nueva información." });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        var correo = Assert.Single(await db.Set<CorreoSaliente>().Where(c => c.Plantilla == "respuesta_caso").ToListAsync());
        Assert.Equal(asignado.Correo, correo.Destinatario);
        Assert.Contains($"CAS-{numero}", correo.Datos);

        await Desactivar(asignado.Id);
        Assert.Equal(HttpStatusCode.Created, (await Enviar(HttpMethod.Post, $"/api/casos/{numero}/mensajes", cookie, new { cuerpo = "Otra respuesta." })).StatusCode);
        Assert.Equal(1, await db.Set<CorreoSaliente>().CountAsync(c => c.Plantilla == "respuesta_caso"));
    }

    [Fact]
    public async Task RF_42_RespuestaPlataforma_NoNotificaAlProveedorDesactivado()
    {
        var (propietaria, _) = await CrearUsuario("Propietaria inactiva", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Soporte activo", Rol.Soporte, TipoOrganizacion.Plataforma);
        var abierto = await Enviar(HttpMethod.Post, "/api/casos", await CrearSesion(propietaria.Id), new { asunto = "Caso", descripcion = "Inicio." });
        var numero = (await abierto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();
        await Desactivar(propietaria.Id);

        Assert.Equal(HttpStatusCode.Created, (await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/mensajes", await CrearSesion(soporte.Id), new { cuerpo = "Respuesta." })).StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.False(await db.Set<CorreoSaliente>().AnyAsync(c => c.Destinatario == propietaria.Correo));
    }

    [Fact]
    public async Task RF_40_Validacion_MuestraDescripcionMaximoYOrganizacionConTildes()
    {
        var (propietaria, _) = await CrearUsuario("Propietaria validación", Rol.Propietario, TipoOrganizacion.Proveedor);
        var respuesta = await Enviar(HttpMethod.Post, "/api/casos", await CrearSesion(propietaria.Id),
            new { asunto = new string('x', 121), descripcion = new string('x', 4001), apiId = Guid.NewGuid() });
        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        var errores = problema.GetProperty("errores");
        Assert.Equal("El asunto es obligatorio y debe tener máximo 120 caracteres.", errores.GetProperty("asunto")[0].GetString());
        Assert.Equal("La descripción es obligatoria y debe tener máximo 4000 caracteres.", errores.GetProperty("descripcion")[0].GetString());
        Assert.Equal("La API no pertenece a la organización.", errores.GetProperty("apiId")[0].GetString());
    }

    private async Task Desactivar(Guid id)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == id);
        usuario.Desactivar();
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(Rol.Soporte)]
    [InlineData(Rol.Administrador)]
    public async Task RF_40_Administracion_RegistraCasoYConsultaResumenSoloLectura(Rol rol)
    {
        var (propietaria, organizacion) = await CrearUsuario("Ana Lucia Morales", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Sofía Menchú", rol, TipoOrganizacion.Plataforma);
        var api = await CrearApi(organizacion.Id, $"api-{Guid.NewGuid():N}"[..20]);
        var cookie = await CrearSesion(soporte.Id);

        var abrir = await Enviar(HttpMethod.Post, "/api/admin/casos", cookie,
            new { organizacionId = organizacion.Id, apiId = api.Id, asunto = "Caso telefonico", descripcion = "Detalle." });
        Assert.Equal(HttpStatusCode.Created, abrir.StatusCode);
        var numero = (await abrir.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();

        var resumen = await Enviar(HttpMethod.Get, $"/api/admin/casos/{numero}/organizacion", cookie);
        Assert.Equal(HttpStatusCode.OK, resumen.StatusCode);
        var json = await resumen.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(organizacion.Nombre, json.GetProperty("organizacion").GetString());
        Assert.Equal(api.Nombre, json.GetProperty("apiAfectada").GetString());
        Assert.True(json.GetProperty("numeroApis").GetInt32() >= 1);
        Assert.True(json.TryGetProperty("numeroConsumidores", out var numeroConsumidores));
        Assert.True(numeroConsumidores.GetInt32() >= 0);

        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.True(await db.Set<CorreoSaliente>().AnyAsync(c => c.Destinatario == propietaria.Correo && c.Plantilla == "nuevo_caso"));
    }

    [Fact]
    public async Task RF_40_Conversacion_ConservaMensajesDeUnMiembroRetirado()
    {
        var (propietaria, organizacion) = await CrearUsuario("Autora retirada", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Soporte historial", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookiePropietaria = await CrearSesion(propietaria.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);
        var abierto = await Enviar(HttpMethod.Post, "/api/casos", cookiePropietaria,
            new { asunto = "Conservar historial", descripcion = "Mensaje de la autora." });
        var numero = (await abierto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            await db.Set<Membresia>().IgnoreQueryFilters()
                .Where(m => m.UsuarioId == propietaria.Id && m.OrganizacionId == organizacion.Id)
                .ExecuteDeleteAsync();
        }

        var detalle = await Enviar(HttpMethod.Get, $"/api/admin/casos/{numero}", cookieSoporte);

        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        var mensaje = (await detalle.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mensajes")[0];
        Assert.Equal(propietaria.Nombre, mensaje.GetProperty("autor").GetString());
        Assert.Equal("exmiembro", mensaje.GetProperty("rol").GetString());
        Assert.False(mensaje.GetProperty("esPersonalPlataforma").GetBoolean());
    }

    [Fact]
    public async Task RF_40_Permisos_SeparanPanelProveedorYAdministracion()
    {
        var (propietario, _) = await CrearUsuario("Propietario", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Soporte", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookieProveedor = await CrearSesion(propietario.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(HttpMethod.Get, "/api/admin/casos", cookieProveedor)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(HttpMethod.Get, "/api/casos", cookieSoporte)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/admin/casos", cookieSoporte)).StatusCode);
    }

    [Fact]
    public async Task RF_40_RespuestaYCierreConcurrentes_NoDejanMensajesEnUnCasoYaCerrado()
    {
        var (propietaria, _) = await CrearUsuario("Propietaria concurrente", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Soporte concurrente", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookiePropietaria = await CrearSesion(propietaria.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);
        var abierto = await Enviar(HttpMethod.Post, "/api/casos", cookiePropietaria,
            new { asunto = "Caso concurrente", descripcion = "Primer mensaje." });
        var numero = (await abierto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();

        var responder = Enviar(HttpMethod.Post, $"/api/casos/{numero}/mensajes", cookiePropietaria,
            new { cuerpo = "Respuesta concurrente." });
        var cerrar = Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/cerrar", cookieSoporte);
        await Task.WhenAll(responder, cerrar);
        var respuestaConcurrente = await responder;
        var cierreConcurrente = await cerrar;

        Assert.Equal(HttpStatusCode.NoContent, cierreConcurrente.StatusCode);
        Assert.True(respuestaConcurrente.StatusCode is HttpStatusCode.Created or HttpStatusCode.UnprocessableEntity);
        if (respuestaConcurrente.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            await AfirmarProblema(respuestaConcurrente, HttpStatusCode.UnprocessableEntity, "caso_cerrado");
        }

        var detalle = await Enviar(HttpMethod.Get, $"/api/casos/{numero}", cookiePropietaria);
        var json = await detalle.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("cerrado", json.GetProperty("estado").GetString());
        Assert.Equal(respuestaConcurrente.StatusCode == HttpStatusCode.Created ? 2 : 1,
            json.GetProperty("mensajes").GetArrayLength());
    }

    [Fact]
    public async Task RF_40_Apertura_SiFallaElCorreo_RevierteCasoYBitacora()
    {
        var (propietaria, _) = await CrearUsuario("Propietaria atomica", Rol.Propietario, TipoOrganizacion.Proveedor);
        await CrearUsuario("Soporte atomico", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookie = await CrearSesion(propietaria.Id);
        var casosAntes = await Contar<Caso>();
        var bitacoraAntes = await Contar<EntradaBitacoraDominio>();
        await using var fabrica = _fabrica.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IColaCorreo>();
            services.AddScoped<IColaCorreo, ColaCorreoQueFalla>();
        }));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var respuesta = await Enviar(cliente, HttpMethod.Post, "/api/casos", cookie,
            new { asunto = "Debe revertirse", descripcion = "El correo falla." });

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal(casosAntes, await Contar<Caso>());
        Assert.Equal(bitacoraAntes, await Contar<EntradaBitacoraDominio>());
    }

    [Fact]
    public async Task RF_40_Cierre_SiFallaLaBitacora_ConservaElCasoAbierto()
    {
        var (propietaria, _) = await CrearUsuario("Propietaria cierre", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Soporte cierre", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookiePropietaria = await CrearSesion(propietaria.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);
        var abierto = await Enviar(HttpMethod.Post, "/api/casos", cookiePropietaria,
            new { asunto = "Cierre atomico", descripcion = "Debe seguir abierto." });
        var numero = (await abierto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();
        await using var fabrica = _fabrica.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBitacora>();
            services.AddScoped<IBitacora, BitacoraQueFalla>();
        }));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var respuesta = await Enviar(cliente, HttpMethod.Post, $"/api/admin/casos/{numero}/cerrar", cookieSoporte);

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        var detalle = await Enviar(HttpMethod.Get, $"/api/casos/{numero}", cookiePropietaria);
        var json = await detalle.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("abierto", json.GetProperty("estado").GetString());
    }

    private async Task<(Usuario Usuario, Organizacion Organizacion)> CrearUsuario(string nombre, Rol rol, TipoOrganizacion tipo)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var organizacion = tipo == TipoOrganizacion.Plataforma
            ? await db.Set<Organizacion>().IgnoreQueryFilters().SingleAsync(o => o.Tipo == tipo)
            : new Organizacion($"Organizacion de {nombre}", tipo);
        var usuario = new Usuario(nombre, $"{Guid.NewGuid():N}@ejemplo.test");
        usuario.DefinirHashContrasena(new PasswordHasher<Usuario>().HashPassword(usuario, Contrasena));
        if (tipo == TipoOrganizacion.Proveedor)
        {
            db.Add(organizacion);
        }

        db.AddRange(usuario, new Membresia(usuario.Id, organizacion.Id, rol));
        await db.SaveChangesAsync();
        return (usuario, organizacion);
    }

    private async Task<ApiDominio> CrearApi(Guid organizacionId, string subdominio)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var api = new ApiDominio(organizacionId, "API de Cotizacion", subdominio, "https://origen.test", "secreto", _reloj.Ahora);
        db.Add(api);
        await db.SaveChangesAsync();
        return api;
    }

    private async Task<string> CrearSesion(Guid usuarioId)
    {
        var valor = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        await using var db = Db(out var scope);
        using var _ = scope;
        db.Add(Sesion.IniciarPersonal(SeguridadTokens.HashearToken(valor), usuarioId, "localhost", null, null, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        return $"shapi_sesion={valor}";
    }

    private Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url, string cookie, object? cuerpo = null)
        => Enviar(_cliente, metodo, url, cookie, cuerpo);

    private static Task<HttpResponseMessage> Enviar(
        HttpClient cliente, HttpMethod metodo, string url, string cookie, object? cuerpo = null)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Add("Cookie", cookie);
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        return cliente.SendAsync(peticion);
    }

    private async Task<int> Contar<T>() where T : class
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        return await db.Set<T>().IgnoreQueryFilters().CountAsync();
    }

    private static async Task<JsonElement> AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(codigo, problema.GetProperty("codigo").GetString());
        return problema;
    }

    private ShapiDbContext Db(out IServiceScope scope)
    {
        scope = _fabrica.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
    }

    private sealed class ColaCorreoQueFalla : IColaCorreo
    {
        public Task Encolar(string plantilla, string destinatario, object datos, CancellationToken cancelacion = default) =>
            throw new InvalidOperationException("Fallo simulado al encolar.");
    }

    private sealed class BitacoraQueFalla : IBitacora
    {
        public Task Registrar(Shapi.Aplicacion.Comun.EntradaBitacora entrada, CancellationToken cancelacion = default) =>
            throw new InvalidOperationException("Fallo simulado al registrar.");
    }
}
