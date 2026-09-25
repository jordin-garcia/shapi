# Bitácora de Dominique Contreras

Cada tarea terminada agrega una entrada **al final** de este archivo (protocolo, paso B10):

```
## AAAA-MM-DD · <ID> · <título>
- Hecho: ...
- Decisiones: ...
- Pendiente o aviso para otros: ...
```

---

## 2026-09-24 · DC-01 · Workspace del frontend y sistema de diseño
- Hecho:
  - Monorepo pnpm en `frontend/` (`apps/panel`, `apps/portal`, `packages/ui`, `packages/api`).
  - Tokens CSS y tema de Tailwind 4 con la variante 4 "Plano azul" (Sora e IBM Plex Sans, 3 estados, radio de 8 px).
  - Componentes base en `@shapi/ui` con pruebas unitarias en Vitest.
  - Cliente `openapi-fetch` en `@shapi/api` con normalización de ProblemDetails (`codigo` y `errores`), interceptores y pruebas con MSW.
  - Generador de contratos OpenAPI modular `generar.mjs` con pruebas unitarias.
  - Lámina de estilo interactiva en `apps/panel/src/paginas/_UI.tsx` con pruebas en Vitest.
  - Configuración unificada de lint (ESLint), typecheck (TypeScript estricto) y Vitest con proyectos.
- Decisiones:
  - Vitest configurado con `test.projects` para separar entornos (`node` para api y generador; `jsdom` para ui y panel).
  - Inclusión de `credentials: 'include'` y `X-Requested-With: shapi` de forma global en `crearCliente`.
- Pendiente o aviso para otros:
  - **Todos:** ya pueden usar los componentes base importando desde `@shapi/ui` y el cliente HTTP desde `@shapi/api`.
  - **DC-02:** implementará las rutas y los layouts del panel sobre esta base.
