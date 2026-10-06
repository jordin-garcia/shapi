---
id: JG-09
titulo: Medición en la compuerta y consolidación del consumo
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-04
depende_de: [JG-06, EM-01]
requisitos: [RF-33, RF-34, RNF-05]
pantallas: []
---

# JG-09 · Medición en la compuerta y consolidación del consumo

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** JG-06, EM-01

## Objetivo
Registrar las métricas de cada petición en Redis sin afectar la latencia y consolidarlas cada 10 segundos en `consumo_diario` de forma idempotente, junto con el cálculo del p95.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §3 (filtro 9) y §7 completo
- `docs/specs/07-modelo-de-datos.md` §3.5 (`consumo_diario`, `lote_consolidado`) y §4 (llaves `met:*`, `salud:*`)
- `docs/specs/06-arquitectura.md` §5.7 y §8

## Archivos que creas o modificas
- `src/Shapi.Compuerta/Medicion/**` (crear: `MedicionMiddleware` y latido `salud:compuerta:{instancia}`)
- `src/Shapi.Aplicacion/Consumo/**` (crear: cálculo del p95 y consultas base)
- `src/Shapi.Trabajador/Consolidacion/**` (crear)
- `tests/*/Consumo/**`, `tests/Shapi.Compuerta.Tests/**`

## Criterios de aceptación
1. Toda petición, incluidos los rechazos, incrementa con `HINCRBY` los campos de `met:{aaaammdd}:{api}:{ruta|-}:{susc|-}:{entorno}` que indica 08 §7. Lo hace en un *pipeline* sin esperar la respuesta de Redis, y agrega la llave a `met:pendientes`.
2. La latencia de la compuerta es la latencia total menos el tiempo hasta el primer byte del origen, y las dos van a los rangos del histograma de 10 posiciones (≤5, ≤10, ≤25, ≤50, ≤100, ≤250, ≤500, ≤1000, ≤2500, >2500 ms).
3. Cada 10 s, el trabajador consolida con el algoritmo de 08 §7: RENAME a `met:lote:{lote}:*`, una transacción con `lote_consolidado` y UPSERT sumando contadores e histogramas, y DEL.
4. Al arrancar, el trabajador procesa los `met:lote:*` que hayan quedado. Si el lote ya existe en `lote_consolidado`, solo borra las llaves. Nunca se pierden ni se duplican datos.
5. La función del p95 suma los histogramas del periodo e interpola dentro del rango, según 08 §7. Si cae en el último rango, devuelve "> 2500 ms".
6. La compuerta escribe su latido `salud:compuerta:{instancia}` cada 10 s, con TTL de 30 s.

## Pruebas obligatorias
- Unitarias del p95 (distribuciones conocidas)
- Integración de la consolidación con una interrupción simulada entre cada paso (no hay pérdida ni duplicado)
- Integración de la compuerta: después de N peticiones, los contadores en Redis coinciden

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Pantallas de consumo (JG-11, JG-12, JG-13)
- Estado de los componentes (JZ-12)

## Resultado

- La compuerta mide todas las peticiones, incluidos rechazos y preflight, con contadores, bytes de cuerpos, llamadas realmente descontadas, sumas de latencia y los dos histogramas. Envía los `HINCRBY` y el `SADD` juntos en un pipeline `MULTI/EXEC` con `FireAndForget`.
- El reenvío mide la espera hasta las cabeceras de la respuesta del origen; si no responde, hasta el fallo. Distingue un código del origen de un rechazo o fallo de la compuerta. Las claves rechazadas no atribuyen consumo a la suscripción leída.
- El trabajador consolida al arrancar y cada 10 s. Separa hasta 256 llaves por lote mediante un script atómico, inserta el marcador y suma todas las columnas e histogramas en una transacción, y borra las instantáneas después del commit.
- Recupera lotes pendientes al arrancar y en cada intervalo, con rollback ante errores de PostgreSQL y unicidad del marcador ante reintentos concurrentes. Los lotes ya aplicados se borran sin volver a leer sus contadores.
- La revisión en contexto limpio detectó que `SCAN` puede repetir una llave dentro del mismo lote. La lectura ahora deduplica antes de leer los hashes, con una prueba que reproduce la duplicación. También se precisó la clasificación: un 413 del cuerpo no es un fallo 502/504 del origen, y una respuesta ya iniciada conserva su código del origen.
- Las rutas retiradas se consolidan con `ruta_id` nulo. Un bloqueo compartido de la API mantiene estables las rutas frente a una recarga de OpenAPI. No hay migraciones ni nuevas dependencias.
- `HistogramaLatencia.CalcularP95` suma el periodo e interpola en su rango, devuelve ausencia de percentil sin peticiones y señala `> 2500 ms` en el último rango. `IConsultaConsumo` ofrece consultas base por API y suscripción, filtradas explícitamente por organización, fechas y entorno.
- La compuerta y el trabajador escriben sus latidos cada 10 s con TTL de 30 s. El latido del trabajador corre independientemente de la consolidación.

**Precisiones de 08 §7:** los incrementos y el corte del lote son atómicos; los bytes son los efectivamente transferidos; el día se toma al iniciar la petición; los hosts sin API se cuentan con el UUID nulo y conservan sus métricas globales en Redis, sin atribuirlas a un proveedor ni violar la FK de `consumo_diario`.

**Pruebas:** histogramas y p95; peticiones concurrentes y bytes; códigos del origen frente a rechazos; ausencia de espera de Redis; tiempo del origen; claves ajenas y de pruebas; latidos y ejecución periódica; interrupciones antes del corte, antes y después del commit y durante el borrado; rollback, reintentos concurrentes, lotes ya aplicados, rutas retiradas y llaves desaparecidas; consultas con aislamiento de organización.

**Archivos principales:** `src/Shapi.Compuerta/Medicion/**`, `src/Shapi.Trabajador/Consolidacion/**`, `src/Shapi.Aplicacion/Consumo/**`, `src/Shapi.Infraestructura/Consumo/**`, `src/Shapi.Contratos/Redis/{HistogramaMetricas,MetricasDiarias}.cs` y `tests/{Shapi.Api.Tests/Consumo,Shapi.Compuerta.Tests/Medicion}/**`.
