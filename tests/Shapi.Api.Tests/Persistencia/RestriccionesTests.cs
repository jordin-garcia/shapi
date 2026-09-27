using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Shapi.Api.Tests.Persistencia;

/// <summary>Restricciones del esquema de 07 §3 que aplica la base de datos, sin pasar por EF (criterios 2 y 3 de EM-01).</summary>
[Collection(nameof(PostgresPersistencia))]
public sealed class RestriccionesTests(PostgresPersistencia postgres) : BaseDePrueba(postgres)
{
    private const string UnicoViolado = PostgresErrorCodes.UniqueViolation;
    private const string CheckViolado = PostgresErrorCodes.CheckViolation;
    private const string LlaveForaneaViolada = PostgresErrorCodes.ForeignKeyViolation;

    // ---------- Criterio 2: restricciones únicas y num_nonnulls ----------

    [Fact]
    public async Task RF_06_Membresia_DosPropietariosEnLaMismaOrganizacion_SeRechaza()
    {
        var organizacion = await NuevaOrganizacion();
        await NuevaMembresia(await NuevoUsuario(), organizacion, "propietario");
        await NuevaMembresia(await NuevoUsuario(), organizacion, "editor");

        var error = await Assert.ThrowsAsync<PostgresException>(
            async () => await NuevaMembresia(await NuevoUsuario(), organizacion, "propietario"));

        Assert.Equal(UnicoViolado, error.SqlState);
    }

    [Fact]
    public async Task RF_20_SuscripcionPlataforma_DosVigentesEnLaMismaOrganizacion_SeRechaza()
    {
        var organizacion = await NuevaOrganizacion();
        var plan = await NuevoPlanPlataforma();
        await NuevaSuscripcionPlataforma(organizacion, plan, "finalizada");
        await NuevaSuscripcionPlataforma(organizacion, plan, "activa");

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => NuevaSuscripcionPlataforma(organizacion, plan, "en_gracia"));

        Assert.Equal(UnicoViolado, error.SqlState);
    }

    [Fact]
    public async Task RF_20_SuscripcionApi_DosVigentesDelMismoConsumidorEnLaMismaApi_SeRechaza()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        var plan = await NuevoPlanApi(api);
        var consumidor = await NuevoConsumidor(organizacion);
        await NuevaSuscripcionApi(consumidor, api, plan, "finalizada");
        await NuevaSuscripcionApi(consumidor, api, plan, "activa");

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => NuevaSuscripcionApi(consumidor, api, plan, "suspendida"));

        Assert.Equal(UnicoViolado, error.SqlState);
    }

    [Fact]
    public async Task RF_26_Clave_DosActivasDelMismoTipo_SeRechaza()
    {
        var suscripcion = await SuscripcionApiCompleta();
        await NuevaClave(suscripcion, "produccion", "rotada");
        await NuevaClave(suscripcion, "produccion", "activa");
        await NuevaClave(suscripcion, "pruebas", "activa");

        var error = await Assert.ThrowsAsync<PostgresException>(() => NuevaClave(suscripcion, "produccion", "activa"));

        Assert.Equal(UnicoViolado, error.SqlState);
    }

    [Fact]
    public async Task RF_34_ConsumoDiario_MismaLlaveConNulos_SeRechaza()
    {
        var api = await NuevaApi(await NuevaOrganizacion());
        await NuevoConsumoDiario(api, rutaId: null, suscripcionId: null);

        // NULLS NOT DISTINCT: dos filas con ruta y suscripción nulas son la misma llave.
        var error = await Assert.ThrowsAsync<PostgresException>(() => NuevoConsumoDiario(api, null, null));

        Assert.Equal(UnicoViolado, error.SqlState);
    }

    [Fact]
    public async Task RF_21_Pago_SinSuscripcionOConLasDos_SeRechaza()
    {
        var organizacion = await NuevaOrganizacion();
        var suscripcionPlataforma = await NuevaSuscripcionPlataforma(organizacion, await NuevoPlanPlataforma());
        var suscripcionApi = await SuscripcionApiCompleta();
        await NuevoPago(suscripcionPlataforma, null);
        await NuevoPago(null, suscripcionApi);

        var sinNinguna = await Assert.ThrowsAsync<PostgresException>(() => NuevoPago(null, null));
        var conLasDos = await Assert.ThrowsAsync<PostgresException>(() => NuevoPago(suscripcionPlataforma, suscripcionApi));

        Assert.Equal(CheckViolado, sinNinguna.SqlState);
        Assert.Equal(CheckViolado, conLasDos.SqlState);
    }

    [Fact]
    public async Task RF_20_MedioPago_SinDuenoOConLosDos_SeRechaza()
    {
        var organizacion = await NuevaOrganizacion();
        var consumidor = await NuevoConsumidor(organizacion);
        await NuevoMedioPago(organizacion, null);
        await NuevoMedioPago(null, consumidor);

        var sinDueno = await Assert.ThrowsAsync<PostgresException>(() => NuevoMedioPago(null, null));
        var conLosDos = await Assert.ThrowsAsync<PostgresException>(() => NuevoMedioPago(organizacion, consumidor));

        Assert.Equal(CheckViolado, sinDueno.SqlState);
        Assert.Equal(CheckViolado, conLosDos.SqlState);
    }

    [Fact]
    public async Task RF_17_PlanPlataforma_DosPlanesDePrueba_SeRechaza()
    {
        await NuevoPlanPlataforma(esPrueba: false);
        await NuevoPlanPlataforma(esPrueba: false);
        await NuevoPlanPlataforma(esPrueba: true);

        var error = await Assert.ThrowsAsync<PostgresException>(() => NuevoPlanPlataforma(esPrueba: true));

        Assert.Equal(UnicoViolado, error.SqlState);
    }

    // ---------- Criterio 3: secuencia de casos y bitácora ----------

    [Fact]
    public async Task RF_40_Caso_NumerosConsecutivos_EmpiezanEn100()
    {
        var organizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await NuevoCaso(organizacion, usuario);
        await NuevoCaso(organizacion, usuario);
        await NuevoCaso(organizacion, usuario);

        var numeros = await Escalar<int[]>("SELECT array_agg(numero ORDER BY numero) FROM caso");

        Assert.Equal([100, 101, 102], numeros);
    }

    [Theory]
    [InlineData("UPDATE bitacora SET descripcion = 'Modificada'", "UPDATE")]
    [InlineData("DELETE FROM bitacora", "DELETE")]
    [InlineData("TRUNCATE bitacora", "TRUNCATE")]
    public async Task RF_41_Bitacora_ModificarBorrarOVaciar_SeRechaza(string sql, string operacion)
    {
        await NuevaEntradaBitacora(null);

        var error = await Rechazo(sql);

        Assert.Equal($"La bitácora solo admite inserciones: no se permite {operacion}.", error.MessageText);
        Assert.Equal(1L, await Escalar<long>("SELECT count(*) FROM bitacora"));
    }

    // ---------- Llaves foráneas, CHECK y valores por defecto de 07 §3 ----------

    [Theory]
    [InlineData("suscripcion_plataforma")]
    [InlineData("suscripcion_api")]
    public async Task RF_25_Suscripcion_PlanSiguienteInexistente_SeRechaza(string tabla)
    {
        var suscripcion = tabla == "suscripcion_plataforma"
            ? await NuevaSuscripcionPlataforma(await NuevaOrganizacion(), await NuevoPlanPlataforma())
            : await SuscripcionApiCompleta();

        var error = await Rechazo($"UPDATE {tabla} SET plan_siguiente_id = gen_random_uuid() WHERE id = '{suscripcion}'");

        Assert.Equal(LlaveForaneaViolada, error.SqlState);
    }

    [Fact]
    public async Task RF_25_SuscripcionApi_PlanSiguienteDePlataforma_SeRechaza()
    {
        var suscripcion = await SuscripcionApiCompleta();
        var planDePlataforma = await NuevoPlanPlataforma();

        var error = await Rechazo($"UPDATE suscripcion_api SET plan_siguiente_id = '{planDePlataforma}' WHERE id = '{suscripcion}'");

        Assert.Equal(LlaveForaneaViolada, error.SqlState);
    }

    [Fact]
    public async Task RF_20_MedioPago_MesDeVencimientoFueraDeRango_SeRechaza()
    {
        var organizacion = await NuevaOrganizacion();
        await NuevoMedioPago(organizacion, null, mesVencimiento: 1);
        await NuevoMedioPago(organizacion, null, mesVencimiento: 12);

        var mes13 = await Assert.ThrowsAsync<PostgresException>(() => NuevoMedioPago(organizacion, null, mesVencimiento: 13));
        var mes0 = await Assert.ThrowsAsync<PostgresException>(() => NuevoMedioPago(organizacion, null, mesVencimiento: 0));

        Assert.Equal(CheckViolado, mes13.SqlState);
        Assert.Equal(CheckViolado, mes0.SqlState);
    }

    [Fact]
    public async Task RF_13_Ruta_CacheEnMetodoDistintoDeGet_SeRechaza()
    {
        var api = await NuevaApi(await NuevaOrganizacion());
        await NuevaRuta(api, "GET", cacheSegundos: 60);
        await NuevaRuta(api, "POST", cacheSegundos: 0);

        var error = await Assert.ThrowsAsync<PostgresException>(() => NuevaRuta(api, "POST", cacheSegundos: 60));

        Assert.Equal(CheckViolado, error.SqlState);
    }

    [Fact]
    public async Task RF_01_Organizacion_NombreDeUnCaracter_SeRechaza()
    {
        await Ejecutar("INSERT INTO organizacion (id, nombre, tipo, estado_admin) VALUES (gen_random_uuid(), 'AB', 'proveedor', 'activa')");

        var error = await Rechazo("INSERT INTO organizacion (id, nombre, tipo, estado_admin) VALUES (gen_random_uuid(), 'A', 'proveedor', 'activa')");

        Assert.Equal(CheckViolado, error.SqlState);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public async Task RF_34_ConsumoDiario_HistogramaSinDiezRangos_SeRechaza(int rangos)
    {
        var api = await NuevaApi(await NuevaOrganizacion());

        var error = await Assert.ThrowsAsync<PostgresException>(() => NuevoConsumoDiario(api, null, null, rangos));

        Assert.Equal(CheckViolado, error.SqlState);
    }

    [Fact]
    public async Task RF_15_Api_LogoDeMasDe512KB_SeRechaza()
    {
        var api = await NuevaApi(await NuevaOrganizacion());
        await Ejecutar($"UPDATE api SET portal_logo = decode(repeat('00', 524288), 'hex') WHERE id = '{api}'");

        var error = await Rechazo($"UPDATE api SET portal_logo = decode(repeat('00', 524289), 'hex') WHERE id = '{api}'");

        Assert.Equal(CheckViolado, error.SqlState);
    }

    // 07 §3: DEFAULT de los estados
    [Fact]
    public async Task Estados_SinValor_TomanElDefaultDeLaEspecificacion()
    {
        var organizacion = await Escalar<Guid>(
            "INSERT INTO organizacion (id, nombre, tipo) VALUES (gen_random_uuid(), 'Envíos', 'proveedor') RETURNING id");
        await Ejecutar("INSERT INTO usuario (id, nombre, correo) VALUES (gen_random_uuid(), 'Ana', 'ana@ejemplo.com')");
        await Ejecutar($"""
            INSERT INTO consumidor (id, organizacion_id, nombre, nombre_empresa, correo, hash_contrasena)
            VALUES (gen_random_uuid(), '{organizacion}', 'Luis', 'Mercadito', 'luis@ejemplo.com', 'hash')
            """);
        await Ejecutar($"""
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, secreto_origen_cifrado)
            VALUES (gen_random_uuid(), '{organizacion}', 'Envíos', 'envios', 'https://origen.ejemplo.com', 'secreto')
            """);

        Assert.Equal("activa", await Escalar<string>("SELECT estado_admin FROM organizacion"));
        Assert.Equal("activo", await Escalar<string>("SELECT estado FROM usuario"));
        Assert.Equal("activo", await Escalar<string>("SELECT estado FROM consumidor"));
        Assert.Equal("borrador", await Escalar<string>("SELECT estado FROM api"));
    }

    // 07 §3.5
    [Fact]
    public async Task ConsumoDiario_Columnas_UsanLosNombresDeLaEspecificacion()
    {
        var columnas = await Escalar<string[]>("""
            SELECT array_agg(column_name::text ORDER BY column_name) FROM information_schema.columns
            WHERE table_name = 'consumo_diario' AND (column_name LIKE 'rechazos%' OR column_name LIKE 'origen%')
            """);

        Assert.Equal(
            ["origen_2xx", "origen_3xx", "origen_4xx", "origen_5xx", "origen_fallo",
             "rechazos_401", "rechazos_403", "rechazos_404", "rechazos_429"],
            columnas);
    }

    // 07 §3: convenciones de creado_en y actualizado_en
    [Theory]
    [InlineData("usuario", true)]
    [InlineData("membresia", true)]
    [InlineData("consumidor", true)]
    [InlineData("token", true)]
    [InlineData("correo_saliente", true)]
    [InlineData("consumo_diario", true)]
    [InlineData("registro_dns_simulado", true)]
    [InlineData("medio_pago", false)]
    [InlineData("caso_mensaje", false)]
    public async Task Tabla_TieneLasColumnasDeAuditoria(string tabla, bool seModifica)
    {
        var columnas = await Escalar<string[]>($"""
            SELECT array_agg(column_name::text) FROM information_schema.columns
            WHERE table_name = '{tabla}' AND column_name IN ('creado_en', 'actualizado_en') AND is_nullable = 'NO'
            """);

        Assert.Contains("creado_en", columnas);
        Assert.Equal(seModifica, columnas.Contains("actualizado_en"));
    }

    // 07 §3.1
    [Fact]
    public async Task Sesion_TieneActualizadoEn()
    {
        var existe = await Escalar<long>("""
            SELECT count(*) FROM information_schema.columns
            WHERE table_name = 'sesion' AND column_name = 'actualizado_en' AND is_nullable = 'NO'
            """);

        Assert.Equal(1L, existe);
    }

    // 07 §3: nombres en snake_case
    [Fact]
    public async Task Restricciones_TienenNombresEnSnakeCase()
    {
        var conMayusculas = await Escalar<long>("""
            SELECT count(*) FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace
            WHERE n.nspname = 'public' AND c.conname <> lower(c.conname)
            """);

        Assert.Equal(0L, conMayusculas);
    }

    // La migración de la auditoría se puede revertir hasta Inicial: su Down quita el DEFAULT antes de borrar la secuencia.
    [Fact]
    public async Task Migraciones_RevertirAInicialYVolverAAplicar_TerminanSinError()
    {
        await using var db = CrearDb();
        var migrador = db.GetService<IMigrator>();

        await migrador.MigrateAsync("20260925073244_Inicial");
        Assert.Equal(44L, await Escalar<long>("SELECT count(*) FROM pg_constraint WHERE conname LIKE 'CK\\_%'"));

        await migrador.MigrateAsync();
        Assert.Equal(0L, await Escalar<long>("SELECT count(*) FROM pg_constraint WHERE conname LIKE 'CK\\_%'"));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    private async Task<Guid> SuscripcionApiCompleta()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        return await NuevaSuscripcionApi(await NuevoConsumidor(organizacion), api, await NuevoPlanApi(api));
    }
}
