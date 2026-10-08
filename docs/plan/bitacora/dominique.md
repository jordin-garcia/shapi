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

## 2026-10-01 · DC-05 · Especificación OpenAPI y rutas expuestas (A3.3 y A3.4)
- Hecho: carga y validación de OpenAPI 3.0/3.1 en JSON o YAML; reconciliación transaccional de rutas; listado y exposición por lote con bitácora y publicación de caché; contratos y tipos generados; pantallas A3.3/A3.4; pruebas de integración y Vitest.
- Decisiones: `ruta.definicion` guarda JSON estable con orden, parámetros, cuerpo y respuestas, con referencias locales resueltas; una recarga conserva identificador, exposición, límite, caché y peso por método/patrón, y consolida las métricas históricas como consumo sin ruta antes de retirar rutas ausentes.
- Pendiente o aviso para otros:
  - **DC-06:** `GET /api/apis/{id}/rutas` ya entrega las rutas configurables y los cambios sobre una API publicada llaman a `IPublicadorCache.PublicarApi` después de persistir.
  - **DC-07 y DC-10:** `ruta.definicion` contiene `orden`, `parametros`, `cuerpo` y `respuestas`; las referencias locales quedan incorporadas en el JSON guardado.

## 2026-10-02 · DC-08 · Avance de las pantallas de acceso del consumidor
- Hecho: se implementaron las seis pantallas, los flujos de registro, invitación, acceso, verificación y recuperación, y ocho pruebas Vitest + MSW enfocadas en los criterios.
- Decisiones: las rutas de acceso usan un encabezado de marca sin navegación ni pie; `/verificar-correo` es la ruta canónica de 10 §1 y 11 §3; A5.10 no muestra el correo, según 11 §Precisiones.
- Pendiente o aviso para otros:
  - **EM-18:** `GET /api/portal/auth/sesion` necesita devolver `destino` para que DC-08 pueda distinguir `/cuenta/suscripcion` de `/planes`. Se creó EM-18 con el contrato y las dos pruebas de integración requeridas.
  - **Dominique:** no marcar DC-08 como hecha hasta integrar EM-18 y completar la comprobación manual con el entorno levantado.

## 2026-10-05 · DC-06 · Configuración por ruta y publicación
- Hecho: configuración transaccional de límite, caché y peso por ruta; publicación y despublicación con requisitos, bitácora y Redis; contrato y tipos; A3.5 y acciones funcionales en A3.1; pruebas dirigidas de integración, Redis y Vitest.
- Decisiones: `detalle.faltan` usa `ruta_expuesta` y `plan_activo`; la caché de rutas no GET se envía como cero; `publicada_en` conserva la fecha de la publicación más reciente al despublicar.
- Pendiente o aviso para otros:
  - **DC-14:** DC-06 publica en Redis después del `commit`; reutilice el mismo orden al regenerar el secreto y verificar o quitar el dominio propio.

## 2026-10-06 · DC-07 · Portal público: inicio y documentación (A5.0, A5.1 y A5.5)
- Hecho: endpoint público de documentación por host; contrato y tipos generados; inicio y detalle de documentación alimentados por la especificación; Markdown sanitizado; marcas de Envíos Xelajú y Agro Precios cubiertas por pruebas.
- Decisiones: las rutas respetan el orden guardado en su definición y se identifican por método y patrón; la URL usa un dominio propio verificado cuando existe y, en otro caso, el host canónico; los ejemplos se obtienen del cuerpo y de la primera respuesta 2xx documentada.
- Pendiente o aviso para otros:
  - **DC-09:** A5.0 deja reservada la sección de planes para su implementación.
  - **DC-10:** A5.1/A5.5 enlazan “Probar en la consola” a `/consola`; el endpoint y los tipos de documentación ya están disponibles.
  - **DC-12:** el portal aplica la marca devuelta por la configuración de forma dinámica.

## 2026-10-07 · DC-09 · Planes y contratación en el portal (A5.4, A5.6 y A5.4b)
- Hecho: se implementaron la lista de planes y su estado vacío, el formulario de pago, el alta directa de planes gratuitos y la confirmación con claves de producción y pruebas de exposición única. A5.0 reutiliza la misma sección de planes.
- Decisiones: los planes se filtran por `activo`; la sesión solo se consulta al intentar contratar desde la página pública; las claves completas viven únicamente en el estado de la confirmación y desaparecen al salir de ella.
- Verificación: lint sin errores (11 avisos existentes de Fast Refresh), typecheck de api/ui/portal/panel, builds de portal/panel y 334 pruebas Vitest aprobadas. El entorno Docker se levantó, pero no incluye API de control para completar el flujo manual.
- Pendiente o aviso para otros:
  - **DC-11:** la pantalla de cuenta deberá consultar la suscripción persistida; A5.4b no guarda ni vuelve a solicitar las claves completas.
  - **DC-10:** `/documentacion` es el destino de salida de la confirmación y conserva el enlace ya implementado.
  - **EM-08:** si se amplía `Contratacion` para devolver la marca y los últimos cuatro dígitos del medio de pago, A5.4b podrá completar esa fila del mockup; por ahora el contrato no los incluye.
- Corrección de revisión: el periodo activo ahora muestra el fin inclusivo y los límites coinciden con el mockup; se documentó en `11-interfaz.md` que el contrato vigente no permite mostrar el medio de pago sin inventar datos.
