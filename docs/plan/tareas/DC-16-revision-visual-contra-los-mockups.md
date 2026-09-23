---
id: DC-16
titulo: Revisión visual contra los mockups
persona: dominique
responsable: Dominique Contreras
avance: final
prioridad: P2
estado: pendiente
depende_de: [DC-11]
no_antes_de: 2026-10-24
requisitos: [RNF-12]
pantallas: []
---

# DC-16 · Revisión visual contra los mockups

**Responsable:** Dominique Contreras · **Avance:** final · **Prioridad:** P2 · **Depende de:** DC-11 · **No antes del:** 2026-10-24

## Objetivo
Comparar cada pantalla implementada con su mockup, corregir las propias y crear tareas para las diferencias en pantallas de otros.

## Contexto que debes leer
- `docs/specs/11-interfaz.md` §3 (catálogo completo)
- `docs/plan/protocolo.md` §B8

## Archivos que creas o modificas
- `docs/plan/revision-visual.md` (crear: tabla con las pantallas, el resultado y las diferencias)
- Páginas propias (corregir)
- `docs/plan/tareas/<nuevas>.md` (para las pantallas de otros)

## Criterios de aceptación
1. Todas las pantallas del catálogo están revisadas con la siembra de demostración (captura contra mockup).
2. Las diferencias en pantallas propias se corrigen. Las de otros quedan como tareas P2 para su dueño, con las capturas y la lista de diferencias.
3. Se verifica a 1280 px de ancho que no aparezca desplazamiento horizontal (RNF-12).

## Pruebas obligatorias
- Las pruebas de las páginas corregidas siguen pasando

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Rediseños
