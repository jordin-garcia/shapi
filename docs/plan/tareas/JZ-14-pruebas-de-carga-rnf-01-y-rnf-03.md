---
id: JZ-14
titulo: Pruebas de carga (RNF-01 y RNF-03)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P2
estado: hecha
programada: 2026-10-17
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

## Resultado

Se implementaron escenarios k6 reproducibles para el mismo POST de cotización
contra la compuerta y el origen, con tasa abierta de 200 peticiones/s durante
cinco minutos, validación de la respuesta, errores, percentiles y omisiones.
Los comandos de preparación y ejecución construyen un ambiente productivo
aislado (`shapi-jz14`), elevan los límites del plan, ruta y plataforma, generan
una clave aleatoria de producción y sincronizan Redis con el trabajador.

La ejecución del 18 de octubre de 2026 completó 60 000 peticiones al origen y
60 001 a través de la compuerta, sin errores, respuestas inválidas ni omisiones.
Los p95 fueron 2.003 ms y 10.585 ms: la diferencia de 8.583 ms cumple RNF-01.
La compuerta alcanzó 200.030 peticiones/s durante la carga programada de cinco
minutos, con 0 % de errores, cumpliendo RNF-03. No se detectó un incumplimiento
que requiriera una tarea de optimización para Jordin.

Decisiones: red, volúmenes y puertos separados para conservar el ambiente
habitual; `hosts` de k6 mantiene los destinos HTTP/SNI y traduce solo el puerto
local; una iteración adicional en el límite temporal se acepta sin reducir el
mínimo de 60 000 ni admitir omisiones. Los valores en claro y resúmenes locales
quedan en `.shapi/carga/`, fuera de Git. José Pablo autorizó agregar al manual
técnico la referencia al informe que exige 08 §8.

Archivos principales: `tests/carga/{escenario,comun,comparar,preparar,ejecutar}.js`,
sus pruebas y README, `docs/pruebas-de-carga.md` y la referencia en
`docs/manual-tecnico.md`. Nueve pruebas de Node, humo Docker y ambas mediciones
completas pasaron; el informe contiene equipo, método, tabla y conclusión.
