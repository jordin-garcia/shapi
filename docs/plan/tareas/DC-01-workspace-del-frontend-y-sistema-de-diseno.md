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

### Correcciones de la auditoría (2026-09-27)

Paso 12 de `docs/plan/auditoria-2026-09-25.md` (H-85 a H-93):
- **Dependencia sin uso (H-85).** Se quitó `@fontsource/instrument-sans` con `pnpm remove`, que regeneró el lockfile.
- **Tokens (H-86).**
  - Están todos los de 11 §1: los dos tonos de cada estado (`--correcto-base` `#1F8A5B` y `--alerta-base` `#C2481F`), la escala tipográfica (`text-titulo`, `text-titulo-tarjeta`, `text-cuerpo`, `text-dato`, `text-etiqueta` y `text-encabezado`) y la de espaciado (`--espacio-4` a `--espacio-80`).
  - Se agregaron `--tinta-inactiva`, `--borde-inactivo` y `--anillo-foco`, que usan el botón deshabilitado y el campo.
  - El tema oscuro (`.dark`) solo redefine los valores de la columna oscura de 11 §1; ya no hay valores inventados. Los demás tokens heredan el claro (decisión de Jordin).
  - **`.dark` no cambiaba ningún color.** Tailwind resolvía `--color-fondo: var(--fondo)` una sola vez en `:root`, y los hijos de `.dark` heredaban el valor claro. Ahora los colores se declaran en `@theme inline`, así que `bg-fondo` usa `var(--fondo)` y toma el valor oscuro. Lo detectó la revisión en contexto limpio, y se comprobó en el navegador: el fondo oscuro de `/_ui` es `#060910`.
  - Se quitó la paleta por defecto de Tailwind (`--color-*: initial`); solo queda `white`. El botón secundario usa `hover:bg-fondo` en lugar de `hover:bg-gray-50`.
- **Medidas de la lámina (H-87).**
  - Botón: texto de 15 px.
  - Campo: 48 px de alto, texto de 15 px, anillo de foco y texto guía en `--tinta-inactiva`.
  - Tarjeta: 20 px de relleno.
  - Tabla: encabezado seminegro con borde `--borde`, relleno solo a la derecha y separador en todas las filas.
  - En `/_ui`: el logotipo tiene 48 px y los trazos de la lámina, y las muestras llevan su descripción y los tonos base, en el orden de la lámina.
- **Accesibilidad (H-88).**
  - `Campo` tiene etiqueta propia (`etiqueta`) y marca el error con `aria-invalid` y `aria-describedby`.
  - `DialogoConfirmacion` admite textos (`textoConfirmar`, `textoCancelar`) y contenido. Además, atrapa el foco (también si se hace clic en el fondo o en el texto del diálogo), se cierra con Escape, devuelve el foco al cerrarse y se nombra con `aria-labelledby`.
  - `Selector` acepta `aria-label`, `id` y los demás atributos de `select`.
- **Avisos (H-89).** `Aviso` y `EstadoError` usan los colores de su estado sin las clases `eti-*`, así que ya no heredan `nowrap`, `uppercase` ni `text-xs`. "Reintentar" es un `Boton` secundario.
- **`generar:api` (H-90).** No falla si `contratos/openapi/` no existe.
- **Pruebas y configuración (H-91 y H-92).**
  - MSW trata como error una petición sin simular.
  - Los proyectos de Vitest incluyen `*.test.{ts,tsx}`.
  - Se borró `apps/panel/vitest.config.ts` y los `test` de cada paquete: se prueba con `pnpm test` desde `frontend/`.
  - Hay pruebas de los tres estados, de los tokens contra 11 §1 y de la paleta y los estados de `/_ui`. Cada muestra se compara con el valor de su token en `style.css`.
- **Nombres en español (H-93).**
  - Se renombraron: `Tabla` (`encabezados`, `filas`), `Selector` (`opciones` con `etiqueta` y `valor`), `DialogoConfirmacion` (`abierto`, `cerrar`, `confirmar`), `CargaDiferida` (antes `Suspensify`), `esAdministrador` y `claseEnlace` en los layouts.
  - `ProblemDetailsError` pasó a ser la clase `ErrorApi`, con `codigo`, `titulo`, `estado` y `errores` como propiedades directas (decisión de Jordin).
  - Se tradujeron los comentarios de `style.css`.
  - El portal muestra "El portal todavía no está disponible." hasta DC-03 (decisión de Jordin).
- Se precisaron `11-interfaz.md` §1 y §4 y `convenciones.md` §7.
- Las pantallas A1 (EM-03) conservan `CampoEtiquetado` y `AvisoError`: unificarlas con los componentes base le queda a DC-16 (decisión de Jordin).

**Auditoría final (paso 17):**
- **H-132:** los botones de `DialogoConfirmacion` llevan `type="button"`: dentro de un formulario ya no lo envían. Tiene prueba.
- **H-133:** `/_ui` muestra enfocado el campo «Subdominio», como la lámina. En la superficie oscura, los rótulos y los códigos de color usan `--tinta-rotulo`, y los nombres de las bandas usan `--tinta-navegacion`. Tiene prueba.
