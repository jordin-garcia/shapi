---
id: JZ-14
titulo: Pruebas de carga (RNF-01 y RNF-03)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P2
estado: pendiente
depende_de: [JG-06, JZ-06]
requisitos: [RNF-01, RNF-03]
pantallas: []
---

# JZ-14 · Pruebas de carga (RNF-01 y RNF-03)

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P2 · **Depende de:** JG-06, JZ-06

## Objetivo
Medir con k6 la latencia que añade la compuerta y el rendimiento sostenido, y documentar los resultados para la exposición.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RNF-01 y RNF-03
- `docs/specs/08-compuerta.md` §8

## Archivos que creas o modificas
- `tests/carga/*.js` (crear: escenarios de k6)
- `tests/carga/README.md`
- `docs/pruebas-de-carga.md` (crear: resultados)

## Criterios de aceptación
1. Escenario A: 200 peticiones por segundo durante 5 minutos contra `https://envios.api.shapi.localhost/cotizaciones` con una clave de un plan de límites altos (de la siembra o creado para la prueba). Se reportan el % de errores de la compuerta (debe ser menor al 1 %), los p50, p95 y p99, y las peticiones por segundo alcanzadas.
2. Escenario B: la misma carga directa al origen (`http://localhost:5101`). La diferencia de p95 entre A y B es la latencia que añade la compuerta, que debe ser de 15 ms o menos (RNF-01).
3. k6 corre con Docker (`docker run --network host grafana/k6 run ...`), así que no hay que instalar nada más.
4. `docs/pruebas-de-carga.md` documenta el equipo, los resultados, las gráficas o tablas y la conclusión. Si no se cumple algún criterio, se crea una tarea para Jordin con los datos.

## Pruebas obligatorias
- Los propios escenarios

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Optimizar (si hace falta, es tarea de Jordin)
