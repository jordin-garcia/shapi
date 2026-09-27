using Microsoft.EntityFrameworkCore;
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

namespace Shapi.Api.Tests.Persistencia;

/// <summary>Comportamiento del modelo de EF: filtro por organización, valores por defecto, fechas de auditoría e identificadores.</summary>
[Collection(nameof(PostgresPersistencia))]
public sealed class ShapiDbContextTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    // ---------- RNF-08 · Filtro global por organización (10 §2) ----------

    private sealed record Grafo(
        Guid Usuario, Guid Membresia, Guid Consumidor, Guid Api, Guid Ruta, Guid DominioPropio, Guid PlanApi,
        Guid SuscripcionApi, Guid Clave, Guid SuscripcionPlataforma, Guid MedioPagoOrganizacion, Guid MedioPagoConsumidor,
        Guid PagoPlataforma, Guid PagoApi, long ConsumoDiario, Guid Caso, Guid CasoMensaje, long Bitacora);

    /// <summary>Una fila de cada tabla que pertenece a la organización, directamente o a través de su padre.</summary>
    private async Task<Grafo> CrearGrafo(Guid organizacion, Guid planPlataforma)
    {
        var usuario = await NuevoUsuario();
        var membresia = await NuevaMembresia(usuario, organizacion, "propietario");
        var consumidor = await NuevoConsumidor(organizacion);
        var api = await NuevaApi(organizacion);
        var ruta = await NuevaRuta(api);
        var dominio = await Escalar<Guid>($"""
            INSERT INTO dominio_propio (id, api_id, dominio, destino_cname, estado)
            VALUES (gen_random_uuid(), '{api}', gen_random_uuid() || '.ejemplo.com', 'envios.api.shapi.localhost', 'pendiente')
            RETURNING id
            """);
        var planApi = await NuevoPlanApi(api);
        var suscripcionApi = await NuevaSuscripcionApi(consumidor, api, planApi);
        var clave = await NuevaClave(suscripcionApi);
        var suscripcionPlataforma = await NuevaSuscripcionPlataforma(organizacion, planPlataforma);
        var medioOrganizacion = await NuevoMedioPago(organizacion, null);
        var medioConsumidor = await NuevoMedioPago(null, consumidor);
        var pagoPlataforma = await NuevoPago(suscripcionPlataforma, null);
        var pagoApi = await NuevoPago(null, suscripcionApi);
        var consumo = await NuevoConsumoDiario(api, ruta, suscripcionApi);
        var caso = await NuevoCaso(organizacion, usuario);
        var mensaje = await NuevoCasoMensaje(caso, usuario);
        var bitacora = await NuevaEntradaBitacora(organizacion);
        return new Grafo(usuario, membresia, consumidor, api, ruta, dominio, planApi, suscripcionApi, clave,
            suscripcionPlataforma, medioOrganizacion, medioConsumidor, pagoPlataforma, pagoApi, consumo, caso, mensaje, bitacora);
    }

    [Fact]
    public async Task RNF_08_FiltroGlobal_ConContextoDeUnaOrganizacion_SoloDevuelveSusFilas()
    {
        var plan = await NuevoPlanPlataforma();
        var organizacionA = await NuevaOrganizacion();
        var a = await CrearGrafo(organizacionA, plan);
        await CrearGrafo(await NuevaOrganizacion(), plan);
        await NuevaEntradaBitacora(null); // acción del sistema sin organización: solo la ve la administración

        Contexto.OrganizacionId = organizacionA;
        await using var db = CrearDb();

        Assert.Equal([a.Usuario], await db.Set<Usuario>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.Membresia], await db.Set<Membresia>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.Consumidor], await db.Set<Consumidor>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.Api], await db.Set<Shapi.Dominio.Apis.Api>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.Ruta], await db.Set<Ruta>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.DominioPropio], await db.Set<DominioPropio>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.PlanApi], await db.Set<PlanApi>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.SuscripcionApi], await db.Set<SuscripcionApi>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.Clave], await db.Set<Clave>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.SuscripcionPlataforma], await db.Set<SuscripcionPlataforma>().Select(x => x.Id).ToListAsync());
        Assert.Equal(
            new HashSet<Guid> { a.MedioPagoOrganizacion, a.MedioPagoConsumidor },
            (await db.Set<MedioPago>().Select(x => x.Id).ToListAsync()).ToHashSet());
        Assert.Equal(
            new HashSet<Guid> { a.PagoPlataforma, a.PagoApi },
            (await db.Set<Pago>().Select(x => x.Id).ToListAsync()).ToHashSet());
        Assert.Equal([a.ConsumoDiario], await db.Set<ConsumoDiario>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.Caso], await db.Set<Caso>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.CasoMensaje], await db.Set<CasoMensaje>().Select(x => x.Id).ToListAsync());
        Assert.Equal([a.Bitacora], await db.Set<EntradaBitacora>().Select(x => x.Id).ToListAsync());
    }

    [Fact]
    public async Task RNF_08_FiltroGlobal_SinContexto_NoDevuelveNada()
    {
        await CrearGrafo(await NuevaOrganizacion(), await NuevoPlanPlataforma());

        Contexto.OrganizacionId = null;
        await using var db = CrearDb();

        Assert.Empty(await db.Set<Shapi.Dominio.Apis.Api>().ToListAsync());
        Assert.Empty(await db.Set<Ruta>().ToListAsync());
        Assert.Empty(await db.Set<Pago>().ToListAsync());
        Assert.Empty(await db.Set<Usuario>().ToListAsync());
        Assert.Single(await db.Set<Usuario>().IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task RNF_08_FiltroGlobal_CubreTodaTablaQueNoSeaGlobal()
    {
        await using var db = CrearDb();

        var sinFiltro = db.Model.GetEntityTypes()
            .Where(tipo => tipo.GetDeclaredQueryFilters().Count == 0)
            .Select(tipo => tipo.GetTableName())
            .Order()
            .ToList();

        // Tablas globales o que se usan antes de conocer la organización (sesiones y enlaces).
        Assert.Equal(
            ["correo_saliente", "lote_consolidado", "organizacion", "plan_plataforma", "registro_dns_simulado", "sesion", "token"],
            sinFiltro);
    }

    // ---------- 07 §3.3 · activo con DEFAULT true ----------

    [Fact]
    public async Task RF_17_PlanPlataforma_GuardadoInactivo_QuedaInactivo()
    {
        await using (var db = CrearDb())
        {
            db.Add(Crear<PlanPlataforma>(new
            {
                Nombre = "Heredado",
                Descripcion = "Un plan que ya no se vende",
                Precio = 99m,
                VigenciaDias = 30,
                CuotaPeticiones = 1000L,
                Orden = 9,
                Activo = false,
            }));
            await db.SaveChangesAsync();
        }

        await using var lectura = CrearDb();
        Assert.False((await lectura.Set<PlanPlataforma>().SingleAsync()).Activo);
    }

    [Fact]
    public async Task RF_18_PlanApi_GuardadoInactivo_QuedaInactivo()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        await using (var db = CrearDb())
        {
            db.Add(Crear<PlanApi>(new
            {
                ApiId = api,
                Nombre = "Básico",
                Descripcion = "Un plan que ya no se vende",
                Precio = 0m,
                EsGratuito = true,
                VigenciaDias = 30,
                CuotaLlamadas = 1000L,
                LimiteMinuto = 60,
                Activo = false,
            }));
            await db.SaveChangesAsync();
        }

        Contexto.OrganizacionId = organizacion;
        await using var lectura = CrearDb();
        Assert.False((await lectura.Set<PlanApi>().SingleAsync()).Activo);
    }

    // ---------- 07 §3 · creado_en y actualizado_en con la hora de IReloj ----------

    [Fact]
    public async Task Interceptor_InsertarYModificar_UsaLaHoraDelReloj()
    {
        var alta = Reloj.Ahora;
        var usuario = new Usuario("Ana", "ana@ejemplo.com");
        await using (var db = CrearDb())
        {
            db.Add(usuario);
            await db.SaveChangesAsync();
        }

        await using (var lectura = CrearDb())
        {
            var guardado = await lectura.Set<Usuario>().IgnoreQueryFilters().SingleAsync();
            Assert.Equal(alta, guardado.CreadoEn);
            Assert.Equal(alta, guardado.ActualizadoEn);
        }

        Reloj.Ahora = alta.AddHours(3);
        await using (var db = CrearDb())
        {
            var guardado = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync();
            guardado.RegistrarIntentoFallido(Reloj.Ahora);
            await db.SaveChangesAsync();
        }

        await using var final = CrearDb();
        var modificado = await final.Set<Usuario>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal(alta, modificado.CreadoEn);
        Assert.Equal(alta.AddHours(3), modificado.ActualizadoEn);
    }

    // ---------- 07 §3 · UUID v7 ----------

    [Fact]
    public async Task Identificadores_GeneradosPorEf_SonUuidV7()
    {
        var plan = Crear<PlanPlataforma>(new
        {
            Nombre = "Lanzamiento",
            Descripcion = "Empezar a cobrar",
            Precio = 199m,
            VigenciaDias = 30,
            CuotaPeticiones = 250000L,
            Orden = 2,
            Activo = true,
        });
        await using var db = CrearDb();
        db.Add(plan);
        await db.SaveChangesAsync();

        Assert.Equal(7, plan.Id.Version);
    }

    /// <summary>Crea una entidad sin constructor público y asigna sus propiedades por nombre.</summary>
    private static T Crear<T>(object valores) where T : class
    {
        var entidad = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        foreach (var valor in valores.GetType().GetProperties())
        {
            typeof(T).GetProperty(valor.Name)!.SetValue(entidad, valor.GetValue(valores));
        }
        return entidad;
    }
}
