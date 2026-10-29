---
id: EM-24
titulo: Diferencias visuales de la revisión DC-16 (A2.1, A6.3 y B1.4)
persona: emilio
responsable: Emilio Méndez
avance: final
prioridad: P2
estado: pendiente
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
