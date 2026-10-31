# Elemento de Jordin · La compuerta como cadena de filtros (RNF-13)

## Patrón y código que mostraré

**Cadena de responsabilidad (patrón de comportamiento).** Cada petición que llega a una API publicada pasa por una cadena de filtros. Cada filtro revisa una sola regla y decide entre dejarla seguir (`Continuar`) o detenerla con un error (`Rechazar`). El primero que rechaza corta la cadena, y la petición nunca llega al servidor del proveedor. Los filtros implementan la misma interfaz, `IFiltroCompuerta` (`src/Shapi.Compuerta/Filtros/IFiltroCompuerta.cs:3-10`), y no se conocen entre sí. `TuberiaCompuerta` arma la cadena y la recorre (`src/Shapi.Compuerta/TuberiaCompuerta.cs:39-52` y `:93-117`). A diferencia de la versión clásica del patrón, y de los *middleware* de ASP.NET Core, donde cada eslabón recibe al siguiente y decide si lo llama, aquí el filtro no conoce al siguiente: el orden vive en una sola lista y la tubería decide si sigue.

Problema que resuelve: la compuerta tiene que aplicar ocho reglas a cada petición (08 §3): CORS, API publicada, clave, organización activa, suscripción activa o en gracia, ruta expuesta, límites y cuotas, y caché. Las reglas que rechazan tienen su propio código de error: 404, 401, 403 o 429. Si todo estuviera en un solo método, cada regla nueva obligaría a tocar las demás. RNF-13 exige lo contrario: **agregar una validación es agregar un filtro sin modificar los demás**. La cadena lo cumple, y hay una prueba real: la caché de respuestas (JG-15, PR #116) entró como una clase nueva, `FiltroCache`, y una línea en `Orden`, sin cambiar ningún otro filtro. Fuera del filtro se agregaron la propiedad `CapturarRespuesta` en `ContextoPeticion` (3 líneas) y su uso en la tubería, para capturar la respuesta del origen y poder guardarla.

**Clean Code que se ve en este elemento:**

1. **Responsabilidad única y abierto/cerrado.** Cada clase revisa una sola regla. `FiltroOrganizacion` y `FiltroSuscripcion` son una sola expresión (`src/Shapi.Compuerta/Filtros/FiltroOrganizacion.cs:9-17` y `FiltroSuscripcion.cs:10-18`). Los filtros existentes quedan cerrados a cambios y la cadena abierta a extensión: para agregar un filtro se escribe la clase y se agrega a `Orden`. El registro en el contenedor de dependencias recorre esa misma lista (`src/Shapi.Compuerta/ServiciosCompuerta.cs:52-62`), así que el orden se define en un solo lugar.
2. **Nombres que revelan la intención y errores explícitos.** `ResultadoFiltro` solo se crea con `Continuar`, `Rechazar` o `Responder` (`src/Shapi.Compuerta/Filtros/ResultadoFiltro.cs:22`, `:40-45`). Cada rechazo es una constante con nombre (`NoDisponible`, `Inactiva`, `Ausente`, `Invalida`), que lleva su estado HTTP, un código de `CodigosError` y un mensaje para el consumidor, sin números sueltos ni textos repetidos (`src/Shapi.Compuerta/Filtros/FiltroOrganizacion.cs:11-13` y `FiltroClave.cs:19-29`). Se lee `contexto.Organizacion is { EstaActiva: true } ? Continuar : NoDisponible` y ya se entiende la regla.
3. **Funciones pequeñas y una sola fuente de datos.** Antes de la cadena, `ILectorContexto` trae de Redis la API, la clave, la organización y la suscripción, en tres viajes como máximo (`TuberiaCompuerta.cs:97`). Los filtros de validación (0 a 5) solo leen ese contexto y no van a Redis por su cuenta, así que se prueban sin red. Los filtros 6 y 7 sí consultan Redis, porque su regla vive ahí: el script Lua atómico de límites y cuotas (`FiltroLimitesYCuotas.cs:131-141`) y las respuestas compartidas de la caché (`FiltroCache.cs:29`); sus pruebas usan Redis real. Si Redis cae en cualquier filtro, la tubería lo maneja una sola vez, con un 503 (`:109-116`).

Fragmentos de `main`. El primero es el orden de la cadena, el único lugar donde se define.

Fragmento de `main`, `src/Shapi.Compuerta/TuberiaCompuerta.cs:39-52` (14 líneas, copiado sin cambios):

```csharp
    /// <summary>
    /// El orden de los filtros, definido solo aquí. Agregar una regla es agregar su clase y una línea (RNF-13).
    /// </summary>
    public static IReadOnlyList<Type> Orden { get; } =
    [
        typeof(FiltroCors), // 0 · CORS: OPTIONS → 204
        typeof(FiltroApi), // 1 · host → API publicada
        typeof(FiltroClave), // 2 · X-Api-Key → hash → Redis
        typeof(FiltroOrganizacion), // 3 · organización activa
        typeof(FiltroSuscripcion), // 4 · suscripción activa o en gracia
        typeof(FiltroRuta), // 5 · método + patrón expuestos
        typeof(FiltroLimitesYCuotas), // 6 · límites por minuto y cuotas (un script Lua)
        typeof(FiltroCache), // 7 · caché de respuestas GET, después de descontar cuota
    ];
```

El segundo es el recorrido: el primer rechazo detiene la cadena.

Fragmento de `main`, `src/Shapi.Compuerta/TuberiaCompuerta.cs:93-108` (16 líneas, copiado sin cambios):

```csharp
    private async Task<ResultadoFiltro> EvaluarAsync(ContextoPeticion contexto)
    {
        try
        {
            await lector.LeerAsync(contexto);
            foreach (var filtro in _filtros)
            {
                var resultado = await filtro.EvaluarAsync(contexto);
                if (!resultado.Continua)
                {
                    return resultado;
                }
            }

            return ResultadoFiltro.Continuar;
        }
```

**Pruebas automatizadas:**

- `Shapi.Compuerta.Tests.Tuberia.TuberiaCompuertaTests.RNF_13_Procesar_UnFiltroRechaza_DetieneLaCadenaYNoReenvia` (`tests/Shapi.Compuerta.Tests/Tuberia/TuberiaCompuertaTests.cs:19-42`): tres filtros de prueba, el segundo rechaza. Comprueba que el tercero no se evalúa, que no se reenvía al origen y que se responde el 403 con el JSON del error.
- `RNF_13_Procesar_TodosContinuan_EvaluaEnOrdenYReenvia` (`:44-60`): si todos dejan pasar, se evalúan en orden y después se reenvía.
- `RNF_13_Orden_DefinidoEnUnSoloLugar` (`:128-135`): fija el orden de 08 §3 y exige que cada tipo de `Orden` implemente `IFiltroCompuerta`.
- Los filtros de validación se prueban por separado, sin Redis: por ejemplo, `Shapi.Compuerta.Tests.Filtros.FiltrosValidacionTests.RF_29_FiltroOrganizacion_Suspendida_Rechaza403ApiNoDisponible` (`tests/Shapi.Compuerta.Tests/Filtros/FiltrosValidacionTests.cs:23-32`) y `RF_29_FiltroSuscripcion_ActivaConElCicloVencido_ContinuaPorqueNoComparaFechas` (`:73-82`). También se prueba la cadena completa, con Redis real, en `Shapi.Compuerta.Tests.Tuberia.ValidacionesCompuertaTests` (`tests/Shapi.Compuerta.Tests/Tuberia/ValidacionesCompuertaTests.cs:16-27`, organización suspendida → 403 y el origen no recibe nada).

Para comprobarlas antes de grabar (el video no muestra la terminal, solo el código de las pruebas en el editor):

```powershell
dotnet test tests/Shapi.Compuerta.Tests --filter "FullyQualifiedName~Shapi.Compuerta.Tests.Tuberia.TuberiaCompuertaTests|FullyQualifiedName~Shapi.Compuerta.Tests.Filtros.FiltrosValidacionTests"
```

En JG-29 (30 oct 2026) pasaron las 27: 0 con error y 0 omitidas, en 203 ms.

## Texto para narrar (2 a 3 minutos)

«Yo trabajé en la compuerta, las claves, la medición del consumo y el modo demostración. Voy a mostrar cómo decide la compuerta si una petición pasa.

Cada petición a una API publicada pasa por ocho filtros. Revisan que la API exista, que la clave sea válida, que la organización esté activa, que la suscripción no esté suspendida, que la ruta esté expuesta y que no se pase de su límite ni de su cuota. Cada uno tiene su error: 404, 401, 403 o 429.

Para no meter todo en un método enorme usamos una cadena de responsabilidad. Cada regla es una clase pequeña que implementa la misma interfaz, `IFiltroCompuerta`, con un solo método: recibe el contexto de la petición y devuelve `Continuar` o `Rechazar`. Este es el filtro de la organización: una línea que dice «si está activa, continúa; si no, API no disponible».

El orden de la cadena está en una sola lista, y la tubería la recorre: evalúa un filtro tras otro y, en cuanto uno rechaza, se detiene y responde el error. La petición no llega al servidor del proveedor.

Lo hicimos así por un requisito de mantenibilidad, RNF-13: agregar una validación es agregar un filtro sin tocar los demás. Y se cumplió: la caché de respuestas entró como una clase nueva y una línea en esta lista, más un pequeño ajuste en el contexto y la tubería para guardar la respuesta. Ningún otro filtro cambió.

En Clean Code: cada clase tiene una sola responsabilidad, y los rechazos son constantes con nombre, con su estado HTTP y su código de error, sin números mágicos. Los filtros de validación no van a Redis: un lector trae los datos antes de la cadena, así que se prueban sin red.

Por último, esta prueba arma tres filtros, el segundo rechaza, y comprueba que el tercero nunca se ejecuta y que no se reenvía nada. Esta otra fija el orden. Estas pruebas y las de cada filtro corren en cada cambio, en la integración continua.»

**Orden de lo que se muestra en pantalla** (duración objetivo: 2 min 15 s; unas 320 palabras):

| Tiempo | Archivo y líneas | Qué resaltar |
|---|---|---|
| 0:00–0:25 | `docs/specs/08-compuerta.md` §3 (el diagrama de la tubería, en la vista previa de Markdown) | Las ocho reglas y sus errores. |
| 0:25–0:50 | `src/Shapi.Compuerta/Filtros/IFiltroCompuerta.cs:3-10` y `src/Shapi.Compuerta/Filtros/FiltroOrganizacion.cs:9-17` | La interfaz de un solo método y un filtro de una línea. |
| 0:50–1:25 | `src/Shapi.Compuerta/TuberiaCompuerta.cs:39-52` y `:93-108` | El orden en un solo lugar; el `foreach` y el `return` en el primer rechazo. |
| 1:25–1:50 | `src/Shapi.Compuerta/Filtros/FiltroClave.cs:19-29` y `src/Shapi.Compuerta/Filtros/ResultadoFiltro.cs:40-45` | Rechazos con nombre, código y mensaje; `Rechazar` y `Responder`. |
| 1:50–2:15 | `tests/Shapi.Compuerta.Tests/Tuberia/TuberiaCompuertaTests.cs:19-42`, `:128-135` | Las aserciones: el tercer filtro no se evalúa, no se reenvía y el orden queda fijo. |

## Funcionalidades de Jordin para el recorrido de la interfaz

| Funcionalidad construida | Tareas | Pantallas que conviene mostrar |
|---|---|---|
| Compuerta: API, clave, organización, suscripción, ruta, CORS y protección SSRF | JG-02, JG-05 | A5.2 (consola): 200, 401 con una clave inválida y 403 con la suscripción suspendida |
| Límites por minuto y cuotas, con sus cabeceras | JG-06 | A5.2: «Límites y cuota» (`X-RateLimit-*` y `X-Cuota-*`) y el 429 al pasar el límite |
| Caché de respuestas | JG-15 | A3.5 (columna de caché por ruta); `X-Shapi-Cache` en la consola |
| Emisión, rotación y revocación de claves | JG-07, JG-10 | A4.3 y A4.3b en el panel; B2.4 y B2.5 en el portal |
| Medición y consolidación del consumo | JG-09 | B1.1 (p95, errores por código, consumo por ruta) |
| Consumo del consumidor y facturación por consumidor | JG-12, JG-13 | B2.1 y B1.2 |
| Publicación de la configuración en Redis y la compuerta sin PostgreSQL | JG-04, JG-19 | Se explica al publicar la API (A3.1): la compuerta enruta sin consultar la base de datos |
| Modo demostración: reloj desde el panel | JG-21, JG-22 | B3.1, tarjeta «Modo demostración» (+30 y +7 días) |
| CI, revisión automática y tablero del plan | JG-01, JG-03, JG-18 | GitHub: checks en verde de un PR (opcional, solo si sobra tiempo) |
