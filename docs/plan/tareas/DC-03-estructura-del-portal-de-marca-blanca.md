---
id: DC-03
titulo: Estructura del portal de marca blanca
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: pendiente
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
