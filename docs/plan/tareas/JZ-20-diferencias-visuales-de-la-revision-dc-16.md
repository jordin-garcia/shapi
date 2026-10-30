---
id: JZ-20
titulo: Diferencias visuales de la revisión DC-16 (A6.2, A6.5 y B3.1)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: final
prioridad: P2
estado: hecha
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

## Resultado

- A6.2 ofrece «Reactivar» para el estado efectivo suspendido y conserva la reactivación administrativa existente, sin alterar la suspensión por falta de pago.
- A6.5 apila lista y formulario a 1280 px; a partir de 1440 px usa dos columnas con formulario de 340 px y desde 1536 px conserva los 440 px. Estado y Acción quedan completos sin desplazamiento horizontal.
- B3.1 usa el artículo y género de cada componente; conserva el texto y el impacto del correo del mockup `Degradado`.
- Regresiones en `Organizaciones.test.tsx`, `CuentasPlataforma.test.tsx` y `Estado.test.tsx`; comprobación real de ancho y visibilidad en `tests/e2e/tests/administracion-visual.spec.ts`. La precisión visual queda en `docs/specs/11-interfaz.md`.
- Verificación local: fase roja con 7 fallos Vitest y el desbordamiento E2E confirmado; después, 545 pruebas del frontend aprobadas (43 archivos), lint sin errores (11 avisos existentes), typecheck y builds correctos. La E2E pasó a 1280 px; Chromium confirmó también ausencia de desbordamiento a 1440 y 1536 px y se compararon las tres pantallas con sus mockups.
- Revisión independiente en contexto limpio según B9: LISTO, sin hallazgos de corrección ni opcionales.
