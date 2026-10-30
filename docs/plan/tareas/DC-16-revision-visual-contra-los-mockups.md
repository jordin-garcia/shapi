---
id: DC-16
titulo: Revisión visual contra los mockups
persona: dominique
responsable: Dominique Contreras
avance: final
prioridad: P2
estado: hecha
programada: 2026-10-24
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
- Páginas propias (corregir), incluido `frontend/apps/portal/src/modulos/sesion/FormulariosAcceso.tsx` (criterio 4)
- `docs/plan/tareas/<nuevas>.md` (para las pantallas de otros)

## Criterios de aceptación
1. Todas las pantallas del catálogo están revisadas con la siembra de demostración (captura contra mockup).
2. Las diferencias en pantallas propias se corrigen. Las de otros quedan como tareas P2 para su dueño, con las capturas y la lista de diferencias.
3. Se verifica a 1280 px de ancho que no aparezca desplazamiento horizontal (RNF-12).
4. «Reintentar» es el botón secundario de 11 §4 en los avisos de error de las pantallas de acceso del panel (los envoltorios de A1, H-89 de la auditoría del 25 sep) y del portal (`AvisoError` de `frontend/apps/portal/src/modulos/sesion/FormulariosAcceso.tsx`, H-83 de la auditoría del 3 oct), y no un texto subrayado.

## Pruebas obligatorias
- Las pruebas de las páginas corregidas siguen pasando

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Rediseños

## Resultado
- Se creó `docs/plan/revision-visual.md` con las 53 rutas del catálogo capturadas a 1280 px en el ambiente productivo simulado con la siembra, y comparadas con sus mockups. Ninguna tiene desplazamiento horizontal (RNF-12).
- Pantallas propias: A3.5 recortaba la columna «Peso en llamadas»; se corrigió con `whitespace-nowrap` en `A3-5-ConfigRutas.tsx`.
- Criterio 4: «Reintentar» es el `Boton` secundario en `AvisoError` del panel (`modulos/identidad/Formularios.tsx`) y del portal (`modulos/sesion/FormulariosAcceso.tsx`).
- Diferencias de otros: EM-24 (A2.1, A6.3 y B1.4) y JZ-20 (A6.2, A6.5 y B3.1), ambas P2.
