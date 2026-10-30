---
id: EM-24
titulo: Diferencias visuales de la revisión DC-16 (A2.1, A6.3 y B1.4)
persona: emilio
responsable: Emilio Méndez
avance: final
prioridad: P2
estado: hecha
programada: 2026-10-31
depende_de: [DC-16]
requisitos: [RNF-12]
pantallas: [A2.1, A6.3, B1.4]
---

# EM-24 · Diferencias visuales de la revisión DC-16

**Responsable:** Emilio Méndez · **Avance:** final · **Prioridad:** P2 · **Depende de:** DC-16

## Objetivo
Corregir las diferencias contra el mockup que encontró la revisión visual de DC-16 (`docs/plan/revision-visual.md`) en pantallas de Emilio.

## Contexto que debes leer
- `docs/plan/revision-visual.md` (filas A2.1, A6.3 y B1.4)
- Mockups: `mockups/A2/Main.dc.html`, `mockups/A6/Pagos.dc.html` y `mockups/B1/Suscripcion*.dc.html`

- Capturas a 1280 px con la siembra: `docs/plan/capturas/dc-16/A2.1.png`, `docs/plan/capturas/dc-16/A6.3.png`, `docs/plan/capturas/dc-16/B1.4.png`

## Archivos que creas o modificas
- Las páginas de A2.1, A6.3 y B1.4 en `frontend/apps/panel/src/paginas/` y sus pruebas

## Criterios de aceptación
1. A2.1 marca el plan vigente con «Plan actual», como el mockup.
2. A6.3 muestra los meses abreviados como el resto de la aplicación («sep», no «sept»).
3. B1.4 muestra el rótulo «Organización», el botón «Cambiar de plan» en el encabezado y la fecha de renovación con el formato del mockup.

## Pruebas obligatorias
- Una prueba de Vitest por criterio

## Verificación
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Rediseños

## Resultado
- A2.1 coloca «Plan actual» en el encabezado de la tarjeta del plan vigente y conserva el indicador «Recomendado» de Producto.
- A6.3 usa abreviaturas de mes consistentes con el resto de la aplicación (por ejemplo, «23 sep 2026»).
- B1.4 reproduce el encabezado de Organización del mockup, ubica «Cambiar de plan» junto al título y muestra la renovación con fecha larga («23 de septiembre de 2026»).
- Se agregó `DiferenciasVisuales.test.tsx`, con una prueba por criterio. Las tres fallaban antes de la implementación; pasan después. La revisión en contexto limpio pidió hacer explícita la prueba de la etiqueta superior de A2.1; se ajustó y se repitieron las verificaciones.
- `node scripts/tareas.mjs --validar`, `pnpm lint`, `pnpm typecheck`, `pnpm test` (44 archivos, 548 pruebas) y `pnpm build` pasan. Lint conserva 11 avisos existentes en el portal; build conserva el aviso de tamaño del paquete del portal.
- Se contrastaron los textos y la estructura con los mockups y las capturas de DC-16. No se pudo recapturar el entorno con datos sembrados: el daemon de Docker no es accesible en este equipo (`permission denied` en el socket) y este worktree no tiene `.env`.
