# Bitácora de Jordin García

Cada tarea terminada agrega una entrada **al final** de este archivo (protocolo, paso B10):

```
## AAAA-MM-DD · <ID> · <título>
- Hecho: ...
- Decisiones: ...
- Pendiente o aviso para otros: ...
```

---

## 2026-09-23 · JG-01 · Andamiaje del backend, CI y reglas del repositorio
- Hecho:
  - Solución .NET 10 con 7 proyectos y 3 de pruebas, y paquetes centralizados.
  - `Program.cs` con los 15 módulos.
  - Tipos comunes (`Resultado`, `Error`, `IReloj`, `IColaCorreo`, `IBitacora`, `IPublicadorCache`, `IContextoOrganizacion`, `AccionesBitacora`) e implementaciones nulas.
  - `CodigosError` y `LlavesRedis`.
  - CI con `plan`, `backend` y `frontend`.
  - 98 pruebas.
- Decisiones:
  - `Microsoft.OpenApi` 2.12.2, por compatibilidad con `Microsoft.AspNetCore.OpenApi` 10.
  - `Microsoft.Extensions.Hosting` para el trabajador.
  - La normalización de las llaves de Redis quedó escrita en 07 §4.
- Pendiente o aviso para otros:
  - **Todos:** registren su módulo solo en `src/Shapi.Api/Modulos/<X>Modulo.cs`, nunca en `Program.cs`. Los paquetes del stack ya están referenciados en los `.csproj`. Los códigos de error se toman de `Shapi.Contratos.CodigosError` y las acciones de la bitácora de `AccionesBitacora`.
  - **EM-01:** registrar `ColaCorreoBaseDatos` y `BitacoraBaseDatos` en su módulo reemplaza a las nulas.
  - **JG-04:** reemplaza a `PublicadorCacheNulo`.

## 2026-09-23 · JG-02 · Compuerta mínima: host → API, clave y reenvío
- Hecho:
  - Tubería de filtros (`IFiltroCompuerta`, `TuberiaCompuerta.Orden`), `FiltroApi`, `FiltroClave` y reenvío con YARP.
  - Errores 404 y 401 en JSON (08 §4).
  - `ContextoApi` y `ContextoClave` en `Shapi.Contratos/Redis`.
  - Comando temporal `sembrar-demo`.
  - 33 pruebas nuevas, con Testcontainers y un origen en memoria.
- Decisiones:
  - El origen recibe el `Host` de `url_origen` (precisado en 08 §5).
  - `/salud` solo responde con el host `localhost` (08 §1).
  - Redis se configura con `SHAPI_REDIS`.
- Pendiente o aviso para otros:
  - **JZ-01:** en `.env.example`, la variable de Redis de la compuerta es `SHAPI_REDIS=localhost:6379`.
  - **JZ-06:** el *healthcheck* de la compuerta debe llamar a `http://localhost:<puerto>/salud`, con el host `localhost`.
  - **JG-04 y JG-07:** escriban `api:{id}` y `clave:{hash}` con `ContextoApi.ACampos()` y `ContextoClave.ACampos()`, y calculen el hash con `ContextoClave.CalcularHash`.
  - **JG-05:** 502, 504 y 413 en JSON, `X-Forwarded-*`, `X-Shapi-Secreto`, quitar las demás `X-Shapi-*` y las cookies del portal, y leer el contexto en un solo *pipeline*.

## 2026-09-23 · JG-03 · Revisión automática con Claude y tablero del plan
- Hecho:
  - Workflow `revision-claude.yml`: revisión automática de cada PR y respuesta a `@claude`. Se paga con la suscripción de Jordin.
  - Workflow `tablero-plan.yml` y `scripts/tablero.mjs`: issue fijo "Tablero del plan", con avisos por persona.
  - Opción `node scripts/tareas.mjs --json`.
  - 11 pruebas con `node:test`, que la CI ejecuta en el *job* `plan`.
- Decisiones:
  - Pruebas de scripts con `node --test "scripts/*.test.mjs"`, porque Node 24 no recorre directorios.
  - Opus 5.5 con esfuerzo `high`.
  - `github_token` en lugar de la GitHub App.
  - El estado del tablero se guarda en el cuerpo del issue.
- Pendiente o aviso para otros:
  - **Todos:** el estado de todas las tareas está en el issue fijo "Tablero del plan", y cuando una tarea de ustedes queda disponible los menciona ahí. No editen ese issue: se reemplaza solo.
  - **Todos:** cada PR recibe un comentario "🤖 Revisión automática con Claude". No es obligatorio y no reemplaza la revisión local del protocolo (B9). Para que encuentre la tarea, el título del PR debe empezar con `[<ID>]`. Usen `@claude` en comentarios con moderación: consume la cuota del plan de Jordin.

## 2026-09-25 · DC-01 · Restaurar la ruta /_ui de la lámina de estilo
- Hecho: la lámina de DC-01 vuelve a abrirse en `/_ui`. DC-02 había reemplazado `main.tsx` por el router sin registrar esa ruta. Se agregó la ruta, una prueba por el router y la ruta de A0.2 en el catálogo de `11-interfaz.md`.
- Decisiones:
  - La corrección la hizo Jordin porque Dominique no está trabajando en el proyecto por ahora.
  - En el catálogo se escribe `shapi.localhost/_ui`, como A0.1, para que la prueba del catálogo de DC-02 no exija el texto "A0-2 ·" en la lámina.
- Pendiente o aviso para otros:
  - **Dominique:** se tocaron `apps/panel/src/rutas.tsx` y `paginasDiferidas.tsx` (una línea en cada uno) para registrar `/_ui`. Actualiza tu rama desde `main` antes de seguir con DC-03.

## 2026-09-25 · EM-02 · Correcciones del registro e inicio de sesión
- Hecho: se corrigieron los 13 hallazgos obligatorios de la revisión en contexto limpio de EM-02 (PR #10), que se había integrado sin revisión completa. El más grave: la membresía se buscaba con el filtro global activo, así que toda sesión quedaba con el rol `sin_rol` y sin organización, y ninguna política autorizaba a nadie. Además: el límite respondía 503 en vez de 429, faltaban `cuenta_bloqueada`, los errores por campo, el ciclo de 09 §4 y ProblemDetails; un 409 filtraba el detalle de PostgreSQL; había una prueba vacía (`Assert.True(true)`), y las 24 entidades del dominio habían quedado con setters públicos. Pruebas del backend: de 159 a 324.
- Decisiones:
  - La corrección la hizo Jordin a pedido del coordinador, para la demostración del Avance 1.
  - Se revirtieron las entidades ajenas a su versión anterior, y en las de Emilio se agregaron constructores y métodos de dominio.
  - Ver las decisiones en el Resultado de EM-02: 423/403/429 y sin límite en `sesion` y `salir`.
- Pendiente o aviso para otros:
  - **Emilio:** revisa el Resultado de EM-02. Las entidades vuelven a tener `private set`: crea objetos con sus constructores o métodos de fábrica (`new Usuario(...)`, `Token.VerificacionCorreo(...)`, `Sesion.IniciarPersonal(...)`, `SuscripcionPlataforma.IniciarPrueba(...)`) y agrega métodos de dominio en lugar de setters públicos. Los errores se devuelven con `Problemas.Crear(estado, codigo, titulo, errores?)`.
  - **EM-03 y EM-17:** el contrato real está en `contratos/openapi/identidad.yaml`. Los errores traen `codigo` y, en el 400, `errores` por campo (`nombre`, `correo`, `organizacion`, `contrasena`). El bloqueo es 423 y la cuenta desactivada es 403 `cuenta_desactivada`.
  - **JZ-03 y EM-03:** los datos de `verificacion_correo` son `{ nombre, token }`. La especificación no fija el formato del enlace. Propuesta: `https://shapi.localhost/verificar-correo?token=<token>` (la ruta de A1.2), y que la pantalla envíe el token a `POST /api/auth/verificar-correo`. Acuérdenlo entre las dos tareas y dejen el formato en 10 §1.
  - **JZ-06:** la API confía en `X-Forwarded-For` si la conexión viene de la máquina o de una red privada (10 §1). En el ambiente productivo simulado no publiquen el puerto de la API: que solo el borde llegue a ella.
  - **Todos:** para exigir un permiso, usen `RequireAuthorization(Permisos.X)`. Si prueban endpoints con sesión, usen `tests/Shapi.Api.Tests/Identidad/AutenticacionTests.cs` como ejemplo (un contenedor por clase y una base por prueba).

## 2026-09-25 · JG-01 · Validar el título de los PR en la CI
- Hecho: el job `plan` rechaza los PR cuyo título no sea `[<ID>] <título>` con una tarea existente (`node scripts/tareas.mjs --validar-titulo`). La CI se vuelve a ejecutar al editar el PR, y la revisión con Claude también, pero solo si cambió el título. Hay 4 pruebas nuevas en `scripts/tareas.test.mjs`.
- Decisiones:
  - La validación va dentro del job `plan`, que ya es obligatorio, para no cambiar la protección de `main`.
  - Al editar la descripción del PR también se vuelve a ejecutar la CI completa. Es el costo de no tener un job obligatorio aparte.
- Pendiente o aviso para otros:
  - **Todos:** el título del PR debe empezar con `[<ID>]`, por ejemplo `[EM-03] Pantallas de registro, verificación y acceso`, o la CI falla en el job `plan`. Si se equivocan, corríjanlo con `gh pr edit --title "..."` y la CI corre sola. Las correcciones de una tarea ya hecha usan el ID de esa tarea.


## 2026-09-25 · JZ-03 · Enlaces del portal y reintentos de correo
- Hecho: corrección posterior de JZ-03 tras revisarla. El motor de plantillas arma los enlaces de verificación y recuperación con `hostPortal` cuando viene en los datos (correos de consumidores), y con el dominio base si no viene (personal). `hostPortal` solo se acepta como `{sub}.{dominio_base}` (una etiqueta ASCII), para que el token no pueda terminar en otro dominio. Los reintentos ahora son 5, con las esperas de 5 s, 30 s, 2 min, 10 min y 1 h, y el correo queda `fallido` al fallar el sexto intento. Pruebas nuevas en `MotorPlantillasCorreoTests` y `CorreoSalienteTests`.
- Decisiones:
  - RF-46 dice "se reintentan hasta 5 veces" y el caso de uso lista 5 esperas; el criterio de JZ-03 ("al quinto fallo, `fallido`") dejaba sin usar la espera de 1 h. Mandó la especificación y se corrigió el criterio de la tarea.
  - El host del enlace lo decide quien encola el correo (`hostPortal`), porque es quien conoce el portal que atendió la petición (`IResolutorPortal`). Quedó en 10 §6 y en el criterio 1 de EM-05.
  - No se agregó bloqueo de filas (`FOR UPDATE SKIP LOCKED`) en la bandeja de salida: el trabajador corre en una sola instancia (06 §8, `salud:trabajador`).
- Pendiente o aviso para otros:
  - **EM-05:** al encolar `verificacion_correo` y `recuperacion` para un consumidor, incluyan `nombrePortal` y `hostPortal` = `{sub}.{dominio_base}` en los datos, armado con el subdominio de la API que resolvió `IResolutorPortal`; no copien la cabecera `Host` ni usen el dominio propio (apunta a la compuerta). Sin `hostPortal` el enlace lleva al panel del personal y el token del consumidor no funciona.
  - **JZ-11:** 10 §6 dice que ningún correo lleva la marca de Shapi en el cuerpo, pero el criterio 2 de JZ-11 dice que los del personal usan la marca de Shapi. Resuélvanlo antes de implementar (§C).
  - **José Pablo:** cambié el módulo `Correo` de JZ-03: los correos que quedan `fallido` ahora tienen `intentos = 6` y `proximo_intento_en` vacío, y el motor de plantillas acepta `hostPortal`. Tenlo en cuenta en JZ-11 y JZ-12 (el estado del correo en B3.1).

## 2026-09-26 · JG-01 · Autorización del coordinador y plan de correcciones de la auditoría
- Hecho: se auditaron las 10 tareas integradas contra su archivo de tarea y las especificaciones. Todo compila y pasa las pruebas, pero hay incumplimientos. El más grave está en EM-01: la secuencia `caso.numero` empieza en 1 y no en 100, y faltan las pruebas de las restricciones. El plan de correcciones está en `docs/plan/auditoria-2026-09-25.md` (114 hallazgos, 17 pasos). El protocolo tiene una sección nueva, §E: el coordinador queda autorizado de forma permanente a corregir directamente el trabajo de cualquier persona.
- Decisiones:
  - Cada corrección se integra con el ID de la tarea original: `[<ID>] Correcciones de la auditoría: <tema>`.
  - Los correos del personal llevan la marca de Shapi y los de los consumidores solo la del portal. Se corrige 10 §6 en el paso 4 del plan.
  - La revisión con Claude pasará a ser bloqueante (paso 3 del plan).
- Pendiente o aviso para otros:
  - **Todos:** desde ahora, Jordin audita cada tarea que se integra en `main` y puede corregir directamente su código, sus pruebas, sus contratos y su documentación, e incluso terminar sus PR abiertos (`protocolo.md` §E). Cuando lo haga, les dejará aquí un aviso en negrita con los archivos que cambió. Actualicen su rama desde `main` antes de seguir trabajando.
  - **Emilio:** Jordin va a terminar EM-03 (#16) y EM-06 (#14), y a corregir EM-01, EM-02 y EM-17, según el plan de la auditoría. Antes de continuar cada PR tuyo, se coordinará contigo; tus ramas no se modifican (se sigue en una rama nueva, protocolo §E4).
  - **José Pablo:** se corregirán JZ-01, JZ-02 y JZ-03 (pasos 7 a 9 del plan).
  - **Dominique:** se corregirán DC-01 y DC-02 (pasos 12 y 13 del plan).

## 2026-09-26 · EM-03 · Pantallas de registro, verificación y acceso (A1.1 a A1.3)
- Hecho: se terminó EM-03 a partir del PR #16 de Emilio (protocolo §E4, paso 2 de `docs/plan/auditoria-2026-09-25.md`). Las tres pantallas coinciden con los mockups y se probaron con el entorno levantado: registro, correo en Mailpit, enlace, panel y entrar. Hay 24 pruebas nuevas con MSW (RF-01, RF-02 y RF-04). Se corrigieron la verificación repetida del `useEffect`, los tipos escritos a mano del contrato, el reenvío sin correo y las etiquetas sin asociar. También se corrigió el `@source` de Tailwind de `packages/ui` (H-115), sin el cual ningún componente base tenía tamaño ni color en el navegador.
- Decisiones:
  - Manda 10 §1: el enlace es `/verificar-correo?token=`.
  - Los estados de A1 sin mockup quedaron en 11 §4.
  - Los formularios muestran los mensajes de validación de la API debajo de cada campo (`noValidate`).
- Pendiente o aviso para otros:
  - **Emilio:** se terminó tu EM-03 en la rama `jordin/EM-03-pantallas-registro-acceso`, que parte de la tuya; tu rama no se modificó. El PR #16 queda cerrado. Se regeneró `packages/api/src/generado/identidad.ts` con `pnpm generar:api`: no lo escribas a mano. Para las pantallas de A1.4 (EM-04), usa `MarcoAcceso`, `Encabezado`, `CampoEtiquetado` y `AvisoError` de `modulos/identidad/Formularios.tsx`.
  - **Dominique:** se cambiaron `packages/ui/src/style.css` (`@source "./"`; sin eso, las clases que solo usan los componentes base no se generan en las apps), `packages/ui/src/style.test.tsx` y `packages/ui/src/vite-env.d.ts` (nuevos), `packages/api/package.json` (exporta `./identidad`), `apps/panel/src/layouts/LayoutPublico.tsx` (`main` flexible sin `p-8`, logotipo con `gap` de 11 px y `tracking-[-0.03em]`), `apps/panel/src/modulos/sesion/useSesion.ts` (expone `consultarSesion`), `apps/panel/src/tests/rutas.test.tsx` y `frontend/vitest.config.ts` (el proyecto `ui` procesa `style.css`). Actualiza tu rama desde `main`.
  - **EM-17:** `identidad.ts` ya está generado y exportado como `@shapi/api/identidad`. Falta reemplazar el contrato provisional `modulos/sesion/contratoSesion.ts`.
