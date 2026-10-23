---
id: JG-14
titulo: Convergencia del Avance 3
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-22
depende_de: []
no_antes_de: 2026-10-22
requisitos: []
pantallas: []
---

# JG-14 · Convergencia del Avance 3

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P1 · **Sin dependencias** · **No antes del:** 2026-10-22

## Objetivo
Igual que JG-08, pero para el Avance 3 y su guion del 23 de octubre.

## Contexto que debes leer
- `docs/plan/prompts/convergencia.md`
- `docs/plan/calendario.md` (Avance 3)

## Archivos que creas o modificas
- `docs/plan/convergencia/2026-10-22.md` (crear)
- `docs/plan/tareas/<nuevas>.md`

## Criterios de aceptación
1. El informe tiene la tabla de requisitos, el resultado del guion 3 y las tareas nuevas asignadas.
2. La carga de la semana final queda revisada: las P3 se descartan si no caben.

## Pruebas obligatorias
- No aplica

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Implementar correcciones

## Resultado
Se integró el 23 oct, un día después de lo programado. El informe está en `docs/plan/convergencia/2026-10-22.md`.

- **Requisitos.** Cuatro subagentes de solo lectura revisaron 46 requisitos, uno por área: 27 ✅, 19 ⚠️ y ningún ❌. Las afirmaciones que generan tareas se comprobaron a mano. Cada ⚠️ tiene su tarea.
- **Guion 3.** Se recorrió en el ambiente productivo simulado con Playwright, a 1366 × 768. Los cinco pasos funcionan. El paso 4 necesitó la API para adelantar el reloj, y en el paso 5 aparecieron errores de B1.4, B3.1 y B3.2.
- **Hallazgos.** Se registraron 35 (H-C01 a H-C35). El más grave es H-C01: un *timeout* de Redis en `RelojSistema` produce un 500 en la API y detiene el trabajador.
- **Cambio de fechas (decisión de Jordin, 23 oct).** El Avance 3 es un video que se entrega el sábado 31 a las 23:59. Todo termina el jueves 29, incluidos los manuales, y se graba el viernes 30. Se actualizaron los hitos de `calendario.md`, `lineamientos.md` §9 y `01-vision-y-alcance.md`, y JG-17 congela el jueves 29.
- **Sin auditorías completas (decisión de Jordin).** Jordin corrige todos los hallazgos: JG-21 (P1), JG-22 (P1), JG-23 a JG-25 (P2) y JG-26 a JG-28 (P3), que incluyen los pendientes de la auditoría del 3 oct y de los ensayos del 8 y el 18 oct.
- **Video.** EM-22, DC-18 y JZ-19 (un elemento de código por persona, hecho por su agente) y JG-29 (guion y elemento de Jordin, jueves 29).
- **Carga.** Además de JG-14, Jordin queda con 11 tareas hasta el jueves 29. Las P3 son las primeras que quedan fuera; JG-23 y JG-24 se pueden reasignar a Emilio y a Dominique.
- **Archivos principales:** `docs/plan/convergencia/2026-10-22.md`, las 12 tareas nuevas en `docs/plan/tareas/`, `docs/plan/calendario.md`, `docs/lineamientos.md`, `docs/specs/01-vision-y-alcance.md` y la tarea JG-17.
