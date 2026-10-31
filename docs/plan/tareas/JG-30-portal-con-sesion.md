---
id: JG-30
titulo: Encabezado y pie del portal con la sesión abierta (A5.0 y A5.2)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-30
depende_de: []
requisitos: [RF-15, RF-16]
pantallas: [A5.0, A5.2]
---

# JG-30 · Encabezado y pie del portal con la sesión abierta (A5.0 y A5.2)

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P1

## Objetivo
Que, con la sesión abierta, todo el portal del consumidor tenga la misma estructura interna que la documentación, los planes y su cuenta: arriba solo la marca del portal, la barra lateral con «API» y «Mi cuenta» y, al pie de la barra, el nombre y la empresa del consumidor con el botón para cerrar sesión. Sin «Entrar», «Crear cuenta» ni el pie público.

## Origen
Ensayo de JG-29 (30 oct 2026, aviso para JG-17 en `docs/plan/bitacora/jordin.md`): con la sesión abierta, la consola (`/consola`) y el inicio (`/`) muestran el encabezado público con «Entrar» y «Crear cuenta», y el pie con «Entrar», aunque la barra lateral ya muestra a la consumidora. En el video del 18 de octubre también se vio el pie público.
- La consola está en el grupo de rutas de `LayoutPublico` y dibuja su propia barra lateral. Su mockup (`mockups/A5/Consola.dc.html`) tiene la misma estructura que la documentación (`LayoutPortal`): solo la marca arriba, sin pie.
- El inicio solo tiene mockup sin sesión (`mockups/A5/Main.dc.html`). El 30 de octubre, Jordin decidió que, con la sesión abierta, el portal «por dentro» se vea siempre con la estructura de la documentación y los planes, para que sea coherente con el panel del proveedor.

## Contexto que debes leer
- `docs/specs/11-interfaz.md` §3 (A5) y §4, «Comportamiento del portal y de sus pantallas de acceso (DC-03 y DC-08)»
- Mockups: `mockups/A5/Main.dc.html`, `mockups/A5/Consola.dc.html` y `mockups/A5/Documentacion.dc.html`

## Archivos que creas o modificas
- `frontend/apps/portal/src/rutas.tsx` (modificar)
- `frontend/apps/portal/src/layouts/LayoutPublico.tsx` (modificar)
- `frontend/apps/portal/src/paginas/A5-2-Consola.tsx` (modificar: quitar la barra lateral propia)
- `frontend/apps/portal/src/paginas/A5-0-Inicio.tsx` (modificar: «Crear cuenta» solo sin sesión)
- `docs/video/guion.md` (modificar: quitar la advertencia del encabezado)
- `frontend/apps/portal/src/tests/*.test.tsx` (crear o modificar)
- `docs/specs/11-interfaz.md` §4 (precisar el encabezado y el pie con sesión)

## Criterios de aceptación
1. A5.2 usa la estructura de la documentación con o sin sesión: encabezado con solo la marca, la barra lateral compartida con «Consola de pruebas» activa, una sola barra lateral y sin pie público.
2. Con la sesión abierta, el inicio (A5.0) se muestra dentro de esa misma estructura: sin «Entrar», «Crear cuenta» ni el pie público, y con el nombre y la empresa del consumidor al pie de la barra.
3. Sin sesión, el inicio conserva el encabezado y el pie públicos de su mockup.
4. Las pantallas de acceso no cambian.

## Pruebas obligatorias
- Frontend: A5.2 con sesión y sin sesión no muestra «Entrar», «Crear cuenta» ni el pie, y tiene una sola barra lateral (RF-16).
- Frontend: A5.0 con sesión no muestra «Entrar», «Crear cuenta» ni el pie, y muestra al consumidor en la barra (RF-15 y RF-16); sin sesión conserva el encabezado y el pie públicos.

## Verificación
Todos estos comandos deben pasar:
```
node scripts/tareas.mjs --validar
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```
Además, comprobar A5.0 y A5.2 con y sin sesión en el ambiente productivo simulado y compararlas con sus mockups (protocolo B8).

## Fuera de alcance
- Cambiar el panel del proveedor o las pantallas de la cuenta (B2), que ya tienen esta estructura.

## Resultado

Con la sesión abierta, todo el portal del consumidor se ve con la estructura interna, como pidió Jordin: arriba solo la marca, la barra lateral «API» y «Mi cuenta» y, al pie, el nombre, la empresa y el botón para cerrar sesión. Ya no aparecen «Entrar», «Crear cuenta» ni el pie público.

- **A5.2:** la ruta `/consola` pasó del grupo de `LayoutPublico` al de `LayoutPortal`, y la página ya no dibuja su propia barra lateral. Con y sin sesión tiene la estructura de su mockup, igual que la documentación.
- **A5.0:** con la sesión abierta, `LayoutPublico` muestra el inicio dentro de `LayoutPortal` y la portada deja solo «Ver documentación». «Página no encontrada» sigue la misma regla. Mientras se consulta la sesión se muestra el estado de carga, para no mostrar un instante el encabezado público; si la sesión responde 5xx, se muestra el aviso con «Reintentar» (11 §4). Sin sesión, el inicio no cambia.
- **Especificación:** `docs/specs/11-interfaz.md` §4 agrega A5.2 a la barra del portal y precisa el portal con la sesión abierta. Se quitó del guion del video la advertencia sobre el encabezado.

### Verificación (30 oct 2026)
- Fase roja: antes del cambio fallaron las 3 pruebas nuevas de la estructura interna (A5.2 con y sin sesión, y A5.0 con sesión); la de A5.0 sin sesión pasaba.
- 134 pruebas del portal y 554 del frontend aprobadas; lint sin errores (11 advertencias previas); tipos y compilación correctos. Tres pruebas que validan el encabezado y el pie públicos de A5.0 ahora declaran la visita sin sesión, que es la de su mockup.
- Ambiente productivo simulado con el borde reconstruido: capturas a 1440 × 900 de A5.0 y A5.2 con y sin sesión, comparadas con los mockups `A5/Main`, `A5/Consola` y `A5/Documentacion`. Con sesión no aparecen «Entrar», «Crear cuenta» ni el pie.

- Revisión en contexto limpio: se corrigieron el 5xx de la sesión en `/`, que caía a la vista pública, y la 404 con sesión, que conservaba el encabezado público. Cada corrección tiene su prueba, con fase roja confirmada. En `/` no se usa `LayoutPortal` para el error, porque su consulta reintenta al montarse y entraba en un ciclo de carga.

### Archivos principales
- `frontend/apps/portal/src/rutas.tsx`, `layouts/LayoutPublico.tsx`, `paginas/A5-2-Consola.tsx` y `paginas/A5-0-Inicio.tsx`
- `frontend/apps/portal/src/tests/Estructura.test.tsx`
- `docs/specs/11-interfaz.md` y `docs/video/guion.md`
