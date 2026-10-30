---
id: JZ-20
titulo: Diferencias visuales de la revisión DC-16 (A6.2, A6.5 y B3.1)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P2
estado: pendiente
programada: 2026-10-31
depende_de: [DC-16]
requisitos: [RNF-12]
pantallas: [A6.2, A6.5, B3.1]
---

# JZ-20 · Diferencias visuales de la revisión DC-16

**Responsable:** José Pablo Zúñiga · **Avance:** final · **Prioridad:** P2 · **Depende de:** DC-16

## Objetivo
Corregir las diferencias contra el mockup que encontró la revisión visual de DC-16 (`docs/plan/revision-visual.md`) en pantallas de José Pablo.

## Contexto que debes leer
- `docs/plan/revision-visual.md` (filas A6.2, A6.5 y B3.1)
- Mockups: `mockups/A6/Organizaciones.dc.html`, `mockups/A6/Cuentas.dc.html` y `mockups/B3/Degradado.dc.html`

- Capturas a 1280 px con la siembra: `docs/plan/capturas/dc-16/A6.2.png`, `docs/plan/capturas/dc-16/A6.5.png`, `docs/plan/capturas/dc-16/B3.1.png`

## Archivos que creas o modificas
- Las páginas de A6.2, A6.5 y B3.1 en `frontend/apps/panel/src/paginas/` y sus pruebas

## Criterios de aceptación
1. En A6.2, una organización suspendida ofrece «Reactivar», no «Suspender».
2. A6.5 no tiene desplazamiento horizontal a 1280 px: las columnas Estado y Acción se ven completas (RNF-12).
3. El aviso de B3.1 concuerda en género con el componente («La compuerta de APIs está degradada») y usa el texto del mockup `Degradado`.

## Pruebas obligatorias
- Una prueba de Vitest por criterio (el 2, con una prueba E2E a 1280 px o la captura en el PR)

## Verificación
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Rediseños
