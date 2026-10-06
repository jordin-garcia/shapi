using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Shapi.Contratos;
using Shapi.Contratos.Redis;
using StackExchange.Redis;

namespace Shapi.Compuerta.Filtros;

/// <summary>
/// Filtro 6 (08 §3): el límite por minuto del plan y el de la ruta, la cuota del consumidor y la cuota de plataforma
/// del proveedor, reservados de forma atómica con <c>evaluar_limites.lua</c> en una sola llamada <c>EVALSHA</c>
/// (RF-30, ADR-21). Con la clave de pruebas usa los límites fijos de RF-45 y no toca las cuotas. Toda respuesta que
/// pasa por aquí, incluidos los 429, lleva las cabeceras de 08 §5 (RF-32).
/// </summary>
public sealed class FiltroLimitesYCuotas(
    IConnectionMultiplexer redis,
    TimeProvider reloj,
    ILogger<FiltroLimitesYCuotas> registro) : IFiltroCompuerta
{
    /// <summary>El <c>X-Shapi-Plan</c> de la clave de pruebas (08 §5).</summary>
    public const string PlanPruebas = "Pruebas";

    /// <summary>RF-45: 10 peticiones por minuto y 1,000 por día.</summary>
    public const int LimiteMinutoPruebas = 10;

    public const int LimiteDiaPruebas = 1_000;

    private const string RecursoScript = "Shapi.Compuerta.Lua.evaluar_limites.lua";

    /// <summary>Los contadores de las cuotas viven 8 días más que su ciclo (07 §4).</summary>
    private static readonly TimeSpan MargenCuota = TimeSpan.FromDays(8);

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre",
        "diciembre",
    ];

    // Guatemala no tiene horario de verano (UTC−6). Si el sistema no trae la base de zonas, se usa ese desfase fijo.
    private static readonly TimeZoneInfo ZonaGuatemala = BuscarZonaGuatemala();

    /// <summary>El texto de <c>evaluar_limites.lua</c>.</summary>
    public static string Script { get; } = LeerScript();

#pragma warning disable CA5350 // Redis identifica los scripts por su SHA-1 (EVALSHA): no es un uso criptográfico.
    private static readonly byte[] HashScript = SHA1.HashData(Encoding.UTF8.GetBytes(Script));
#pragma warning restore CA5350

    public async ValueTask<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        var clave = contexto.Clave ?? throw new InvalidOperationException("FiltroLimitesYCuotas debe ir después de FiltroClave.");
        var ruta = contexto.Ruta ?? throw new InvalidOperationException("FiltroLimitesYCuotas debe ir después de FiltroRuta.");
        var ahora = reloj.GetUtcNow();
        var reserva = clave.Tipo == ContextoClave.TipoPruebas
            ? ReservaDePruebas(clave, ahora)
            : ReservaDeProduccion(contexto, ruta, ahora);

        var db = redis.GetDatabase();
        var valores = (long[]?)await EjecutarScriptAsync(db, reserva.Llaves(), reserva.Argumentos())
            ?? throw new InvalidOperationException("evaluar_limites.lua no devolvió los contadores.");
        var conteo = new Conteo(valores);

        // Las cabeceras se calculan al responder: si el origen no se pudo conectar, ya reflejan la cuota devuelta.
        // Reemplazan las que haya mandado el origen con el mismo nombre.
        var respuesta = contexto.Http.Response;
        respuesta.OnStarting(() =>
        {
            foreach (var (nombre, valor) in Cabeceras(reserva, conteo, ahora))
            {
                respuesta.Headers[nombre] = valor;
            }

            return Task.CompletedTask;
        });

        if (conteo.Codigo != Conteo.Reservado)
        {
            return Rechazo(reserva, conteo.Codigo, ahora);
        }

        if (!reserva.EsPruebas)
        {
            contexto.LlamadasDescontadas = reserva.Peso;
            contexto.DevolverReserva = async () =>
            {
                if (await DevolverAsync(db, reserva, conteo))
                {
                    contexto.LlamadasDescontadas = 0;
                }
            };
        }

        return ResultadoFiltro.Continuar;
    }

    /// <summary>
    /// El valor de <c>X-Shapi-Plan</c> (08 §5): el nombre del plan codificado como componente de URI, en UTF-8 con
    /// porcentajes ("Básico" → "B%C3%A1sico"), porque Kestrel rechaza las cabeceras de respuesta que no son ASCII.
    /// Un nombre ASCII sin espacios ni símbolos ("Comercio", "Pruebas") queda igual; se recupera con
    /// <c>decodeURIComponent</c> o <see cref="Uri.UnescapeDataString(string)"/>.
    /// </summary>
    public static string ValorCabeceraPlan(string nombre) => Uri.EscapeDataString(nombre);

    /// <summary>Segundos hasta el siguiente minuto (<c>Retry-After</c> y <c>X-RateLimit-Reset</c>): de 1 a 60.</summary>
    public static long SegundosParaSiguienteMinuto(DateTimeOffset ahora) => 60 - (ahora.ToUnixTimeSeconds() % 60);

    /// <summary>
    /// Segundos hasta <paramref name="momento"/>, redondeados hacia arriba para no invitar a reintentar antes de
    /// tiempo. Si el momento ya pasó (un ciclo en gracia), 1.
    /// </summary>
    public static long SegundosHasta(DateTimeOffset momento, DateTimeOffset ahora) =>
        Math.Max(1, (long)Math.Ceiling((momento - ahora).TotalSeconds));

    /// <summary>El día en Guatemala, con el que se cuenta el límite diario de la clave de pruebas (07 §4).</summary>
    public static DateOnly DiaGuatemala(DateTimeOffset momento) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(momento, ZonaGuatemala).DateTime);

    /// <summary>La medianoche de Guatemala que sigue a <paramref name="momento"/>, en UTC.</summary>
    public static DateTimeOffset FinDelDiaGuatemala(DateTimeOffset momento)
    {
        var medianoche = TimeZoneInfo.ConvertTime(momento, ZonaGuatemala).Date.AddDays(1);
        return new DateTimeOffset(medianoche, ZonaGuatemala.GetUtcOffset(medianoche)).ToUniversalTime();
    }

    /// <summary>
    /// Criterio 8: una sola llamada <c>EVALSHA</c>. Si Redis no tiene el script (recién iniciado, o vació su caché de
    /// scripts), responde <c>NOSCRIPT</c> sin ejecutar nada; entonces se ejecuta una vez con <c>EVAL</c>, que lo deja
    /// guardado, y las siguientes peticiones vuelven a usar <c>EVALSHA</c>.
    /// </summary>
    private static async Task<RedisResult> EjecutarScriptAsync(IDatabase db, RedisKey[] llaves, RedisValue[] argumentos)
    {
        try
        {
            return await db.ScriptEvaluateAsync(HashScript, llaves, argumentos);
        }
        catch (RedisServerException excepcion) when (excepcion.Message.StartsWith("NOSCRIPT", StringComparison.Ordinal))
        {
            return await db.ScriptEvaluateAsync(Script, llaves, argumentos, CommandFlags.NoScriptCache);
        }
    }

    private static Reserva ReservaDeProduccion(ContextoPeticion contexto, RutaCache ruta, DateTimeOffset ahora)
    {
        var suscripcion = contexto.Suscripcion
            ?? throw new InvalidOperationException("FiltroLimitesYCuotas debe ir después de FiltroSuscripcion.");
        var organizacion = contexto.Organizacion
            ?? throw new InvalidOperationException("FiltroLimitesYCuotas debe ir después de FiltroOrganizacion.");
        var minuto = MinutoEpoch(ahora);
        var finCuota = DateTimeOffset.FromUnixTimeSeconds(suscripcion.Fin);

        // La organización de la plataforma no tiene suscripción de plataforma, ni cuota ni ciclo (07 §4).
        var plataforma = organizacion is { CuotaPeticiones: > 0, CicloInicio: { } cicloInicio, CicloFin: { } cicloFin }
            ? new CuotaPlataforma(LlavesRedis.CuotaOrganizacion(organizacion.OrganizacionId, cicloInicio),
                organizacion.CuotaPeticiones.Value, DateTimeOffset.FromUnixTimeSeconds(cicloFin))
            : null;

        return new Reserva(
            Plan: suscripcion.PlanNombre,
            Minuto: LlavesRedis.LimiteMinutoSuscripcion(suscripcion.SuscripcionId, minuto),
            LimiteMinuto: suscripcion.LimiteMinuto,
            Ruta: ruta.LimiteMinuto is > 0
                ? (RedisKey?)LlavesRedis.LimiteMinutoRuta(suscripcion.SuscripcionId, ruta.RutaId, minuto)
                : null,
            LimiteRuta: ruta.LimiteMinuto is > 0 ? ruta.LimiteMinuto.Value : 0,
            Cuota: LlavesRedis.CuotaSuscripcion(suscripcion.SuscripcionId, suscripcion.Inicio),
            LimiteCuota: suscripcion.CuotaLlamadas,
            Peso: Math.Max(1, ruta.Peso),
            FinCuota: finCuota,
            ExpiraCuota: Expiracion(finCuota, ahora),
            Plataforma: plataforma,
            ExpiraPlataforma: plataforma is null ? ahora : Expiracion(plataforma.Fin, ahora),
            EsPruebas: false);
    }

    private static Reserva ReservaDePruebas(ContextoClave clave, DateTimeOffset ahora)
    {
        // El límite diario se cuenta como una "cuota" de 1,000 peticiones que termina a medianoche (08 §5).
        var finDelDia = FinDelDiaGuatemala(ahora);
        return new Reserva(
            Plan: PlanPruebas,
            Minuto: LlavesRedis.LimiteMinutoPruebas(clave.ClaveId, MinutoEpoch(ahora)),
            LimiteMinuto: LimiteMinutoPruebas,
            Ruta: null,
            LimiteRuta: 0,
            Cuota: LlavesRedis.LimiteDiaPruebas(clave.ClaveId, DiaGuatemala(ahora)),
            LimiteCuota: LimiteDiaPruebas,
            Peso: 1,
            FinCuota: finDelDia,
            ExpiraCuota: finDelDia.AddDays(1),
            Plataforma: null,
            ExpiraPlataforma: ahora,
            EsPruebas: true);
    }

    /// <summary>
    /// <c>fin + 8 días</c> (07 §4). Si el trabajador no ha cerrado un ciclo que ya terminó, esa hora puede haber
    /// pasado; un <c>EXPIREAT</c> en el pasado borraría el contador y reiniciaría la cuota en cada petición. Por eso
    /// se cuenta desde ahora si el fin ya pasó.
    /// </summary>
    private static DateTimeOffset Expiracion(DateTimeOffset fin, DateTimeOffset ahora) =>
        (fin > ahora ? fin : ahora) + MargenCuota;

    private static long MinutoEpoch(DateTimeOffset ahora) => ahora.ToUnixTimeSeconds() / 60;

    private static IEnumerable<(string Nombre, string Valor)> Cabeceras(Reserva reserva, Conteo conteo, DateTimeOffset ahora)
    {
        var limiteMinuto = reserva.Ruta is null ? reserva.LimiteMinuto : Math.Min(reserva.LimiteMinuto, reserva.LimiteRuta);
        var restanteMinuto = reserva.LimiteMinuto - conteo.Minuto;
        if (reserva.Ruta is not null)
        {
            restanteMinuto = Math.Min(restanteMinuto, reserva.LimiteRuta - conteo.Ruta);
        }

        yield return (CabecerasCompuerta.Plan, ValorCabeceraPlan(reserva.Plan));
        yield return (CabecerasCompuerta.LimiteMinuto, Texto(limiteMinuto));
        yield return (CabecerasCompuerta.RestanteMinuto, Texto(Math.Max(0, restanteMinuto)));
        yield return (CabecerasCompuerta.ReinicioMinuto, Texto(SegundosParaSiguienteMinuto(ahora)));
        yield return (CabecerasCompuerta.CuotaLimite, Texto(reserva.LimiteCuota));
        yield return (CabecerasCompuerta.CuotaRestante, Texto(Math.Max(0, reserva.LimiteCuota - conteo.Cuota)));
        yield return (CabecerasCompuerta.CuotaReinicio,
            reserva.FinCuota.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
    }

    private static ResultadoFiltro Rechazo(Reserva reserva, long codigo, DateTimeOffset ahora)
    {
        var segundosMinuto = SegundosParaSiguienteMinuto(ahora);
        var (codigoError, mensaje, reintento) = codigo switch
        {
            Conteo.LimitePlan => (CodigosError.LimitePorMinuto,
                reserva.EsPruebas
                    ? $"Superó el límite de {Numero(reserva.LimiteMinuto)} peticiones por minuto de su clave de pruebas. Intente de nuevo en {segundosMinuto} segundos."
                    : $"Superó el límite de {Numero(reserva.LimiteMinuto)} peticiones por minuto de su plan {reserva.Plan}. Intente de nuevo en {segundosMinuto} segundos.",
                segundosMinuto),
            Conteo.LimiteRuta => (CodigosError.LimitePorMinuto,
                $"Superó el límite de {Numero(reserva.LimiteRuta)} peticiones por minuto de esta ruta. Intente de nuevo en {segundosMinuto} segundos.",
                segundosMinuto),
            Conteo.CuotaAgotada => (CodigosError.CuotaAgotada,
                reserva.EsPruebas
                    ? $"Agotó las {Numero(reserva.LimiteCuota)} peticiones diarias de su clave de pruebas. El límite se renueva a medianoche."
                    : $"Agotó las {Numero(reserva.LimiteCuota)} llamadas de su plan {reserva.Plan} en este ciclo. La cuota se renueva el {Fecha(reserva.FinCuota)}.",
                SegundosHasta(reserva.FinCuota, ahora)),
            Conteo.PlataformaAgotada => (CodigosError.CuotaPlataformaAgotada,
                "El proveedor de esta API agotó la cuota de peticiones de su plan en Shapi. Intente de nuevo más tarde.",
                SegundosHasta(reserva.Plataforma!.Fin, ahora)),
            _ => throw new InvalidOperationException($"evaluar_limites.lua devolvió un código desconocido: {codigo}."),
        };

        return ResultadoFiltro.Rechazar(StatusCodes.Status429TooManyRequests, codigoError, mensaje,
            new Dictionary<string, string> { ["Retry-After"] = Texto(reintento) });
    }

    /// <summary>
    /// 08 §3: si el origen no se pudo conectar (502), la petición no llegó y se devuelve la cuota reservada. Los
    /// contadores por minuto no se devuelven. Si Redis falla, solo se registra: el 502 se responde igual.
    /// </summary>
    private async Task<bool> DevolverAsync(IDatabase db, Reserva reserva, Conteo conteo)
    {
        try
        {
            var lote = db.CreateBatch();
            var pendientes = new List<Task> { lote.StringDecrementAsync(reserva.Cuota, reserva.Peso) };
            if (reserva.Plataforma is { } plataforma)
            {
                pendientes.Add(lote.StringDecrementAsync(plataforma.Llave));
            }

            lote.Execute();
            await Task.WhenAll(pendientes);
            conteo.Cuota -= reserva.Peso;
            return true;
        }
        catch (RedisException excepcion)
        {
            registro.LogWarning("No se pudo devolver la cuota de una petición que no llegó al origen: {Motivo}", excepcion.Message);
            return false;
        }
    }

    private static string Texto(long valor) => valor.ToString(CultureInfo.InvariantCulture);

    /// <summary>Con separador de miles, como en 08 §4: "50,000".</summary>
    private static string Numero(long valor) => valor.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>"1 de octubre de 2026", en la fecha de Guatemala. Sin depender de la cultura del sistema.</summary>
    private static string Fecha(DateTimeOffset momento)
    {
        var local = TimeZoneInfo.ConvertTime(momento, ZonaGuatemala);
        return $"{local.Day} de {Meses[local.Month - 1]} de {local.Year}";
    }

    private static string LeerScript()
    {
        using var flujo = typeof(FiltroLimitesYCuotas).Assembly.GetManifestResourceStream(RecursoScript)
            ?? throw new InvalidOperationException($"Falta el recurso embebido {RecursoScript}.");
        using var lector = new StreamReader(flujo);
        return lector.ReadToEnd();
    }

    private static TimeZoneInfo BuscarZonaGuatemala()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("America/Guatemala", TimeSpan.FromHours(-6), "America/Guatemala", "America/Guatemala");
        }
    }

    /// <param name="Fin">El fin del ciclo de plataforma.</param>
    private sealed record CuotaPlataforma(RedisKey Llave, long Limite, DateTimeOffset Fin);

    /// <summary>Lo que el script reserva para una petición: las llaves, los límites y los momentos en que vencen.</summary>
    /// <param name="Ruta"><c>null</c> si la ruta no tiene límite propio.</param>
    /// <param name="Cuota">La cuota del consumidor o, con la clave de pruebas, el contador diario.</param>
    private sealed record Reserva(
        string Plan,
        RedisKey Minuto,
        int LimiteMinuto,
        RedisKey? Ruta,
        int LimiteRuta,
        RedisKey Cuota,
        long LimiteCuota,
        int Peso,
        DateTimeOffset FinCuota,
        DateTimeOffset ExpiraCuota,
        CuotaPlataforma? Plataforma,
        DateTimeOffset ExpiraPlataforma,
        bool EsPruebas)
    {
        /// <summary>En el orden que espera el script, solo las que aplican.</summary>
        public RedisKey[] Llaves() =>
        [
            Minuto,
            .. Ruta is { } ruta ? [ruta] : Array.Empty<RedisKey>(),
            Cuota,
            .. Plataforma is { } plataforma ? [plataforma.Llave] : Array.Empty<RedisKey>(),
        ];

        public RedisValue[] Argumentos() =>
        [
            LimiteMinuto,
            Ruta is null ? 0 : LimiteRuta,
            Peso,
            LimiteCuota,
            ExpiraCuota.ToUnixTimeSeconds(),
            Plataforma?.Limite ?? 0,
            Plataforma is null ? 0 : ExpiraPlataforma.ToUnixTimeSeconds(),
        ];
    }

    /// <summary>Lo que devuelve el script: el código y los contadores después de reservar o de revertir.</summary>
    private sealed class Conteo(long[] valores)
    {
        public const long Reservado = 0;
        public const long LimitePlan = 1;
        public const long LimiteRuta = 2;
        public const long CuotaAgotada = 3;
        public const long PlataformaAgotada = 4;

        public long Codigo { get; } = valores[0];

        public long Minuto { get; } = valores[1];

        public long Ruta { get; } = valores[2];

        /// <summary>Baja si la reserva se devuelve (502).</summary>
        public long Cuota { get; set; } = valores[3];
    }
}
