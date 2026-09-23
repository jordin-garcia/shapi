---
id: JG-02
titulo: Compuerta mínima: host → API, clave y reenvío
persona: jordin
responsable: Jordin García
avance: 1
prioridad: P1
estado: hecha
depende_de: [JG-01]
requisitos: [RF-29, RF-31]
pantallas: []
---

# JG-02 · Compuerta mínima: host → API, clave y reenvío

**Responsable:** Jordin García · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** JG-01

## Objetivo
Que la compuerta resuelva la API por el host desde Redis, valide la clave por su hash, reenvíe la petición al origen con YARP y responda los errores 404 y 401 en el formato JSON acordado. Es la base de la demostración del Avance 1.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §1, §2, §3 (filtros 1, 2 y 8) y §4 (contrato de errores)
- `docs/specs/07-modelo-de-datos.md` §4 (llaves `api:host:*`, `api:{id}`, `clave:{sha256}`)
- `docs/specs/06-arquitectura.md` §5.1 (secuencia de la petición)
- `docs/plan/convenciones.md` §9 (puertos: la compuerta escucha en 5090)

## Archivos que creas o modificas
- `src/Shapi.Compuerta/**` (crear): `Filtros/IFiltroCompuerta.cs`, `TuberiaCompuerta.cs`, `Filtros/FiltroApi.cs`, `Filtros/FiltroClave.cs`, el reenvío con YARP (`IHttpForwarder`) y las respuestas de error
- `src/Shapi.Contratos/Redis/` (crear los DTO `ContextoApi` y `ContextoClave`)
- `tests/Shapi.Compuerta.Tests/**` (crear)

## Criterios de aceptación
1. Cuando llega una petición con el `Host` de una API publicada en Redis y una `X-Api-Key` válida, la compuerta la reenvía a `url_origen` conservando método, ruta, query y cuerpo, y devuelve la respuesta del origen tal cual.
2. Cuando el host no existe o la API no está publicada, responde 404 con `{"error":{"codigo":"api_no_encontrada",...}}` y `Content-Type: application/json; charset=utf-8`.
3. Cuando falta `X-Api-Key`, responde 401 `clave_ausente` con `WWW-Authenticate: ApiKey header="X-Api-Key"`. Cuando la clave no está en Redis o pertenece a otra API, responde 401 `clave_invalida`.
4. La clave se busca por `SHA-256` en hex minúsculas de la clave completa, en UTF-8. Nunca se registra en claro.
5. El origen nunca recibe `X-Api-Key`. Recibe `X-Shapi-Consumidor` y `X-Shapi-Entorno` (`produccion` o `pruebas`, según el tipo de clave).
6. El orden de los filtros se define en un solo lugar (`TuberiaCompuerta`) y cada filtro es una clase independiente que implementa `IFiltroCompuerta` (RNF-13).
7. Existe el comando temporal `dotnet run --project src/Shapi.Compuerta -- sembrar-demo`, que solo funciona con `SHAPI_MODO_DEMO=true`. Escribe en Redis la API `envios` (host `envios.api.shapi.localhost`, origen `http://localhost:5101`) y la clave `shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e`. JG-04 lo elimina más adelante.

## Pruebas obligatorias
- Integración con Testcontainers (Redis) y un origen falso en memoria, cubriendo los criterios 1 a 5
- Unitaria de la tubería: un filtro que rechaza detiene la cadena

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

Verificación manual con el entorno levantado:
- Con JZ-01 y JZ-02 terminadas: `docker compose -f infra/compose.yml up -d`, `dotnet run --project src/Shapi.Compuerta -- sembrar-demo` y `dotnet run --project src/Shapi.Compuerta`
- `curl -X POST -H "X-Api-Key: shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e" -H "Content-Type: application/json" -d '{"origen":"0901","destino":"0301","peso_kg":2.5}' https://envios.api.shapi.localhost/cotizaciones` → 200 del origen
- La misma petición con una clave inválida → 401 JSON. Si JZ-01 todavía no está, prueba contra `http://localhost:5090` con la cabecera `Host: envios.api.shapi.localhost`

## Fuera de alcance
- Filtros de organización, suscripción y ruta, SSRF y CORS (JG-05)
- Límites y cuotas (JG-06)
- Medición (JG-09)
- Publicar desde PostgreSQL (JG-04)

## Resultado
- **Tubería** (`src/Shapi.Compuerta/TuberiaCompuerta.cs`):
  - `TuberiaCompuerta.Orden` es la única lista del orden de los filtros. Por ahora tiene `FiltroApi` y `FiltroClave`.
  - Cada filtro implementa `IFiltroCompuerta.EvaluarAsync(ContextoPeticion)` y devuelve `ResultadoFiltro.Continuar` o `ResultadoFiltro.Rechazar(estado, codigo, mensaje, cabeceras?)`.
  - El primer rechazo se escribe con `RespuestaError`, en el formato de 08 §4. Si ningún filtro rechaza, la petición pasa a `IReenvioOrigen`.
  - **Para agregar un filtro:** se crea la clase y se agrega una línea en `Orden`. `ServiciosCompuerta.AgregarCompuerta()` lo registra solo.
- **Contexto de la petición** (`ContextoPeticion`): `FiltroApi` llena `Api` y `FiltroClave` llena `Clave`. Los filtros siguientes pueden leer los dos.
- **Contratos** (`src/Shapi.Contratos/Redis/`):
  - `ContextoApi` y `ContextoClave` definen los campos de los hashes `api:{id}` y `clave:{sha256}`. Tienen `ACampos()` para escribir en Redis y `DesdeCampos()` para leer.
  - `ContextoClave.CalcularHash(clave)` devuelve el SHA-256 en hex minúsculas de la clave en UTF-8.
  - **JG-04 y JG-07:** su publicador debe usar `ACampos()`, no los nombres de campo escritos a mano.
- **Reenvío** (`Reenvio/`): usa `IHttpForwarder` de YARP con un tiempo de espera de 30 s, sin redirecciones ni cookies. `TransformadorOrigen` hace tres cosas:
  - quita `X-Api-Key`;
  - reemplaza `X-Shapi-Consumidor` y `X-Shapi-Entorno`, aunque el cliente las mande;
  - deja que el origen reciba el `Host` de `url_origen`.
- **Configuración:** la conexión a Redis sale de `SHAPI_REDIS` (por defecto `localhost:6379`). `GET /salud` solo responde cuando el `Host` es `localhost`. Todo lo demás pasa por la tubería.
- **`sembrar-demo`** (`Demo/SembradoDemo.cs`, temporal): solo funciona con `SHAPI_MODO_DEMO=true`. Escribe la API `envios` y la clave `shp_prod_4fN8…2e`, con UUID fijos que terminan en `e001` a `e005`. El origen sale de `SHAPI_URL_ORIGEN_ENVIOS` y, si no existe, es `http://localhost:5101`. Lo elimina JG-04.
- **Pruebas** (33 nuevas en `tests/Shapi.Compuerta.Tests`, 132 en total):
  - de integración con Testcontainers (Redis 7.4) y un origen falso con TestServer (`Soporte/`), para los criterios 1 a 5 y el 7;
  - unitarias de la tubería y de los contextos de Redis;
  - una prueba que revisa todos los registros y confirma que la clave nunca aparece en claro.
- **Decisiones:**
  - Mensajes de error: "No hay ninguna API publicada en este dominio.", "Falta la cabecera X-Api-Key con su clave de acceso." y "La clave de acceso no es válida para esta API.".
  - Si la petición trae varias `X-Api-Key`, se responde `clave_invalida`.
  - Se precisaron en 08 §1 y §5 el `Host` que recibe el origen y la ruta `/salud`.
- **Pendiente para JG-05 y JG-06:**
  - 502, 504 y 413 en formato JSON. Hoy YARP responde 502 o 504 sin cuerpo.
  - `X-Forwarded-*`, `X-Shapi-Secreto` y quitar el resto de las `X-Shapi-*` que mande el cliente.
  - Quitar las cookies del portal.
  - Leer el contexto en un solo *pipeline* de Redis (08 §8). Hoy son 3 viajes seguidos.
