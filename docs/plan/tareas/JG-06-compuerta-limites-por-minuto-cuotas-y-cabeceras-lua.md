---
id: JG-06
titulo: Compuerta: límites por minuto, cuotas y cabeceras (Lua)
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: pendiente
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
