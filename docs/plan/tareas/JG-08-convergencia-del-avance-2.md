---
id: JG-08
titulo: Convergencia del Avance 2
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: pendiente
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
