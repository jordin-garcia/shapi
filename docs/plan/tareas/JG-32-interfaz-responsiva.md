---
id: JG-32
titulo: Interfaz responsiva en celulares y tabletas
persona: jordin
responsable: Jordin García
avance: final
prioridad: P2
estado: hecha
programada: 2026-11-10
depende_de: []
requisitos: [RNF-12]
pantallas: []
---

# JG-32 · Interfaz responsiva en celulares y tabletas

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P2

## Objetivo
Que el panel, la administración, los portales de marca blanca y el sitio público se puedan usar en un celular (desde 360 px de ancho) y en una tableta, sin desplazamiento horizontal de la página, y que en escritorio (desde 1024 px) se vean igual que antes.

## Origen
El 10 de noviembre de 2026, al probar Shapi en un celular, Jordin vio que el panel y el portal no se podían usar: las barras laterales miden 272 px fijos y en un celular de 390 px le dejan 118 px al contenido. Las capturas a 390 × 844 de todas las pantallas lo confirmaron: además de las barras, había columnas laterales de 400 a 520 px, formularios de ancho fijo, tablas sin desplazamiento propio y encabezados con la navegación en fila. RNF-12 solo exigía escritorio desde 1280 px, así que es un requisito nuevo (ADR-37).

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RNF-12.
- `docs/specs/11-interfaz.md` §1 (Estructura del panel y Portal).
- `docs/specs/12-decisiones.md` ADR-37.
- `frontend/apps/panel/src/layouts/` y `frontend/apps/portal/src/layouts/`.

## Archivos que creas o modificas
- `docs/specs/03-requisitos.md`, `docs/specs/11-interfaz.md` y `docs/specs/12-decisiones.md` (modificar).
- `frontend/packages/ui/src/index.tsx` (modificar): `BotonMenu`, `FondoCajon`, y márgenes de `Toast` y `DialogoConfirmacion`.
- `frontend/packages/ui/src/cajon.ts` (crear) y `frontend/packages/ui/package.json` (modificar, export `./cajon`): `useCajonMovil` y `clasesCajon`.
- `docs/manual-usuario.md` y `docs/manual-tecnico.md` (modificar): RNF-12 y el navegador.
- `frontend/apps/panel/src/layouts/*` y `frontend/apps/portal/src/layouts/*` (modificar).
- `frontend/apps/panel/src/modulos/**` y `frontend/apps/portal/src/modulos/**` (modificar): formularios de acceso, avisos de cierre de sesión y componentes con anchos fijos.
- `frontend/apps/panel/src/paginas/*` y `frontend/apps/portal/src/paginas/*` (modificar): las páginas con anchos o columnas fijas.
- `frontend/apps/panel/src/tests/` y `frontend/apps/portal/src/tests/` (crear): pruebas del menú móvil.
- `tests/e2e/tests/responsivo.spec.ts` (crear).

## Criterios de aceptación
1. Cuando la ventana mide menos de 1024 px, el panel, la administración y el portal deberán ocultar la barra lateral y mostrar un botón «Abrir menú» en la barra superior. El botón abre la barra lateral como un cajón sobre el contenido, con todas sus opciones y su pie. El cajón se cierra con «Cerrar menú», con la tecla Escape, al tocar fuera de él y al elegir una opción.
2. Desde 1024 px, la barra lateral deberá verse fija como antes, sin el botón de menú.
3. En todas las rutas del catálogo, a 360, 390 y 768 px de ancho, el documento no deberá tener desplazamiento horizontal (`scrollWidth` ≤ ancho de la ventana).
4. Las tablas que no caben deberán desplazarse dentro de su propia tarjeta, sin ensanchar la página.
5. Las columnas laterales de formularios y resúmenes deberán apilarse debajo del contenido principal cuando no caben.
6. En escritorio, las pruebas de pantallas existentes (`barras-laterales.spec.ts`, `administracion-visual.spec.ts` y las pruebas de Vitest) deberán seguir pasando. Solo se puede precisar una aserción para distinguir una clase de pantallas angostas (`max-lg:`) de la de escritorio.

## Pruebas obligatorias
- Vitest: el botón de menú abre y cierra el cajón (Escape, opción elegida) en el panel y en el portal (criterio 1).
- E2E `responsivo.spec.ts`: recorre las rutas del catálogo con cada rol a 360, 390 y 768 px y comprueba el criterio 3; abre y cierra el menú a 390 px (criterio 1) y comprueba que a 1366 px no hay botón de menú (criterio 2).
- Las pruebas E2E y de Vitest existentes (criterio 6).

## Verificación
Todos estos comandos deben pasar:
```
node scripts/tareas.mjs --validar
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
cd tests/e2e && pnpm exec playwright test tests/responsivo.spec.ts tests/barras-laterales.spec.ts tests/administracion-visual.spec.ts
```

## Fuera de alcance
- Mockups nuevos para celular: el diseño móvil se deriva de los mockups de escritorio (ADR-37).
- Convertir las tablas en tarjetas: en celular se desplazan dentro de su tarjeta.
- Aplicación móvil nativa (sigue fuera de alcance, 01 §6).

## Resultado

**Hecho (10 y 11 de noviembre de 2026):**
- **Estructura:** debajo de 1024 px, las barras laterales del panel, la administración y el portal se abren como un cajón con el botón «Abrir menú» de la barra superior (64 px). El cajón se cierra con «Cerrar menú», con Escape, al tocar el fondo y en cada navegación, aunque sea a la misma página (`useLocation().key`). Las piezas compartidas son `BotonMenu` y `FondoCajon` (`packages/ui/src/index.tsx`), y `useCajonMovil` y `clasesCajon` (`packages/ui/src/cajon.ts`).
- **Márgenes:** el contenido pasa de 44 px a 32 o 20 px. Los encabezados públicos y del portal, de 80 px a 32 o 16 px. Los formularios de acceso, de 40 px de relleno a 24 px.
- **Páginas:**
  - las columnas laterales de 340 a 520 px se apilan;
  - los títulos con botón ponen el botón debajo;
  - los formularios de ancho fijo pasan a `w-full max-w-…`;
  - las rejillas responsivas tienen `grid-cols-1` como base, para que una tabla ancha no ensanche la columna;
  - las tablas de A6.4 y A7.1 se desplazan dentro de su tarjeta;
  - la URL de A5.1 se parte;
  - la vista previa de A3.7 se reduce al ancho disponible debajo de 1024 px.
- **Escritorio:** desde 1024 px todo se ve como antes. Las clases nuevas llevan `max-lg:`, o la clase de escritorio pasa a `lg:`.
- **Especificaciones:** RNF-12, 11 §1 («Pantallas angostas»), ADR-37 y los manuales en Markdown (el Word y los PDF no se regeneraron).

**Decisiones:**
- Las tablas se desplazan a lo ancho dentro de su tarjeta en vez de convertirse en tarjetas (ADR-37).
- Sin mockups de celular: el diseño móvil se deriva de los de escritorio.
- En `Estructura.test.tsx`, la aserción de que la barra no tiene `overflow-y-auto` se precisó para aceptar solo `max-lg:overflow-y-auto` (el cajón) y seguir rechazando la clase sin prefijo.

**Verificación:**
- `pnpm lint` (0 errores; las 11 advertencias del portal ya existían), `pnpm typecheck`, `pnpm test` (560 de 560) y `pnpm build`.
- E2E `responsivo.spec.ts` a 360, 390 y 768 px y a 1024 px, más `barras-laterales.spec.ts` y `administracion-visual.spec.ts`.
- `node scripts/tareas.mjs --validar`.
- Capturas de antes y después a 390 × 844, fuera del repositorio.

**Revisión en contexto limpio (subagente `revisor`):** 6 correcciones, todas aplicadas:
- la escala fija de la vista previa en escritorio;
- las columnas de 440 px de A6.4, A6.4b y A7.1 desde `lg`;
- el cierre del cajón al elegir la página actual;
- la prueba del toque en el fondo;
- las rutas de contratación y cambio de plan en la E2E;
- el texto de ADR-37 y del criterio 6.

De las opcionales se aplicaron: la barra de A0.1 a 64 px, la E2E de escritorio en su propio contexto con el portal, la E2E del menú de la administración, la lista de archivos de esta tarea y la copia de RNF-12 del manual técnico. Queda sin aplicar devolver el foco al botón al cerrar el cajón con Escape.

**Archivos principales:** `frontend/packages/ui/src/cajon.ts`, `frontend/packages/ui/src/index.tsx`, `frontend/apps/panel/src/layouts/Navegacion.tsx`, `frontend/apps/portal/src/layouts/LayoutPortal.tsx`, `frontend/apps/portal/src/layouts/BarraLateralPortal.tsx` y `tests/e2e/tests/responsivo.spec.ts`.
