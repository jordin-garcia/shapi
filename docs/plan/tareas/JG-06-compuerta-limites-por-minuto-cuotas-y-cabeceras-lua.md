---
id: JG-06
titulo: Compuerta: límites por minuto, cuotas y cabeceras (Lua)
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-02
depende_de: [JG-05]
requisitos: [RF-30, RF-32, RF-45]
pantallas: []
---

# JG-06 · Compuerta: límites por minuto, cuotas y cabeceras (Lua)

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JG-05

## Objetivo
Aplicar de forma atómica los límites por minuto (del plan y de la ruta), la cuota del consumidor y la cuota de plataforma del proveedor, con un solo script Lua en Redis, y devolver las cabeceras de cuota.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §3 (filtro 6 y script `evaluar_limites.lua`), §4 (errores 429) y §5 (cabeceras)
- `docs/specs/07-modelo-de-datos.md` §4 (llaves `cuota:*`, `rl:*`, `dia:p:*`)
- `docs/specs/02-glosario.md` (petición y llamada)
- `docs/specs/03-requisitos.md` RF-30, RF-32 y RF-45

## Archivos que creas o modificas
- `src/Shapi.Compuerta/Filtros/FiltroLimitesYCuotas.cs` (crear)
- `src/Shapi.Compuerta/Lua/evaluar_limites.lua` (crear, como recurso embebido)
- `tests/Shapi.Compuerta.Tests/**`

## Criterios de aceptación
1. Si se supera `limite_minuto` del plan dentro del minuto actual → 429 `limite_por_minuto`, con `Retry-After` en segundos hasta el siguiente minuto.
2. Lo mismo con el `limite_minuto` de la ruta, cuando la ruta lo tiene.
3. Cada petición que llega al origen descuenta `peso_llamadas` de `cuota:susc:{id}:{inicio}`. Si se agota → 429 `cuota_agotada`, con `Retry-After` hasta el fin del ciclo. **Nunca se excede**, ni con peticiones concurrentes.
4. La cuota de plataforma (`cuota:org:*`, en peticiones) se descuenta en cada petición que llega al origen. Si se agota → 429 `cuota_plataforma_agotada`.
5. Si el origen no se pudo conectar (502), se devuelve la reserva (`DECRBY` y `DECR`). Los 4xx y 5xx del origen y los 504 sí descuentan.
6. La clave de pruebas usa el límite fijo de 10 peticiones por minuto y 1,000 por día, no toca las cuotas y devuelve `X-Shapi-Plan: Pruebas`.
7. Toda respuesta con una clave válida, incluidos los 429, lleva `X-Shapi-Plan`, `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`, `X-Cuota-Limite`, `X-Cuota-Restante` y `X-Cuota-Reinicio`.
8. Todo lo anterior se resuelve con una sola llamada `EVALSHA` a Redis por petición.

## Pruebas obligatorias
- Integración con Redis (Testcontainers) de cada criterio
- Concurrencia: 50 peticiones simultáneas con cuota 30 → exactamente 30 llegan al origen y 20 reciben 429
- Unitaria del cálculo de `Retry-After`

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Medición y consolidación (JG-09)
- Caché de respuestas (JG-15)

## Resultado

**Qué se hizo**
- `FiltroLimitesYCuotas` (filtro 6, después de `FiltroRuta` en `TuberiaCompuerta.Orden`) arma las llaves y los límites de la petición y ejecuta `Lua/evaluar_limites.lua`, embebido en el ensamblado. El script reserva, en este orden, el límite por minuto del plan (`rl:s`), el de la ruta (`rl:r`, solo si la ruta tiene `limite_minuto`), la cuota del consumidor (`cuota:susc`, que descuenta `peso`) y la cuota de plataforma (`cuota:org`, solo si `org:{id}` la tiene). Si algo se pasa, revierte todo lo anterior y devuelve el código. Siempre devuelve los contadores, para las cabeceras.
- Una sola llamada por petición (criterio 8): `EVALSHA` con el SHA-1 del script. Si Redis responde `NOSCRIPT` porque se reinició o vació su caché de scripts, se ejecuta una vez con `EVAL`, que lo deja cargado. StackExchange.Redis, con el texto del script, mandaba `EVAL` en la primera llamada de cada conexión.
- Rechazos 429 con el contrato de 08 §4: `limite_por_minuto` (plan o ruta, `Retry-After` hasta el siguiente minuto), `cuota_agotada` (`Retry-After` hasta el fin del ciclo, con el mensaje de 08 §4) y `cuota_plataforma_agotada` (`Retry-After` hasta el fin del ciclo de plataforma).
- Las siete cabeceras de 08 §5 se ponen al responder (`OnStarting`), en el reenvío y en todos los rechazos desde el filtro 6, incluidos los 429, 502 y 504. Reemplazan las del origen con el mismo nombre. Los nombres quedaron en `CabecerasCompuerta`.
- 502: `ReenvioOrigen` llama a `ContextoPeticion.DevolverReserva`, que hace `DECRBY` de la cuota y `DECR` de la cuota de plataforma en un lote. Si Redis falla, solo se registra. Los 4xx y 5xx del origen y los 504 descuentan.
- Clave de pruebas: el mismo script, con `rl:p` (10 por minuto) y `dia:p` (1,000 por día de Guatemala) en el lugar de la cuota. No usa el límite de la ruta ni toca `cuota:*`, y responde `X-Shapi-Plan: Pruebas`.
- Pruebas: 21 de integración con Redis real y un reloj fijo (`Tuberia/LimitesYCuotasTests.cs`), incluida la de 50 peticiones simultáneas con cuota 30, y unitarias del cálculo de `Retry-After` y del día de Guatemala (`Filtros/CalculoLimitesTests.cs`). `RNF_01_ContextoDeRedis_TresViajesPorPeticion` comprueba ahora los tres viajes de 08 §8.

**Decisiones** (quedaron en 08 §3, §4 y §5 y en 07 §4)
- Con la clave de pruebas, pasar las 1,000 peticiones del día responde 429 `cuota_agotada`, con `Retry-After` hasta la medianoche de Guatemala. Las `X-Cuota-*` informan ese límite diario. La especificación no decía qué código usar.
- Si el `fin` del ciclo ya pasó, por ejemplo con el trabajador caído, los contadores de cuota vencen 8 días después de ahora en vez de `fin + 8 días`. Si no, el `EXPIREAT` en el pasado borraría el contador y la cuota se reiniciaría en cada petición. En ese caso, `Retry-After` es 1.
- `X-RateLimit-Remaining` es lo que queda del menor de los dos límites. `X-Cuota-Reinicio` va en UTC (`2026-10-31T06:00:00Z`).
- Un 502 devuelve las cuotas, pero no los contadores por minuto, como dice 08 §3.
- Los rechazos de los filtros 1 a 5 no llevan las cabeceras: el filtro 6 no se ejecutó y calcularlas costaría otro viaje a Redis.
- Los mensajes escriben las fechas y los miles sin depender de la cultura del sistema, porque la imagen Alpine no trae ICU.

**Archivos principales:** `src/Shapi.Compuerta/Filtros/FiltroLimitesYCuotas.cs`, `src/Shapi.Compuerta/Lua/evaluar_limites.lua`, `src/Shapi.Compuerta/{TuberiaCompuerta,ContextoPeticion,CabecerasCompuerta}.cs`, `src/Shapi.Compuerta/Reenvio/ReenvioOrigen.cs`, `src/Shapi.Compuerta/Shapi.Compuerta.csproj` y `tests/Shapi.Compuerta.Tests/**`.
