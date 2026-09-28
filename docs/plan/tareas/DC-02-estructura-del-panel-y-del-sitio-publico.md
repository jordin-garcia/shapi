---
id: DC-02
titulo: Estructura del panel y del sitio público
persona: dominique
responsable: Dominique Contreras
avance: 1
prioridad: P1
estado: hecha
depende_de: [DC-01]
requisitos: [RF-07, RNF-12]
pantallas: [N.1]
---

# DC-02 · Estructura del panel y del sitio público

**Responsable:** Dominique Contreras · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** DC-01

## Objetivo
Crear los layouts del sitio público, del panel del proveedor y de la administración, la barra lateral de cada uno, los guardias por sesión y rol, y el router con **todas** las rutas del catálogo apuntando a páginas de relleno. Así cada persona solo reemplaza el archivo de su pantalla, sin tocar el router.

## Contexto que debes leer
- `docs/specs/11-interfaz.md` §3 **completo** (catálogo: IDs, archivos y rutas; el responsable de cada pantalla sale de la tarea que la implementa) y §4
- `docs/specs/04-roles-y-permisos.md` §3.1 y §3.2 (qué ve cada rol)
- `docs/specs/10-identidad-y-seguridad.md` §1 (destinos por rol)
- Mockups: `mockups/Navegacion/Main.dc.html` (barra lateral del proveedor), `mockups/A6/Main.dc.html` y `mockups/B3/Soporte.dc.html` (barras de administración y de soporte), y `mockups/A1/Main.dc.html` (encabezado público)
- `docs/plan/convenciones.md` §3 (regla de `rutas.tsx`)

## Archivos que creas o modificas
- `frontend/apps/panel/src/rutas.tsx` (crear)
- `frontend/apps/panel/src/layouts/{LayoutPublico,LayoutPanel,LayoutAdmin}.tsx` (crear)
- `frontend/apps/panel/src/paginas/<ID>-<Nombre>.tsx` (crear **una página de relleno por cada pantalla** del panel, la administración y el sitio público del catálogo, que muestre "Pantalla pendiente · <ID> · <nombre> · responsable <persona>")
- `frontend/apps/panel/src/modulos/sesion/**` (crear: `RequiereSesion`, `RequiereRol` y un `useSesion` provisional que llama a `GET /api/auth/sesion`)
- `frontend/apps/panel/src/modulos/apis/SelectorApi.tsx` (crear)
- Páginas 404 y "sin permiso"

## Criterios de aceptación
1. Todas las rutas del panel, de la administración y del sitio público de 11 §3 existen, y cada una muestra su página de relleno con su ID. Las rutas cargan las páginas en forma diferida (`lazy`). **Implementar una pantalla es reemplazar el contenido de su archivo en `paginas/`, sin tocar `rutas.tsx`.**
2. La barra lateral del proveedor reproduce N.1: grupos Publicación / API (con el selector de API) / Organización (con "Casos de soporte"), el pie con el nombre y el rol que lleva a `/panel/perfil`, y el botón para salir. El elemento activo se resalta.
3. La barra de administración reproduce A6 (Plataforma / Soporte / Sistema) para el administrador, y la versión reducida de B3.1 (Soporte / Sistema) para el soporte.
4. `RequiereSesion` redirige a `/entrar` si no hay sesión. `RequiereRol` muestra "No tiene permiso para ver esta página" si el rol no corresponde. El personal proveedor no entra a `/admin/*`, y el administrador y el soporte no entran a `/panel/*`.
5. El selector de API lista las APIs con `GET /api/apis` y guarda la seleccionada en la URL (`/panel/apis/:id/...`). Mientras DC-04 no exista, muestra la lista vacía sin fallar.
6. Los estados base de 11 §4 (cargando, error y sin permiso) quedan como componentes reutilizables.

## Pruebas obligatorias
- Vitest: todas las rutas del catálogo resuelven a una página, la barra lateral por rol y los guardias

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Captura de la barra lateral del proveedor comparada con `mockups/Navegacion/Main.dc.html`

## Fuera de alcance
- Pantallas concretas (cada responsable)
- Estructura del portal (DC-03)

## Resultado
- Se crearon los Layouts (`LayoutPublico`, `LayoutPanel`, `LayoutAdmin`), incluyendo la estructura principal y la barra lateral de los mockups N.1 y A6.
- Se agregaron las vistas placeholder (paginas de relleno) para todas las rutas del panel y sitio público, permitiendo trabajo en paralelo sin conflictos en el router.
- El archivo `rutas.tsx` define todo el árbol de navegación usando Lazy Loading.
- Se generó el contrato OpenAPI de `/api/apis`; el cliente provisional de sesión mantiene su tipo dentro del módulo hasta que Identidad publique su contrato propio.
- Los guardias `RequiereSesion` y `RequiereRol` están configurados y funcionando en el router.

### Correcciones verificadas · 2026-09-25

- Se corrigieron las URLs de sesión y APIs para no duplicar `/api`.
- El selector permite elegir una API desde la lista, conserva la sección al cambiarla y los enlaces usan el ID de la URL. Sin una API seleccionada no se generan enlaces con IDs ficticios.
- Los guardias y menús aplican los permisos de propietario, editor, lector, administrador y soporte. El soporte conserva acceso de consulta a organizaciones, según 04 §3.2; su barra sigue la versión reducida B3.
- Cerrar sesión llama a `POST /api/auth/salir` con CSRF, cancela las consultas y limpia la caché privada. Un fallo conserva la sesión y permite reintentar.
- Se distinguen una sesión ausente (401) y un error de red/servidor; se reutilizan los estados de carga, error y sin permiso de `@shapi/ui`.
- El encabezado público, la organización del proveedor, los roles y las fuentes Sora/IBM Plex Sans se ajustaron a los mockups. La generación de tipos ejecuta los contratos por módulo y propaga los errores.
- Las páginas siguen siendo de relleno, como exige DC-02; su implementación corresponde a las tareas de cada responsable. Los componentes diferidos están en `paginasDiferidas.tsx`, sin cambiar la regla de reemplazar únicamente el archivo de cada página.

Evidencia local (Node 24.14.0):

```text
pnpm lint                  OK (4 paquetes)
pnpm typecheck             OK (4 paquetes)
pnpm test                  6 archivos; 99 pruebas aprobadas
pnpm build                 OK (panel y portal)
pnpm generar:api           OK (apis.yaml)
node scripts/tareas.mjs --validar  Plan válido: 65 tareas
git diff --check           OK
```

- Fase roja: una ruta protegida no resolvía a su página por la consulta incorrecta de sesión; la prueba pasa tras la corrección.
- Comparación con Chromium/Playwright y API simulada: proveedor, administrador, soporte y encabezado público; estados cargando, error, sin APIs, sin permiso y 404. Capturas locales en `/tmp/shapi-dc02-capturas/`. Se comprobó el proveedor a 1440 y 1280 px, encabezado de 76 px, barra de 272 px y contenido/grupos de N.1.
- Revisión independiente según `docs/plan/prompts/revision.md`: **LISTO**, sin hallazgos pendientes tras corregir encabezado y ampliar pruebas.
- La rama se rebasó sobre `main` después de integrar DC-01 y se conservó su generador de contratos y la normalización segura de errores.
- El cliente provisional de sesión se alineó con la respuesta real de la rama EM-02 (`usuario` y `organizacion` anidados, respuesta 200 al salir) y la transforma al modelo que consumen los layouts, sin apropiarse del contrato OpenAPI de Identidad.

### Correcciones de la auditoría (2026-09-27)

Paso 13 de `docs/plan/auditoria-2026-09-25.md` (H-94 a H-102):
- **A0.1 fuera del encabezado público (H-94).** `/` es una ruta propia, como `/_ui`. `LayoutPublico` es una ruta sin `path` que solo envuelve las pantallas de acceso.
- **Errores de ruta (H-95).** `paginas/Error-Ruta.tsx` distingue un 404 (`isRouteErrorResponse`) de los demás errores. Para estos muestra `EstadoError`, y "Reintentar" vuelve a cargar la página (`modulos/navegador.ts`). Hay un `path: '*'` en la raíz.
- **Rutas índice, 403 y 404 (H-96).**
  - `/panel` y `/admin` llevan al destino del rol (`IrAlDestino`), y `/panel/apis/:id` lleva a `especificacion`.
  - `RequiereRol` tiene la opción `area`: el 403 entre áreas muestra "Ir a su panel" y "Cerrar sesión" (decisión de Jordin).
  - `/panel/*` y `/admin/*` tienen su propio 404, que conserva el layout.
- **Medidas de N.1 (H-97).**
  - Barra superior: 0 44 0 28 px.
  - Barra lateral: 24 16 20 px.
  - Rótulos con interlineado de 1.2 y enlaces de 1.5.
  - `Selector` con la flecha y las medidas del selector de N.1.
  - Botón de salir con borde `--principal` y texto `--tinta` con el cursor encima.
  - Se comprobó en Chrome a 1440×900, contra el mockup.
- **Tokens (H-98).** Las barras llevan la clase `dark` y usan tokens. De N.1, A6 y B3 se agregaron `--borde-barra`, `--tinta-rotulo`, `--tinta-navegacion` y `--fondo-activo` a 11 §1 (decisión de Jordin del paso 12). Las piezas comunes están en `layouts/Navegacion.tsx`.
- **Altura y HMR (H-99).**
  - La estructura usa `h-screen`, así que solo se desplaza el contenido y el pie de la barra lateral queda fijo.
  - Se quitó `hmr.clientPort: 443` de Vite en el panel y en el portal. El HMR se conectó con `vite` directo (5173) y a través de Caddy (`wss://shapi.localhost`).
- **Sesión y selector (H-100).**
  - `useCerrarSesion` usa el `clienteSesion` de `useSesion`.
  - La consulta de la sesión tiene `staleTime` de 5 minutos (decisión de Jordin).
  - `SelectorApi` usa `Selector`, que admite opciones deshabilitadas.
- **`/_ui` solo en desarrollo (H-101).** `crearRutas({ desarrollo })` agrega `/_ui` solo con `import.meta.env.DEV`, y la lámina no entra al build de producción (`laminaDiferida.ts`).
- **Pruebas (H-102).** `tests/Estructura.test.tsx` cubre:
  - el orden completo de N.1, A6 y B3;
  - el 501 como "Sin APIs";
  - A0.1 sin encabezado;
  - un error que no es 404;
  - las rutas índice, el 403 entre áreas y el 404 con layout;
  - las medidas, los tokens, la altura, el HMR, el `staleTime` y `/_ui` en producción.
- Se precisaron `11-interfaz.md` §1 (tokens de las barras), §3 (A0.2) y la estructura de navegación.

**Auditoría final (paso 17):**
- **H-150:** el contrato mínimo `contratos/openapi/apis.yaml` lo completa DC-04: `security`, 401 y `required`.
- Los textos de las páginas de relleno («A3-4», nombre del archivo) se aceptan como están, porque son temporales.
