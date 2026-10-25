---
id: JG-21
titulo: Timeout de Redis en el reloj y en los trabajos del trabajador
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-23
depende_de: []
requisitos: [RNF-06, RF-22, RF-34]
pantallas: []
---

# JG-21 · Timeout de Redis en el reloj y en los trabajos del trabajador

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P1 · **Sin dependencias**

## Objetivo
Que un *timeout* de Redis no produzca un 500 en la API de control ni detenga el trabajador. Hoy pasa en la PC donde se grabará el video del Avance 3.

## Origen
Hallazgo **H-C01** de la convergencia del 23 oct (`docs/plan/convergencia/2026-10-22.md` §4). En el ambiente productivo simulado, en 7 minutos hubo 18 `RedisTimeoutException`. El portal de Envíos Xelajú respondió 500 en `/api/portal/configuracion` y el trabajador se reinició 6 veces. Redis respondía en menos de 30 ms: los *timeouts* aparecen cuando la PC está saturada (por ejemplo, al abrir el navegador).

- `src/Shapi.Infraestructura/Comun/RelojSistema.cs:22-27` (EM-10): con `SHAPI_MODO_DEMO=true`, cada lectura de `IReloj.Ahora` hace un `StringGet` **síncrono** a Redis, con `SyncTimeout = 1000` (`ServiciosCache.cs:36`), y solo atrapa `RedisException`. `RedisTimeoutException` hereda de `TimeoutException`, no de `RedisException`, así que la excepción sube.
- `src/Shapi.Trabajador/Consolidacion/TrabajoConsolidacion.cs:62` (JG-09): `LatidoTrabajador` también atrapa solo `RedisException`. Con `BackgroundServiceExceptionBehavior.StopHost`, la excepción detiene el host completo (registro «BackgroundService failed … IHost instance is stopping»).

## Contexto que debes leer
- `docs/specs/09-cobros-y-suscripciones.md` §9 (modo demostración)
- `docs/specs/06-arquitectura.md` §8 (observabilidad y latidos)
- `docs/specs/07-modelo-de-datos.md` §4 (`demo:reloj:desplazamiento`)
- `src/Shapi.Api/Suscripciones/RelojDemoApi.cs`: publica el desplazamiento en la llave y en el canal del mismo nombre
- `src/Shapi.Trabajador/CierreCiclo/TrabajoCierreCiclo.cs:17-18`: ya se suscribe a ese canal

## Archivos que creas o modificas
- `src/Shapi.Infraestructura/Comun/RelojSistema.cs` (modificar)
- `src/Shapi.Trabajador/Consolidacion/TrabajoConsolidacion.cs` y los demás `BackgroundService` de `src/Shapi.Trabajador/` que puedan recibir un *timeout* de Redis (modificar)
- `tests/Shapi.Api.Tests/Comun/RelojSistemaTests.cs` y las pruebas del trabajador (modificar o crear)
- `docs/specs/09-cobros-y-suscripciones.md` §9 (precisar cómo el reloj conoce el desplazamiento)

## Criterios de aceptación
1. `IReloj.Ahora` **no espera a Redis** en cada lectura. El desplazamiento del modo demostración se guarda en memoria y se actualiza con el canal `demo:reloj:desplazamiento` y con una relectura periódica. Hoy el canal publica el incremento en días (`RelojDemoApi.cs:26`) y la llave guarda el total en segundos. La tarea decide qué lleva el mensaje, lo anota en 09 §9 y garantiza que `TrabajoCierreCiclo`, que escucha el mismo canal, lea la hora ya actualizada antes de cerrar el ciclo. Si Redis falla o tarda, por cualquier excepción, incluida `RedisTimeoutException`, se usa el último desplazamiento conocido. Fuera del modo demostración el comportamiento no cambia.
2. Después de `POST /api/admin/demo/reloj`, la API y el trabajador ven la hora nueva en menos de 1 segundo, como hoy. El cierre de ciclo que dispara ese endpoint usa la hora ya adelantada.
3. Ningún `BackgroundService` del trabajador detiene el host por un *timeout* o una caída de Redis. Registra una advertencia con el tipo de la excepción, como ya hace `TrabajoConsolidacion.cs:64`, y lo reintenta en su siguiente intervalo.
4. Una petición a la API de control no responde 500 por un *timeout* de Redis al leer la hora.

## Pruebas obligatorias
- `RelojSistema`: con un `IConnectionMultiplexer` que lanza `RedisTimeoutException` o `RedisConnectionException`, `Ahora` devuelve la hora real más el último desplazamiento conocido, sin excepción (RNF-06).
- `RelojSistema`: un mensaje del canal cambia el desplazamiento sin consultar la llave (09 §9).
- Cada `BackgroundService` del trabajador: si Redis lanza `RedisTimeoutException`, el trabajo sigue vivo y vuelve a intentarlo (RNF-06).
- Regresión de `RelojDemoApiTests` y `CierreCicloTests` (RF-22).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests
```
Además, en el ambiente productivo simulado (`docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml --profile demo up -d --build`), después de 10 minutos de uso, incluido abrir el navegador: `docker inspect -f '{{.RestartCount}}' shapi-trabajador-1` debe dar `0` y los registros de la API no deben tener `RedisTimeoutException` sin atrapar.

## Fuera de alcance
- Cambiar `SyncTimeout` o `AsyncTimeout` de la conexión (JG-04): siguen en 1 s.
- La compuerta: no usa `IReloj` y no tuvo *timeouts*.

## Resultado

- `RelojSistema` conserva el desplazamiento en memoria: `Ahora` no hace llamadas a Redis. El servicio carga y relee la llave de forma asíncrona cada cinco segundos, reintenta la suscripción y conserva la última hora conocida ante errores, incluidos los *timeouts*.
- El canal publica el desplazamiento total en segundos. La API actualiza primero su reloj y el trabajador recibe el cambio antes de disparar el cierre de ciclo; los mensajes duplicados, atrasados o inválidos no alteran incorrectamente la hora. La siembra inicializa el reloj antes del arranque del host, también en el comando de demostración. Fuera del modo demostración no se registra el servicio de sincronización.
- Todos los servicios de fondo del trabajador atrapan tanto `RedisException` como `RedisTimeoutException`, registran una advertencia con el tipo de excepción y continúan en su siguiente intervalo. No se cambiaron los límites de tiempo, los contratos HTTP ni las dependencias de la aplicación.
- Se precisó el protocolo del reloj en `09-cobros-y-suscripciones.md` §9 y `07-modelo-de-datos.md` §4. Archivos principales: `Comun/RelojSistema.cs`, `Comun/ServiciosComunes.cs`, `Suscripciones/RelojDemoApi.cs`, `Siembra/Base/SiembraBase.cs`, los trabajos de `Shapi.Trabajador` y sus pruebas de reloj, suscripciones, siembra y resincronización.
- Verificación: compilación completa con **0 errores y 0 advertencias**; formato sin cambios; **1.012 pruebas de `Shapi.Api.Tests` aprobadas, sin fallos ni omisiones**, con dos hilos por la memoria disponible. Incluye Redis real, propagación entre API y trabajador en **76 ms**, los seis trabajos ante desconexiones y *timeouts*, y las regresiones de autorización, siembra y cierre de ciclo. La prueba previa de resincronización exigía nivel Error; se ajustó a la advertencia requerida por el criterio 3 y se reforzó para exigir dos intentos, el tipo de excepción y que el trabajo siga vivo.
- Ambiente productivo simulado reconstruido con el comando de la tarea: ensayo de **627 segundos**, navegador abierto, **90 respuestas HTTP 200 y ningún fallo**. Redis estuvo pausado 12 segundos; el portal siguió respondiendo, el latido se recuperó, el trabajador terminó con **0 reinicios** y la API con **0 errores de Redis sin atrapar**. Evidencia local en `.shapi/jg21/` (registros, TRX, resumen y captura del portal).
- Revisión independiente: se corrigió la carga del reloj antes de la siembra y se verificó que no abre Redis fuera del modo demostración. Segunda revisión del código: **LISTO**, sin hallazgos pendientes.
