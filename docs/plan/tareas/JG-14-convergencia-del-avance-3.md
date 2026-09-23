---
id: JG-14
titulo: Convergencia del Avance 3
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: pendiente
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
