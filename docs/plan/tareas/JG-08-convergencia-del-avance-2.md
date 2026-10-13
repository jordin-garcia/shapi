---
id: JG-08
titulo: Convergencia del Avance 2
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-08
depende_de: []
no_antes_de: 2026-10-08
requisitos: []
pantallas: []
---

# JG-08 · Convergencia del Avance 2

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P1 · **Sin dependencias** · **No antes del:** 2026-10-08

## Objetivo
Comparar lo que exige el Avance 2 con lo que hay en `main`, convertir cada brecha en una tarea para su dueño y dejar el guion de demostración del 9 de octubre funcionando o con tareas P1 asignadas.

## Contexto que debes leer
- `docs/plan/prompts/convergencia.md` (seguir paso a paso)
- `docs/plan/calendario.md` (tareas y guion del Avance 2)
- `docs/specs/03-requisitos.md`

## Archivos que creas o modificas
- `docs/plan/convergencia/2026-10-08.md` (crear)
- `docs/plan/tareas/<nuevas>.md` (crear las que haga falta)

## Criterios de aceptación
1. El informe tiene la tabla de requisitos del avance (✅/⚠️/❌) y el resultado de cada paso del guion 2.
2. Cada ⚠️ o ❌ tiene una tarea pendiente con su dueño y su prioridad.
3. Si alguna persona queda sobrecargada, las tareas de menor valor bajan a P3, y se deja constancia en el informe.

## Pruebas obligatorias
- No aplica: esta tarea no implementa código

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Implementar correcciones

## Resultado
- Informe en `docs/plan/convergencia/2026-10-08.md`, con la tabla de los 33 requisitos del avance (25 ✅, 7 ⚠️ y 1 ❌) y el resultado de cada paso del guion 2, recorrido en el ambiente productivo simulado.
- El paso 1 falla: `sembrar-demo` se cae dentro del contenedor del trabajador (`CultureNotFoundException`, `es-GT` en `SiembraDemo.cs:295`). Los pasos 2 a 6 funcionan.
- Tareas nuevas, cada una con dueño y prioridad: JZ-17 (P1, siembra en el ambiente productivo), JZ-18 (P1, RNF-14), JG-19 (P2, RNF-02 y RNF-04), DC-17 (P2, barra lateral de A5.4/A5.6/A5.4b y texto de A5.3), EM-19 (P2, pruebas de backend del portal) y EM-20 (P3, medio de pago en A5.4b).
- Ninguna persona queda sobrecargada, así que ninguna tarea baja a P3. Se regeneró el calendario y DC-09 vuelve a `avance: 3`.
- Archivos: `docs/plan/convergencia/2026-10-08.md`, las seis tareas nuevas, `docs/plan/calendario.md`, `docs/plan/tareas/DC-09-*.md` y `docs/plan/bitacora/jordin.md`.
