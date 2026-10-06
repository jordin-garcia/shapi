using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Consumo;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Consumo;
using Shapi.Infraestructura.Consumo;
using Shapi.Trabajador.Consolidacion;
using StackExchange.Redis;

namespace Shapi.Api.Tests.Consumo;

[Collection(nameof(RedisCache))]
public sealed class ConsolidacionTests(PostgresPersistencia postgres, RedisCache redis) : BaseCache(postgres, redis)
{
    private static readonly DateOnly Fecha = new(2026, 10, 5);

    // RNF-05, RF-33, RF-34: interrupciones antes de separar, después del RENAME, después del COMMIT y durante el DEL.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task RNF_05_Interrupcion_RecuperaSinPerderNiDuplicar(int paso)
    {
        var api = await NuevaApi(await NuevaOrganizacion());
        var ruta = await NuevaRuta(api);
        var llave1 = LlavesRedis.Metricas(Fecha, api, ruta, null, "produccion");
        var llave2 = LlavesRedis.Metricas(Fecha, api, null, null, "produccion");
        await Medir(llave1, 3);
        await Medir(llave2, 2);
        var lote = Guid.NewGuid();
        await using (var servicios = CrearServicios(ajustar: s => s.AgregarConsolidacion()))
        using (var alcance = servicios.CreateScope())
        {
            var capturador = alcance.ServiceProvider.GetRequiredService<LotesMetricasRedis>();
            if (paso >= 1)
            {
                await capturador.SepararAsync(lote, CancellationToken.None);
                (await Redis.SetLengthAsync(LlavesRedis.MetricasPendientes)).Should().Be(0);
                // Las peticiones que llegan después pertenecen al siguiente lote.
                await Medir(llave1, 4);
            }
            if (paso >= 2)
            {
                var filas = await capturador.LeerAsync(lote, CancellationToken.None);
                await alcance.ServiceProvider.GetRequiredService<RepositorioConsolidacion>()
                    .GuardarAsync(lote, filas, CancellationToken.None);
            }
            if (paso >= 3)
            {
                await Redis.KeyDeleteAsync(LlavesRedis.LoteMetricas(lote, llave1));
            }
        }
        // Servicios nuevos: equivale a reiniciar el proceso del trabajador.
        await Consolidar();
        await Consolidar();
        await using var db = CrearDb();
        var filasFinales = await db.Set<ConsumoDiario>().ToListAsync();
        filasFinales.Sum(x => x.Peticiones).Should().Be(paso == 0 ? 5 : 9);
        filasFinales.Sum(x => x.HistLatenciaTotal.Sum()).Should().Be(paso == 0 ? 5 : 9);
        filasFinales.Sum(x => x.Llamadas).Should().Be(paso == 0 ? 10 : 18);
        filasFinales.Should().HaveCount(2);
        (await Redis.SetLengthAsync(LlavesRedis.MetricasPendientes)).Should().Be(0);
        var lotesRestantes = new List<RedisKey>();
        await foreach (var llave in redis.Conexion.GetServers()[0].KeysAsync(pattern: LlavesRedis.PatronLotes))
        {
            lotesRestantes.Add(llave);
        }
        lotesRestantes.Should().BeEmpty();
    }

    [Fact]
    public async Task RNF_05_ErrorDentroDeLaTransaccion_RevierteElLoteYRecuperaLosDatos()
    {
        var api = await NuevaApi(await NuevaOrganizacion());
        var ruta = await NuevaRuta(api);
        var llave = LlavesRedis.Metricas(Fecha, api, ruta, null, "produccion");
        await Medir(llave, 3);
        // Un fallo real de PostgreSQL entre INSERT lote_consolidado y UPSERT consumo_diario.
        await Ejecutar("""
            CREATE FUNCTION interrumpir_consumo() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'Interrupción simulada'; END $$;
            CREATE TRIGGER interrupcion BEFORE INSERT ON consumo_diario
            FOR EACH ROW EXECUTE FUNCTION interrumpir_consumo();
            """);
        await Assert.ThrowsAnyAsync<Exception>(Consolidar);
        (await Escalar<long>("SELECT count(*) FROM lote_consolidado")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM consumo_diario")).Should().Be(0);
        await Ejecutar("DROP TRIGGER interrupcion ON consumo_diario");
        await Consolidar();
        (await Escalar<long>("SELECT sum(peticiones)::bigint FROM consumo_diario")).Should().Be(3);
    }

    [Fact]
    public async Task RF_33_Consolidacion_SumaTodosLosCamposYActualizaLaFechaConElReloj()
    {
        var api = await NuevaApi(await NuevaOrganizacion());
        var llave = LlavesRedis.Metricas(Fecha, api, null, null, "pruebas");
        await Medir(llave, 3);
        await Consolidar();
        Reloj.Ahora = Reloj.Ahora.AddDays(1);
        await Medir(llave, 2);
        await Consolidar();
        await using var db = CrearDb();
        var fila = await db.Set<ConsumoDiario>().SingleAsync();
        fila.Fecha.Should().Be(Fecha);
        fila.Entorno.Should().Be(EntornoConsumo.Pruebas);
        fila.Peticiones.Should().Be(5);
        fila.Llamadas.Should().Be(10);
        fila.BytesEntrada.Should().Be(15);
        fila.BytesSalida.Should().Be(20);
        fila.Rechazos401.Should().Be(5);
        fila.Rechazos403.Should().Be(5);
        fila.Rechazos404.Should().Be(5);
        fila.Rechazos429.Should().Be(5);
        fila.Origen2xx.Should().Be(5);
        fila.Origen3xx.Should().Be(5);
        fila.Origen4xx.Should().Be(5);
        fila.Origen5xx.Should().Be(5);
        fila.OrigenFallo.Should().Be(5);
        fila.HistLatenciaTotal.Should().Equal(Enumerable.Repeat(5, 10));
        fila.HistLatenciaCompuerta.Should().Equal(Enumerable.Repeat(5, 10));
        fila.LatenciaTotalSumaMs.Should().Be(50);
        fila.LatenciaCompuertaSumaMs.Should().Be(25);
        fila.ActualizadoEn.Should().Be(Reloj.Ahora);
        fila.CreadoEn.Should().Be(Reloj.Ahora.AddDays(-1));
    }

    [Fact]
    public async Task RNF_08_ConsultaDeConsumo_FiltraOrganizacionFechaYEntorno()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        await Medir(LlavesRedis.Metricas(Fecha, api, null, null, "produccion"), 3);
        await Medir(LlavesRedis.Metricas(Fecha.AddDays(-1), api, null, null, "produccion"), 7);
        await Medir(LlavesRedis.Metricas(Fecha, api, null, null, "pruebas"), 11);
        await Consolidar();
        await using var servicios = CrearServicios(ajustar: s => s.AgregarConsolidacion());
        using var alcance = servicios.CreateScope();
        var consulta = alcance.ServiceProvider.GetRequiredService<IConsultaConsumo>();
        var resultado = await consulta.PorApiAsync(organizacion, api, Fecha, Fecha, EntornoConsumo.Produccion);
        resultado.Peticiones.Should().Be(3);
        var ajena = await consulta.PorApiAsync(Guid.NewGuid(), api, Fecha, Fecha);
        ajena.Peticiones.Should().Be(0);
    }

    [Fact]
    public void RNF_05_Intervalos_DeConsolidacionYLatido_SonDiezSegundos()
    {
        new OpcionesConsolidacion().Intervalo.Should().Be(TimeSpan.FromSeconds(10));
    }

    private async Task Consolidar()
    {
        await using var servicios = CrearServicios(ajustar: s => s.AgregarConsolidacion());
        using var alcance = servicios.CreateScope();
        await alcance.ServiceProvider.GetRequiredService<ConsolidarConsumo>().EjecutarAsync(CancellationToken.None);
    }

    private async Task Medir(string llave, long cantidad)
    {
        HashEntry[] campos =
        [
            new("peticiones", cantidad), new("llamadas", cantidad * 2), new("bytes_entrada", cantidad * 3),
            new("bytes_salida", cantidad * 4), new("lt_suma", cantidad * 10), new("lc_suma", cantidad * 5),
            .. new[] { "r401", "r403", "r404", "r429", "o2xx", "o3xx", "o4xx", "o5xx", "ofallo" }
                .Select(x => new HashEntry(x, cantidad)),
            .. Enumerable.Range(0, 10).SelectMany(i => new[] { new HashEntry($"h_t_{i}", cantidad), new HashEntry($"h_c_{i}", cantidad) }),
        ];
        await Redis.HashSetAsync(llave, campos);
        await Redis.SetAddAsync(LlavesRedis.MetricasPendientes, llave);
    }
}
