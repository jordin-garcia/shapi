---
id: JG-05
titulo: Compuerta: organización, suscripción, ruta, secreto, SSRF y CORS
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: hecha
programada: 2026-09-30
depende_de: [JG-04, DC-04]
requisitos: [RF-29, RF-31, RF-47, RNF-10]
pantallas: []
---

# JG-05 · Compuerta: organización, suscripción, ruta, secreto, SSRF y CORS

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JG-04, DC-04

## Objetivo
Completar los filtros de validación de la compuerta (0, 3, 4 y 5), las cabeceras hacia el origen, la protección contra SSRF en cada conexión y los errores de reenvío.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §1, §3 (filtros 0 a 5 y 8), §4, §5 (cabeceras hacia el origen), §6 (CORS) y §8 (viajes a Redis)
- `docs/specs/10-identidad-y-seguridad.md` §4 (SSRF)
- `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs` (lo creó DC-04; léelo en su sección Resultado)

## Archivos que creas o modificas
- `src/Shapi.Compuerta/Filtros/{FiltroCors,FiltroOrganizacion,FiltroSuscripcion,FiltroRuta}.cs` (crear)
- `src/Shapi.Compuerta/**` (modificar: tubería, transformaciones y `SocketsHttpHandler`)
- `tests/Shapi.Compuerta.Tests/**`

## Criterios de aceptación
1. Si la organización tiene `estado_efectivo=suspendida` → 403 `api_no_disponible`.
2. Si la suscripción está `suspendida` o `finalizada` → 403 `suscripcion_inactiva`. El filtro **no compara fechas** (08 §3, filtro 4).
3. Si el método y la ruta no existen o están ocultos → 403 `ruta_no_permitida`. La coincidencia usa patrones OpenAPI (`/guias/{numero}` coincide con `/guias/GT123`) con la regla de especificidad de 08 §1. Las rutas se guardan en memoria 5 segundos como máximo, validando la `version`.
4. Hacia el origen: se quitan `X-Api-Key` y cualquier `X-Shapi-*` que haya enviado el cliente, y se agregan `X-Shapi-Consumidor`, `X-Shapi-Entorno`, `X-Shapi-Secreto` (si la API lo tiene) y `X-Forwarded-For/Proto/Host`.
5. Cada conexión pasa por un `ConnectCallback` que usa `ValidadorDireccionOrigen` y rechaza direcciones internas con 502 `origen_inaccesible`. En modo demostración se permite `SHAPI_ORIGENES_PERMITIDOS` (entradas `host:puerto`). No se siguen redirecciones del origen.
6. Si no se puede conectar al origen → 502 `origen_inaccesible`. Si el origen tarda más de 30 s → 504 `origen_sin_respuesta`. Si el cuerpo pesa más de 10 MB → 413 `cuerpo_demasiado_grande`.
7. CORS según 08 §6: el *preflight* `OPTIONS` se responde sin clave con 204 y `Access-Control-Allow-Origin` igual al `portal_host` de la API. Otros orígenes no reciben esa cabecera. Las peticiones sin `Origin` pasan normal.
8. Hacia el origen nunca se envían las cookies de Shapi: se quitan `shapi_sesion` y `portal_sesion` de la cabecera `Cookie` (08 §5); las demás cookies pasan.
9. El contexto de cada petición se lee de Redis en dos *pipelines* (08 §8): el primero con `api:host:{host}` y `clave:{hash}`, y el segundo con `api:{id}`, sus rutas, `org:{id}` y `susc:{id}`.

## Pruebas obligatorias
- Unitarias por filtro (casos válidos y de rechazo)
- Integración de la tubería completa con Redis (Testcontainers) y un origen falso
- SSRF: origen `127.0.0.1` y `10.0.0.5` → 502; origen permitido en modo demostración → 200
- El origen no recibe `shapi_sesion` ni `portal_sesion`, y sí las demás cookies (criterio 8)
- Cada petición hace exactamente dos viajes a Redis para el contexto (criterio 9), contados con un contador de comandos o de *pipelines*

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Límites y cuotas (JG-06)
- Caché de respuestas (JG-15)

## Resultado

**Qué se hizo**
- `LectorContexto` (`ILectorContexto`) lee de Redis el contexto de cada petición antes de los filtros, en dos *pipelines* (criterio 9): primero `api:host:{host}` y `clave:{hash}`, y después `api:{id}`, `api:{id}:rutas`, `org:{id}` y `susc:{id}`. Lo deja en `ContextoPeticion` (`Api`, `Rutas`, `Clave`, `Organizacion`, `Suscripcion`). Los filtros ya no van a Redis: cada uno comprueba lo suyo. Una caída de Redis en el lector sigue respondiendo 503.
- `CacheRutas` guarda las rutas de cada API 5 segundos como máximo, con la `version` de `api:{id}`. Mientras están en memoria, el segundo *pipeline* no las pide. Si la `version` cambió, se vuelven a leer en un viaje más; solo pasa en la primera petición después de publicar.
- `PatronRuta` y `TablaRutas` comparan con la sintaxis de OpenAPI (`/guias/{numero}`, también segmentos mixtos como `{nombre}.json`) y aplican la regla de especificidad de 08 §1.
- Filtros nuevos, en este orden en `TuberiaCompuerta.Orden`: `FiltroCors` (0), `FiltroOrganizacion` (3), `FiltroSuscripcion` (4) y `FiltroRuta` (5). `FiltroRuta` deja la ruta en `ContextoPeticion.Ruta`. `ResultadoFiltro.Responder(estado)` contesta sin cuerpo y sin reenviar (el *preflight*).
- `ConexionOrigen` es el `ConnectCallback`: valida cada conexión con `ValidadorDireccionOrigen` y se conecta a una de las direcciones ya validadas. Una prueba con un resolver falso confirma que la conexión no vuelve a resolver el nombre (*DNS rebinding*). `ProteccionOrigen` lee `SHAPI_MODO_DEMO` y `SHAPI_ORIGENES_PERMITIDOS`. La lista por defecto quedó en `ValidadorDireccionOrigen.OrigenesPermitidosPorDefecto`, que también usa `ApisModulo`.
- `TransformadorOrigen` quita `X-Api-Key`, cualquier `X-Shapi-*` del cliente y las cookies `shapi_sesion` y `portal_sesion`. Agrega `X-Shapi-Consumidor`, `X-Shapi-Entorno`, `X-Shapi-Secreto` (si la API tiene) y `X-Forwarded-For/Proto/Host`.
- `ReenvioOrigen` traduce los fallos de YARP al contrato de errores: 502 `origen_inaccesible`, 504 `origen_sin_respuesta` y 413 `cuerpo_demasiado_grande`. La tubería rechaza con 413 un `Content-Length` de más de 10 MB antes de leer Redis, y fija `MaxRequestBodySize` para los cuerpos por partes.
- `infra/compose.prod.yml` pasa `SHAPI_MODO_DEMO` y `SHAPI_ORIGENES_PERMITIDOS` a la compuerta. Sin eso, la protección contra SSRF rechazaría `origen-envios` y `origen-agro`, que están en la red de Docker.

**Decisiones** (quedaron en 08 §1, §3, §5, §6 y §8)
- `FiltroRuta` busca entre **todas** las rutas del método, expuestas y ocultas, y exige que la que gana esté expuesta. Si una ruta oculta es más específica que una expuesta (`/guias/recientes` frente a `/guias/{numero}`), se rechaza. En un empate total gana la oculta (RF-10: solo las expuestas pasan).
- Si `org:{id}` no está en Redis, se responde `api_no_disponible`: el estado no se puede comprobar. Una `susc:{id}` ausente es una suscripción finalizada: `suscripcion_inactiva`.
- `org:{id}` se lee con el `organizacion_id` de la clave, que es el de la API. `FiltroClave` exige además que coincida con el de la API.
- CORS: se acepta un `Origin` cuyo host es el `portal_host`, con `http` o `https` y cualquier puerto, y se devuelve tal cual. El *preflight* es un `OPTIONS` con `Origin` y `Access-Control-Request-Method`. El de otro origen recibe 204 sin cabeceras, y el de un host sin API publicada sigue hasta el 404. Las cabeceras se ponen en todas las respuestas, también en los rechazos, para que el portal pueda leer el error. Las `Access-Control-*` del origen se quitan, y toda respuesta a una petición con `Origin` lleva `Vary: Origin`.
- `X-Forwarded-For` agrega la IP de la conexión a la cadena que llegó. `X-Forwarded-Proto` conserva el que manda el borde (Caddy le habla a la compuerta por `http`); si no llega uno válido, se usa el esquema de la conexión.
- Los segmentos literales de los patrones no distinguen mayúsculas. Si las distinguieran, `/guias/RECIENTES` se saltaría una ruta oculta `/guias/recientes` cuando el origen tampoco las distingue (hallazgo opcional de la revisión en contexto limpio).
- El 413 de un `Content-Length` de más de 10 MB y el 503 de Redis no disponible salen antes de conocer la API, así que no llevan cabeceras de CORS.
- El límite del cuerpo es 10 × 1024 × 1024 bytes.

**Archivos principales:** `src/Shapi.Compuerta/Contexto/{ILectorContexto,LectorContexto}.cs`, `src/Shapi.Compuerta/Rutas/{PatronRuta,TablaRutas,CacheRutas}.cs`, `src/Shapi.Compuerta/Filtros/{FiltroCors,FiltroOrganizacion,FiltroSuscripcion,FiltroRuta}.cs`, `src/Shapi.Compuerta/Reenvio/{ConexionOrigen,ProteccionOrigen,TransformadorOrigen,ReenvioOrigen}.cs`, `src/Shapi.Compuerta/TuberiaCompuerta.cs` y `tests/Shapi.Compuerta.Tests/**`.

### Correcciones de la auditoría (2026-10-03)

Paso 1 de `docs/plan/auditoria-2026-10-03.md` (H-02 y H-04):
- **Tiempo de conexión vencido (H-02).** YARP 2.3.0 reporta como `RequestTimedOut` (504) cualquier cancelación que no venga del token que se le pasa, incluido el vencimiento de `ConnectTimeout` (10 s) y una resolución del nombre que no termina. La compuerta respondía 504 `origen_sin_respuesta` y descontaba la cuota, cuando la petición nunca llegó al origen. Ahora `ReenvioOrigen` decide por la excepción (`NoSeConecto`): un `TimeoutException` adentro o un `HttpRequestException` con `ConnectionError` o `NameResolutionError` es 502 `origen_inaccesible` y devuelve la cuota. La prueba nueva usa un resolutor que no termina y una conexión de 300 ms, y sin la corrección recibía 504.
- **Decisión del paso 1.** Si el origen acepta la conexión, recibe la petición y la corta sin responder, la respuesta sigue siendo 502 `origen_inaccesible`, pero la cuota **se descuenta**, porque la petición llegó. Antes se devolvía. Un `SocketException` solo no basta para decir que no hubo conexión: también aparece cuando el origen corta. Se precisó en 08 §1, §3 y §4, con una prueba que usa un origen Kestrel que aborta.
- **RF-47 (H-04).** La prueba del secreto de origen se llama `RF_47_HaciaElOrigen_ConSecreto_AgregaXShapiSecreto`.
