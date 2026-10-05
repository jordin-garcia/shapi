---
id: DC-03
titulo: Estructura del portal de marca blanca
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: hecha
programada: 2026-09-28
depende_de: [DC-02, EM-01]
requisitos: [RF-15, RF-16]
pantallas: []
---

# DC-03 · Estructura del portal de marca blanca

**Responsable:** Dominique Contreras · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** DC-02, EM-01

## Objetivo
Crear la resolución del portal por host (backend) y la estructura de la aplicación del portal: marca dinámica, layout, navegación y todas las rutas de A5 y B2 apuntando a páginas de relleno.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §4 (hosts)
- `docs/specs/11-interfaz.md` §1 (el portal usa `--marca-principal`) y §3 (rutas de A5 y B2)
- `docs/specs/04-roles-y-permisos.md` §3.3
- Mockups: `mockups/A5/Main.dc.html`, `mockups/A5/InicioAgro.dc.html` (otra marca) y `mockups/B2/Suscripcion.dc.html` (barra de la cuenta)

## Archivos que creas o modificas
- `src/Shapi.{Aplicacion,Infraestructura,Api}/Portal/**` (crear: `IResolutorPortal` y `GET /api/portal/configuracion`, `GET /api/portal/logo`)
- `contratos/openapi/portal.yaml` (crear)
- `frontend/apps/portal/src/rutas.tsx`, `layouts/**` y `paginas/<ID>-*.tsx` de relleno (crear)
- `frontend/apps/portal/vite.config.ts` (`server.allowedHosts: ['.shapi.localhost']`, puerto 5174)
- `tests/*/Portal/**`

## Criterios de aceptación
1. `IResolutorPortal` convierte el host `{sub}.{dominio_base}` en la API **publicada** y su organización. Si la API no existe o está despublicada, devuelve un resultado vacío (404 en los endpoints). Queda disponible para EM-05 y los demás endpoints `/api/portal/*`.
2. `GET /api/portal/configuracion` devuelve el nombre del portal (o el de la API), el color, la URL del logo, la bienvenida, el nombre y la descripción de la API (de la especificación) y los hosts del portal y de la API. `GET /api/portal/logo` sirve el logo con su `Content-Type` y, si es SVG, con `Content-Security-Policy: sandbox` y `X-Content-Type-Options: nosniff`.
3. El portal lee la configuración al cargar y aplica `--marca-principal`. El encabezado muestra el logo o las iniciales y el nombre. No aparece la marca de Shapi.
4. Todas las rutas de A5 y B2 de 11 §3 existen con su página de relleno. Las de `/cuenta/*` exigen la sesión de consumidor (hook provisional que llama a `GET /api/portal/auth/sesion`).
5. Si la API no existe o está despublicada, se muestra la página "API no disponible".

## Pruebas obligatorias
- Integración de `IResolutorPortal` y de la configuración (API publicada, despublicada e inexistente)
- Vitest: marca aplicada y rutas

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Pantallas del portal (DC-07 a DC-11)

## Resultado
- Se creó `IResolutorPortal` y su implementación con EF Core para resolver únicamente APIs publicadas por el host `{sub}.{dominio_base}`, antes de contar con un contexto de organización.
- Se publicaron los endpoints anónimos `GET /api/portal/configuracion` y `GET /api/portal/logo`, con contrato OpenAPI, hosts canónicos y aislamiento de SVG mediante CSP.
- El frontend carga la configuración, aplica `--marca-principal`, muestra logotipo o iniciales sin la marca de Shapi y presenta "API no disponible" ante un 404.
- Se crearon los layouts público y de cuenta, todas las rutas con URL de A5 y B2 y sus páginas de relleno. Las rutas de `/cuenta/*` usan una consulta provisional a `GET /api/portal/auth/sesion` y el pie permite cerrar con `POST /api/portal/auth/salir`.
- Las pruebas de integración cubren API publicada, despublicada e inexistente, configuración y logotipos PNG/SVG. Las pruebas de Vitest cubren marca, rutas, navegación y sesión del consumidor.
- Decisiones: los hosts devueltos se construyen con el subdominio almacenado y `SHAPI_DOMINIO_BASE`, nunca con la cabecera recibida. A5.4b y B2.4 a B2.6 no crean rutas porque el catálogo las define como estados o diálogos sin URL propia.

### Correcciones de la auditoría (2026-10-04)

Paso 9 de `docs/plan/auditoria-2026-10-03.md`, en el mismo PR que DC-08 (`[DC-03] Correcciones de la auditoría: portal de marca blanca`).
- **H-73:** `GET /api/portal/configuracion` devuelve `nombreOrganizacion` (contrato `portal.yaml`). El pie de las páginas públicas muestra la insignia de 24 px (radio de 6 px, como en `A5/Main.dc.html`) con sus iniciales y el nombre.
- **H-74:** los colores de la marca (`--principal`, `--principal-hover` y `--anillo-foco`) se definen en la raíz de `App`, con `coloresMarca`, y ya no solo en `MarcoAcceso`.
- **H-75:** el enlace activo de la barra de la cuenta usa el fondo teñido con la marca y el texto en la marca, y los demás enlaces van en `#2B3547`, como el mockup B2.
- **H-76:** cerrar sesión cancela y borra de la caché las consultas del consumidor (solo queda la configuración) y, si falla, muestra «No se pudo cerrar la sesión.» con «Reintentar».
- **H-110 (auditoría final, paso 13):** H-76 había quedado incompleto. Si la sesión vencía, se iba a `/entrar` sin limpiar la caché, y otro consumidor del mismo navegador podía ver los datos del anterior. Ahora `limpiarCacheConsumidor` (`modulos/sesion/cacheConsumidor.ts`) se llama:
  - al cerrar sesión;
  - en `RequiereSesionConsumidor`, cuando la sesión resulta nula;
  - en `useIrAlDestinoConsumidor`, antes de ir al destino (entrar, registrarse, aceptar una invitación o restablecer la contraseña).

  Hay dos pruebas de Vitest, una para cada camino nuevo.
- **H-77:** los dos grupos de rutas tienen `errorElement` (`Error-Ruta.tsx`), en español. La prueba monta una página que falla y comprueba «Reintentar».
- **H-78:** `/cuenta` redirige a `/cuenta/suscripcion`.
- **H-79:** pruebas de la configuración con 5xx, de la sesión con 5xx en `/cuenta/*` y de la ruta comodín.
- **Decidido (3 oct):** (a). Los enlaces de «Documentación» llevan a `/documentacion`, que existe y por ahora muestra A5.1. DC-07 la redirigirá a la primera ruta expuesta (aviso en la bitácora).
