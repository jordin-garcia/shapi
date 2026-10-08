using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using Shapi.Infraestructura.Siembra.Base;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Infraestructura.Siembra.Demo;

/// <summary>Carga los datos relativos al día de ejecución que usan los mockups (RNF-14, 07 §6).</summary>
public sealed class SiembraDemo(
    ShapiDbContext db,
    IReloj reloj,
    IProtectorSecretoOrigen protector,
    ILogger<SiembraDemo> registro)
{
    public const string Contrasena = "Shapi2026!demo";

    /// <summary>Las cuentas del personal y de los proveedores de la demostración.</summary>
    private static readonly string[] CorreosUsuariosDemo =
    [
        "rodrigo.alvarado@shapi.localhost", "lucia.ramirez@shapi.localhost", "sofia.menchu@shapi.localhost",
        "julio.estrada@shapi.localhost", "ana.morales@enviosxelaju.com", "diego.us@enviosxelaju.com",
        "karla.batres@enviosxelaju.com", "carlos.tzul@agroprecios.com", "lucia.cotom@cafetaleraaltiplano.com",
        "mario.pop@transportespeten.com", "gabriela.sac@datoschapines.com"
    ];

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
    ];

    private static readonly string[] OrganizacionesDemo =
    [
        "Envíos Xelajú, S.A.", "Agro Precios, S.A.", "Cafetalera del Altiplano, S.A.",
        "Transportes Petén, S.A.", "Datos Chapines, S.A."
    ];

    public async Task<bool> EjecutarAsync(
        bool modoDemo,
        bool reiniciar,
        string urlOrigenEnvios,
        string urlOrigenAgro,
        string? secretoOrigenEnvios = null,
        string? secretoOrigenAgro = null,
        CancellationToken cancelacion = default)
    {
        if (!modoDemo)
        {
            throw new InvalidOperationException("La siembra de demostración solo funciona con SHAPI_MODO_DEMO=true.");
        }

        await SiembraBase.EjecutarAsync(
            db, null, null, null, reloj, new PasswordHasher<Usuario>(), registro);

        if (!reiniciar && await db.Set<Organizacion>().IgnoreQueryFilters()
            .AnyAsync(o => o.Nombre == OrganizacionesDemo[0], cancelacion))
        {
            registro.LogInformation("La siembra de demostración ya existe; no se hicieron cambios.");
            return false;
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        if (reiniciar)
        {
            await BorrarDatosDemo(cancelacion);
        }

        var ahora = reloj.Ahora;
        var hashUsuarios = new PasswordHasher<Usuario>();
        var hashConsumidores = new PasswordHasher<Consumidor>();

        var plataforma = await db.Set<Organizacion>().IgnoreQueryFilters()
            .SingleAsync(o => o.Tipo == TipoOrganizacion.Plataforma, cancelacion);
        // Si ya existen (un reinicio), se restablecen en vez de crearse otra vez (auditoría 2026-10-03, H-29).
        var usuariosExistentes = await db.Set<Usuario>().IgnoreQueryFilters()
            .Where(u => CorreosUsuariosDemo.Contains(u.Correo))
            .ToDictionaryAsync(u => u.Correo, cancelacion);
        var personal = new[]
        {
            CrearUsuario("Rodrigo Alvarado", "rodrigo.alvarado@shapi.localhost", hashUsuarios, ahora, usuariosExistentes),
            CrearUsuario("Lucía Ramírez Pineda", "lucia.ramirez@shapi.localhost", hashUsuarios, ahora, usuariosExistentes),
            CrearUsuario("Sofía Menchú Cojtí", "sofia.menchu@shapi.localhost", hashUsuarios, ahora, usuariosExistentes),
            CrearUsuario("Julio Estrada Ixcot", "julio.estrada@shapi.localhost", hashUsuarios, ahora, usuariosExistentes, desactivado: true),
        };
        db.AddRange(Nuevas(personal));
        db.AddRange(
            new Membresia(personal[0].Id, plataforma.Id, Rol.Administrador),
            new Membresia(personal[1].Id, plataforma.Id, Rol.Administrador),
            new Membresia(personal[2].Id, plataforma.Id, Rol.Soporte),
            new Membresia(personal[3].Id, plataforma.Id, Rol.Soporte));

        var organizacionesExistentes = await db.Set<Organizacion>().IgnoreQueryFilters()
            .Where(o => OrganizacionesDemo.Contains(o.Nombre))
            .ToDictionaryAsync(o => o.Nombre, cancelacion);
        var envios = ObtenerOCrearOrganizacion(OrganizacionesDemo[0], organizacionesExistentes);
        var agro = ObtenerOCrearOrganizacion(OrganizacionesDemo[1], organizacionesExistentes);
        var cafetalera = ObtenerOCrearOrganizacion(OrganizacionesDemo[2], organizacionesExistentes);
        var peten = ObtenerOCrearOrganizacion(OrganizacionesDemo[3], organizacionesExistentes);
        var datos = ObtenerOCrearOrganizacion(OrganizacionesDemo[4], organizacionesExistentes);
        foreach (var organizacion in new[] { envios, agro, cafetalera, peten, datos })
        {
            Poner(organizacion, nameof(Organizacion.EstadoAdmin), EstadoAdmin.Activa);
            Poner(organizacion, nameof(Organizacion.MotivoSuspension), null);
        }

        Poner(datos, nameof(Organizacion.EstadoAdmin), EstadoAdmin.Suspendida);
        Poner(datos, nameof(Organizacion.MotivoSuspension), "Cobros pendientes y verificación administrativa");
        db.AddRange(new[] { envios, agro, cafetalera, peten, datos }.Where(o => db.Entry(o).State == EntityState.Detached));

        var propietarios = new[]
        {
            CrearUsuario("Ana Lucía Morales", "ana.morales@enviosxelaju.com", hashUsuarios, ahora, usuariosExistentes),
            CrearUsuario("Carlos Tzul", "carlos.tzul@agroprecios.com", hashUsuarios, ahora, usuariosExistentes),
            CrearUsuario("Lucía Cotom", "lucia.cotom@cafetaleraaltiplano.com", hashUsuarios, ahora, usuariosExistentes),
            CrearUsuario("Mario Pop", "mario.pop@transportespeten.com", hashUsuarios, ahora, usuariosExistentes),
            CrearUsuario("Gabriela Sac", "gabriela.sac@datoschapines.com", hashUsuarios, ahora, usuariosExistentes),
        };
        var diego = CrearUsuario("Diego Us Pérez", "diego.us@enviosxelaju.com", hashUsuarios, ahora, usuariosExistentes);
        var karla = CrearUsuario("Karla Batres", "karla.batres@enviosxelaju.com", hashUsuarios, ahora, usuariosExistentes);
        db.AddRange(Nuevas(propietarios));
        db.AddRange(Nuevas([diego, karla]));
        var organizaciones = new[] { envios, agro, cafetalera, peten, datos };
        for (var i = 0; i < organizaciones.Length; i++)
        {
            db.Add(new Membresia(propietarios[i].Id, organizaciones[i].Id, Rol.Propietario));
        }
        db.AddRange(
            new Membresia(diego.Id, envios.Id, Rol.Editor),
            new Membresia(karla.Id, envios.Id, Rol.Lector));
        await db.SaveChangesAsync(cancelacion);

        var mediosOrganizacion = new[]
        {
            CrearMedioPago(envios.Id, null, MarcaTarjeta.Visa, "4821", "Ana Lucía Morales", ahora),
            CrearMedioPago(agro.Id, null, MarcaTarjeta.Mastercard, "3312", "Carlos Tzul", ahora),
            CrearMedioPago(cafetalera.Id, null, MarcaTarjeta.Visa, "6180", "Lucía Cotom", ahora),
            CrearMedioPago(peten.Id, null, MarcaTarjeta.Mastercard, "7744", "Mario Pop", ahora),
            CrearMedioPago(datos.Id, null, MarcaTarjeta.Visa, "9052", "Gabriela Sac", ahora),
        };
        db.AddRange(mediosOrganizacion);

        var planesPlataforma = await db.Set<PlanPlataforma>().IgnoreQueryFilters()
            .ToDictionaryAsync(p => p.Nombre, cancelacion);
        // 07 §6: los ciclos y los pagos de plataforma siguen A6.2, A6.3 y B1.3, cuyo «hoy» es el 13 de septiembre.
        // Cada pago cubre el ciclo que empieza ese día; una renovación rechazada, el ciclo siguiente.
        var suscripcionesPlataforma = new[]
        {
            CrearSuscripcionPlataforma(envios.Id, planesPlataforma["Producto"].Id, EstadoSuscripcion.Activa, ahora.AddDays(-20), 30, mediosOrganizacion[0].Id),
            CrearSuscripcionPlataforma(agro.Id, planesPlataforma["Lanzamiento"].Id, EstadoSuscripcion.Activa, ahora.AddDays(-12), 30, mediosOrganizacion[1].Id),
            CrearSuscripcionPlataforma(cafetalera.Id, planesPlataforma["Prueba"].Id, EstadoSuscripcion.Activa, ahora.AddDays(-16), 30, mediosOrganizacion[2].Id),
            CrearSuscripcionPlataforma(peten.Id, planesPlataforma["Lanzamiento"].Id, EstadoSuscripcion.EnGracia, ahora.AddDays(-32), 30, mediosOrganizacion[3].Id, Suscripcion.InicioDeCiclo(ahora.AddDays(-32)).AddDays(30 + 7)),
            CrearSuscripcionPlataforma(datos.Id, planesPlataforma["Lanzamiento"].Id, EstadoSuscripcion.Suspendida, ahora.AddDays(-39), 30, mediosOrganizacion[4].Id),
        };
        db.AddRange(suscripcionesPlataforma);

        var apiEnvios = CrearApi(envios.Id, "API de Cotización de Envíos", "envios", urlOrigenEnvios,
            secretoOrigenEnvios, EstadoApi.Publicada, ahora, "Envíos Xelajú", "#B8322A",
            "Cotice y genere guías de envío a todo Guatemala desde su tienda en línea.");
        var apiRecolecciones = CrearApi(envios.Id, "API de Recolecciones", "recolecciones", urlOrigenEnvios,
            secretoOrigenEnvios, EstadoApi.Despublicada, ahora);
        var apiAgro = CrearApi(agro.Id, "API de Precios de Mercado", "agro", urlOrigenAgro,
            secretoOrigenAgro, EstadoApi.Publicada, ahora, "Agro Precios", "#3F7021",
            "Consulte los precios del día en los mercados mayoristas de Guatemala.");
        // A6.2 cuenta 1, 2 y 1 APIs para Cafetalera, Petén y Datos Chapines (auditoría 2026-10-03, H-31).
        var apisOtras = new[]
        {
            CrearApi(cafetalera.Id, "API de Lotes de Café", "lotes-cafe", urlOrigenAgro, null, EstadoApi.Borrador, ahora),
            CrearApi(peten.Id, "API de Rutas de Carga", "rutas-carga", urlOrigenEnvios, null, EstadoApi.Borrador, ahora),
            CrearApi(peten.Id, "API de Seguimiento de Flota", "seguimiento-flota", urlOrigenEnvios, null, EstadoApi.Borrador, ahora),
            CrearApi(datos.Id, "API de Datos Demográficos", "datos-demograficos", urlOrigenAgro, null, EstadoApi.Borrador, ahora),
        };
        db.AddRange(apiEnvios, apiRecolecciones, apiAgro);
        db.AddRange(apisOtras);
        db.Add(Crear<DominioPropio>(
            (nameof(DominioPropio.Id), Guid.CreateVersion7()), (nameof(DominioPropio.ApiId), apiEnvios.Id),
            (nameof(DominioPropio.Dominio), "api.enviosxelaju.localhost"),
            (nameof(DominioPropio.DestinoCname), "envios.api.shapi.localhost"),
            (nameof(DominioPropio.Estado), EstadoDominio.Pendiente),
            (nameof(DominioPropio.Motivo), null),
            (nameof(DominioPropio.UltimoIntentoEn), ahora.AddHours(-3))));
        await db.SaveChangesAsync(cancelacion);

        var rutasEnvios = await CrearRutas(apiEnvios, EspecificacionesDemo.Envios, "envios.yaml", ahora, cancelacion);
        ConfigurarRuta(rutasEnvios, "/cotizaciones", true, 120, 0, 1, ahora);
        ConfigurarRuta(rutasEnvios, "/guias", true, 60, 0, 5, ahora);
        ConfigurarRuta(rutasEnvios, "/tarifas", false, 300, 300, 1, ahora);
        ConfigurarRuta(rutasEnvios, "/rastreo", true, 300, 30, 1, ahora);
        ConfigurarRuta(rutasEnvios, "/cobertura", true, 120, 3_600, 1, ahora);
        var rutasAgro = await CrearRutas(apiAgro, EspecificacionesDemo.Agro, "agro.yaml", ahora, cancelacion);
        foreach (var ruta in rutasAgro)
        {
            ConfigurarRuta([ruta], ruta.Patron, true, 120, ruta.Metodo == MetodoHttp.Get ? 300 : 0,
                ruta.Patron == "/historial" ? 3 : 1, ahora);
        }
        db.AddRange(rutasEnvios);
        db.AddRange(rutasAgro);

        var basico = PlanApi.Crear(Guid.CreateVersion7(), apiEnvios.Id, "Básico", "Para tiendas que empiezan a vender en línea.", 149m, false, 30, 5_000, 30);
        var comercio = PlanApi.Crear(Guid.CreateVersion7(), apiEnvios.Id, "Comercio", "Para tiendas con envíos todos los días.", 450m, false, 30, 50_000, 120);
        var volumen = PlanApi.Crear(Guid.CreateVersion7(), apiEnvios.Id, "Volumen", "Para operaciones con alto volumen de guías.", 1_200m, false, 30, 500_000, 600);
        var consulta = PlanApi.Crear(Guid.CreateVersion7(), apiAgro.Id, "Consulta", "Para comercios que consultan pocos productos.", 99m, false, 30, 3_000, 20);
        var mayorista = PlanApi.Crear(Guid.CreateVersion7(), apiAgro.Id, "Mayorista", "Para distribuidores que consultan precios a diario.", 350m, false, 30, 30_000, 60);
        var integracion = PlanApi.Crear(Guid.CreateVersion7(), apiAgro.Id, "Integración", "Para sistemas que muestran precios a sus clientes.", 900m, false, 30, 200_000, 300);
        db.AddRange(basico, comercio, volumen, consulta, mayorista, integracion);
        await db.SaveChangesAsync(cancelacion);

        var consumidores = new[]
        {
            CrearConsumidor(envios.Id, "María José Quiñónez", "Mercadito Antigua", "mariajose@mercaditoantigua.com", hashConsumidores, ahora),
            CrearConsumidor(envios.Id, "Isabel Herrera", "Boutique Cayalá", "isabel@boutiquecayala.com", hashConsumidores, ahora),
            CrearConsumidor(envios.Id, "Óscar Méndez", "Ferretería Zona 11", "oscar@ferreteriazona11.com", hashConsumidores, ahora),
            CrearConsumidor(envios.Id, "Rosa Choc", "Tienda Sololá", "rosa@tiendasolola.com", hashConsumidores, ahora),
            CrearConsumidor(agro.Id, "Andrea Xiloj", "Distribuidora San Lucas", "andrea@distribuidorasl.com", hashConsumidores, ahora),
        };
        db.AddRange(consumidores);
        var mediosConsumidor = new[]
        {
            CrearMedioPago(null, consumidores[0].Id, MarcaTarjeta.Mastercard, "3057", "María José Quiñónez", ahora),
            CrearMedioPago(null, consumidores[1].Id, MarcaTarjeta.Visa, "1184", "Isabel Herrera", ahora),
            CrearMedioPago(null, consumidores[2].Id, MarcaTarjeta.Mastercard, "6639", "Óscar Méndez", ahora),
            CrearMedioPago(null, consumidores[3].Id, MarcaTarjeta.Visa, "2946", "Rosa Choc", ahora),
            CrearMedioPago(null, consumidores[4].Id, MarcaTarjeta.Mastercard, "8201", "Andrea Xiloj", ahora),
        };
        db.AddRange(mediosConsumidor);
        var suscripcionesApi = new[]
        {
            CrearSuscripcionApi(consumidores[0].Id, apiEnvios.Id, comercio.Id, ahora.AddDays(-21), 30, mediosConsumidor[0].Id),
            CrearSuscripcionApi(consumidores[1].Id, apiEnvios.Id, basico.Id, ahora.AddDays(-25), 30, mediosConsumidor[1].Id),
            CrearSuscripcionApi(consumidores[2].Id, apiEnvios.Id, volumen.Id, ahora.AddDays(-17), 30, mediosConsumidor[2].Id),
            CrearSuscripcionApi(consumidores[3].Id, apiEnvios.Id, basico.Id, ahora.AddDays(-13), 30, mediosConsumidor[3].Id),
            CrearSuscripcionApi(consumidores[4].Id, apiAgro.Id, mayorista.Id, ahora.AddDays(-10), 30, mediosConsumidor[4].Id),
        };
        db.AddRange(suscripcionesApi);
        await db.SaveChangesAsync(cancelacion);

        var claves = new[]
        {
            ClaveConocida(suscripcionesApi[0].Id, TipoClave.Produccion, "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e", ahora),
            ClaveConocida(suscripcionesApi[0].Id, TipoClave.Pruebas, "shp_prueba_Jp5sX1cV8nB3yG7tQe2Kv0a19d", ahora),
            ClaveConocida(suscripcionesApi[1].Id, TipoClave.Produccion, "shp_prod_BoutiqueCayalaDemo00004b80", ahora),
            ClaveConocida(suscripcionesApi[1].Id, TipoClave.Produccion, "shp_prod_BoutiqueCayalaRotada00e513", ahora, EstadoClave.Rotada),
            ClaveConocida(suscripcionesApi[1].Id, TipoClave.Pruebas, "shp_prueba_BoutiqueCayalaDem0000031c7", ahora),
            ClaveConocida(suscripcionesApi[2].Id, TipoClave.Produccion, "shp_prod_FerreteriaZona11Demo0090fb", ahora),
            ClaveConocida(suscripcionesApi[2].Id, TipoClave.Pruebas, "shp_prueba_FerreteriaZona11De00005e28", ahora),
            ClaveConocida(suscripcionesApi[3].Id, TipoClave.Produccion, "shp_prod_TiendaSololaDemo000000b3a4", ahora),
            ClaveConocida(suscripcionesApi[3].Id, TipoClave.Pruebas, "shp_prueba_TiendaSololaDemo000000d6f1", ahora, EstadoClave.Revocada, ahora.AddDays(-3).AddHours(-8)),
            ClaveConocida(suscripcionesApi[4].Id, TipoClave.Produccion, "shp_prod_AgroPreciosDemo0000000a7f2", ahora),
            ClaveConocida(suscripcionesApi[4].Id, TipoClave.Pruebas, "shp_prueba_AgroPreciosDemo00000004c8d", ahora),
        };
        db.AddRange(claves);

        db.AddRange(
            CrearPago(suscripcionesApi[0], mediosConsumidor[0].Id, "Suscripción al plan Comercio", 450m, ahora.AddDays(-21)),
            CrearPago(suscripcionesApi[1], mediosConsumidor[1].Id, "Suscripción al plan Básico", 149m, ahora.AddDays(-25)),
            CrearPago(suscripcionesApi[2], mediosConsumidor[2].Id, "Suscripción al plan Volumen", 1_200m, ahora.AddDays(-17)),
            CrearPago(suscripcionesApi[3], mediosConsumidor[3].Id, "Suscripción al plan Básico", 149m, ahora.AddDays(-13)),
            CrearPago(suscripcionesApi[4], mediosConsumidor[4].Id, "Suscripción al plan Mayorista", 350m, ahora.AddDays(-10)));
        db.AddRange(
            CrearPagoPlataforma(suscripcionesPlataforma[3], mediosOrganizacion[3].Id, "Lanzamiento", 199m, EstadoPago.Rechazado, ahora.AddDays(-2), periodoInicio: suscripcionesPlataforma[3].Fin),
            CrearPagoPlataforma(suscripcionesPlataforma[0], mediosOrganizacion[0].Id, "Lanzamiento → Producto · diferencia prorrateada", 173.34m, EstadoPago.Autorizado, ahora.AddDays(-3), ConceptoPago.CambioPlan),
            CrearPagoPlataforma(suscripcionesPlataforma[4], mediosOrganizacion[4].Id, "Lanzamiento", 199m, EstadoPago.Rechazado, ahora.AddDays(-9), periodoInicio: suscripcionesPlataforma[4].Fin),
            CrearPagoPlataforma(suscripcionesPlataforma[1], mediosOrganizacion[1].Id, "Lanzamiento", 199m, EstadoPago.Autorizado, ahora.AddDays(-12)),
            CrearPagoPlataforma(suscripcionesPlataforma[0], mediosOrganizacion[0].Id, "Lanzamiento", 199m, EstadoPago.Autorizado, ahora.AddDays(-20)),
            CrearPagoPlataforma(suscripcionesPlataforma[3], mediosOrganizacion[3].Id, "Lanzamiento", 199m, EstadoPago.Autorizado, ahora.AddDays(-32)),
            CrearPagoPlataforma(suscripcionesPlataforma[4], mediosOrganizacion[4].Id, "Lanzamiento", 199m, EstadoPago.Revertido, ahora.AddDays(-39), ConceptoPago.Renovacion, personal[0].Id, null, ahora.AddDays(-3)));

        // 07 §6: CAS-100 a CAS-104. Si otra organización ya tiene alguno de esos números (un caso creado antes de
        // sembrar), los de la demostración toman los siguientes libres (auditoría 2026-10-03, H-32).
        var numeroOcupado = await db.Set<Caso>().IgnoreQueryFilters().AnyAsync(c => c.Numero >= 100 && c.Numero <= 104, cancelacion);
        var primerCaso = numeroOcupado ? await db.Set<Caso>().IgnoreQueryFilters().MaxAsync(c => c.Numero, cancelacion) + 1 : 100;
        var casos = new[]
        {
            CrearCaso(primerCaso, envios.Id, apiEnvios.Id, propietarios[0].Id, personal[2].Id, "Cómo rotar una clave de producción", EstadoCaso.Cerrado, ahora.AddDays(-11)),
            CrearCaso(primerCaso + 1, cafetalera.Id, null, propietarios[2].Id, personal[2].Id, "No llegó el correo de verificación", EstadoCaso.Cerrado, ahora.AddDays(-8)),
            CrearCaso(primerCaso + 2, datos.Id, null, propietarios[4].Id, personal[3].Id, "Sus APIs dejaron de responder", EstadoCaso.Abierto, ahora.AddDays(-4)),
            CrearCaso(primerCaso + 3, agro.Id, apiAgro.Id, propietarios[1].Id, personal[2].Id, "Un consumidor recibe 429 con cuota disponible", EstadoCaso.Abierto, ahora.AddDays(-3)),
            CrearCaso(primerCaso + 4, envios.Id, apiEnvios.Id, propietarios[0].Id, personal[2].Id, "El dominio propio no verifica", EstadoCaso.Abierto, ahora.AddDays(-2)),
        };
        db.AddRange(casos);
        await db.SaveChangesAsync(cancelacion);
        db.AddRange(
            CrearMensaje(casos[0].Id, propietarios[0].Id, "¿Cómo puedo rotar una clave sin interrumpir el servicio?", ahora.AddDays(-11)),
            CrearMensaje(casos[0].Id, personal[2].Id, "Rote la clave desde el portal; la anterior seguirá funcionando durante 24 horas para que actualice su integración.", ahora.AddDays(-10)),
            CrearMensaje(casos[1].Id, propietarios[2].Id, "No recibí el correo de verificación de mi cuenta.", ahora.AddDays(-8)),
            CrearMensaje(casos[2].Id, propietarios[4].Id, "Desde la suspensión, ninguna de nuestras APIs responde.", ahora.AddDays(-4)),
            CrearMensaje(casos[3].Id, propietarios[1].Id, "Un consumidor recibe 429 aunque su cuota todavía tiene llamadas disponibles.", ahora.AddDays(-3)),
            CrearMensaje(casos[4].Id, propietarios[0].Id, $"El dominio api.enviosxelaju.localhost sigue pendiente de verificación desde el {DiaYMes(ahora.ToOffset(TimeSpan.FromHours(-6)).AddDays(-3))}. Ya creé el registro CNAME que me indicó la pantalla de dominios.", ahora.AddDays(-2)),
            CrearMensaje(casos[4].Id, personal[2].Id, "Gracias, Ana Lucía. Estoy revisando el registro en el DNS y le escribo en cuanto tenga el resultado.", ahora.AddDays(-2).AddMinutes(18)),
            CrearMensaje(casos[4].Id, personal[2].Id, "El registro apunta a envios.shapi.localhost, que es la dirección del portal. Cámbielo a envios.api.shapi.localhost y pulse «Verificar registro DNS» en Dominios.", ahora.AddDays(-2).AddMinutes(23)));

        var consumoMercadito = CrearConsumo(rutasEnvios, suscripcionesApi[0].Id, ahora).ToList();
        var consumoOtros = CrearConsumoProveedor(rutasEnvios,
            [suscripcionesApi[1].Id, suscripcionesApi[2].Id, suscripcionesApi[3].Id], ahora, consumoMercadito).ToList();
        AjustarLlamadas(consumoOtros, suscripcionesApi[1].Id, 4_116);
        AjustarLlamadas(consumoOtros, suscripcionesApi[2].Id, 69_600);
        AjustarLlamadas(consumoOtros, suscripcionesApi[3].Id, 1_436);
        var consumoEnvios = consumoMercadito.Concat(consumoOtros).ToList();
        ConfigurarMetricasB1(consumoEnvios, rutasEnvios, ahora);
        db.AddRange(consumoEnvios);
        var entradasBitacora = CrearBitacora(ahora, personal, propietarios, diego, consumidores, casos,
            envios.Id, cafetalera.Id, datos.Id).ToList();
        // La bitácora es solo de inserción (07 §3.6): tampoco al reiniciar se repiten las entradas que ya existen
        // (auditoría 2026-10-03, decisión del paso 4).
        var descripcionesDemo = entradasBitacora.Select(e => e.Descripcion).ToArray();
        var descripcionesExistentes = await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters()
            .Where(e => descripcionesDemo.Contains(e.Descripcion))
            .Select(e => e.Descripcion)
            .ToHashSetAsync(cancelacion);
        db.AddRange(entradasBitacora.Where(e => !descripcionesExistentes.Contains(e.Descripcion)));
        await db.SaveChangesAsync(cancelacion);
        await db.Database.ExecuteSqlRawAsync(
            $"SELECT setval('{ShapiDbContext.SecuenciaNumeroCaso}', GREATEST((SELECT COALESCE(MAX(numero), 99) FROM caso), 99), true)",
            cancelacion);
        await transaccion.CommitAsync(cancelacion);

        registro.LogInformation("Datos de demostración creados para {Fecha}.", ahora);
        return true;
    }

    private Api CrearApi(Guid organizacionId, string nombre, string subdominio, string url, string? secretoOrigen,
        EstadoApi estado, DateTimeOffset ahora, string? portalNombre = null, string? portalColor = null,
        string? bienvenida = null)
    {
        var api = new Api(organizacionId, nombre, subdominio, url, protector.Cifrar(secretoOrigen ?? string.Empty), ahora);
        Poner(api, nameof(Api.Estado), estado);
        Poner(api, nameof(Api.PublicadaEn), estado == EstadoApi.Publicada ? ahora.AddDays(-30) : null);
        if (portalNombre is not null)
        {
            Poner(api, nameof(Api.PortalNombre), portalNombre);
        }

        if (portalColor is not null)
        {
            Poner(api, nameof(Api.PortalColor), portalColor);
        }

        if (bienvenida is not null)
        {
            Poner(api, nameof(Api.PortalBienvenida), bienvenida);
        }

        return api;
    }

    /// <summary>
    /// Crea la cuenta o, si ya existe, la restablece: el nombre, la contraseña de la demostración, el correo
    /// verificado, sin bloqueo y en su estado. No se borra al reiniciar, porque pudo dejar casos, mensajes o
    /// reversiones en otras organizaciones (auditoría 2026-10-03, H-29).
    /// </summary>
    /// <summary>"24 de septiembre". Sin depender de la cultura del sistema: las imágenes Alpine no traen ICU (JZ-17).</summary>
    private static string DiaYMes(DateTimeOffset fecha) => $"{fecha.Day} de {Meses[fecha.Month - 1]}";

    private static Usuario CrearUsuario(string nombre, string correo, PasswordHasher<Usuario> hasher,
        DateTimeOffset ahora, IReadOnlyDictionary<string, Usuario> existentes, bool desactivado = false)
    {
        var usuario = existentes.GetValueOrDefault(Usuario.NormalizarCorreo(correo)) ?? new Usuario(nombre, correo);
        Poner(usuario, nameof(Usuario.Nombre), nombre);
        usuario.DefinirHashContrasena(hasher.HashPassword(usuario, Contrasena));
        usuario.VerificarCorreo(ahora);
        usuario.RegistrarInicioExitoso();
        Poner(usuario, nameof(Usuario.Estado), desactivado ? EstadoCuenta.Desactivado : EstadoCuenta.Activo);
        return usuario;
    }

    /// <summary>Las entidades que todavía no están en el contexto: las que ya existían se actualizan, no se agregan.</summary>
    private IEnumerable<T> Nuevas<T>(IEnumerable<T> entidades) where T : class =>
        entidades.Where(e => db.Entry(e).State == EntityState.Detached);

    private static Consumidor CrearConsumidor(Guid organizacionId, string nombre, string empresa, string correo,
        PasswordHasher<Consumidor> hasher, DateTimeOffset ahora)
    {
        var consumidor = new Consumidor(organizacionId, nombre, empresa, correo, "temporal");
        consumidor.DefinirHashContrasena(hasher.HashPassword(consumidor, Contrasena));
        consumidor.VerificarCorreo(ahora);
        return consumidor;
    }

    private static SuscripcionPlataforma CrearSuscripcionPlataforma(Guid organizacionId, Guid planId,
        EstadoSuscripcion estado, DateTimeOffset inicio, int dias, Guid medioPagoId,
        DateTimeOffset? graciaHasta = null) =>
        Crear<SuscripcionPlataforma>(
            (nameof(Suscripcion.Id), Guid.CreateVersion7()),
            (nameof(SuscripcionPlataforma.OrganizacionId), organizacionId),
            (nameof(Suscripcion.PlanId), planId), (nameof(Suscripcion.Estado), estado),
            (nameof(Suscripcion.Inicio), Suscripcion.InicioDeCiclo(inicio)),
            (nameof(Suscripcion.Fin), Suscripcion.InicioDeCiclo(inicio).AddDays(dias)),
            (nameof(Suscripcion.MedioPagoId), medioPagoId),
            (nameof(Suscripcion.GraciaHasta), graciaHasta));

    private static SuscripcionApi CrearSuscripcionApi(Guid consumidorId, Guid apiId, Guid planId,
        DateTimeOffset inicio, int dias, Guid medioPagoId)
    {
        var inicioCiclo = Suscripcion.InicioDeCiclo(inicio);
        return SuscripcionApi.Crear(consumidorId, apiId, planId, inicioCiclo, inicioCiclo.AddDays(dias), medioPagoId);
    }

    private static MedioPago CrearMedioPago(Guid? organizacionId, Guid? consumidorId, MarcaTarjeta marca,
        string ultimos4, string titular, DateTimeOffset ahora)
    {
        // El dominio solo tiene fábrica para la tarjeta de un consumidor; la de una organización la agrega EM-09.
        var medio = consumidorId is { } consumidor
            ? MedioPago.CrearParaConsumidor(consumidor, $"demo-{Guid.NewGuid():N}",
                marca == MarcaTarjeta.AmericanExpress ? "American Express" : marca.ToString(), ultimos4, titular, 12, 2030)
            : Crear<MedioPago>(
                (nameof(MedioPago.Id), Guid.CreateVersion7()), (nameof(MedioPago.OrganizacionId), organizacionId),
                (nameof(MedioPago.TokenPasarela), $"demo-{Guid.NewGuid():N}"), (nameof(MedioPago.Marca), marca),
                (nameof(MedioPago.Ultimos4), ultimos4), (nameof(MedioPago.Titular), titular),
                (nameof(MedioPago.MesVencimiento), (short)12), (nameof(MedioPago.AnioVencimiento), (short)2030));
        Poner(medio, nameof(MedioPago.CreadoEn), ahora);
        return medio;
    }

    private async Task<List<Ruta>> CrearRutas(Api api, string contenido, string archivo, DateTimeOffset ahora,
        CancellationToken cancelacion)
    {
        var leida = await new LectorEspecificacionOpenApi().Leer(contenido, archivo, cancelacion);
        if (!leida.EsValida)
        {
            throw new InvalidOperationException($"La especificación demo {archivo} no es válida: {leida.Error?.Mensaje}");
        }
        var especificacion = leida.Especificacion!;
        api.CargarEspecificacion(contenido, especificacion.Formato, especificacion.Titulo,
            especificacion.Descripcion, especificacion.Version, ahora);
        return especificacion.Operaciones.Select(o =>
            new Ruta(api.Id, o.Metodo, o.Patron, o.Resumen, o.Descripcion, o.Definicion, ahora)).ToList();
    }

    private static void ConfigurarRuta(IEnumerable<Ruta> rutas, string patron, bool expuesta,
        int limite, int cache, int peso, DateTimeOffset ahora)
    {
        var ruta = rutas.Single(r => r.Patron == patron);
        ruta.CambiarExposicion(expuesta, ahora);
        Poner(ruta, nameof(Ruta.LimiteMinuto), limite);
        Poner(ruta, nameof(Ruta.CacheSegundos), cache);
        Poner(ruta, nameof(Ruta.PesoLlamadas), peso);
    }

    private static Clave ClaveConocida(Guid suscripcionId, TipoClave tipo, string valor,
        DateTimeOffset ahora, EstadoClave estado = EstadoClave.Activa, DateTimeOffset? eventoEstado = null) =>
        Crear<Clave>(
            (nameof(Clave.Id), Guid.CreateVersion7()), (nameof(Clave.SuscripcionId), suscripcionId),
            (nameof(Clave.Tipo), tipo), (nameof(Clave.Prefijo), GeneradorClave.Prefijo(tipo)),
            (nameof(Clave.Ultimos4), valor[^4..]), (nameof(Clave.HashSha256), GeneradorClave.CalcularHash(valor)),
            (nameof(Clave.Estado), estado),
            (nameof(Clave.ExpiraEn), estado == EstadoClave.Rotada ? ahora.AddHours(15) : null),
            (nameof(Clave.RevocadaEn), estado == EstadoClave.Revocada ? eventoEstado ?? ahora.AddDays(-1) : null),
            (nameof(Clave.RevocadaPor), estado == EstadoClave.Revocada ? RevocadaPor.Proveedor : null));

    private static Pago CrearPago(SuscripcionApi suscripcion, Guid medioPagoId, string descripcion, decimal monto,
        DateTimeOffset fecha)
    {
        var pago = Pago.ContratacionAutorizada(suscripcion.Id, medioPagoId, monto, descripcion, $"demo-{Guid.NewGuid():N}",
            suscripcion.Inicio, suscripcion.Fin);
        Poner(pago, nameof(Pago.CreadoEn), fecha); // la fecha del cobro, en el pasado
        return pago;
    }

    private static Pago CrearPagoPlataforma(SuscripcionPlataforma suscripcion, Guid medioPagoId, string descripcion, decimal monto,
        EstadoPago estado, DateTimeOffset fecha, ConceptoPago concepto = ConceptoPago.Renovacion,
        Guid? revertidoPor = null, DateTimeOffset? periodoInicio = null, DateTimeOffset? revertidoEn = null)
    {
        var inicio = periodoInicio ?? suscripcion.Inicio;
        var fin = periodoInicio is null ? suscripcion.Fin : inicio.AddDays(30);
        return Crear<Pago>((nameof(Pago.Id), Guid.CreateVersion7()), (nameof(Pago.SuscripcionPlataformaId), suscripcion.Id),
            (nameof(Pago.MedioPagoId), medioPagoId),
            (nameof(Pago.Concepto), concepto), (nameof(Pago.Descripcion), descripcion),
            (nameof(Pago.Monto), monto), (nameof(Pago.Estado), estado),
            (nameof(Pago.ReferenciaPasarela), $"demo-{Guid.NewGuid():N}"),
            (nameof(Pago.MotivoRechazo), estado == EstadoPago.Rechazado ? "Fondos insuficientes" : null),
            (nameof(Pago.PeriodoInicio), inicio), (nameof(Pago.PeriodoFin), fin),
            (nameof(Pago.RevertidoEn), estado == EstadoPago.Revertido ? revertidoEn ?? fecha.AddDays(1) : null),
            (nameof(Pago.RevertidoPor), revertidoPor), (nameof(Pago.CreadoEn), fecha));
    }

    private static Caso CrearCaso(int numero, Guid organizacionId, Guid? apiId, Guid creadoPor, Guid? asignadoA,
        string asunto, EstadoCaso estado, DateTimeOffset creadoEn) =>
        Crear<Caso>((nameof(Caso.Id), Guid.CreateVersion7()), (nameof(Caso.Numero), numero),
            (nameof(Caso.OrganizacionId), organizacionId), (nameof(Caso.ApiId), apiId),
            (nameof(Caso.CreadoPor), creadoPor), (nameof(Caso.AsignadoA), asignadoA),
            (nameof(Caso.Asunto), asunto), (nameof(Caso.Estado), estado),
            (nameof(Caso.CerradoEn), estado == EstadoCaso.Cerrado ? creadoEn.AddDays(1) : null),
            (nameof(Caso.CreadoEn), creadoEn));

    private static CasoMensaje CrearMensaje(Guid casoId, Guid autorId, string cuerpo, DateTimeOffset fecha) =>
        Crear<CasoMensaje>((nameof(CasoMensaje.Id), Guid.CreateVersion7()), (nameof(CasoMensaje.CasoId), casoId),
            (nameof(CasoMensaje.AutorId), autorId), (nameof(CasoMensaje.Cuerpo), cuerpo),
            (nameof(CasoMensaje.CreadoEn), fecha));

    private static IEnumerable<ConsumoDiario> CrearConsumo(IReadOnlyCollection<Ruta> rutas,
        Guid suscripcionId, DateTimeOffset ahora)
    {
        var hoyGuatemala = DateOnly.FromDateTime(ahora.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var totales = new Dictionary<string, (long Peticiones, int Peso)>
        {
            ["/cotizaciones"] = (15_120, 1),
            ["/rastreo"] = (11_340, 1),
            ["/guias"] = (892, 5),
            ["/cobertura"] = (560, 1),
        };
        foreach (var ruta in rutas.Where(r => totales.ContainsKey(r.Patron)))
        {
            var (total, peso) = totales[ruta.Patron];
            long acumulado = 0;
            for (var dia = 0; dia < 30; dia++)
            {
                // El ciclo de Mercadito empieza hace 21 días. Se conservan filas para los 30 días del
                // periodo de B1.1, pero su consumo queda dentro del ciclo para que B2.1 sume exactamente 31,480.
                var diaDelCiclo = dia - 8;
                var peticiones = diaDelCiclo < 0
                    ? 0
                    : dia == 29 ? total - acumulado : total * (diaDelCiclo + 1) / 253;
                acumulado += peticiones;
                yield return CrearConsumoDiario(ruta, suscripcionId, hoyGuatemala.AddDays(dia - 29), peticiones, peso);
            }
        }
    }

    private static IEnumerable<ConsumoDiario> CrearConsumoProveedor(IReadOnlyCollection<Ruta> rutas,
        IReadOnlyList<Guid> suscripciones, DateTimeOffset ahora, IReadOnlyCollection<ConsumoDiario> consumoMercadito)
    {
        var hoyGuatemala = DateOnly.FromDateTime(ahora.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var fechas = Enumerable.Range(-12, 10).Select(hoyGuatemala.AddDays).ToArray();
        long[] totalesDiarios = [3_180, 3_640, 3_910, 3_036, 13_720, 11_460, 16_280, 18_700, 15_140, 16_390];
        var totalesRuta = new Dictionary<string, long>
        {
            ["/cotizaciones"] = 50_620,
            ["/rastreo"] = 37_964,
            ["/guias"] = 9_492,
            ["/cobertura"] = 7_380,
        };
        var rutasOrdenadas = totalesRuta.Keys.Select(patron => rutas.Single(r => r.Patron == patron)).ToArray();
        var restantesDia = fechas.Select((fecha, indice) => totalesDiarios[indice]
            - consumoMercadito.Where(c => c.Fecha == fecha).Sum(c => c.Peticiones)).ToArray();
        var restantesRuta = rutasOrdenadas.Select(ruta => totalesRuta[ruta.Patron]
            - consumoMercadito.Where(c => c.RutaId == ruta.Id && fechas.Contains(c.Fecha)).Sum(c => c.Peticiones)).ToArray();

        var dia = 0;
        var ruta = 0;
        while (dia < fechas.Length && ruta < rutasOrdenadas.Length)
        {
            var peticiones = Math.Min(restantesDia[dia], restantesRuta[ruta]);
            if (peticiones > 0)
            {
                var actual = rutasOrdenadas[ruta];
                var suscripcionId = suscripciones[(dia + ruta) % suscripciones.Count];
                yield return CrearConsumoDiario(actual, suscripcionId, fechas[dia], peticiones, actual.PesoLlamadas);
                restantesDia[dia] -= peticiones;
                restantesRuta[ruta] -= peticiones;
            }

            if (restantesDia[dia] == 0)
            {
                dia++;
            }
            if (ruta < restantesRuta.Length && restantesRuta[ruta] == 0)
            {
                ruta++;
            }
        }

        if (restantesDia.Any(v => v != 0) || restantesRuta.Any(v => v != 0))
        {
            throw new InvalidOperationException("No se pudo distribuir el consumo de demostración de B1.1.");
        }
    }

    private static void AjustarLlamadas(IReadOnlyCollection<ConsumoDiario> consumos, Guid suscripcionId, long total)
    {
        var filas = consumos.Where(c => c.SuscripcionId == suscripcionId).ToList();
        DistribuirExacto(filas, total, nameof(ConsumoDiario.Llamadas));
    }

    private static void ConfigurarMetricasB1(IReadOnlyCollection<ConsumoDiario> consumos,
        IReadOnlyCollection<Ruta> rutas, DateTimeOffset ahora)
    {
        var hoyGuatemala = DateOnly.FromDateTime(ahora.ToOffset(TimeSpan.FromHours(-6)).DateTime);
        var fechas = Enumerable.Range(-12, 10).Select(hoyGuatemala.AddDays).ToHashSet();
        var filas = consumos.Where(c => fechas.Contains(c.Fecha)).ToList();

        DistribuirExacto(filas, 310_000_000, nameof(ConsumoDiario.BytesEntrada));
        DistribuirExacto(filas, 1_530_000_000, nameof(ConsumoDiario.BytesSalida));
        DistribuirExacto(filas, 476, nameof(ConsumoDiario.Rechazos401));
        DistribuirExacto(filas, 74, nameof(ConsumoDiario.Rechazos403));
        DistribuirExacto(filas, 1_476, nameof(ConsumoDiario.Rechazos429));
        DistribuirExacto(filas, 250, nameof(ConsumoDiario.Origen4xx));
        DistribuirExacto(filas, 66, nameof(ConsumoDiario.Origen5xx));

        var patrones = rutas.ToDictionary(r => r.Id, r => r.Patron);
        foreach (var fila in filas)
        {
            var patron = fila.RutaId is null
                ? throw new InvalidOperationException("El consumo de B1.1 debe pertenecer a una ruta.")
                : patrones[fila.RutaId.Value];
            Poner(fila, nameof(ConsumoDiario.HistLatenciaTotal),
                EscalarHistograma(fila.Peticiones, PesosHistograma(patron, compuerta: false)));
            Poner(fila, nameof(ConsumoDiario.HistLatenciaCompuerta),
                EscalarHistograma(fila.Peticiones, PesosHistograma(patron, compuerta: true)));
        }
    }

    private static int[] PesosHistograma(string patron, bool compuerta) => (patron, compuerta) switch
    {
        ("/cotizaciones", false) => [0, 0, 0, 0, 92_684, 3_047, 4_269, 0, 0, 0],
        ("/rastreo", false) => [0, 0, 0, 0, 94_972, 100, 4_928, 0, 0, 0],
        ("/guias", false) => [0, 0, 0, 0, 88_839, 0, 11_161, 0, 0, 0],
        ("/cobertura", false) => [0, 0, 0, 94_015, 1_087, 4_898, 0, 0, 0, 0],
        ("/cotizaciones", true) => [0, 94_867, 1_000, 4_133, 0, 0, 0, 0, 0, 0],
        ("/rastreo", true) => [94_200, 1_000, 4_800, 0, 0, 0, 0, 0, 0, 0],
        ("/guias", true) => [0, 94_933, 1_000, 4_067, 0, 0, 0, 0, 0, 0],
        ("/cobertura", true) => [94_400, 1_000, 4_600, 0, 0, 0, 0, 0, 0, 0],
        _ => throw new InvalidOperationException($"No hay perfil de latencia para {patron}.")
    };

    private static void DistribuirExacto(IReadOnlyList<ConsumoDiario> filas, long total, string propiedad)
    {
        if (filas.Count == 0)
        {
            throw new InvalidOperationException($"No hay filas para distribuir {propiedad}.");
        }

        var pesoTotal = filas.Sum(f => f.Peticiones);
        long asignado = 0;
        for (var i = 0; i < filas.Count; i++)
        {
            var valor = i == filas.Count - 1 ? total - asignado : total * filas[i].Peticiones / pesoTotal;
            Poner(filas[i], propiedad, valor);
            asignado += valor;
        }
    }

    private static ConsumoDiario CrearConsumoDiario(Ruta ruta, Guid suscripcionId, DateOnly fecha,
        long peticiones, int peso) =>
        Crear<ConsumoDiario>(
            (nameof(ConsumoDiario.Fecha), fecha),
            (nameof(ConsumoDiario.ApiId), ruta.ApiId), (nameof(ConsumoDiario.RutaId), ruta.Id),
            (nameof(ConsumoDiario.SuscripcionId), suscripcionId),
            (nameof(ConsumoDiario.Entorno), EntornoConsumo.Produccion),
            (nameof(ConsumoDiario.Peticiones), peticiones), (nameof(ConsumoDiario.Llamadas), peticiones * peso),
            (nameof(ConsumoDiario.BytesEntrada), peticiones * 320), (nameof(ConsumoDiario.BytesSalida), peticiones * 780),
            (nameof(ConsumoDiario.Rechazos401), peticiones / 220),
            (nameof(ConsumoDiario.Rechazos403), peticiones / 1400),
            (nameof(ConsumoDiario.Rechazos429), peticiones / 72),
            (nameof(ConsumoDiario.Origen2xx), peticiones - peticiones / 40),
            (nameof(ConsumoDiario.Origen4xx), peticiones / 50),
            (nameof(ConsumoDiario.Origen5xx), peticiones / 330),
            (nameof(ConsumoDiario.HistLatenciaTotal), EscalarHistograma(peticiones, [0, 1, 4, 12, 28, 45, 8, 2, 0, 0])),
            (nameof(ConsumoDiario.HistLatenciaCompuerta), EscalarHistograma(peticiones, [4, 24, 50, 18, 4, 0, 0, 0, 0, 0])),
            (nameof(ConsumoDiario.LatenciaTotalSumaMs), peticiones * 180),
            (nameof(ConsumoDiario.LatenciaCompuertaSumaMs), peticiones * 10));

    private static int[] EscalarHistograma(long peticiones, IReadOnlyList<int> proporciones)
    {
        var histograma = new int[proporciones.Count];
        var totalProporciones = proporciones.Sum();
        long asignadas = 0;
        var residuos = new (int Indice, long Residuo)[proporciones.Count];
        for (var i = 0; i < proporciones.Count; i++)
        {
            histograma[i] = checked((int)(peticiones * proporciones[i] / totalProporciones));
            asignadas += histograma[i];
            residuos[i] = (i, peticiones * proporciones[i] % totalProporciones);
        }

        foreach (var (indice, _) in residuos.OrderByDescending(r => r.Residuo).ThenBy(r => r.Indice)
            .Take(checked((int)(peticiones - asignadas))))
        {
            histograma[indice]++;
        }

        return histograma;
    }

    private static IEnumerable<EntradaBitacoraDominio> CrearBitacora(DateTimeOffset ahora, Usuario[] personal,
        Usuario[] propietarios, Usuario diego, Consumidor[] consumidores, Caso[] casos,
        Guid enviosId, Guid cafetaleraId, Guid datosId)
    {
        yield return Entrada(ahora.AddDays(-2), personal[0], AccionesBitacora.OrganizacionSuspendida, "Suspendió la organización Datos Chapines, S.A.", datosId);
        yield return Entrada(ahora.AddDays(-2).AddHours(-3), personal[2], AccionesBitacora.CasoAbierto, $"Abrió el caso CAS-{casos[4].Numero} de Envíos Xelajú, S.A.", enviosId, casos[4].Id);
        yield return Entrada(ahora.AddDays(-3).AddHours(1), personal[0], AccionesBitacora.CuentaPlataformaDesactivada, "Desactivó la cuenta de plataforma de Julio Estrada Ixcot");
        yield return Entrada(ahora.AddDays(-3), personal[0], AccionesBitacora.PagoRevertido, "Revirtió el cobro de Q 199.00 de Datos Chapines, S.A.", datosId);
        yield return Entrada(ahora.AddDays(-3).AddHours(-5), propietarios[0], AccionesBitacora.SuscripcionPlataformaCambiada, "Cambió su suscripción de plataforma de Lanzamiento a Producto", enviosId);
        yield return new EntradaBitacoraDominio(ahora.AddHours(-9), ActorTipo.Consumidor, consumidores[1].Id,
            consumidores[1].NombreEmpresa, enviosId, AccionesBitacora.ClaveRotada, null, null,
            "Rotó su clave de producción en la API de Cotización de Envíos", null, null);
        yield return Entrada(ahora.AddDays(-3).AddHours(-8), propietarios[0], AccionesBitacora.ClaveRevocadaPorProveedor, "Revocó la clave de pruebas de Tienda Sololá en la API de Cotización de Envíos", enviosId);
        yield return Entrada(ahora.AddDays(-4), diego, AccionesBitacora.RutaOcultada, "Ocultó la ruta GET /tarifas de la API de Cotización de Envíos", enviosId);
        yield return Entrada(ahora.AddDays(-5), propietarios[0], AccionesBitacora.MiembroInvitado, "Invitó a karla.batres@enviosxelaju.com con el rol de lector", enviosId);
        yield return Entrada(ahora.AddDays(-7), personal[2], AccionesBitacora.CasoCerrado, $"Cerró el caso CAS-{casos[1].Numero} de Cafetalera del Altiplano, S.A.", cafetaleraId, casos[1].Id);
        yield return Entrada(ahora.AddDays(-8), personal[0], AccionesBitacora.CuentaPlataformaCreada, "Creó la cuenta de plataforma de Lucía Ramírez Pineda con el rol de administrador");

        static EntradaBitacoraDominio Entrada(DateTimeOffset fecha, Usuario actor, string accion, string descripcion,
            Guid? organizacionId = null, Guid? objetivo = null) =>
            new(fecha, ActorTipo.Usuario, actor.Id, actor.Nombre, organizacionId, accion,
                objetivo is null ? null : "caso", objetivo, descripcion, null, null);
    }

    private static Organizacion ObtenerOCrearOrganizacion(
        string nombre,
        IReadOnlyDictionary<string, Organizacion> organizacionesExistentes) =>
        organizacionesExistentes.GetValueOrDefault(nombre) ?? new Organizacion(nombre, TipoOrganizacion.Proveedor);

    private async Task BorrarDatosDemo(CancellationToken cancelacion)
    {
        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE demo_org ON COMMIT DROP AS
            SELECT id FROM organizacion WHERE nombre IN
              ('Envíos Xelajú, S.A.','Agro Precios, S.A.','Cafetalera del Altiplano, S.A.','Transportes Petén, S.A.','Datos Chapines, S.A.');
            DELETE FROM token WHERE organizacion_id IN (SELECT id FROM demo_org);
            DELETE FROM consumo_diario WHERE api_id IN (SELECT id FROM api WHERE organizacion_id IN (SELECT id FROM demo_org));
            DELETE FROM clave WHERE suscripcion_id IN (SELECT id FROM suscripcion_api WHERE api_id IN (SELECT id FROM api WHERE organizacion_id IN (SELECT id FROM demo_org)));
            DELETE FROM pago WHERE suscripcion_api_id IN (SELECT id FROM suscripcion_api WHERE api_id IN (SELECT id FROM api WHERE organizacion_id IN (SELECT id FROM demo_org)))
                OR suscripcion_plataforma_id IN (SELECT id FROM suscripcion_plataforma WHERE organizacion_id IN (SELECT id FROM demo_org));
            DELETE FROM pago WHERE consumidor_id IN (SELECT id FROM consumidor WHERE organizacion_id IN (SELECT id FROM demo_org))
                OR api_id IN (SELECT id FROM api WHERE organizacion_id IN (SELECT id FROM demo_org));
            DELETE FROM caso_mensaje WHERE caso_id IN (SELECT id FROM caso WHERE organizacion_id IN (SELECT id FROM demo_org));
            DELETE FROM caso WHERE organizacion_id IN (SELECT id FROM demo_org);
            DELETE FROM suscripcion_api WHERE api_id IN (SELECT id FROM api WHERE organizacion_id IN (SELECT id FROM demo_org));
            DELETE FROM suscripcion_plataforma WHERE organizacion_id IN (SELECT id FROM demo_org);
            DELETE FROM medio_pago WHERE organizacion_id IN (SELECT id FROM demo_org)
                OR consumidor_id IN (SELECT id FROM consumidor WHERE organizacion_id IN (SELECT id FROM demo_org));
            DELETE FROM consumidor WHERE organizacion_id IN (SELECT id FROM demo_org);
            DELETE FROM membresia WHERE organizacion_id IN (SELECT id FROM demo_org);
            DELETE FROM api WHERE organizacion_id IN (SELECT id FROM demo_org);
            """, cancelacion);
        // Las cuentas de la demostración no se borran: pueden ser autoras de casos, mensajes o reversiones en otras
        // organizaciones (FK sin cascada). Se quitan sus sesiones, sus enlaces y sus membresías, y la siembra las
        // restablece (auditoría 2026-10-03, H-29).
        var cuentas = await db.Set<Usuario>().IgnoreQueryFilters()
            .Where(u => CorreosUsuariosDemo.Contains(u.Correo)).Select(u => u.Id).ToListAsync(cancelacion);
        await db.Set<Sesion>().IgnoreQueryFilters().Where(x => x.UsuarioId != null && cuentas.Contains(x.UsuarioId.Value))
            .ExecuteDeleteAsync(cancelacion);
        await db.Set<Token>().IgnoreQueryFilters().Where(x => x.UsuarioId != null && cuentas.Contains(x.UsuarioId.Value))
            .ExecuteDeleteAsync(cancelacion);
        await db.Set<Membresia>().IgnoreQueryFilters().Where(x => cuentas.Contains(x.UsuarioId))
            .ExecuteDeleteAsync(cancelacion);
    }

    private static T Crear<T>(params (string Propiedad, object? Valor)[] valores) where T : class
    {
        var entidad = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        foreach (var (propiedad, valor) in valores)
        {
            Poner(entidad, propiedad, valor);
        }

        return entidad;
    }

    private static void Poner(object entidad, string propiedad, object? valor) =>
        entidad.GetType().GetProperty(propiedad)!.SetValue(entidad, valor);
}
