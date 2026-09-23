---
id: JG-09
titulo: Medición en la compuerta y consolidación del consumo
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: pendiente
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
