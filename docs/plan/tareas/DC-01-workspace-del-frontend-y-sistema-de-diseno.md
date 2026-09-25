---
id: DC-01
titulo: Workspace del frontend y sistema de diseño
persona: dominique
responsable: Dominique Contreras
avance: 1
prioridad: P1
estado: hecha
depende_de: []
requisitos: [RNF-12]
pantallas: []
---

# DC-01 · Workspace del frontend y sistema de diseño

**Responsable:** Dominique Contreras · **Avance:** 1 · **Prioridad:** P1 · **Sin dependencias**

## Objetivo
Crear el workspace pnpm del frontend con las dos aplicaciones (panel y portal), el paquete de UI con los tokens y los componentes base de la variante 4, y el cliente HTTP base. Todas las dependencias del frontend quedan instaladas desde el inicio.

## Contexto que debes leer
- `docs/specs/11-interfaz.md` §1 **completo** (tokens y componentes)
- `docs/specs/06-arquitectura.md` §9 (stack del frontend)
- `docs/plan/convenciones.md` §1, §3 (lockfile) y §7
- Mockups: `mockups/A0/Lamina.dc.html` (lámina de estilo oficial) y cualquier pantalla de `mockups/A3/` como referencia

## Archivos que creas o modificas
- `frontend/package.json` (crear; scripts: `dev` levanta el panel en 5173 y el portal en 5174 a la vez, más `lint`, `typecheck`, `test`, `build` y `generar:api`)
- `frontend/pnpm-workspace.yaml`, `frontend/pnpm-lock.yaml`, `frontend/tsconfig.base.json` y la configuración de ESLint (crear)
- `frontend/packages/ui/**` (crear): tokens como variables CSS y tema de Tailwind 4; fuentes Sora e IBM Plex Sans con `@fontsource`; componentes `Boton` (principal, secundario, deshabilitado), `Campo` (con error), `Etiqueta` (correcto, alerta, neutro), `Tarjeta`, `Tabla`, `Aviso`, `Esqueleto`, `Toast`, `DialogoConfirmacion` y `Selector`
- `frontend/packages/api/**` (crear): un cliente con `openapi-fetch` que agrega `credentials: 'include'` y `X-Requested-With: shapi`, y convierte ProblemDetails en `ErrorApi {codigo, titulo, errores}`. El script `generar:api` genera `src/generado/<modulo>.ts` desde `../contratos/openapi/*.yaml` con `openapi-typescript`
- `frontend/apps/panel/**` y `frontend/apps/portal/**` (crear aplicaciones Vite + React 19 + TypeScript estricto que compilan)
- `frontend/apps/panel/src/paginas/_UI.tsx` (crear una página `/_ui` que muestre la lámina de estilo con los componentes)

## Criterios de aceptación
1. Quedan instaladas desde el inicio: react, react-dom, react-router, @tanstack/react-query, tailwindcss 4 y @tailwindcss/vite, openapi-fetch, openapi-typescript, msw, vitest, @testing-library/react, @testing-library/user-event, jsdom, recharts, react-markdown, rehype-sanitize, @fontsource/sora, @fontsource/ibm-plex-sans, eslint y typescript. Así nadie tiene que tocar el lockfile después.
2. Los tokens son exactamente los colores, tipografías, radios y espaciados de 11 §1. Solo hay tres colores de estado.
3. La página `/_ui` reproduce los elementos de `mockups/A0/Lamina.dc.html` (paleta, tipografía, botones, etiquetas, campo, tarjeta y tabla).
4. `pnpm lint`, `pnpm typecheck`, `pnpm test` y `pnpm build` pasan. Hay al menos una prueba por componente base.
5. `pnpm generar:api` funciona aunque todavía no haya contratos, y no falla con la carpeta vacía.
6. `frontend/package.json` fija la versión de pnpm en `packageManager` (la estable actual, 11.x) y `engines.node >= 24`, para que todos generen el mismo lockfile.
7. Vitest queda configurado con jsdom, Testing Library y MSW (`test/servidor.ts`), para que las demás tareas lo reutilicen.

## Pruebas obligatorias
- Vitest de cada componente de `packages/ui`
- Prueba del cliente: agrega la cabecera y convierte ProblemDetails

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Layouts, barra lateral y rutas (DC-02)
- Pantallas concretas

## Notas
- Si JG-01 todavía no se integró, no hay CI. Verifica localmente e integra con `gh pr merge --squash --delete-branch`. Cuando exista la CI, el *job* `frontend` se activará solo.

## Resultado
- Monorepo pnpm configurado para el frontend con aplicaciones `panel` y `portal`, y paquetes compartidos `@shapi/ui` y `@shapi/api`.
- Sistema de diseño de la variante 4 ("Plano azul") implementado con Tailwind 4, variables CSS y tipografías Sora e IBM Plex Sans.
- Componentes base (`Boton`, `Campo`, `Etiqueta`, `Tarjeta`, `Tabla`, `Aviso`, `Esqueleto`, `Toast`, `DialogoConfirmacion`, `Selector`) implementados y probados.
- Cliente HTTP base en `@shapi/api` con `openapi-fetch`, credenciales incluidas, cabecera `X-Requested-With: shapi` y manejo de ProblemDetails con `ErrorApi`.
- Script `generar:api` para procesar contratos OpenAPI y generar tipos TypeScript de forma modular y estricta.
- Lámina interactiva `/_ui` basada en `mockups/A0/Lamina.dc.html` con todos los tokens y componentes.

### Corrección · 2026-09-25
- DC-02 reemplazó `main.tsx` por el router y la lámina quedó sin ruta: `/_ui` mostraba "Página no encontrada". Se agregó la ruta `/_ui` en `apps/panel/src/rutas.tsx`, fuera de los layouts y sin consultar la sesión, con carga diferida (`A0Lamina` en `paginasDiferidas.tsx`).
- `tests/rutas.test.tsx` comprueba ahora `/_ui` a través del router. La prueba de `_UI.test.tsx` renderiza el componente directo y por eso no detectó la regresión.
- `docs/specs/11-interfaz.md` indica la ruta de A0.2: `shapi.localhost/_ui`.
