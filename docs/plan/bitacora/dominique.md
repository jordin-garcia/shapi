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

## 2026-09-25 · DC-02 · Estructura del panel y del sitio público
- Hecho:
  - Layouts para el sitio público, panel de proveedor y administrador (`LayoutPublico`, `LayoutPanel`, `LayoutAdmin`).
  - Barras laterales según los *mockups* N.1 y A6.
  - Generación de un componente *lazy placeholder* por cada pantalla del catálogo de 11 §3.
  - Guardias de acceso `RequiereSesion` y `RequiereRol` con redirección a `/entrar` y pantalla de error 403.
  - Router (`rutas.tsx`) con **todas** las rutas del catálogo apuntando a las páginas de relleno.
  - Tipos generados en `@shapi/api` para `GET /api/apis` y un contrato provisional de sesión encapsulado en el módulo del panel.
  - Pruebas en Vitest para Layouts y Guardias del Router.
- Decisiones:
  - El selector de API y `useSesion` llaman a los endpoints usando `openapi-fetch`.
  - Las pantallas de error general (`Error-403.tsx` y `Error-404.tsx`) se manejan como páginas.
- Pendiente o aviso para otros:
  - **EM-02:** falta publicar `contratos/openapi/identidad.yaml`; el módulo provisional consume la forma actual de tu endpoint `GET /api/auth/sesion`.
  - **Todos:** ya pueden implementar sus pantallas modificando el archivo generado de su componente en `frontend/apps/panel/src/paginas/`. ¡No toquen `rutas.tsx`!

## 2026-09-25 · DC-02 · Correcciones de revisión
- Hecho: URLs sin prefijo duplicado; selección y enlaces de API; permisos por ruta y menú; cierre real de sesión desde ambos paneles; estados reutilizables; encabezados y tipografía; generación de contratos corregida. Lint, tipos, 99 pruebas y compilaciones aprobados con Node 24.21.0. Comparación visual en Chromium y revisión independiente sin hallazgos.
- Decisiones: 401 de sesión redirige a entrar; fallos de red/servidor muestran Reintentar. El selector considera 404/501 una lista vacía mientras DC-04 no exista. Sin selección no hay enlaces a una API ficticia. Se conservaron las páginas de relleno. La respuesta anidada de sesión de EM-02 se adapta dentro de `useSesion` para mantener simples los layouts.
- Pendiente o aviso para otros:
  - **EM-02:** el frontend consume la forma real de `GET /api/auth/sesion` (`usuario`, `organizacion`, `rol`, `correoVerificado`, `destino`) y `POST /api/auth/salir` con respuesta 200. El botón de salida limpia la caché solo después de revocar la sesión o recibir 401.
  - **EM-17:** se creó esta tarea para publicar el contrato OpenAPI de Identidad y reemplazar el tipo provisional sin modificar archivos propiedad de Emilio desde DC-02.
  - **DC-04:** el selector consume `GET /api/apis`, admite endpoint pendiente (404/501) y distingue errores recuperables de servidor/red.

## 2026-09-28 · DC-03 · Estructura del portal de marca blanca
- Hecho: resolución de APIs publicadas por host; endpoints públicos de configuración y logo; contrato y tipos generados; marca dinámica; layouts público y de cuenta; rutas de A5/B2 con páginas de relleno; guardia provisional de sesión; pruebas backend y frontend.
- Decisiones: los hosts canónicos salen del subdominio persistido y `SHAPI_DOMINIO_BASE`; el nombre de la API respalda al nombre opcional del portal; los estados y diálogos sin URL del catálogo no crean rutas propias.
- Pendiente o aviso para otros:
  - **EM-05:** `IResolutorPortal.Resolver(host)` devuelve `ApiId`, `OrganizacionId`, `NombreOrganizacion`, `Subdominio`, marca y hosts canónicos. El módulo provisional espera que `GET /api/portal/auth/sesion` responda `{ consumidor: { nombre, nombreEmpresa }, correoVerificado }` y usa `POST /api/portal/auth/salir`; reemplázalo por los tipos generados al publicar `identidad.yaml`.
  - **Emilio:** se agregó `/api/portal/configuracion` y `/api/portal/logo` a la lista de endpoints anónimos en `tests/Shapi.Api.Tests/Identidad/AutenticacionTests.cs`; actualiza cualquier rama abierta desde `main`.
  - **DC-07 a DC-11 y JG-12:** ya están disponibles la configuración de marca, los layouts y las rutas del portal para reemplazar cada página de relleno.

## 2026-09-29 · DC-04 · Registrar una API y lista de APIs (A3.1 y A3.2)
- Hecho: registro y listado de APIs por organización; validación contra SSRF y prueba de conexión sin redirecciones; secreto de origen cifrado y mostrado una sola vez; contrato OpenAPI y tipos TypeScript; pantallas A3.1/A3.2 y pruebas unitarias, de integración y Vitest.
- Decisiones: la conexión usa las direcciones ya validadas para impedir reenlaces DNS; API y entrada `api.registrada` se guardan en la misma transacción; un borrador no se publica en Redis. Se modificó el archivo enrutado existente `A3-2-Registro.tsx`, cuyo nombre difiere del indicado por la tarea.
- Pendiente o aviso para otros:
  - **DC-05:** el registro dirige a `/panel/apis/:id/especificacion`; ya están disponibles el identificador y el secreto de origen cifrado.
  - **DC-06:** la lista muestra la acción visual Publicar/Despublicar, pero su comportamiento sigue fuera de DC-04.
  - **EM-13:** `GET /api/apis` ya devuelve `total`, `planNombre` y `maxApis`; el registro todavía no aplica el límite del plan.
  - **JG-05:** `ValidadorDireccionOrigen` devuelve la URI y direcciones resueltas válidas para reutilizar la misma política SSRF sin volver a resolver DNS.
