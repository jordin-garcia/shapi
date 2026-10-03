using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Claves;
using Shapi.Dominio.Consumo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Soporte;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Apis;
using Shapi.Infraestructura.Persistencia;
using Shapi.Infraestructura.Siembra.Demo;
using ApiDominio = Shapi.Dominio.Apis.Api;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Siembra;

// RNF-14 · La demostración se puede reconstruir de forma determinista antes de cada presentación.
[Collection(nameof(PostgresPersistencia))]
public sealed class SiembraDemoTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    [Fact]
    public void RNF_14_UsaUrlsLocalesEnDesarrolloYDeServicioEnProduccion()
    {
        ConfiguracionSiembraDemo.ResolverUrls(null, null, false).Should().Be(
            ("http://localhost:5101", "http://localhost:5102"));
        ConfiguracionSiembraDemo.ResolverUrls(null, null, true).Should().Be(
            ("http://origen-envios:8080", "http://origen-agro:8080"));
        ConfiguracionSiembraDemo.ResolverUrls("http://envios-personalizado", "http://agro-personalizado", true)
            .Should().Be(("http://envios-personalizado", "http://agro-personalizado"));

        var composeProduccion = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../..", "infra", "compose.prod.yml")));
        composeProduccion.Should().Contain("SHAPI_URL_ORIGEN_ENVIOS: ${SHAPI_URL_ORIGEN_ENVIOS:-http://origen-envios:8080}");
        composeProduccion.Should().Contain("SHAPI_URL_ORIGEN_AGRO: ${SHAPI_URL_ORIGEN_AGRO:-http://origen-agro:8080}");
        composeProduccion.Should().Contain("SHAPI_SECRETO_ORIGEN_ENVIOS: ${SHAPI_SECRETO_ORIGEN_ENVIOS:-}");
        composeProduccion.Should().Contain("SHAPI_SECRETO_ORIGEN_AGRO: ${SHAPI_SECRETO_ORIGEN_AGRO:-}");
    }

    [Fact]
    public async Task RNF_14_SinModoDemo_RechazaLaSiembra()
    {
        await using var db = CrearDb();
        var siembra = CrearSiembra(db);

        var accion = () => siembra.EjecutarAsync(false, false, "http://envios", "http://agro");

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SHAPI_MODO_DEMO=true*");
    }

    [Fact]
    public async Task RNF_14_EjecutadaDosVeces_NoDuplicaDatos()
    {
        await using var db = CrearDb();
        var siembra = CrearSiembra(db);

        await siembra.EjecutarAsync(true, false, "http://envios", "http://agro");
        var primera = await Conteos(db);
        await siembra.EjecutarAsync(true, false, "http://envios", "http://agro");

        (await Conteos(db)).Should().BeEquivalentTo(primera);
    }

    [Fact]
    public async Task RNF_14_Reiniciar_RecreaSoloLosDatosDeDemostracion()
    {
        await using var db = CrearDb();
        var siembra = CrearSiembra(db);
        await siembra.EjecutarAsync(true, false, "http://envios", "http://agro");
        var enviosAnterior = await db.Set<Organizacion>().SingleAsync(o => o.Nombre == "Envíos Xelajú, S.A.");
        var plataformaAnterior = await db.Set<Organizacion>().SingleAsync(o => o.Tipo == TipoOrganizacion.Plataforma);
        db.Add(new EntradaBitacoraDominio(
            Reloj.Ahora, ActorTipo.Sistema, null, "Sistema", enviosAnterior.Id,
            "demostracion.usada", null, null, "Acción real durante la demostración", null, null));
        db.Add(Token.InvitacionConsumidor(
            new string('a', 64), enviosAnterior.Id, "pendiente@demostracion.test", Reloj.Ahora));
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        Reloj.Ahora = Reloj.Ahora.AddDays(1);
        await siembra.EjecutarAsync(true, true, "http://envios", "http://agro");

        var enviosNuevo = await db.Set<Organizacion>().SingleAsync(o => o.Nombre == "Envíos Xelajú, S.A.");
        enviosNuevo.Id.Should().Be(enviosAnterior.Id);
        (await db.Set<Organizacion>().SingleAsync(o => o.Tipo == TipoOrganizacion.Plataforma)).Id
            .Should().Be(plataformaAnterior.Id);
        (await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().CountAsync()).Should().Be(23);
        (await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters()
            .SingleAsync(e => e.Accion == "demostracion.usada")).OrganizacionId.Should().Be(enviosAnterior.Id);
        (await db.Set<Token>().IgnoreQueryFilters().CountAsync(t => t.OrganizacionId == enviosAnterior.Id))
            .Should().Be(0);
    }

    [Fact]
    public async Task RNF_14_Reiniciar_SiFallaLaRecreacion_ConservaLaSiembraAnterior()
    {
        await using var db = CrearDb();
        await CrearSiembra(db).EjecutarAsync(true, false, "http://envios", "http://agro");
        var enviosAnterior = await db.Set<Organizacion>()
            .SingleAsync(o => o.Nombre == "Envíos Xelajú, S.A.");
        db.ChangeTracker.Clear();
        var siembraQueFalla = new SiembraDemo(
            db,
            Reloj,
            new ProtectorQueFalla(),
            NullLogger<SiembraDemo>.Instance);

        var accion = () => siembraQueFalla.EjecutarAsync(true, true, "http://envios", "http://agro");

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fallo simulado al cifrar*");
        db.ChangeTracker.Clear();
        (await db.Set<Organizacion>().SingleAsync(o => o.Nombre == "Envíos Xelajú, S.A.")).Id
            .Should().Be(enviosAnterior.Id);
    }

    [Fact]
    public async Task RNF_14_CreaLasMuestrasObligatoriasYLasClavesExactas()
    {
        await using var db = CrearDb();
        await CrearSiembra(db).EjecutarAsync(true, false, "http://envios", "http://agro");

        var envios = await db.Set<Organizacion>().SingleAsync(o => o.Nombre == "Envíos Xelajú, S.A.");
        (await db.Set<ApiDominio>().IgnoreQueryFilters().CountAsync(a => a.OrganizacionId == envios.Id)).Should().Be(2);

        var mercadito = await db.Set<Consumidor>().IgnoreQueryFilters()
            .SingleAsync(c => c.NombreEmpresa == "Mercadito Antigua");
        var suscripcion = await db.Set<SuscripcionApi>().IgnoreQueryFilters()
            .SingleAsync(s => s.ConsumidorId == mercadito.Id);
        var claves = await db.Set<Clave>().IgnoreQueryFilters()
            .Where(c => c.SuscripcionId == suscripcion.Id && c.Estado == EstadoClave.Activa)
            .OrderBy(c => c.Tipo)
            .ToListAsync();
        claves.Should().HaveCount(2);
        claves.Should().Contain(c => c.HashSha256 == GeneradorClave.CalcularHash("shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e"));
        claves.Should().Contain(c => c.HashSha256 == GeneradorClave.CalcularHash("shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d"));

        string[] clavesFijas =
        [
            "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e",
            "shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d",
            "shp_prod_BoutiqueCayalaDemo00004b80",
            "shp_prod_BoutiqueCayalaRotada00e513",
            "shp_prueba_BoutiqueCayalaDem0000031c7",
            "shp_prod_FerreteriaZona11Demo0090fb",
            "shp_prueba_FerreteriaZona11De00005e28",
            "shp_prod_TiendaSololaDemo000000b3a4",
            "shp_prueba_TiendaSololaDemo000000d6f1",
            "shp_prod_AgroPreciosDemo0000000a7f2",
            "shp_prueba_AgroPreciosDemo00000004c8d",
        ];
        foreach (var clave in clavesFijas)
        {
            var prefijo = clave.StartsWith("shp_prod_", StringComparison.Ordinal) ? "shp_prod_" : "shp_prueba_";
            var cuerpo = clave.Substring(prefijo.Length);
            cuerpo.Should().HaveLength(26);
            cuerpo.All(char.IsAsciiLetterOrDigit).Should().BeTrue();
        }
        var hashesSembrados = await db.Set<Clave>().IgnoreQueryFilters().Select(c => c.HashSha256).ToListAsync();
        hashesSembrados.Should().BeEquivalentTo(clavesFijas.Select(GeneradorClave.CalcularHash));

        (await db.Set<Caso>().IgnoreQueryFilters().SingleAsync(c => c.Numero == 104)).Asunto
            .Should().Be("El dominio propio no verifica");
    }

    [Fact]
    public async Task RNF_14_CreaElCatalogoCompletoDe07Seccion6ConLaContrasenaDemo()
    {
        await using var db = CrearDb();
        await CrearSiembra(db).EjecutarAsync(true, false, "http://envios", "http://agro");

        var proveedores = await db.Set<Organizacion>().IgnoreQueryFilters()
            .Where(o => o.Tipo == TipoOrganizacion.Proveedor).ToListAsync();
        proveedores.Select(o => o.Nombre).Should().BeEquivalentTo(
            "Envíos Xelajú, S.A.", "Agro Precios, S.A.", "Cafetalera del Altiplano, S.A.",
            "Transportes Petén, S.A.", "Datos Chapines, S.A.");
        proveedores.Single(o => o.Nombre == "Datos Chapines, S.A.").EstadoAdmin.Should().Be(EstadoAdmin.Suspendida);

        var suscripcionesPlataforma = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().ToListAsync();
        suscripcionesPlataforma.Should().ContainSingle(s => s.Estado == EstadoSuscripcion.EnGracia);
        suscripcionesPlataforma.Should().ContainSingle(s => s.Estado == EstadoSuscripcion.Suspendida);

        var envios = await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Subdominio == "envios");
        var rutas = await db.Set<Ruta>().IgnoreQueryFilters().Where(r => r.ApiId == envios.Id).ToListAsync();
        rutas.Should().HaveCount(5);
        rutas.Single(r => r.Patron == "/guias").PesoLlamadas.Should().Be(5);
        rutas.Single(r => r.Patron == "/tarifas").Expuesta.Should().BeFalse();
        rutas.Single(r => r.Patron == "/rastreo").Should().Match<Ruta>(r =>
            r.LimiteMinuto == 300 && r.CacheSegundos == 30 && r.PesoLlamadas == 1);
        rutas.Single(r => r.Patron == "/cobertura").Should().Match<Ruta>(r =>
            r.LimiteMinuto == 120 && r.CacheSegundos == 3_600 && r.PesoLlamadas == 1);
        envios.PortalNombre.Should().Be("Envíos Xelajú");
        envios.PortalColor.Should().Be("#B8322A");
        envios.PortalBienvenida.Should().Be("Cotice y genere guías de envío a todo Guatemala desde su tienda en línea.");
        var dominio = await db.Set<DominioPropio>().IgnoreQueryFilters().SingleAsync(d => d.ApiId == envios.Id);
        dominio.Dominio.Should().Be("api.enviosxelaju.localhost");
        dominio.Estado.Should().Be(EstadoDominio.Pendiente);

        var archivoOrigen = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../..", "origenes-demo", "envios-xelaju", "cotizacion-envios.yaml"));
        envios.Especificacion.Should().Be((await File.ReadAllTextAsync(archivoOrigen)).ReplaceLineEndings("\n"));
        var especificacionReal = await new LectorEspecificacionOpenApi().Leer(
            await File.ReadAllTextAsync(archivoOrigen), "cotizacion-envios.yaml");
        rutas.Select(r => (r.Metodo, r.Patron)).Should().BeEquivalentTo(
            especificacionReal.Especificacion!.Operaciones.Select(o => (o.Metodo, o.Patron)));

        var agro = await db.Set<ApiDominio>().IgnoreQueryFilters().SingleAsync(a => a.Subdominio == "agro");
        var archivoAgro = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../..", "origenes-demo", "agro-precios", "openapi.yaml"));
        agro.Especificacion.Should().Be((await File.ReadAllTextAsync(archivoAgro)).ReplaceLineEndings("\n"));
        agro.PortalColor.Should().Be("#3F7021");
        agro.PortalBienvenida.Should().Be("Consulte los precios del día en los mercados mayoristas de Guatemala.");
        var rutasAgro = await db.Set<Ruta>().IgnoreQueryFilters().Where(r => r.ApiId == agro.Id).ToListAsync();
        rutasAgro.Single(r => r.Patron == "/historial").PesoLlamadas.Should().Be(3);

        (await db.Set<PlanApi>().IgnoreQueryFilters().Where(p => p.ApiId == envios.Id).Select(p => p.Nombre).ToListAsync())
            .Should().BeEquivalentTo("Básico", "Comercio", "Volumen");
        (await db.Set<PlanApi>().IgnoreQueryFilters().Where(p => p.ApiId == agro.Id)
            .Select(p => new { p.Nombre, p.Precio, p.CuotaLlamadas, p.LimiteMinuto }).ToListAsync())
            .Should().BeEquivalentTo(new[]
            {
                new { Nombre = "Consulta", Precio = 99m, CuotaLlamadas = 3_000L, LimiteMinuto = 20 },
                new { Nombre = "Mayorista", Precio = 350m, CuotaLlamadas = 30_000L, LimiteMinuto = 60 },
                new { Nombre = "Integración", Precio = 900m, CuotaLlamadas = 200_000L, LimiteMinuto = 300 },
            });
        var consumidores = await db.Set<Consumidor>().IgnoreQueryFilters().ToListAsync();
        consumidores.Should().HaveCount(5);
        consumidores.Should().Contain(c => c.Nombre == "María José Quiñónez" && c.NombreEmpresa == "Mercadito Antigua");
        consumidores.Should().Contain(c => c.Nombre == "Andrea Xiloj" && c.NombreEmpresa == "Distribuidora San Lucas");
        var claveRotada = await db.Set<Clave>().IgnoreQueryFilters().SingleAsync(c => c.Estado == EstadoClave.Rotada
            && c.Ultimos4 == "e513");
        var eventoRotacion = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters()
            .SingleAsync(e => e.Accion == "clave.rotada");
        (claveRotada.ExpiraEn - eventoRotacion.Fecha).Should().Be(Clave.VigenciaTrasRotar);
        (await db.Set<Caso>().IgnoreQueryFilters().OrderBy(c => c.Numero).Select(c => c.Numero).ToListAsync())
            .Should().Equal(100, 101, 102, 103, 104);
        (await db.Set<Pago>().IgnoreQueryFilters().CountAsync()).Should().Be(12);
        (await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().CountAsync()).Should().Be(11);

        var hasherUsuarios = new PasswordHasher<Usuario>();
        foreach (var usuario in await db.Set<Usuario>().IgnoreQueryFilters().ToListAsync())
        {
            hasherUsuarios.VerifyHashedPassword(usuario, usuario.HashContrasena!, SiembraDemo.Contrasena)
                .Should().NotBe(PasswordVerificationResult.Failed);
        }
        var hasherConsumidores = new PasswordHasher<Consumidor>();
        foreach (var consumidor in await db.Set<Consumidor>().IgnoreQueryFilters().ToListAsync())
        {
            hasherConsumidores.VerifyHashedPassword(consumidor, consumidor.HashContrasena, SiembraDemo.Contrasena)
                .Should().NotBe(PasswordVerificationResult.Failed);
        }
    }

    [Fact]
    public async Task RNF_14_ReproducePagosCasosYBitacoraDeLosMockups()
    {
        await using var db = CrearDb();
        Reloj.Ahora = new DateTimeOffset(2026, 9, 27, 18, 0, 0, TimeSpan.Zero);
        await CrearSiembra(db).EjecutarAsync(true, false, "http://envios", "http://agro");

        var organizaciones = await db.Set<Organizacion>().IgnoreQueryFilters()
            .ToDictionaryAsync(o => o.Id, o => o.Nombre);
        var suscripciones = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters()
            .ToDictionaryAsync(s => s.Id, s => organizaciones[s.OrganizacionId]);
        var medios = await db.Set<MedioPago>().IgnoreQueryFilters().ToDictionaryAsync(m => m.Id);
        medios.Values.Should().Contain(m => m.OrganizacionId == organizaciones.Single(o => o.Value == "Envíos Xelajú, S.A.").Key
            && m.Marca == MarcaTarjeta.Visa && m.Ultimos4 == "4821");
        medios.Values.Should().Contain(m => m.ConsumidorId != null && m.Marca == MarcaTarjeta.Mastercard && m.Ultimos4 == "3057");

        var pagos = await db.Set<Pago>().IgnoreQueryFilters()
            .Where(p => p.SuscripcionPlataformaId != null)
            .OrderByDescending(p => p.CreadoEn)
            .Select(p => new
            {
                Organizacion = suscripciones[p.SuscripcionPlataformaId!.Value],
                p.Monto,
                p.Estado,
                Tarjeta = medios[p.MedioPagoId!.Value].Marca + " " + medios[p.MedioPagoId.Value].Ultimos4,
                p.CreadoEn,
            })
            .ToListAsync();
        pagos.Should().BeEquivalentTo(new[]
        {
            new { Organizacion = "Transportes Petén, S.A.", Monto = 199m, Estado = EstadoPago.Rechazado, Tarjeta = "Mastercard 7744", CreadoEn = Reloj.Ahora.AddDays(-2) },
            new { Organizacion = "Envíos Xelajú, S.A.", Monto = 173.34m, Estado = EstadoPago.Autorizado, Tarjeta = "Visa 4821", CreadoEn = Reloj.Ahora.AddDays(-3) },
            new { Organizacion = "Datos Chapines, S.A.", Monto = 199m, Estado = EstadoPago.Rechazado, Tarjeta = "Visa 9052", CreadoEn = Reloj.Ahora.AddDays(-9) },
            new { Organizacion = "Agro Precios, S.A.", Monto = 199m, Estado = EstadoPago.Autorizado, Tarjeta = "Mastercard 3312", CreadoEn = Reloj.Ahora.AddDays(-12) },
            new { Organizacion = "Envíos Xelajú, S.A.", Monto = 199m, Estado = EstadoPago.Autorizado, Tarjeta = "Visa 4821", CreadoEn = Reloj.Ahora.AddDays(-20) },
            new { Organizacion = "Transportes Petén, S.A.", Monto = 199m, Estado = EstadoPago.Autorizado, Tarjeta = "Mastercard 7744", CreadoEn = Reloj.Ahora.AddDays(-32) },
            new { Organizacion = "Datos Chapines, S.A.", Monto = 199m, Estado = EstadoPago.Revertido, Tarjeta = "Visa 9052", CreadoEn = Reloj.Ahora.AddDays(-39) },
        }, opciones => opciones.WithStrictOrdering());

        var fechasCasos = await db.Set<Caso>().IgnoreQueryFilters().ToDictionaryAsync(c => c.Numero, c => c.CreadoEn);
        fechasCasos.Should().BeEquivalentTo(new Dictionary<int, DateTimeOffset>
        {
            [100] = Reloj.Ahora.AddDays(-11),
            [101] = Reloj.Ahora.AddDays(-8),
            [102] = Reloj.Ahora.AddDays(-4),
            [103] = Reloj.Ahora.AddDays(-3),
            [104] = Reloj.Ahora.AddDays(-2),
        });
        var mensajesPorCaso = await db.Set<CasoMensaje>().IgnoreQueryFilters()
            .GroupBy(m => m.CasoId).ToDictionaryAsync(g => g.Key, g => g.OrderBy(m => m.CreadoEn).ToList());
        foreach (var caso in await db.Set<Caso>().IgnoreQueryFilters().ToListAsync())
        {
            mensajesPorCaso[caso.Id].Should().NotBeEmpty();
        }
        mensajesPorCaso[(await db.Set<Caso>().IgnoreQueryFilters().SingleAsync(c => c.Numero == 104)).Id]
            .Select(m => m.Cuerpo).Should().Equal(
                $"El dominio api.enviosxelaju.localhost sigue pendiente de verificación desde el {Reloj.Ahora.AddDays(-17).ToString("d 'de' MMMM", System.Globalization.CultureInfo.GetCultureInfo("es-GT"))}. Ya creé el registro CNAME que me indicó la pantalla de dominios.",
                "Gracias, Ana Lucía. Estoy revisando el registro en el DNS y le escribo en cuanto tenga el resultado.");

        var pagosConPeriodo = await db.Set<Pago>().IgnoreQueryFilters().ToListAsync();
        pagosConPeriodo.Should().OnlyContain(p => p.PeriodoInicio != null && p.PeriodoFin != null);
        pagosConPeriodo.Should().Contain(p => p.Descripcion == "Suscripción al plan Comercio");
        pagosConPeriodo.Should().Contain(p => p.Descripcion == "Lanzamiento → Producto · diferencia prorrateada");

        var bitacora = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters()
            .OrderByDescending(e => e.Fecha)
            .Select(e => new { e.ActorNombre, e.Descripcion })
            .ToListAsync();
        bitacora.Should().BeEquivalentTo(new[]
        {
            new { ActorNombre = "Rodrigo Alvarado", Descripcion = "Suspendió la organización Datos Chapines, S.A." },
            new { ActorNombre = "Sofía Menchú Cojtí", Descripcion = "Abrió el caso CAS-104 de Envíos Xelajú, S.A." },
            new { ActorNombre = "Boutique Cayalá", Descripcion = "Rotó su clave de producción en la API de Cotización de Envíos" },
            new { ActorNombre = "Rodrigo Alvarado", Descripcion = "Desactivó la cuenta de plataforma de Julio Estrada Ixcot" },
            new { ActorNombre = "Rodrigo Alvarado", Descripcion = "Revirtió el cobro de Q 199.00 de Datos Chapines, S.A." },
            new { ActorNombre = "Ana Lucía Morales", Descripcion = "Cambió su suscripción de plataforma de Lanzamiento a Producto" },
            new { ActorNombre = "Ana Lucía Morales", Descripcion = "Revocó la clave de pruebas de Tienda Sololá en la API de Cotización de Envíos" },
            new { ActorNombre = "Diego Us Pérez", Descripcion = "Ocultó la ruta GET /tarifas de la API de Cotización de Envíos" },
            new { ActorNombre = "Ana Lucía Morales", Descripcion = "Invitó a karla.batres@enviosxelaju.com con el rol de lector" },
            new { ActorNombre = "Sofía Menchú Cojtí", Descripcion = "Cerró el caso CAS-101 de Cafetalera del Altiplano, S.A." },
            new { ActorNombre = "Rodrigo Alvarado", Descripcion = "Creó la cuenta de plataforma de Lucía Ramírez Pineda con el rol de administrador" },
        }, opciones => opciones.WithStrictOrdering());
    }

    [Fact]
    public async Task RNF_14_UsaUrlsConfiguradasYFechasRelativasAlReloj()
    {
        await using var db = CrearDb();
        Reloj.Ahora = new DateTimeOffset(2026, 9, 27, 2, 0, 0, TimeSpan.Zero);
        await CrearSiembra(db).EjecutarAsync(true, false, "http://origen-envios:8080", "http://origen-agro:8080",
            "secreto-envios", "secreto-agro");

        var apis = await db.Set<ApiDominio>().IgnoreQueryFilters().ToListAsync();
        apis.Single(a => a.Subdominio == "envios").UrlOrigen.Should().Be("http://origen-envios:8080");
        apis.Single(a => a.Subdominio == "agro").UrlOrigen.Should().Be("http://origen-agro:8080");
        apis.Single(a => a.Subdominio == "envios").SecretoOrigenCifrado.Should().Be("cifrado:secreto-envios");
        apis.Single(a => a.Subdominio == "agro").SecretoOrigenCifrado.Should().Be("cifrado:secreto-agro");

        var mercadito = await db.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(c => c.NombreEmpresa == "Mercadito Antigua");
        var suscripcion = await db.Set<SuscripcionApi>().IgnoreQueryFilters().SingleAsync(s => s.ConsumidorId == mercadito.Id);
        suscripcion.Inicio.Should().Be(Suscripcion.InicioDeCiclo(Reloj.Ahora.AddDays(-21)));

        var consumos = await db.Set<ConsumoDiario>().IgnoreQueryFilters().ToListAsync();
        consumos.Select(c => c.Fecha).Distinct().Should().HaveCount(30)
            .And.Contain(new DateOnly(2026, 8, 28))
            .And.Contain(new DateOnly(2026, 9, 26));
        consumos.Should().OnlyContain(c => c.HistLatenciaTotal.Sum() == c.Peticiones
            && c.HistLatenciaCompuerta.Sum() == c.Peticiones);

        var hoyGuatemala = DateOnly.FromDateTime(Reloj.Ahora.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var patrones = await db.Set<Ruta>().IgnoreQueryFilters().ToDictionaryAsync(r => r.Id, r => r.Patron);
        var consumosMercadito = consumos.Where(c => c.SuscripcionId == suscripcion.Id).ToList();
        consumosMercadito.GroupBy(c => patrones[c.RutaId!.Value])
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Peticiones))
            .Should().BeEquivalentTo(new Dictionary<string, long>
            {
                ["/cotizaciones"] = 15_120,
                ["/rastreo"] = 11_340,
                ["/guias"] = 892,
                ["/cobertura"] = 560,
            });
        consumosMercadito.Where(c => c.Fecha >= hoyGuatemala.AddDays(-21))
            .Sum(c => c.Llamadas).Should().Be(31_480);
        consumosMercadito.Where(c => c.Fecha < hoyGuatemala.AddDays(-21))
            .Should().OnlyContain(c => c.Peticiones == 0 && c.Llamadas == 0);

        var diasB1 = Enumerable.Range(-12, 10).Select(d => hoyGuatemala.AddDays(d)).ToArray();
        consumos.Where(c => diasB1.Contains(c.Fecha)).GroupBy(c => c.Fecha)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Peticiones))
            .Should().BeEquivalentTo(diasB1.Zip(new long[]
            {
                3_180, 3_640, 3_910, 3_036, 13_720, 11_460, 16_280, 18_700, 15_140, 16_390,
            }).ToDictionary(x => x.First, x => x.Second));
        consumos.Where(c => diasB1.Contains(c.Fecha)).GroupBy(c => patrones[c.RutaId!.Value])
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Peticiones))
            .Should().BeEquivalentTo(new Dictionary<string, long>
            {
                ["/cotizaciones"] = 50_620,
                ["/rastreo"] = 37_964,
                ["/guias"] = 9_492,
                ["/cobertura"] = 7_380,
            });

        var consumidores = await db.Set<Consumidor>().IgnoreQueryFilters().ToDictionaryAsync(c => c.Id, c => c.NombreEmpresa);
        var suscripciones = await db.Set<SuscripcionApi>().IgnoreQueryFilters()
            .Where(s => consumidores.Keys.Contains(s.ConsumidorId)).ToDictionaryAsync(s => s.Id, s => consumidores[s.ConsumidorId]);
        consumos.GroupBy(c => suscripciones[c.SuscripcionId!.Value])
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Llamadas))
            .Should().BeEquivalentTo(new Dictionary<string, long>
            {
                ["Mercadito Antigua"] = 31_480,
                ["Boutique Cayalá"] = 4_116,
                ["Ferretería Zona 11"] = 69_600,
                ["Tienda Sololá"] = 1_436,
            });

        var periodoB1 = consumos.Where(c => diasB1.Contains(c.Fecha)).ToList();
        periodoB1.Sum(c => c.BytesEntrada).Should().Be(310_000_000);
        periodoB1.Sum(c => c.BytesSalida).Should().Be(1_530_000_000);
        periodoB1.Sum(c => c.Rechazos401).Should().Be(476);
        periodoB1.Sum(c => c.Rechazos403).Should().Be(74);
        periodoB1.Sum(c => c.Rechazos429).Should().Be(1_476);
        (periodoB1.Sum(c => c.Origen4xx) + periodoB1.Sum(c => c.Origen5xx)).Should().Be(316);
        CalcularP95(periodoB1.Select(c => c.HistLatenciaTotal)).Should().BeInRange(235, 237);
        CalcularP95(periodoB1.Select(c => c.HistLatenciaCompuerta)).Should().BeInRange(10, 12);
        var p95PorRuta = periodoB1.GroupBy(c => patrones[c.RutaId!.Value]).ToDictionary(
            g => g.Key,
            g => (Total: CalcularP95(g.Select(c => c.HistLatenciaTotal)),
                Compuerta: CalcularP95(g.Select(c => c.HistLatenciaCompuerta))));
        p95PorRuta.Should().BeEquivalentTo(new Dictionary<string, (int Total, int Compuerta)>
        {
            ["/cotizaciones"] = (214, 12),
            ["/rastreo"] = (142, 9),
            ["/guias"] = (388, 11),
            ["/cobertura"] = (96, 8),
        });
    }

    private static int CalcularP95(IEnumerable<int[]> histogramas)
    {
        int[] limites = [5, 10, 25, 50, 100, 250, 500, 1_000, 2_500];
        var histograma = histogramas.Aggregate(new long[10], (suma, actual) =>
        {
            for (var i = 0; i < suma.Length; i++)
            {
                suma[i] += actual[i];
            }
            return suma;
        });
        var objetivo = histograma.Sum() * 0.95;
        long acumulado = 0;
        for (var i = 0; i < limites.Length; i++)
        {
            var anterior = acumulado;
            acumulado += histograma[i];
            if (acumulado >= objetivo)
            {
                var inferior = i == 0 ? 0 : limites[i - 1];
                var proporcion = histograma[i] == 0 ? 0 : (objetivo - anterior) / histograma[i];
                return (int)Math.Round(inferior + proporcion * (limites[i] - inferior), MidpointRounding.AwayFromZero);
            }
        }
        return 2_501;
    }

    private SiembraDemo CrearSiembra(ShapiDbContext db) => new(
        db,
        Reloj,
        new ProtectorPrueba(),
        NullLogger<SiembraDemo>.Instance);

    private static async Task<object> Conteos(ShapiDbContext db)
    {
        db.ChangeTracker.Clear();
        return new
        {
            Organizaciones = await db.Set<Organizacion>().IgnoreQueryFilters().CountAsync(),
            Usuarios = await db.Set<Usuario>().IgnoreQueryFilters().CountAsync(),
            Consumidores = await db.Set<Consumidor>().IgnoreQueryFilters().CountAsync(),
            Apis = await db.Set<ApiDominio>().IgnoreQueryFilters().CountAsync(),
            Claves = await db.Set<Clave>().IgnoreQueryFilters().CountAsync(),
            Casos = await db.Set<Caso>().IgnoreQueryFilters().CountAsync(),
            Consumos = await db.Set<ConsumoDiario>().IgnoreQueryFilters().CountAsync(),
        };
    }

    private sealed class ProtectorPrueba : IProtectorSecretoOrigen
    {
        public string Cifrar(string secreto) => $"cifrado:{secreto}";
        public string Descifrar(string secretoCifrado) => secretoCifrado[8..];
    }

    private sealed class ProtectorQueFalla : IProtectorSecretoOrigen
    {
        public string Cifrar(string secreto) => throw new InvalidOperationException("Fallo simulado al cifrar el secreto.");
        public string Descifrar(string secretoCifrado) => throw new NotSupportedException();
    }
}
