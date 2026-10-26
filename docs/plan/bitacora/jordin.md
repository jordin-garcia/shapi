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
  - El host del enlace lo decide quien encola el correo (`hostPortal`), porque es quien conoce el portal que atendió la petición (`IResolutorPortal`). Quedó en 10 §1 y en el criterio 1 de EM-05.
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

## 2026-09-26 · JG-03 · La revisión con Claude pasa a ser obligatoria
- Hecho: `revision-claude` es una verificación obligatoria de `main` (paso 3 de `docs/plan/auditoria-2026-09-25.md`, H-09 a H-11). Un paso final lee el veredicto publicado para el commit (`scripts/veredicto-revision.mjs`, con pruebas y comprobado con los comentarios reales de #14, #16 y #22): pasa con `VEREDICTO: LISTO` y falla con `CORREGIR` o si no hay revisión. Un commit con una revisión completa no se revisa otra vez (ni con `rerun` ni reabriendo el PR) salvo que cambie la tarea del título. `@claude` pasó a `claude-interactivo.yml`, para que ningún check "omitido" apruebe un commit con hallazgos.
- Decisiones:
  - Si la revisión no se completa (cuota, caída o tiempo), el PR se bloquea hasta reintentarla.
  - Un falso positivo solo lo desbloquea Jordin, con `--admin`; por eso `enforce_admins` queda desactivado.
- Pendiente o aviso para otros:
  - **Todos:** desde este PR, un PR no se integra si la revisión automática con Claude tiene hallazgos de corrección. Esto reemplaza el aviso del 23 de septiembre que decía que no era obligatoria. Corrijan y hagan *push*: la revisión se repite sola. Si falló por algo externo, `gh run rerun <id> --failed`; volver a ejecutarla no repite una revisión que ya terminó. Si creen que es un falso positivo, explíquenlo en el PR y avísenle a Jordin, que es el único que puede integrarlo con `--admin`. La cuota sigue siendo la de Jordin: eviten *pushes* innecesarios.

## 2026-09-26 · JG-03 · Falso positivo del check de revisión
- Hecho: `revision-claude` bloqueó el #24 aunque su revisión decía `VEREDICTO: LISTO` sin hallazgos. `scripts/veredicto-revision.mjs` confundía el texto libre "Corrección de auditoría…" con la sección `CORRECCIÓN`, y además contaba el propio encabezado como hallazgo. Ahora los encabezados se buscan en MAYÚSCULAS y solo en su línea. Hay una prueba con la estructura real de esa revisión. Ningún PR se integró por este error: la barrera bloqueó de más, nunca de menos.
- Pendiente o aviso para otros:
  - **Todos:** si `revision-claude` falla y el comentario de la revisión dice `VEREDICTO: LISTO`, avísenle a Jordin: es un error del check, no de su PR.

## 2026-09-26 · JG-01 · Coherencia de las especificaciones y del plan
- Hecho: paso 4 de `docs/plan/auditoria-2026-09-25.md` (H-12 a H-29). Se corrigieron contradicciones y referencias entre las specs, el plan y los mockups, sin cambiar código. H-10 quedó aplicado: `revision-claude` ya es obligatorio en `main`.
- Decisiones (de Jordin, 26 sep):
  - ADR-35 pasa a Node 24 o posterior.
  - La salud es `/salud` en la API y en la compuerta (se quitó `/interno/salud` de 06 §4). El trabajador no tiene HTTP: su salud es el latido.
  - Los colores de Correcto y Alerta de los mockups (`#1F8A5B` y `#C2481F`) son correctos: 11 §1 los agrega como tono base y conserva el de etiqueta (`#146542` y `#8E3315`).
  - El contexto de la compuerta se lee en dos *pipelines* más el script Lua (08 §8).
  - Los correos del personal llevan la marca de Shapi y los de los consumidores la del portal.
- Pendiente o aviso para otros:
  - **JZ-06:** nuevos criterios 6 y 7: el *healthcheck* de la API y de la compuerta consulta `/salud` en `localhost` dentro del contenedor, el trabajador no lleva *healthcheck* y la API no publica su puerto. Ya no se comprueba `/api/salud` a través de Caddy.
  - **JZ-11:** se resolvió la contradicción de la marca: los correos del personal usan la marca de Shapi y los de los consumidores la del portal (10 §6).
  - **JG-05:** nuevos criterios 8 y 9: se quitan `shapi_sesion` y `portal_sesion` hacia el origen, y el contexto se lee en dos *pipelines*. Tiene sus pruebas obligatorias.
  - **EM-17:** `identidad.yaml`, `identidad.ts` y la exportación ya existen. Falta reemplazar el contrato provisional de la sesión.
  - **Dominique:** 11 §1 tiene ahora dos tonos por color de estado: el base, `#1F8A5B` y `#C2481F`, para iconos, muestras, códigos y texto en tablas, y el de etiqueta, `#146542` y `#8E3315`. Los tokens `--correcto-base` y `--alerta-base` se agregan en el paso 12 (H-86). También cambió el contexto de DC-02: el catálogo de 11 §3 no trae responsables.
  - **Emilio:** se agregó una nota en tu bitácora (entrada de EM-01): `Program.cs` sí migra en Development. En el criterio 1 de EM-05 se corrigió la cita de sección (10 §1).
  - **José Pablo:** se agregaron notas en tu bitácora y en el `## Resultado` de JZ-03 sobre el sexto intento, y se corrigió la cita del criterio 3 (10 §1). En `infra/verificar.mjs`, la comprobación de `/interno/*` ahora usa `/interno/tls/autorizar`, porque `/interno/salud` no existe.

## 2026-09-26 · EM-01 · Correcciones de la auditoría: esquema de la base de datos
- Hecho: paso 5 de `docs/plan/auditoria-2026-09-25.md` (H-30 a H-48). La migración nueva `AjustesDelEsquemaAuditoria` hace esto:
  - `caso.numero` empieza en 100;
  - agrega la FK de `plan_siguiente_id`;
  - renombra las columnas `rechazos_*` y `origen_*`;
  - agrega los CHECK que faltaban, los DEFAULT de los estados y las columnas `creado_en` y `actualizado_en`;
  - pasa a minúsculas los nombres de las restricciones (`ck_*`);
  - hace que los disparadores de la bitácora, ya en español, rechacen también TRUNCATE.

  El filtro por organización cubre ahora todas las tablas que pertenecen a una organización. `actualizado_en` se actualiza con `IReloj`. Los identificadores son UUID v7. Hay 50 casos de prueba en `tests/Shapi.Api.Tests/Persistencia/` y `tests/Shapi.Dominio.Tests/IdentificadoresTests.cs`. Se borraron los scripts de Python de la raíz, el paquete InMemory, `ColaCorreoNula` y `BitacoraNula`.
- Decisiones (de Jordin, 26 sep):
  - H-40: un solo rol de base de datos; la bitácora se protege solo con disparadores (07 §3.6).
  - H-38: el filtro cubre también las tablas que pertenecen a una organización a través de su padre (10 §2).
  - H-37: `creado_en` y `actualizado_en` según el uso real de cada tabla (07 §3).
  - H-43: se borran las implementaciones nulas de la cola de correo y de la bitácora.
- Pendiente o aviso para otros:
  - **Todos:** actualicen su rama desde `main`. La API aplica la migración nueva al arrancar en *Development*. Ahora el filtro por organización también esconde `usuario`, `ruta`, `dominio_propio`, `plan_api`, `suscripcion_api`, `clave`, `pago`, `medio_pago`, `consumo_diario`, `caso_mensaje` y `bitacora`, y sin organización en el contexto no devuelven nada. Donde todavía no se conoce la organización (registro, inicio de sesión, portal antes de resolver el host) o se trabaja entre organizaciones (administración, trabajador, compuerta), usen `IgnoreQueryFilters()` de forma explícita. `actualizado_en` lo llena un interceptor al guardar con EF; si usan `ExecuteUpdate` o SQL directo, pónganlo ustedes con la hora de `IReloj` (07 §3).
  - **Emilio:** se modificaron tu esquema y tus entidades:
    - `Persistencia/` (todas las configuraciones, `ShapiDbContext.cs`, el *snapshot*, la migración nueva e `InterceptorFechasAuditoria.cs`);
    - `Dominio/Identidad/{Usuario,Consumidor,Token,Sesion}.cs`, `Dominio/Organizaciones/{Organizacion,Membresia}.cs` y `Dominio/Suscripciones/SuscripcionPlataforma.cs`;
    - `Dominio/Planes/{PlanApi,PlanPlataforma}.cs` (`Activo` empieza en `true`);
    - `Api/Modulos/IdentidadModulo.cs` (al marcar el token usado también pone `ActualizadoEn`) y la prueba `RF_02_VerificarCorreo_MarcaElCorreoYElTokenEIniciaSesion` de `AutenticacionTests.cs`, que ahora comprueba `creado_en` y `actualizado_en`;
    - `tests/Shapi.Api.Tests/Persistencia/`, el `## Resultado` de EM-01 y tu bitácora (el nombre del disparador, la migración en *Development* y el formato de los avisos).

    Los constructores usan `Guid.CreateVersion7()`, y `PlanApi.Activo` y `PlanPlataforma.Activo` ya guardan `false`. Si necesitas cambiar el esquema en tu rama, borra tu migración, toma el *snapshot* de `main` y vuelve a generarla (convenciones §3).
  - **José Pablo:** se modificaron `Dominio/Bitacora/EntradaBitacora.cs` (su constructor recibe la fecha primero), `Infraestructura/Bitacora/BitacoraBaseDatos.cs` (usa `IReloj`) y `Dominio/Correo/CorreoSaliente.cs` (UUID v7, `creado_en` y `actualizado_en`). Para la siembra de JZ-05: los casos toman su número de la secuencia, así que CAS-100 a CAS-104 salen solos si se insertan en orden y sin `numero`. Además, la bitácora rechaza UPDATE, DELETE y TRUNCATE: para limpiarla en las pruebas, crea una base nueva.
  - **Dominique:** se modificó `Dominio/Apis/RegistroDnsSimulado.cs` (`creado_en` y `actualizado_en`). La columna `api.portal_logo` rechaza más de 512 KB y `ruta` rechaza la caché en métodos distintos de GET.

## 2026-09-26 · EM-02 · Correcciones de la auditoría: seguridad de la identidad del personal
- Hecho: paso 6 de `docs/plan/auditoria-2026-09-25.md` (H-49 a H-60).
  - Se deniega por defecto: todo endpoint exige una sesión, salvo los públicos de `/api/auth`, `/salud` y `/openapi` en *Development*.
  - Los intentos fallidos se cuentan de forma atómica, y la ruta de la cuenta inexistente hace el mismo trabajo en la base que la de la contraseña incorrecta.
  - `X-Forwarded-For` solo se acepta de `SHAPI_REDES_BORDE`.
  - Hay un límite de 3 reenvíos por hora.
  - El `Origin` se compara con su esquema y su puerto.
  - `salir` siempre borra la cookie.
  - El correo se valida con más cuidado y se hace el *rehash*.
  - El JSON mal formado responde 400 `datos_invalidos`.
  - Los endpoints se movieron a `Identidad/Endpoints.cs`.
  - Hay 26 casos de prueba nuevos en `AutenticacionTests`.
- Decisiones (de Jordin, 26 sep):
  - H-49: se editó `Program.cs` (`AllowAnonymous` en `/salud` y `/openapi`), como excepción puntual de convenciones §3.
  - H-52: `SHAPI_REDES_BORDE`. Por defecto incluye la máquina, desde donde llega Caddy con Docker Desktop (`127.0.0.1`, medido en esta PC), y la red `shapi`, que ahora tiene la subred fija `172.30.0.0/24` en `infra/compose.yml`, desde donde llega en Linux. Así funciona sin configurar nada en Windows, en Linux y en la CI. De ahí también se toma `X-Forwarded-Proto`, que el CSRF usa para comparar el `Origin`. Se probó de punta a punta por Caddy: el `Origin` del navegador pasa y uno ajeno recibe 403.
  - H-53: 3 reenvíos por hora.
  - H-55: `salir` es público.
- Pendiente o aviso para otros:
  - **Todos:** actualicen su rama desde `main`. Desde este PR, **un endpoint sin `RequireAuthorization(...)` ni `AllowAnonymous()` exige una sesión** (04 §4, regla 6). Si su endpoint debe ser público según 04 §4 (regla 6), como el portal sin sesión o `/interno/tls/autorizar`, declárenlo con `AllowAnonymous()` y agréguenlo a la lista de la prueba `Autorizacion_EndpointsAnonimos_SonSoloLosPublicos`.
  - **Todos:** la red `shapi` ahora tiene una subred fija. Una sola vez, recreen el entorno con `docker compose -f infra/compose.yml down` y `docker compose -f infra/compose.yml up -d` (los volúmenes se conservan). Si no, Compose avisa que la red no coincide. No hace falta configurar `SHAPI_REDES_BORDE`. Si `up` falla porque otra red de Docker ya usa `172.30.0.0/24`, avísenle a Jordin.
  - **Emilio:** se modificaron tus archivos de identidad:
    - `src/Shapi.Api/Identidad/` (el nuevo `Endpoints.cs`, `CsrfMiddleware.cs` y `PoliticasAutorizacion.cs`), `src/Shapi.Api/Modulos/IdentidadModulo.cs` (ahora solo registra servicios y el *pipeline*) y `src/Shapi.Aplicacion/Identidad/RegistroProveedor.cs` (`PatronCorreo`);
    - `tests/Shapi.Api.Tests/Identidad/AutenticacionTests.cs` y `contratos/openapi/identidad.yaml`;
    - el `## Resultado` de EM-02 y tu bitácora (se agregó la entrada de EM-02 que faltaba).

    Para EM-04 y EM-05, agrega tus endpoints en `Identidad/Endpoints.cs`, no en el módulo. Los intentos fallidos se cuentan con `ExecuteUpdate`; usa el mismo patrón para el consumidor.
  - **Dominique:** se regeneró `packages/api/src/generado/identidad.ts` con `pnpm generar:api`. `PeticionEntrar`, `PeticionReenviar` y `PeticionVerificacion` ya no tienen campos obligatorios. El *typecheck* y las pruebas del frontend pasan.
  - **José Pablo:** se modificó `infra/compose.yml`: la red `shapi` tiene la subred fija `172.30.0.0/24`, y `infra/verificar.mjs` sigue pasando. También se agregó `SHAPI_REDES_BORDE` a `.env.example`. El criterio 7 de JZ-06 explica que la API ya acepta esa subred, y que solo si `compose.prod.yml` usa otra red hay que pasarla en `SHAPI_REDES_BORDE`. Para JZ-07: en Linux, la CI no necesita configurar nada.

## 2026-09-26 · JZ-03 · Correcciones de la auditoría: envío de correos
- Hecho: paso 7 de `docs/plan/auditoria-2026-09-25.md` (H-61 a H-67).
  - Cada correo se toma en su propia transacción con `FOR UPDATE SKIP LOCKED`: dos trabajadores ya no envían el mismo correo.
  - El `token` se borra de `correo_saliente.datos` al quedar `enviado` o `fallido`.
  - Si el trabajador se detiene justo después de entregar un correo, este queda `enviado` y no se reenvía: el resultado se guarda con `CancellationToken.None`. MailKit 4.18 ya ignoraba los errores del `QUIT`; se agregó un `try/catch` explícito por si cambia, con su prueba de regresión.
  - SMTPS implícito en el puerto 465.
  - `hostPortal` rechaza los subdominios reservados.
  - Índice `(estado, proximo_intento_en)` en `correo_saliente`.
  - Hay 16 pruebas nuevas y 2 ampliadas en `tests/*/Correo/` y `tests/Shapi.Dominio.Tests/Apis/`, incluido un Mailpit con STARTTLS y autenticación.
- Decisiones (de Jordin, 26 sep):
  - H-61: una transacción por correo; la entrega es "al menos una vez".
  - H-64: no se usa `SecureSocketOptions.Auto`; con `SHAPI_SMTP_TLS=true` el cifrado es obligatorio (10 §6).
  - H-65: la lista de subdominios reservados vive en `Shapi.Dominio/Apis/SubdominiosReservados.cs`.
- Pendiente o aviso para otros:
  - **José Pablo:** se modificaron tus archivos de correo: `src/Shapi.Trabajador/Correo/ProcesadorCorreos.cs`, `src/Shapi.Infraestructura/Correo/EnviadorSmtp.cs` y `MotorPlantillasCorreo.cs`, `src/Shapi.Dominio/Correo/CorreoSaliente.cs`, `tests/Shapi.Api.Tests/Correo/` (nuevo `EnviadorSmtpTests.cs`) y `tests/Shapi.Dominio.Tests/Correo/CorreoSalienteTests.cs`, además del `## Resultado` de JZ-03 y un comentario de `SHAPI_SMTP_TLS` en `.env.example`. `EnviadorSmtp` tiene un constructor interno para las pruebas (`InternalsVisibleTo` en `Shapi.Infraestructura.csproj`). Para JZ-11, el procesador ya maneja cualquier plantilla: solo agrega las plantillas y sus asuntos. Actualiza tu rama desde `main`.
  - **Emilio:** se modificaron `Persistencia/Configuraciones/CorreoSalienteConfiguracion.cs` (índice) y el snapshot, y se agregó la migración `IndiceCorreoSalientePendientes`. Si tienes una migración en curso, actualiza tu rama desde `main` y vuelve a generarla (convenciones §3).
  - **Dominique:** se creó `src/Shapi.Dominio/Apis/SubdominiosReservados.cs`, con su prueba en `tests/Shapi.Dominio.Tests/Apis/`.
  - **DC-04:** para validar que el subdominio no está reservado (RF-08), usa `SubdominiosReservados.Contiene(subdominio)`; no crees otra lista.

## 2026-09-27 · JZ-01 · Correcciones de la auditoría: infraestructura local
- Hecho: paso 8 de `docs/plan/auditoria-2026-09-25.md` (H-68 a H-73).
  - Los comandos de Compose pasan `--env-file .env`.
  - El puerto de PostgreSQL se configura con `SHAPI_POSTGRES_PUERTO`.
  - Mailpit queda fijo en `v1.27`.
  - Todos los puertos se publican en `127.0.0.1`.
  - Los orígenes tienen *healthcheck*.
  - Hay `.dockerignore` en la raíz y `.shapi/` en el `.gitignore`.
  - `infra/verificar.mjs` comprueba todo lo anterior.
- Decisiones (de Jordin, 27 sep):
  - H-68: `--env-file .env` en los comandos, no `include`. También se actualizaron los comandos de JG-02 (Jordin). `infra/verificar.mjs` revisa que ningún documento de comandos (manual, instalación, `AGENTS.md`, calendario y tareas) omita `--env-file`.
  - H-71: también el 80 y el 443 de Caddy van en `127.0.0.1`.
  - H-72: `curl` en la imagen de los orígenes.
- Pendiente o aviso para otros:
  - **Todos:** desde ahora, el entorno se levanta con `docker compose --env-file .env -f infra/compose.yml up -d` (y lo mismo para `down` y `cp`). Sin `--env-file`, Compose ignora el `.env` de la raíz. Agreguen a su `.env` las variables nuevas de `.env.example`: `SHAPI_POSTGRES_PUERTO`, `SHAPI_SECRETO_ORIGEN_ENVIOS` y `SHAPI_SECRETO_ORIGEN_AGRO`. Si otro PostgreSQL ocupa el 5432, cambien `SHAPI_POSTGRES_PUERTO` y el `Port=` de `SHAPI_POSTGRES_CADENA`. Recreen el entorno una vez con `up -d --build` (los volúmenes se conservan). Los puertos solo responden en `127.0.0.1`.
  - **José Pablo:** se modificaron tus archivos:
    - `infra/compose.yml` e `infra/verificar.mjs`;
    - los `Dockerfile` de `origenes-demo/envios-xelaju` y `origenes-demo/agro-precios` (instalan `curl` para el *healthcheck*);
    - `origenes-demo/OrigenesDemo.Tests/InfraestructuraTests.cs` (JZ-02 CA5 ahora exige los puertos en `127.0.0.1`);
    - `.env.example`, `docs/manual-tecnico.md`, el `## Resultado` de JZ-01 y los comandos de las tareas JZ-01 y JZ-02, que ahora llevan `--env-file .env`.

    Además, se creó `.dockerignore` en la raíz. Actualiza tu rama desde `main`.
  - **JZ-06:** se precisó el criterio 1 y los comandos llevan `--env-file .env`. Como `compose.yml` publica en `127.0.0.1` los puertos de desarrollo, `compose.prod.yml` debe quitarlos con `ports: !reset []`, porque Compose suma las listas de puertos. El 80 y el 443 del borde siguen en `127.0.0.1`: `infra/verificar.mjs` lo comprueba. Para la API y la compuerta, el *healthcheck* puede seguir el patrón de los orígenes (`curl` en la imagen).

## 2026-09-27 · JZ-02 · Correcciones de la auditoría: orígenes de demostración
- Hecho: paso 9 de `docs/plan/auditoria-2026-09-25.md` (H-74 a H-77, más H-116 y H-117, encontrados en este paso).
  - Los OpenAPI de los orígenes quedan como A3.3 y A5.1: sin `/salud`, sin `X-Shapi-Secreto` ni sus 401, y con los textos del mockup.
  - "Secreto de origen inválido." lleva tilde.
  - `/precios` y `/historial` de Agro leen la misma tabla, y todo el catálogo tiene precio.
  - La cotización de Envíos se calcula con el peso.
  - Hay 29 casos de prueba nuevos, entre ellos uno que compara cada ejemplo del OpenAPI con la respuesta real.
- Decisiones (de Jordin, 27 sep):
  - H-74: también se quitan las 401 del secreto.
  - H-76: una fecha sin precio responde 404, una mal escrita 400, y la fecha sale como "8 sep 2026".
  - H-116: precios de demostración para los 3 productos en los 3 mercados.
  - H-117: tarifa = `precio_base + precio_por_kg × peso_kg`.
- Pendiente o aviso para otros:
  - **José Pablo:** se modificaron tus archivos:
    - `origenes-demo/envios-xelaju/Program.cs` y `cotizacion-envios.yaml`;
    - `origenes-demo/agro-precios/Program.cs` y `openapi.yaml`;
    - las pruebas `OpenApiTests`, `EnviosXelajuTests` y `AgroPreciosTests`;
    - el `## Resultado` de JZ-02.

    Actualiza tu rama desde `main`. **JZ-05:** si la siembra usa estos OpenAPI para las rutas, ahora son 5 en Envíos y 4 en Agro; ya no aparece `/salud`.
  - **Dominique:** en DC-05 se corrigió la ruta del archivo de ejemplo de Envíos (`origenes-demo/envios-xelaju/cotizacion-envios.yaml`). Al cargarlo deben salir las 5 rutas de A3.3. **DC-05, DC-07 y DC-10:** la documentación y la consola ya no mostrarán `X-Shapi-Secreto`, porque el OpenAPI no lo declara.

## 2026-09-27 · JG-03 · Correcciones de la auditoría: secciones extra en la revisión con Claude
- Hecho: H-118. En el #30, `revision-claude` falló aunque la revisión decía `CORRECCIÓN: Ninguno` y `VEREDICTO: LISTO`, porque la revisión agregó una sección "Comprobado:" antes de OPCIONAL. Ahora, si la sección de corrección empieza con "Ninguno", una sección posterior no cuenta como hallazgo; con cualquier otro comienzo todo sigue contando. `revision.md` pide poner lo comprobado antes de CORRECCIÓN.
- Decisiones (de Jordin, 27 sep): el #30 se integró con `--admin` y el arreglo va en este PR aparte.
- Pendiente o aviso para otros:
  - **Todos:** si `revision-claude` falla y la revisión dice "Ninguno" y LISTO, avísenle a Jordin (protocolo B11). No hagan *commits* vacíos para pedir otra revisión.

## 2026-09-27 · JG-02 · Correcciones de la auditoría: compuerta mínima
- Hecho: paso 10 de `docs/plan/auditoria-2026-09-25.md` (H-78 a H-82).
  - Una clave en la query string responde 401 `clave_en_url` y no se reenvía.
  - YARP ya no registra la URL de destino.
  - Si Redis no está disponible, la compuerta responde 503 `servicio_no_disponible` en JSON.
  - El tiempo con el origen es de 30 s en total, y la conexión de 10 s.
  - Hay 29 casos de prueba nuevos, y las pruebas llevan el nombre de su requisito.
  - Se probó con el entorno real, por Caddy: clave válida 200, inválida 401, en la URL 401 y con Redis detenido 503.
- Decisiones (de Jordin, 27 sep):
  - H-78: se detecta por el formato de clave, con 401 `clave_en_url`.
  - H-79: 503 `servicio_no_disponible` con `Retry-After: 5`.
  - H-80: conexión de 10 s.
- Pendiente o aviso para otros:
  - **JG-05:** `ReenvioOrigen` ya pone 504 cuando vence el tiempo total, y YARP pone 502 si no conecta; falta traducirlos a JSON (08 §4). Los tiempos están en `TiemposOrigen`. La tubería ya atrapa `RedisConnectionException` y `RedisTimeoutException` de los filtros y responde 503: los filtros nuevos no tienen que hacerlo.
  - **Todos:** `CodigosError` tiene dos códigos nuevos de la compuerta: `ClaveEnUrl` y `ServicioNoDisponible`.

## 2026-09-27 · EM-17 · Correcciones de la auditoría: contrato de identidad en el frontend
- Hecho: paso 11 de `docs/plan/auditoria-2026-09-25.md` (H-83 y H-84). EM-17 queda `hecha`.
  - `@shapi/api` exporta `"./*"`: cada contrato generado se importa como `@shapi/api/<modulo>` sin editar `package.json`.
  - Se borró el contrato provisional `contratoSesion.ts`. La sesión y el cierre de sesión usan los tipos generados de `@shapi/api/identidad`.
  - `identidad.yaml` ya coincidía con el backend de EM-02, así que no cambió.
  - Hay 7 casos de prueba nuevos, y los mocks de sesión tienen el tipo del esquema `Sesion` generado.
- Decisiones: ninguna nueva. Se conserva la tolerancia a un 401 al salir, para no cambiar el comportamiento de DC-02.
- Pendiente o aviso para otros:
  - **Dominique:** se modificaron tus archivos:
    - `frontend/packages/api/package.json` (exportaciones), `packages/api/src/index.test.ts` y `packages/api/generar.test.mjs`;
    - `apps/panel/src/modulos/sesion/useSesion.ts` y `CerrarSesion.tsx`, y se borró `contratoSesion.ts`;
    - `apps/panel/src/tests/rutas.test.tsx`: la respuesta de sesión simulada y los `it.each` de roles tienen el tipo del esquema.

    Actualiza tu rama desde `main`. Si tienes código que importa `./contratoSesion`, cámbialo por `@shapi/api/identidad`. `SesionActual.destino` ahora es la unión de las tres rutas del contrato.
  - **Emilio:** se cerró tu tarea EM-17 (archivo y `## Resultado`). Si cambias `identidad.yaml`, corre `pnpm generar:api` en el mismo PR: el *typecheck* del panel detecta si la sesión deja de coincidir.
  - **Todos:** para los tipos de un módulo, usen `import type { paths } from '@shapi/api/<modulo>'` (convenciones §7). Un contrato nuevo no requiere tocar `package.json`.


## 2026-09-27 · DC-01 · Correcciones de la auditoría: sistema de diseño
- Hecho: paso 12 de `docs/plan/auditoria-2026-09-25.md` (H-85 a H-93).
  - Los tokens son los de 11 §1, con los tonos base de estado y las escalas tipográfica y de espaciado. El tema oscuro no tiene valores inventados y ya no está la paleta por defecto de Tailwind.
  - `.dark` ahora sí cambia los colores: antes no hacía nada, porque Tailwind resolvía los `--color-*` en `:root`. Los colores van en `@theme inline`.
  - El campo, el botón, la tarjeta, la tabla y `/_ui` tienen las medidas de la lámina.
  - `Campo`, `DialogoConfirmacion` y `Selector` son accesibles. `Aviso` y `EstadoError` ya no heredan el formato de las etiquetas, y "Reintentar" es un `Boton`.
  - `generar:api` funciona sin la carpeta de contratos. MSW falla ante una petición sin simular, y queda una sola configuración de Vitest.
  - Los identificadores están en español: `ErrorApi`, `encabezados` y `filas`, `opciones`, `abierto`/`cerrar`/`confirmar`, `CargaDiferida`, `esAdministrador` y `claseEnlace`.
  - Se quitó `@fontsource/instrument-sans`.
  - Hay 28 casos de prueba más (el frontend pasó de 134 a 162).
- Decisiones (de Jordin, 27 sep):
  - H-86: el tema oscuro solo tiene los valores de 11 §1; los demás tokens heredan el claro.
  - H-93: `ErrorApi` es una clase con las propiedades directas, y el portal muestra un texto neutro hasta DC-03.
  - H-88 y H-89: solo cambia `packages/ui`. Los envoltorios de A1 se quedan hasta DC-16.
- Pendiente o aviso para otros:
  - **Dominique:** se modificaron tus archivos:
    - `packages/ui/src/` (`style.css`, `index.tsx` y sus pruebas), `packages/api/` (`index.ts`, `generar.mjs` y sus pruebas) y los `package.json` de los paquetes y las apps (sin el script `test`);
    - `frontend/package.json`, el lockfile, `vitest.config.ts` y `test/servidor.ts`; se borró `apps/panel/vitest.config.ts`;
    - `layouts/LayoutAdmin.tsx`, `layouts/LayoutPanel.tsx`, `rutas.tsx`, `paginasDiferidas.tsx`, `modulos/apis/SelectorApi.tsx`, `modulos/sesion/` y `paginas/_UI.tsx` con su prueba;
    - `apps/portal/src/main.tsx`; se agregaron `App.tsx` y su prueba.
    - `tests/rutas.test.tsx`: la prueba de `/_ui` tiene 10 s de límite y espera hasta 5 s a la lámina, que se carga diferida y con todos los proyectos en paralelo tardaba más de 1 s.

    **DC-02, DC-03 y DC-15:** todo lo que va bajo `.dark` ya toma los colores oscuros de 11 §1.

    Actualiza tu rama desde `main`. **DC-03:** reemplaza `apps/portal/src/App.tsx` por la estructura del portal. **DC-04 y siguientes:** usa `Tabla` con `encabezados` y `filas`, `Selector` con `opciones` (`etiqueta` y `valor`), `DialogoConfirmacion` con `abierto`, `cerrar` y `confirmar`, y `Campo` con `etiqueta`.
  - **Emilio:** en `modulos/identidad/useIdentidad.ts` (EM-03), `ProblemDetailsError` pasó a ser `ErrorApi`: `error.details.codigo` ahora es `error.codigo`, y `error.status` ahora es `error.estado`. Actualiza desde `main` cualquier rama que tengas abierta. También cambió igual en `modulos/sesion/useSesion.ts` y `CerrarSesion.tsx`, que tocaste en EM-03 y EM-17. En `tests/Identidad.test.tsx`, "RF-01 registra con CSRF…" tiene 15 s de límite: escribe unos 80 caracteres y con carga pasaba de los 5 s. Las aserciones no cambiaron.
  - **Todos:**
    - Los errores de la API llegan como `ErrorApi` (convenciones §7).
    - Una petición que la prueba no simula con MSW ahora falla la prueba.
    - Los colores van con los tokens (`text-correcto-base`, `bg-panel`…); `gray-*`, `red-*` y el resto de la paleta de Tailwind ya no existen.

## 2026-09-27 · DC-02 · Correcciones de la auditoría: estructura del panel y del sitio público
- Hecho: paso 13 de `docs/plan/auditoria-2026-09-25.md` (H-94 a H-102).
  - A0.1 va sin el encabezado de A1. Un error que no es 404 ofrece "Reintentar".
  - `/panel`, `/admin` y `/panel/apis/:id` tienen ruta índice. El 403 entre áreas ofrece una salida, y el 404 dentro del panel conserva el layout.
  - Las barras tienen las medidas de N.1 y usan tokens. La estructura ocupa la altura de la ventana, y el HMR funciona con Caddy y sin él.
  - Hay un solo cliente de sesión, con 5 minutos de vigencia. El selector de API usa `Selector`, y `/_ui` solo existe en desarrollo.
  - Hay 27 casos de prueba más (el frontend pasó de 162 a 189). Se comprobó en Chrome contra N.1.
- Decisiones (de Jordin, 27 sep):
  - H-96: el 403 entre áreas ofrece "Ir a su panel" y "Cerrar sesión".
  - H-100: `staleTime` de 5 minutos.
  - H-98: los colores oscuros de las barras se agregaron a 11 §1.
- Pendiente o aviso para otros:
  - **Dominique:** se modificaron tus archivos:
    - `layouts/`: `LayoutPanel`, `LayoutAdmin` y `LayoutPublico`, y el nuevo `Navegacion.tsx` con las piezas comunes;
    - `rutas.tsx`, que ahora exporta `crearRutas({ desarrollo })` además de `router`;
    - `paginasDiferidas.tsx` (la lámina pasó a `laminaDiferida.ts`) y `modulos/apis/SelectorApi.tsx`;
    - `modulos/sesion/`: `CerrarSesion`, `useSesion` y `RequiereRol`, y los nuevos `useCerrarSesion` e `IrAlDestino`;
    - `paginas/Error-403.tsx` y el nuevo `Error-Ruta.tsx`;
    - las pruebas `Layouts.test.tsx` y `Estructura.test.tsx` (nueva);
    - `vite.config.ts` del panel y del portal (sin `clientPort`);
    - en `packages/ui`: `Selector` (flecha, medidas de N.1, `className` en el contenedor y `deshabilitada` en las opciones) y los tokens de las barras en `style.css`, con sus pruebas en `index.test.tsx` y `style.test.tsx`.

    Actualiza tu rama desde `main`. **DC-03:** usa `MarcaShapi` y los tokens de `Navegacion.tsx` si el portal los necesita. **DC-15:** A0.1 ya no tiene el encabezado de A1; el suyo va en su página.
  - **Emilio:** en `tests/Sesion.test.tsx` (EM-17), la prueba de H-84 ahora busca `clienteSesion` en `useCerrarSesion.ts`, porque ese hook salió de `CerrarSesion.tsx`.
  - **Todos:**
    - Para ocultar una opción de menú o proteger una sección, sigue valiendo `RequiereRol`; `area` es solo para `/panel` y `/admin`.
    - `Selector` recibe el ancho en `className` (por ejemplo, `w-full`).
    - Si una pantalla cambia el nombre o el rol del usuario, que invalide `claveSesion` para que se vea de inmediato.

## 2026-09-27 · EM-06 · Pasarela de pagos simulada
- Hecho: se terminó EM-06 a partir del PR #14 de Emilio (protocolo §E4, paso 14 de `docs/plan/auditoria-2026-09-25.md`, H-103 a H-105 y H-119).
  - `PasarelaSimulada` reconoce las tarjetas de prueba por el número completo y guarda su comportamiento en el token, sin memoria.
  - La demora es de 300 a 800 ms por defecto. El CVV solo acepta dígitos, y el vencimiento se evalúa con el mes de Guatemala.
  - Hay 63 casos de prueba nuevos (RF-20) en `tests/Shapi.Api.Tests/Pagos/`. Se borró `generar_pagos.py`.
  - Las tarjetas de ejemplo de 09 §2 y de los mockups A2.2 y A5.6 no pasaban Luhn; se cambió un dígito del medio y se conservaron los últimos 4 (H-119).
- Decisiones (de Jordin, 27 sep), agregadas a 09 §2:
  - `0002`, `0069` y `0341` llevan su comportamiento en el token, porque el Trabajador cobra las renovaciones en otro proceso.
  - Sin `Pagos:DemoraMs`, la demora es al azar entre 300 y 800 ms; con un valor, es ese valor.
  - El "mes actual" del vencimiento es el de America/Guatemala.
  - `0341` se rechaza en las renovaciones con `fondos_insuficientes`.
- Pendiente o aviso para otros:
  - **Emilio:** se terminó tu EM-06 en la rama `jordin/EM-06-pasarela-de-pagos`, que parte de la tuya; tu rama no se modificó. El PR #14 queda cerrado. Cambiaron `src/Shapi.Infraestructura/Pagos/PasarelaSimulada.cs` (reescrita), `docs/plan/tareas/EM-06-pasarela-de-pagos-simulada.md` (criterio 1 y `## Resultado`), tu bitácora (nota en la entrada de EM-06), `mockups/A2/Contratacion.dc.html` y su exportación, y se agregó `tests/Shapi.Api.Tests/Pagos/PasarelaSimuladaTests.cs`. Actualiza tu rama desde `main`.
  - **EM-08 y EM-10:** la pasarela solo está registrada en la API (`PagosModulo`). EM-10, que cobra las renovaciones, tiene que registrarla también en el Trabajador (corregido en la auditoría final: antes decía EM-09). En las pruebas con `WebApplicationFactory` que cobren, fijen `Pagos:DemoraMs=0`; si no, cada operación tarda de 300 a 800 ms.
  - **Dominique:** en `mockups/A5/Pago.dc.html` y en `mockups/a5-portal-marca-blanca.html`, la tarjeta de ejemplo ahora es `5412 7534 1203 3057` (**DC-09**, A5.6). Actualiza tu rama desde `main`.
  - **Todos:** para probar la contratación en la demostración, usen las tarjetas de 09 §2. Las de los mockups ya pasan Luhn: `4024 0071 2284 4821` y `5412 7534 1203 3057`.

## 2026-09-27 · JG-01 · Correcciones de la auditoría: CI y reglas del repositorio
- Hecho: paso 15 de `docs/plan/auditoria-2026-09-25.md` (H-106 a H-112).
  - La CI falla si el modelo tiene cambios sin migración o si los tipos generados no coinciden con los contratos.
  - Todos los jobs tienen tiempo máximo. La acción de Claude va fijada por SHA.
  - `.claude/settings.json` niega más variantes de `git push` a `main` (`HEAD:main`, `refs/heads/main`, `+main`, entre comillas, `--all` y `--mirror`). La única que ninguna regla de texto cubre es `git push origin HEAD` estando en `main`.
  - La plantilla del PR pide toda la evidencia de B7, y el `README.md` tiene las carpetas y el arranque rápido.
  - `scripts/reglas-repositorio.test.mjs` tiene 8 pruebas nuevas que leen los workflows y `.claude/settings.json`.
- Decisiones (de Jordin, 27 sep):
  - El título del PR lo valida un workflow aparte, `titulo-pr.yml`, con el check obligatorio `titulo`. Editar el PR ya no repite ni cancela la CI.
  - Para las pruebas del backend en Windows, se documenta `-m:1`; la CI no cambia.
- Pendiente o aviso para otros:
  - **Todos:**
    - Hay una verificación obligatoria nueva, `titulo`. Si el título del PR está mal, falla `titulo`, ya no `plan`. Al corregirlo con `gh pr edit --title`, solo se repiten `titulo` y la revisión con Claude.
    - Si cambian una entidad o una configuración de EF Core, generen la migración en el mismo PR (`dotnet ef migrations add <Nombre> -p src/Shapi.Infraestructura -s src/Shapi.Api`): la CI ahora lo exige.
    - Si cambian un contrato de `contratos/openapi/`, ejecuten `pnpm generar:api` en `frontend/` y hagan *commit* de `packages/api/src/generado/`: la CI ahora lo exige.
    - Si en Windows `dotnet test Shapi.slnx` falla con errores de Docker, repitan con `dotnet test Shapi.slnx -m:1`.
    - La plantilla del PR ahora pide la evidencia de `node scripts/tareas.mjs --validar`, `dotnet format` y `pnpm build`.

## 2026-09-27 · JG-01 · Correcciones de la auditoría: huecos de requisitos
- Hecho: paso 16 de `docs/plan/auditoria-2026-09-25.md` (H-113 y H-114).
  - RNF-06 queda como objetivo de diseño, sin medición. Hay una tarea nueva, JZ-18 (RNF-11). (JZ-18 se borró el mismo día.)
  - 03 §2 dice cómo se verifica RNF-11 (ahora dice que es un objetivo de diseño). JG-02 declara RNF-13.
  - `calendario.md` precisa qué cubren las P1.
- Decisiones (de Jordin, 27 sep):
  - RNF-06 no se mide en el ambiente simulado, porque no corre de forma continua. Queda como objetivo de diseño para un despliegue real, respaldado por el reinicio automático (JZ-06), RNF-04 y el estado de los componentes (JZ-12).
  - JZ-18 es de José Pablo, P2, del avance final. RNF-11 se verifica solo con una prueba E2E cronometrada, sin pruebas con personas. (Anulado el mismo día: JZ-18 se borró y RNF-11 quedó como objetivo de diseño. Ver la entrada «RNF-11 como objetivo de diseño».)
  - No se sube ninguna prioridad: las P1 cubren el mínimo de cada lineamiento, y las P2 EM-12, DC-14, JG-13 y JZ-12 completan la trazabilidad (EM-12 y EM-13 cubren además RF-43, que no está en la tabla). Si hay que recortarlas, Jordin lo decide y lo anota aquí.
- Pendiente o aviso para otros:
  - **José Pablo:** tienes una tarea nueva en el avance final, JZ-18 (prueba E2E cronometrada de la publicación, RNF-11), que reutiliza el flujo de JZ-13. Léela con `node scripts/tareas.mjs --ver JZ-18`. En el manual técnico, RNF-06 se presenta como objetivo de diseño con los mecanismos de 03 §2, sin medición: se agregó al criterio 1 de **JZ-15**. (Lo de JZ-18 se anuló el mismo día: la tarea se borró. Ver la entrada «RNF-11 como objetivo de diseño». Lo de JZ-15 sigue vigente.)
  - **Emilio, Dominique y José Pablo:** EM-12, EM-13, DC-14 y JZ-12 siguen en P2, pero ahora `calendario.md` las nombra: completan los lineamientos o RF-43. Avísenle a Jordin antes de dejarlas para después.

## 2026-09-27 · JG-01 · RNF-11 como objetivo de diseño
- Hecho: se borró JZ-18, la prueba E2E cronometrada de RNF-11 que se había creado en el PR #38. RNF-11 queda en 03 §2 como objetivo de diseño sin medición, respaldado por un flujo sin pasos manuales ni aprobaciones, y con una fila nueva en 03 §4. También se actualizaron el calendario, el paso 16 del plan de la auditoría y el `## Resultado` de JG-01.
- Decisiones (de Jordin, 27 sep): RNF-11, igual que RNF-06, no es un lineamiento del curso y medirlo podía atrasar el avance final.
- Pendiente o aviso para otros:
  - **José Pablo:** JZ-18 ya no existe; no tienes que hacer la prueba cronometrada. JZ-13 no cambia. En el manual técnico (JZ-15) basta con presentar RNF-06 como objetivo de diseño, como ya dice su criterio 1.

## 2026-09-27 · JG-01 · Correcciones de la auditoría: auditoría final
- Hecho: paso 17 de `docs/plan/auditoria-2026-09-25.md`.
  - Se ejecutó la verificación completa y se volvieron a auditar las 13 tareas hechas en contexto limpio. No volvió ningún hallazgo anterior.
  - Se corrigieron los 25 nuevos (H-120 a H-144) y las 6 dudas que Jordin decidió resolver (H-145 a H-150), en un solo PR.
  - Queda pendiente H-151, que aplica Jordin a mano: negar `gh pr merge *--admin*` en `.claude/settings.json`. El modo automático no deja que un agente edite sus propios permisos.
- Decisiones (de Jordin, 27 sep):
  - Todo el paso va en un solo PR con el ID de JG-01.
  - Los registros de los tres procesos van en JSON (06 §8).
  - Los correos del personal salen como «Shapi».
  - Vite sigue escuchando en `0.0.0.0`, porque en Linux Caddy llega por el puente de Docker.
  - Se quedan como están los textos de las páginas de relleno, `/historial` vacío con 200, los tokens desconocidos de la pasarela y la especificación sin CHECK de 2 MB.
- Pendiente o aviso para otros:
  - **Emilio:**
    - **EM-01:** cambiaron `OrganizacionConfiguracion.cs`, `CasoConfiguracion.cs`, `ApiConfiguracion.cs` y `LoteConsolidadoConfiguracion.cs`, y hay una migración nueva, `TextoConLargoEnCheck`. Si tu rama tiene una migración, sigue convenciones §3: bórrala, toma el snapshot de `main` y vuelve a generarla.
    - **Siembra:** `SiembraBase.cs` ahora usa una transacción y los constructores del dominio.
    - **EM-02:** en `Identidad/Endpoints.cs`, el reenvío bloquea la fila del usuario.
    - **EM-06:** `PasarelaSimulada.cs` usa `CodigosError`.
    - **EM-10:** registra `IPasarelaPagos` en `src/Shapi.Trabajador/Program.cs`. Está en sus archivos y en sus pruebas; antes el aviso decía EM-09.
    - **Pruebas nuevas:** `AutenticacionTests.cs`, `RestriccionesTests.cs`, `SiembraBaseTests.cs` y, en el frontend, `Identidad.test.tsx` y `Sesion.test.tsx`.
    - **A1.1:** `A1-1-Registro.tsx` muestra los enlaces de CU-01 2a.
    - **Tareas:** el criterio 5 de EM-02 (CSRF: «método no seguro») y los `## Resultado` de EM-01, EM-02, EM-03, EM-06 y EM-17.
    - Actualiza tu rama desde `main`.
  - **Dominique:**
    - cambiaron `packages/ui/src/index.tsx` (`type="button"` en `DialogoConfirmacion`) y su prueba, `paginas/_UI.tsx` y `_UI.test.tsx`, un comentario en `apps/panel/vite.config.ts` y `apps/portal/vite.config.ts`, y los `## Resultado` de DC-01 y DC-02;
    - **DC-04:** usa `SubdominiosReservados.Contiene` y completa `apis.yaml` (`security`, 401 y `required`);
    - Actualiza tu rama desde `main`.
  - **José Pablo:**
    - cambiaron `origenes-demo/envios-xelaju/Program.cs`, `cotizacion-envios.yaml`, `agro-precios/openapi.yaml` y sus pruebas, además de `MotorPlantillasCorreo.cs`, `CorreoRenderizado.cs`, `EnviadorSmtp.cs`, `docs/manual-tecnico.md`, `instalacion.md`, el `README.md` y los `## Resultado` de JZ-01, JZ-02 y JZ-03;
    - **JZ-11** precisa la marca de las dos plantillas que ya existen y los `datos` del portal (`colorPortal` y `logoPortal`);
    - Actualiza tu rama desde `main`.
  - **Todos:**
    - los registros de la API, la compuerta y el trabajador ahora salen en JSON;
    - para `dotnet run`, carguen antes el `.env` con el comando del manual técnico.

## 2026-09-28 · JG-01 · Correcciones de la auditoría: H-151
- Hecho: último pendiente del paso 17 de `docs/plan/auditoria-2026-09-25.md`.
  - `.claude/settings.json` niega `gh pr merge *--admin*` a los agentes.
  - Una prueba nueva de `scripts/reglas-repositorio.test.mjs` comprueba que `--admin` está negado y que el *auto-merge* normal de B11 sigue permitido.
  - Con esto quedan corregidos los 151 hallazgos de la auditoría.
- Decisiones (de Jordin, 28 sep): Jordin agregó la regla a mano, porque el modo automático no deja que un agente edite sus propios permisos.
- Pendiente o aviso para otros:
  - **Todos:** sus agentes ya no pueden integrar un PR con `--admin`. Si `revision-claude` da un falso positivo, avísenle a Jordin (B11). Abran una sesión nueva de Claude Code para que tome la regla.

## 2026-09-28 · JG-03 · Calendario diario y recordatorio en el tablero
- Hecho:
  - Cada tarea pendiente tiene `programada: AAAA-MM-DD`, el día del calendario en que se integra. La tabla por día está en `docs/plan/calendario.md` y se genera con `node scripts/tareas.mjs --calendario --escribir`.
  - `node scripts/tareas.mjs --hoy <clave>` muestra lo de hoy, lo atrasado y quién lo espera; `--persona` y `--siguiente` ordenan por fecha.
  - El issue "Tablero del plan" publica cada día a las 07:00 las "Tareas del día" y menciona solo a quien tiene algo ese día o algo atrasado.
- Decisiones (de Jordin, 28 sep): el campo se llama `programada`; el aviso menciona solo a quien tiene algo; va como `[JG-03]`; las fechas son las del calendario acordado hoy.
- Pendiente o aviso para otros:
  - **Todos:** su agente ya sigue el calendario: "¿qué me toca?" empieza por lo de hoy y lo atrasado, y "continúa" toma la tarea de fecha más cercana. Si una tarea ya está disponible, se puede adelantar. Las fechas solo las cambia Jordin; si no van a llegar, avísenle.
  - **Emilio, Dominique y José Pablo:** hoy (lunes 28) les tocan EM-04 y DC-03; José Pablo empieza mañana con JZ-06. Revisen su columna en `docs/plan/calendario.md` §"Calendario por día".
  - **Todos:** si crean una tarea nueva (protocolo §C), pónganle `programada` y ejecuten `node scripts/tareas.mjs --calendario --escribir`, o `--validar` falla en la CI.

## 2026-09-28 · JG-04 · Publicador de configuración en Redis y resincronización
- Hecho:
  - `PublicadorCacheRedis` reemplaza al publicador nulo. Lee PostgreSQL y escribe `api:*`, `clave:*`, `susc:*` y `org:*` (07 §4).
  - Si Redis falla, reintenta 3 veces, registra el error y no lanza.
  - El trabajador resincroniza Redis al arrancar y cada 5 minutos, borra las llaves de configuración que sobran y no toca los contadores.
  - `IProtectorSecretoOrigen` usa Data Protection con el propósito `Shapi.SecretoOrigen` y las llaves en `SHAPI_DPKEYS_DIR`.
  - Se eliminó `sembrar-demo` de la compuerta.
- Decisiones (del agente, locales y reversibles, protocolo §C; Jordin las revisa en el PR):
  - El `EXPIREAT` de una clave rotada se traslada a la hora real, para que el reloj del modo demostración no la alargue.
  - La resincronización borra las llaves sobrantes.
  - `PublicarSuscripcion` de una suscripción de plataforma publica la organización.
  - Todo quedó en 07 §4 y en 10 §3.
- Pendiente o aviso para otros:
  - **Todos:** `IPublicadorCache` ahora es *scoped* y escribe en Redis de verdad. Llámenlo **después** del `SaveChanges`/*commit*, nunca dentro de la transacción. No lanza si Redis falla, así que no lo envuelvan en `try`. Siempre publica el estado actual de la base de datos: basta con pasarle el ID.
  - **DC-04:** cifren el secreto de origen con `IProtectorSecretoOrigen.Cifrar` (ya registrado en `AgregarServiciosComunes`). No registren Data Protection de nuevo: el trabajador usa el mismo nombre de aplicación y el mismo directorio para descifrarlo.
  - **DC-06 y DC-14:** llamen a `PublicarApi(apiId)` al publicar, al despublicar, al cambiar rutas de una API publicada, al regenerar el secreto y al verificar o quitar el dominio propio. Si se quita un dominio, la resincronización borra su host en 5 minutos como máximo.
  - **EM-07, EM-09 y JZ-08:** llamen a `PublicarSuscripcion(id)` en cada cambio de estado o de plan de una suscripción, y a `PublicarOrganizacion(id)` al suspender o reactivar una organización. Para la de plataforma basta `PublicarSuscripcion`, que publica `org:{id}`.
  - **JZ-05:** al sembrar APIs, cifren el secreto con `IProtectorSecretoOrigen` y, al terminar, ejecuten `ResincronizarCache.EjecutarAsync` (se registra con `AgregarResincronizacion`). El `sembrar-demo` de la compuerta ya no existe.
  - **JZ-06:** la API y el trabajador deben montar el mismo volumen `dpkeys` con el mismo `SHAPI_DPKEYS_DIR`. Sin eso, el trabajador no descifra los secretos: la resincronización no reescribe esas APIs (conserva lo que publicó la API) y lo registra como error. Además, el trabajador ahora usa el marco `Microsoft.AspNetCore.App` (Data Protection), así que su imagen base debe ser `aspnet` y no `runtime`.
  - **JZ-05:** `src/Shapi.Trabajador/Program.cs` ahora termina en `internal partial class Program;`. Consérvenlo: sin eso, .NET 10 hace pública la clase y choca con el `Program` de la API en las pruebas.
  - **JG-05 y JG-07:** lean `susc:` y `org:` con `ContextoSuscripcion.DesdeCampos` y `ContextoOrganizacion.DesdeCampos`, y las rutas con `RutaCache.Deserializar`. Una `susc:` que no existe equivale a una suscripción inactiva. JG-07 publica con `PublicarClave`, `ExpirarClave` y `EliminarClave`.

## 2026-09-29 · JG-07 · Servicio de claves: emisión, rotación y revocación (backend)
- Hecho:
  - `IServicioClaves` emite, rota y revoca claves (RF-26 a RF-28). Guarda en PostgreSQL y, después del *commit*, publica en Redis.
  - Endpoints del panel (`/api/apis/{apiId}/claves`) y del portal (`/api/portal/claves`), con el contrato `contratos/openapi/claves.yaml` y los tipos en `@shapi/api/claves`.
  - Bitácora de `clave.rotada`, `clave.revocada_por_consumidor` y `clave.revocada_por_proveedor`.
  - Código de error nuevo: 409 `clave_activa_existente`.
- Decisiones (del agente, locales y reversibles, protocolo §C; Jordin las revisa en el PR): qué claves se listan y en qué orden, el 409 al emitir con otra activa y el bloqueo de la fila al rotar y revocar. Todo quedó en 05 CU-13 y en 10 §2.
- Pendiente o aviso para otros:
  - **EM-05:** los endpoints de claves del portal esperan que la sesión del consumidor ponga tres claims: `ClaimTypes.NameIdentifier` = id del consumidor, `PoliticasAutorizacion.ClaimOrganizacion` = su organización (con eso funciona el filtro global) y `PoliticasAutorizacion.ClaimAmbito` = `Consumidor`. Si la organización no es la de la API del host, responden 404. Mientras no exista la sesión, `tests/Shapi.Api.Tests/Claves/ClavesTests.cs` la simula con `SesionConsumidorDePrueba`; cuando la publiques, avísame si cambian los claims.
  - **EM-08:** al activar una suscripción de API, llama a `IServicioClaves.EmitirClavesParaSuscripcion(suscripcionId)` **después** de confirmar la suscripción y de `PublicarSuscripcion`, dentro de la petición del consumidor (con el contexto de su organización). Guarda las claves, las publica en Redis y devuelve las dos claves completas (`ClaveEmitida.Clave`) para mostrarlas una sola vez en A5.4b. Solo emite los tipos que no tienen ya una clave activa.
  - **DC-11:** B2.3 a B2.6 usan `GET /api/portal/claves`, `POST /api/portal/claves/{id}/rotar` (devuelve la clave nueva y `anterior.expiraEn` para B2.5), `POST /api/portal/claves/{id}/revocar` y `POST /api/portal/claves/emitir` `{tipo}` (409 `clave_activa_existente` si ya hay una activa). La lista ya viene ordenada y filtrada (05 CU-13).
  - **JG-10:** A4.3 usa `GET /api/apis/{apiId}/claves` (agrupada por consumidor, con `plan`) y A4.3b, `POST /api/apis/{apiId}/claves/{id}/revocar`.
  - **JZ-05:** para sembrar las claves de la demostración con valores conocidos, insértalas con `GeneradorClave.CalcularHash`, el prefijo y los últimos 4, y deja que la resincronización las publique. `EmitirClavesParaSuscripcion` genera claves al azar y necesita el contexto de la organización.

## 2026-10-01 · JG-05 · Compuerta: organización, suscripción, ruta, secreto, SSRF y CORS
- Hecho: filtros 0 (CORS), 3 (organización), 4 (suscripción) y 5 (ruta) de 08 §3. Lector del contexto en dos *pipelines* de Redis, con las rutas en memoria 5 s según su `version`. `ConnectCallback` contra SSRF con `ValidadorDireccionOrigen`. Cabeceras y cookies hacia el origen. Errores 502, 504 y 413 con el contrato de 08 §4.
- Decisiones: una ruta oculta más específica que una expuesta se rechaza (RF-10). Sin `org:{id}` en Redis, `api_no_disponible`. Para CORS, el `Origin` debe tener el host del `portal_host` (http o https, cualquier puerto) y las `Access-Control-*` del origen se reemplazan. `X-Forwarded-For` se agrega a la cadena y `X-Forwarded-Proto` conserva el del borde. Todo quedó en 08 §1, §3, §5, §6 y §8.
- Pendiente o aviso para otros:
  - **Dominique:** agregué `ValidadorDireccionOrigen.OrigenesPermitidosPorDefecto` en `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs` y `src/Shapi.Api/Modulos/ApisModulo.cs` lo usa en vez del texto repetido. La compuerta usa el mismo valor. Actualiza tu rama desde `main`.
  - **José Pablo:** en `infra/compose.prod.yml`, el servicio `compuerta` ahora recibe `SHAPI_MODO_DEMO` y `SHAPI_ORIGENES_PERMITIDOS`. Sin eso, la compuerta rechaza los orígenes de demostración, porque están en la red de Docker. Actualiza tu rama desde `main`.
  - **DC-05 y DC-06:** la compuerta solo deja pasar las rutas `expuesta = true` de `api:{id}:rutas`, y cualquier cambio de rutas se aplica cuando `PublicarApi` sube la `version` (si no, tarda hasta 5 s). Los patrones pueden mezclar texto y parámetros en un segmento (`{nombre}.json`), y sus literales no distinguen mayúsculas. Uno que no empiece con `/` o tenga llaves sin cerrar no coincide con nada.
  - **DC-10:** las respuestas de la compuerta, también los errores, llevan `Access-Control-Allow-Origin` cuando el `Origin` es el portal de la API, y exponen las cabeceras de 08 §5. El *preflight* no necesita clave.
  - **JZ-05 y JZ-07:** para llamar a la compuerta, la API necesita, además de `api:*` y `clave:*`, `org:{id}` activa, `susc:{id}` activa o en gracia y la ruta expuesta. Con la resincronización basta.

## 2026-10-01 · EM-06 · Corrección de una prueba intermitente de la pasarela
- Hecho: `RF_20_Tokenizar_DevuelveLaMarcaLosUltimos4YElTitularSinElNumeroNiElCvv` fallaba cuando el UUID al azar del token contenía "987", el CVV de la prueba (CI del PR #51). Ahora la prueba exige el formato exacto `tok_sim_{uuid}` y busca el CVV fuera del token.
- Decisiones: no se cambió el código de la pasarela, solo la prueba. Revisé las demás aserciones `NotContain` con textos cortos y no dependen de valores al azar.
- Pendiente o aviso para otros:
  - **Emilio:** cambié `tests/Shapi.Api.Tests/Pagos/PasarelaSimuladaTests.cs` (solo esa prueba) y agregué una subsección a `## Resultado` de EM-06. Actualiza tu rama desde `main`. Si una prueba busca que un dato no aparezca en un texto que lleva un UUID o un token al azar, quita primero ese valor del texto.

## 2026-10-02 · JG-06 · Compuerta: límites por minuto, cuotas y cabeceras (Lua)
- Hecho: filtro 6 de 08 §3 con `evaluar_limites.lua`: límite por minuto del plan y de la ruta, cuota del consumidor (por peso) y cuota de plataforma, reservados de forma atómica en una sola llamada `EVALSHA`. Respuestas 429 con `Retry-After` y las cabeceras de 08 §5 en toda respuesta que pasa por el filtro. La cuota se devuelve si el origen no se pudo conectar (502). La clave de pruebas usa 10 por minuto y 1,000 por día.
- Decisiones: el límite diario de la clave de pruebas responde `cuota_agotada` y lo informan las `X-Cuota-*`. Si el ciclo ya terminó, los contadores de cuota vencen 8 días después de ahora. Si Redis no tiene el script, se ejecuta una vez con `EVAL`. Todo quedó en 08 §3 a §5 y en 07 §4.
- Pendiente o aviso para otros:
  - **JZ-14:** la compuerta ahora aplica los límites. Para la carga de k6, usa una clave de producción cuyo plan tenga `limite_minuto` y `cuota_llamadas` mayores que la carga, o recibirás 429. La clave de pruebas solo permite 10 peticiones por minuto. Cada petición hace tres viajes a Redis (08 §8).
  - **DC-10:** con la clave de pruebas, `X-RateLimit-Limit` es 10 y las `X-Cuota-*` informan el límite diario: `X-Cuota-Limite: 1000`, las que quedan en el día y la medianoche de Guatemala en `X-Cuota-Reinicio` (ISO 8601 en UTC). Al pasar las 1,000 del día, la compuerta responde 429 `cuota_agotada`. Las cabeceras ya se exponen por CORS.
  - **EM-13:** `cuota:org:{organizacion_id}:{ciclo_inicio}` cuenta las peticiones del ciclo de plataforma. Un 502 las devuelve. Vence a los `ciclo_fin + 8 días`; si no existe, el uso es 0.


## 2026-10-03 · JG-18 · Optimización de las pruebas y la CI
- Hecho:
  - `Shapi.Api.Tests` usa un solo PostgreSQL (`Persistencia/PostgresCompartido.cs`), con `template1` ya migrada: cada base nueva nace migrada. Antes cada clase arrancaba su propio contenedor (unos 12) y cada prueba migraba su base.
  - En mi PC bajó de 13 min 18 s a 7 min 16 s. Las pruebas de un módulo tardan alrededor de 1 min.
  - El check `titulo` valida el cierre de la tarea (`node scripts/tareas.mjs --validar-cierre`).
  - Las pruebas E2E corren también en el PR (job `ambiente-productivo`).
  - B7 permite correr en local solo las pruebas de lo que se tocó.
- Decisiones: no se borró ni se debilitó ninguna prueba. La de RF-08 que espera 5 s se queda, porque comprueba ese límite. `dotnet format` no se cambió.
- Pendiente o aviso para otros:
  - **Todos:**
    - B7 cambió: en local basta con `dotnet build`, `dotnet format` y las pruebas de lo que tocaste, por ejemplo `dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Planes"`. La suite completa la corre el check `backend`.
    - Antes del *push*, corran `node scripts/tareas.mjs --validar-cierre "[<ID>] <título>"`. El check `titulo` ahora falla si la tarea no queda `hecha` con `## Resultado` o si el PR no agrega una entrada en la bitácora; antes eso solo lo detectaba la revisión con Claude, después de varios minutos.
    - Una clase de pruebas nueva que necesite PostgreSQL usa un fixture de una línea (`public sealed class MiFixture : PostgresDePrueba;`) y su propia base (`Database = $"prueba_{Guid.NewGuid():N}"`), en vez de arrancar su propio contenedor. Al terminar, la borra en su `DisposeAsync` con `await PostgresCompartido.EliminarBaseAsync(_cadena);`. Si no se borra, las bases se acumulan y el borrado final hace fallar por tiempo las pruebas de la compuerta. Nada debe conectarse a `template1`.
  - **Emilio:** cambié los fixtures de `tests/Shapi.Api.Tests/Identidad/AutenticacionTests.cs`, `Identidad/ConsumidorPortalTests.cs`, `Identidad/RecuperacionTests.cs`, `Planes/PlanesTests.cs` y `Persistencia/BaseDePrueba.cs` (`PostgresPersistencia`), y agregué `Persistencia/PostgresCompartido.cs`. Solo cambiaron la preparación y el `DisposeAsync`, que ahora borra la base; las pruebas son las mismas. Una migración nueva no requiere nada: `template1` se migra al arrancar. Actualiza tu rama desde `main`.
  - **Dominique:** cambié los fixtures de `tests/Shapi.Api.Tests/Apis/ApisTests.cs` y `Portal/PortalTests.cs`; las pruebas no cambiaron. Actualiza tu rama desde `main`. Tu PR #56 (DC-08) va a fallar en el check `titulo` mientras la tarea siga en `estado: pendiente` sin `## Resultado`, que es lo que pide B10.
  - **José Pablo:**
    - Cambié los fixtures de `tests/Shapi.Api.Tests/Bitacora/BitacoraTests.cs` y `Correo/EnvioCorreoTests.cs`: `EntornoCorreo` ya no tiene la propiedad `Postgres`, y su base vive en el servidor compartido. Las pruebas no cambiaron.
    - En `.github/workflows/publicar-imagenes.yml`, el job `ambiente-productivo` ahora instala Playwright y corre `tests/e2e` después de `infra/verificar.mjs`. `e2e.yml` sigue igual.
    - Actualiza tu rama desde `main`.
  - **Pendiente (Jordin):** omitir los pasos de los jobs `backend` y `frontend` cuando el PR no toca nada que lean. Por ejemplo, un PR solo de frontend como DC-08 corrió el backend completo 4 veces. El job seguiría existiendo y en `main` se verificaría todo. No se aplicó: el clasificador de permisos de Claude Code lo bloqueó por reducir verificaciones de la CI. Si se aprueba, se hace en otro PR. (Resuelto en la entrada siguiente.)

## 2026-10-03 · JG-18 · Verificaciones según el área y pruebas de humo
- Hecho:
  - En los PR, los jobs `backend` y `frontend` omiten sus pasos si el PR no cambia nada que lean sus verificaciones (`scripts/cambios-ci.mjs`). El job siempre se ejecuta y en `main` se verifica todo.
  - Se borraron las tres pruebas de humo (`SaludTests` de la API y de la compuerta, `HumoTests` del dominio). La comprobación de que `/salud` responde 200 pasó a `ApisTests` y `CompuertaTests`.
  - Corregí un error de la compuerta (JG-06) que hacía fallar al azar sus pruebas de tiempo en la CI. La conexión a Redis se abría con la primera petición, de forma síncrona, y 50 peticiones simultáneas agotaban el *pool* de hilos. Ahora se abre al arrancar (`ConexionRedisAlArrancar`, 08 §8).
  - El PostgreSQL compartido de las pruebas guarda sus datos en memoria (`tmpfs`).
- Decisiones: lo autoricé después del PR #58. La regla es conservadora: cada área enumera las rutas que seguro no le afectan, y cualquier otra la ejecuta.
- Pendiente o aviso para otros:
  - **Todos:**
    - Un PR que solo toca `frontend/` ya no corre las pruebas de .NET, y uno que solo toca el backend no corre las de Vitest. En los checks aparecen en verde, con pasos omitidos y el motivo en el resumen del job.
    - Si una prueba de la compuerta falla por tiempo con `RedisTimeoutException` o con esperas de varios segundos, sospechen de un *pool* de hilos agotado: alguna espera síncrona (`Connect`, `.Result`, `.Wait()`) en un camino que reciben muchas peticiones a la vez.
    - Si una prueba empieza a leer un archivo de otra carpeta (por ejemplo, una de Vitest que lea una especificación distinta de `11-interfaz.md`), hay que quitar esa ruta de la lista de `scripts/cambios-ci.mjs` en el mismo PR.
  - **Dominique:** agregué `Salud_ApiEnEjecucion_Responde200` en `tests/Shapi.Api.Tests/Apis/ApisTests.cs`, que reemplaza a `SaludTests`. Actualiza tu rama desde `main`.

## 2026-10-03 · EM-18 · Publicar el destino de la sesión del consumidor
- Hecho: `GET /api/portal/auth/sesion` devuelve `destino` (`/cuenta/suscripcion` con una suscripción vigente a la API del portal; si no, `/planes`). Actualicé el contrato `SesionConsumidor` y regeneré los tipos. Agregué pruebas de integración de los cinco casos y de la suscripción a otra API del mismo proveedor.
- Decisiones: "tiene suscripción" es "tiene una suscripción vigente", es decir, no `finalizada` (07 y ADR-19). Implementé la tarea de Emilio porque DC-08 la necesita para cumplir su criterio 3 (protocolo §E1). Dominique la había creado en su PR #56, que no estaba integrado.
- Pendiente o aviso para otros:
  - **Emilio:** EM-18 es una tarea tuya que creó Dominique en su PR #56 y nunca llegó a `main`. Ya está hecha. Cambié `src/Shapi.Api/Identidad/EndpointsPortal.cs` (`SesionActual`), `contratos/openapi/identidad.yaml` (`SesionConsumidor.destino`) y `tests/Shapi.Api.Tests/Identidad/ConsumidorPortalTests.cs`. Actualiza tu rama desde `main`.
  - **Dominique:** `SesionConsumidor` ya trae `destino` en el contrato y en los tipos generados. Termino DC-08 a partir de tu PR #56 en el siguiente PR (protocolo §E4).

## 2026-10-03 · DC-08 · Pantallas de acceso del consumidor (terminada por el coordinador)
- Hecho:
  - Terminé DC-08 a partir del PR #56 de Dominique (protocolo §E4), después de integrar EM-18 (PR #60): el portal usa `sesion.destino` con el tipo generado.
  - En A5.3b, «Reintentar» ya no vuelve a aceptar una invitación ya aceptada.
  - El botón principal y el campo enfocado usan la marca del portal: salían en el azul de Shapi en las seis pantallas.
  - A5.8 permite pedir otro enlace cuando el enlace está vencido o usado, y reintentar el reenvío.
  - A5.3 ofrece entrar o recuperar la contraseña cuando el correo ya está registrado (CU-11 2a).
  - Reformateé las páginas y agregué pruebas.
  - Hice la verificación manual completa en el ambiente productivo simulado y comparé las capturas con los mockups.
- Decisiones: los colores de la marca se aplican en `MarcoAcceso`, redefiniendo `--principal`, `--principal-hover` y `--anillo-foco`, para no cambiar `Boton` de `@shapi/ui`. Las demás son las de Dominique.
- Pendiente o aviso para otros:
  - **Dominique:**
    - Cerré tu PR #56 con un enlace al nuevo. Tu rama `dominique/DC-08-acceso-consumidor` queda intacta.
    - Cambié `frontend/apps/portal/src/paginas/A5-{3,3b,7,8,9,10}-*.tsx`, `frontend/apps/portal/src/modulos/sesion/{useSesionConsumidor.ts,useIdentidadConsumidor.ts,FormulariosAcceso.tsx}`, `frontend/apps/portal/src/tests/AccesoConsumidor.test.tsx` y tu archivo de tarea.
    - Ojo para las próximas pantallas del portal: `className="bg-[var(--marca-principal)]"` en un `Boton` no tiene efecto, porque gana `bg-principal`. Dentro de `MarcoAcceso` el botón ya toma la marca. Fuera de él, redefine `--principal` en el contenedor, como hace `MarcoAcceso`.
    - Actualiza tu rama desde `main`.
  - **JZ-11:** en la verificación de DC-08, el correo de verificación del consumidor llegó sin la marca de Envíos Xelajú (plantilla básica de JZ-03). Es tu criterio 2: los datos ya traen `nombrePortal`, `hostPortal` y `colorPortal`.

## 2026-10-03 · JG-06 · Correcciones de la auditoría: compuerta (cabeceras y vencimientos)
- Hecho: auditoría de los 20 PR integrados desde la anterior, con 10 subagentes en contexto limpio y la verificación completa (todo en verde). El plan quedó en `docs/plan/auditoria-2026-10-03.md`: 97 hallazgos, 7 de severidad alta. En este PR, paso 1 para JG-06: `X-Shapi-Plan` codificado por porcentajes (H-01; con «Básico» Kestrel respondía 500) y pruebas del vencimiento de las llaves por minuto, de la cuota de plataforma y del día de pruebas (H-03).
- Decisiones: Jordin pidió corregir todo sin detenerse; las decisiones pendientes del plan se resuelven con la recomendación del agente y quedan anotadas como «Decidido (3 oct)». `X-Shapi-Plan` va codificado como componente de URI (08 §5). El `Retry-After` de las cuotas en modo demostración se mide con la hora real (08 §3).
- Pendiente o aviso para otros:
  - **DC-10:** `X-Shapi-Plan` llega codificado por porcentajes (`B%C3%A1sico`). Para mostrarlo en la consola de pruebas, léelo con `decodeURIComponent` (08 §5).
  - **Todos:** el plan de la auditoría del 3 oct está en `docs/plan/auditoria-2026-10-03.md`. Lo corrijo yo, paso a paso; cada PR deja aquí el aviso con los archivos que cambié de cada uno.

## 2026-10-03 · JG-05 · Correcciones de la auditoría: conexión con el origen
- Hecho: paso 1 de la auditoría del 3 oct para JG-05. Si el origen no acepta la conexión en 10 s, la compuerta responde 502 `origen_inaccesible` y devuelve la cuota (H-02; antes, 504 y descontaba). La prueba del secreto de origen lleva RF-47 (H-04).
- Decisiones: la cuota solo se devuelve si la petición no llegó al origen. Si el origen la recibe y corta la conexión, es 502 y se descuenta (08 §1, §3 y §4).
- Pendiente o aviso para otros:
  - **JZ-14:** en las pruebas de carga, un 502 por un origen que corta la conexión ahora sí cuenta en la cuota; solo los fallos de conexión la devuelven.

## 2026-10-03 · EM-05 · Correcciones de la auditoría: ámbitos de sesión
- Hecho: paso 2 de la auditoría del 3 oct para EM-05. Una sesión del portal ya no sirve en las rutas del panel: el esquema se elige por la ruta y la política por defecto exige el ámbito personal (H-05). Pruebas de contraseñas cruzadas entre portales, de `salir`, de la cookie, del límite por IP en el portal y del correo con el host canónico (H-06 a H-10). Nombres de las pruebas, contrato `identidad.yaml` y 10 §1 (H-11 a H-13).
- Decisiones: el esquema se elige por la ruta (`/api/portal/*`) y no por el host, porque el selector es síncrono y resolver el host necesita la base de datos. `RequireAuthorization()` sin nombre exige ahora una sesión del personal.
- Pendiente o aviso para otros:
  - **Emilio:** cambié `src/Shapi.Api/Modulos/IdentidadModulo.cs`, `src/Shapi.Api/Identidad/PoliticasAutorizacion.cs`, `tests/Shapi.Api.Tests/Identidad/ConsumidorPortalTests.cs`, `contratos/openapi/identidad.yaml` y tu archivo de tarea EM-05. Un endpoint nuevo del consumidor debe ir bajo `/api/portal/` y declarar una política `Consumidor*`; `RequireAuthorization()` a secas solo acepta al personal. Actualiza tu rama desde `main`.
  - **Todos:** `frontend/packages/api/src/generado/identidad.ts` se regeneró (los 422 del portal tienen cuerpo `Problema`).

## 2026-10-04 · EM-18 · Correcciones de la auditoría: filtro por organización en la sesión del portal
- Hecho: paso 2 de la auditoría del 3 oct para EM-18 (H-14). `GET /api/portal/auth/sesion` lee el consumidor y sus suscripciones con el filtro global por organización, sin `IgnoreQueryFilters()`.
- Decisiones: ninguna.
- Pendiente o aviso para otros:
  - **Emilio:** cambié `src/Shapi.Api/Identidad/EndpointsPortal.cs` (`SesionActual`) y el comentario de `ContextoOrganizacionHttp.cs`. Actualiza tu rama desde `main`.

## 2026-10-04 · EM-04 · Correcciones de la auditoría: recuperación y Mi perfil
- Hecho: paso 3 de la auditoría del 3 oct para EM-04 (H-15 a H-26).
  - Recuperar la contraseña hace el mismo trabajo en la base exista o no la cuenta.
  - `restablecer` lleva el límite por IP.
  - La contraseña actual incorrecta es un error del campo.
  - `/api/perfil` usa `EditarPerfil`.
  - A8.1 usa los tokens de borde, el `Toast` y «Reintentar».
  - Se agregaron pruebas de recuperación, del perfil y de A8.1.
  - Las precisiones de 11 pasaron a §4.
- Decisiones (10 §1):
  - 3 solicitudes de recuperación por hora por cuenta;
  - al restablecer se invalidan los demás enlaces y se reinicia el bloqueo;
  - una contraseña actual incorrecta en Mi perfil cuenta para el bloqueo.
- Pendiente o aviso para otros:
  - **Emilio:** cambié estos archivos tuyos:
    - `src/Shapi.Api/Identidad/{ServicioRecuperacion,Endpoints,EndpointsPortal}.cs`;
    - `tests/Shapi.Api.Tests/Identidad/{RecuperacionTests,AutenticacionTests,ConsumidorPortalTests}.cs`, y agregué `ContadorComandos.cs`;
    - `contratos/openapi/identidad.yaml`;
    - `frontend/apps/panel/src/paginas/A8-1-Perfil.tsx`, `frontend/apps/panel/src/modulos/identidad/useIdentidad.ts` y `frontend/apps/panel/src/tests/Perfil.test.tsx`;
    - tu archivo de tarea.

    Actualiza tu rama desde `main`.
  - **Todos:** `POST /api/perfil/contrasena` ya no responde 401 con la contraseña actual incorrecta, sino 400 con `errores.contrasenaActual`. Los tipos de `identidad.ts` se regeneraron.

## 2026-10-04 · JZ-05 · Correcciones de la auditoría: siembra de demostración
- Hecho: paso 4 de la auditoría del 3 oct para JZ-05 (H-27 a H-35).
  - En producción, los orígenes de la siembra son los nombres de servicio y no el `localhost` del `.env`.
  - `--reiniciar` funciona con pagos rechazados y con actividad del personal en otras organizaciones: las cuentas de la demo se restablecen, no se borran.
  - Los ciclos y los pagos de plataforma coinciden con A6.
  - Las otras organizaciones tienen sus APIs.
  - Los casos no chocan con uno previo.
  - El comando `sembrar-demo` tiene pruebas.
  - Se usan las fábricas del dominio que existen.
- Decisiones:
  - la bitácora no se duplica al reiniciar;
  - los «hoy» de los mockups quedan en 07 §6;
  - `SHAPI_MODO_DEMO=true` en el ambiente de la exposición, anotado en el manual.
- Pendiente o aviso para otros:
  - **José Pablo:** cambié estos archivos tuyos:
    - `src/Shapi.Infraestructura/Siembra/Demo/SiembraDemo.cs`;
    - `src/Shapi.Trabajador/Program.cs` y el nuevo `src/Shapi.Trabajador/Siembra/ComandoSembrarDemo.cs`;
    - `infra/compose.prod.yml` y `infra/verificar.mjs`;
    - `tests/Shapi.Api.Tests/Siembra/SiembraDemoTests.cs` y el nuevo `ComandoSembrarDemoTests.cs`;
    - `docs/manual-tecnico.md` y tu archivo de tarea.

    Actualiza tu rama desde `main`.
  - **JZ-13:** el entorno E2E es el productivo, sin PostgreSQL publicado. Para reconstruir los datos antes de los flujos, usa `docker compose --env-file .env -f infra/compose.yml -f infra/compose.prod.yml exec trabajador dotnet Shapi.Trabajador.dll sembrar-demo --reiniciar`, no `dotnet run` (el aviso de la bitácora de José Pablo del 3 oct).
  - **JZ-08 y EM-14:** la siembra ya tiene las APIs de Cafetalera, Petén y Datos Chapines, y los ciclos y pagos de plataforma de A6.2 y A6.3.

## 2026-10-04 · JG-18 · Correcciones de la auditoría: CI y plan
- Hecho: paso 5 de la auditoría del 3 oct para JG-18 y JG-03 (H-36 a H-42).
  - Un archivo movido ya no omite las verificaciones (`--no-renames`).
  - `--validar-cierre` falla en la CI si no puede leer el diff.
  - 06 §7.3, §9 y los textos del protocolo, `AGENTS.md`, la plantilla e `instalacion.md` describen la verificación según el área.
  - `--hoy` usa `resumenHoy`, con prueba.
- Decisiones:
  - `ambiente-productivo` pasa a ser check obligatorio;
  - el decisor es parte del PR, como `ci.yml`;
  - un solo PR para JG-18 y JG-03.
- Pendiente o aviso para otros:
  - **Jordin:** agrega `ambiente-productivo` a la protección de `main` (el modo automático no deja que el agente lo haga): `gh api -X POST repos/jordin-garcia/shapi/branches/main/protection/required_status_checks/contexts -f "contexts[]=ambiente-productivo"`.
  - **José Pablo:** cambié el `if` del job `verificar-ambiente` (`ambiente-productivo`) en `.github/workflows/publicar-imagenes.yml`: ahora es `${{ !cancelled() && github.event_name == 'pull_request' }}`, para que no se omita si falla una imagen, y `scripts/reglas-repositorio.test.mjs` lo vigila. Actualiza tu rama desde `main`.
  - **Todos:** cuando se agregue a la protección de `main`, `ambiente-productivo` (las E2E en el ambiente productivo simulado) bloqueará la integración si falla.

## 2026-10-04 · JZ-06 · Correcciones de la auditoría: infraestructura y pruebas E2E
- Hecho: paso 6 de la auditoría del 3 oct para JZ-06 y JZ-07 (H-43 a H-47).
  - `docker/build-push-action` fijada por SHA.
  - `packages: write` solo en el job que publica.
  - El borde corre sin privilegios.
  - `verificar.mjs` comprueba el puerto 8080.
  - La E2E vuelve a entrar por A1.3.
  - `AGENTS.md` y el manual explican cómo preparar las E2E.
- Decisiones: un solo PR para JZ-06 y JZ-07.
- Pendiente o aviso para otros:
  - **José Pablo:** cambié estos archivos tuyos:
    - `.github/workflows/publicar-imagenes.yml`;
    - `infra/borde/Dockerfile` e `infra/verificar.mjs`;
    - `tests/e2e/tests/registro-y-acceso.spec.ts`;
    - `docs/manual-tecnico.md` y tus archivos de tarea JZ-06 y JZ-07.

    Actualiza tu rama desde `main`.
  - **Todos:** en el ambiente productivo, Caddy ahora corre sin privilegios. No hay que hacer nada: el contenedor ajusta solo los permisos del volumen `caddydata`, que sigue compartido con desarrollo, y se usa la misma autoridad certificadora.

## 2026-10-04 · DC-05 · Correcciones de la auditoría: APIs
- Hecho: paso 7 de la auditoría del 3 oct para DC-05 y DC-04 (H-48 a H-57).
  - El lector puede ver las rutas de una API.
  - `GET /rutas` devuelve el resumen de la especificación, y A3.3 muestra la tarjeta del archivo al volver.
  - Las cargas simultáneas ya no chocan, y el lote de exposición se valida con 400.
  - Sin la parte `archivo`, la carga responde 400.
  - A3.4 usa los radios del mockup, y A3.2 muestra el motivo del 422.
  - El subdominio se recorta antes de validarse.
  - Los errores de la biblioteca de OpenAPI llegan en español, con el original como detalle técnico.
- Decisiones: las tres del paso 7 (06 §5.3, pruebas de documentos inválidos y mensajes en español) y un solo PR para DC-05 y DC-04.
- Pendiente o aviso para otros:
  - **Dominique:** cambié estos archivos tuyos:
    - `src/Shapi.Api/Apis/Endpoints.cs`;
    - `src/Shapi.Aplicacion/Apis/` (`ContratosApis.cs`, `GestionarEspecificacion.cs` y `RegistrarApi.cs`);
    - `src/Shapi.Infraestructura/Apis/` (`LectorEspecificacionOpenApi.cs` y `RepositorioApis.cs`);
    - `contratos/openapi/apis.yaml` y los tipos generados;
    - A3.2, A3.3 y A3.4 en `frontend/apps/panel/src/paginas/`, con sus pruebas (`Apis.test.tsx`) y `tests/Shapi.Api.Tests/Apis/ApisTests.cs`;
    - tus archivos de tarea DC-04 y DC-05.

    Actualiza tu rama desde `main`.
  - **DC-06:** `ListaRutas` (`GET /api/apis/{id}/rutas` y `PUT …/rutas/exposicion`) lleva un campo nuevo, `especificacion`, que puede ser `null`. Cada elemento del lote de exposición debe traer `rutaId` y `expuesta`; si no, responde 400.
  - **DC-07 y DC-10:** la forma de `ruta.definicion` está en 07 §3.2: `{orden, parametros, cuerpo, respuestas}`, con las referencias locales resueltas.
  - **JG-09:** al recargar una especificación, el consumo de las rutas retiradas se suma a la fila con `ruta_id` nulo y la ruta se borra (07 §3.5). Después de una recarga, pueden llegar eventos de la compuerta con una `ruta_id` que ya no existe: la consolidación debe guardarlos con `ruta_id` nulo, no fallar por la FK.

## 2026-10-04 · EM-07 · Correcciones de la auditoría: planes y contratación
- Hecho: paso 8 de la auditoría del 3 oct para EM-07 y EM-08 (H-58 a H-72).
  - Planes: ordenados por precio, validación del precio (decimales, máximo y precio 0 en un plan de pago, con `ck_plan_api_pago`) y solo el UNIQUE como 409.
  - Prueba con Redis real, A4.1 con «149.00» y «Q 0.00», textos de la bitácora y `errores` por campo.
  - Respuestas con `moneda` y sin fechas de auditoría.
  - Contratación: reembolso si falla después del cobro, 503 si la pasarela no responde al cobrar, 400 con `errores` y pruebas del ciclo, de Redis, del rechazo y de la concurrencia.
- Decisiones: las siete del paso 8 y un solo PR para EM-07 y EM-08.
- Pendiente o aviso para otros:
  - **Emilio:** cambié estos archivos tuyos:
    - `src/Shapi.Api/Planes/GestionarPlanes.cs` y `Endpoints.cs`;
    - `src/Shapi.Api/Suscripciones/ContratacionApi.cs`;
    - `src/Shapi.Infraestructura/Persistencia/Configuraciones/PlanApiConfiguracion.cs`, con la migración `PlanDePagoConPrecio`;
    - `contratos/openapi/planes.yaml` y `suscripciones.yaml`, con los tipos generados;
    - `frontend/apps/panel/src/paginas/A4-1-PlanesApi.tsx` y su prueba;
    - `tests/Shapi.Api.Tests/Planes/` (incluida la nueva `PlanesRedisTests.cs`) y `tests/Shapi.Api.Tests/Suscripciones/ContratacionTests.cs`;
    - tus archivos de tarea EM-07 y EM-08.

    Actualiza tu rama desde `main`; la migración nueva va antes de cualquiera que crees.
  - **EM-13:** la lógica de planes y contratación sigue en `Shapi.Api` (`Planes/` y `Suscripciones/ContratacionApi.cs`). Si la necesitas, muévela a `Shapi.Aplicacion` con `Resultado<T>` (convenciones §6). Además, con una suscripción `suspendida`, contratar responde 409 `suscripcion_existente`: el caso «suspendida → finalizada: el consumidor contrata otro plan» de 09 §3 te corresponde. Queda una carrera muy improbable: si un plan gratuito se convierte en uno de pago mientras alguien lo contrata, la suscripción nueva queda sin tarjeta. Para cerrarla, la contratación y la edición tendrían que bloquear la fila del plan (`FOR UPDATE`).
  - **DC-09 y DC-11:** `/api/portal/planes` devuelve los planes ordenados por precio, con `moneda: "GTQ"` y sin `creadoEn` ni `actualizadoEn`. La contratación responde 400 con `errores` (`tarjeta`, `tarjeta.vencimiento` y `tarjeta.titular`) y 503 si la pasarela no responde al cobrar.
  - **Todos:** un plan de pago debe tener precio mayor que 0 (07 §3.3); la siembra y las pruebas que creen planes de pago con precio 0 fallarán con `ck_plan_api_pago`.

## 2026-10-04 · DC-03 · Correcciones de la auditoría: portal de marca blanca
- Hecho: paso 9 de la auditoría del 3 oct para DC-03 y DC-08 (H-73 a H-84).
  - La configuración del portal devuelve `nombreOrganizacion`, y el pie muestra su insignia.
  - Los colores de la marca se definen en la raíz.
  - El enlace activo de la cuenta usa la marca.
  - Cerrar sesión limpia la caché y avisa si falla.
  - Hay `errorElement` en español, `/cuenta` lleva a la suscripción y `/documentacion` existe.
  - A5.3b ofrece entrar o recuperar ante `correo_ya_registrado`.
  - A5.9 ya no repite la frase, «Contraseña» es un rótulo en A5.7 y A5.10 nombra el portal.
  - 11 §4 tiene la sección del portal.
- Decisiones: las dos del paso 9 y un solo PR para DC-03 y DC-08.
- Pendiente o aviso para otros:
  - **Dominique:** cambié estos archivos tuyos:
    - `src/Shapi.Api/Portal/Endpoints.cs`, `contratos/openapi/portal.yaml` y los tipos generados;
    - en `frontend/apps/portal/src/`: `App.tsx`, `rutas.tsx`, los layouts, `CerrarSesionConsumidor.tsx`, `FormulariosAcceso.tsx` y A5.3b, A5.7, A5.9 y A5.10;
    - archivos nuevos en `frontend/apps/portal/src/`: `layouts/iniciales.ts`, `modulos/configuracion/coloresMarca.ts` y `paginas/Error-Ruta.tsx`;
    - las pruebas `Estructura.test.tsx`, `AccesoConsumidor.test.tsx` y `tests/Shapi.Api.Tests/Portal/PortalTests.cs`;
    - tus archivos de tarea DC-03, DC-08 y DC-16 (criterio 4 nuevo).

    Actualiza tu rama desde `main`.
  - **DC-07:** los enlaces de «Documentación» llevan a `/documentacion`, que por ahora muestra A5.1. Haz que redirija a la primera ruta expuesta.
  - **DC-16:** criterio 4 nuevo. «Reintentar» debe ser el botón secundario de 11 §4 en los avisos de error de A1 (panel) y en `AvisoError` del portal.
  - **Todos (portal):** los colores de la marca ya valen en todas las pantallas del portal (`coloresMarca` en `App`); no hace falta redefinir `--principal` en cada página. Al cerrar sesión se borran todas las consultas salvo `['portal', 'configuracion']`.

## 2026-10-04 · JZ-04 · Correcciones de la auditoría: bitácora
- Hecho: paso 10 de la auditoría del 3 oct para JZ-04 (H-85 a H-91).
  - B3.2 muestra «Plataforma Shapi» y tiene paginador.
  - Pruebas de los actores consumidor, sistema, miembro de proveedor y usuario sin membresía.
  - El 400 trae `errores`, y `rol` es un `enum` en el contrato.
  - 10 §7 explica de dónde salen el rol y la organización.
  - Vitest del error y del periodo predeterminado.
- Decisiones: las dos del paso 10.
- Pendiente o aviso para otros:
  - **José Pablo:** cambié estos archivos tuyos:
    - `src/Shapi.Api/Bitacora/Endpoints.cs` y `contratos/openapi/sistema.yaml`, con los tipos generados;
    - `frontend/apps/panel/src/paginas/B3-2-Bitacora.tsx` y su prueba `Bitacora.test.tsx`;
    - `tests/Shapi.Api.Tests/Bitacora/BitacoraTests.cs`;
    - tu archivo de tarea JZ-04.

    Actualiza tu rama desde `main`.

## 2026-10-04 · JG-04 · Correcciones de la auditoría: publicador de Redis y claves
- Hecho: paso 11 de la auditoría del 3 oct para JG-04 y JG-07 (H-92 a H-97).
  - `IPublicadorCache.EliminarHost` borra el host de un dominio quitado.
  - Prueba del paso 4 de la resincronización.
  - Avisos llevados a DC-14 y EM-09.
  - `claves.yaml` sin `revocadaPor`.
  - Las claves se bloquean solo después del filtro por organización.
  - Prueba con la cookie `portal_sesion` real.
- Decisiones: (a) de H-92, y un solo PR para JG-04 y JG-07.
- Pendiente o aviso para otros:
  - **DC-14:** criterio 3 actualizado. Al quitar un dominio (o si deja de estar verificado), llama a `IPublicadorCache.EliminarHost(dominio, apiId)` y a `PublicarApi` después del *commit*.
  - **EM-09:** criterios 2 y 3 actualizados. Contratar y subir de plan publican la suscripción con `PublicarSuscripcion`, que publica `org:{id}`, después del *commit*.
  - **Todos:** `IPublicadorCache` tiene un método nuevo, `EliminarHost`. Si tienen un publicador falso en sus pruebas, agréguenlo (por ejemplo, `=> Task.CompletedTask`).
  - **Todos (pruebas):** `SesionConsumidorDePrueba`, sin su cabecera, ahora sigue la selección real de la aplicación (por la ruta). En `/api/portal/*` funciona la cookie `portal_sesion` verdadera, y la del personal responde 401.
  - **Emilio y Dominique:** cambié los publicadores falsos de `tests/Shapi.Api.Tests/Apis/ApisTests.cs` y `tests/Shapi.Api.Tests/Planes/PlanesTests.cs` (agregué `EliminarHost`), y las tareas DC-14 y EM-09. Actualicen sus ramas desde `main`.

## 2026-10-04 · JG-01 · Correcciones de la auditoría: auditoría final
- Hecho: paso 12 de la auditoría del 3 oct.
  - Correcciones de los pasos 7 a 11 integradas: #72 a #76.
  - Verificación completa sobre `main`, todo en verde:
    - `--validar`, 102 pruebas de `scripts/`, compilación, formato y migraciones;
    - 1042 pruebas del backend con `-m:1`;
    - tipos generados, lint, *typecheck*, 295 pruebas del frontend y *build*.
  - Nueva auditoría en contexto limpio de las 19 tareas corregidas: H-01 a H-97 corregidos, sin regresiones de seguridad ni pruebas debilitadas.
  - Salieron 21 hallazgos nuevos (H-98 a H-118: 1 de severidad media y 20 de baja) y 15 dudas, anotados como paso 13 propuesto.
  - H-109 corregido aquí: `ambiente-productivo` ya es obligatorio en la protección de `main`.
- Decisiones: los hallazgos nuevos no se corrigen en este PR; Jordin decide el paso 13.
- Pendiente o aviso para otros:
  - **Jordin:** decidir el paso 13 de `docs/plan/auditoria-2026-10-03.md` (H-98 a H-118 y las 15 dudas). El único de severidad media es H-110, la caché del portal cuando vence la sesión.
  - **Todos:** `ambiente-productivo` (las E2E en el ambiente productivo simulado) ya bloquea la integración si falla.

## 2026-10-04 · DC-03 · Correcciones de la auditoría: caché del portal al vencer la sesión
- Hecho: paso 13 de la auditoría del 3 oct, solo H-110. La caché del consumidor también se limpia si la sesión vence y al entrar, con el helper `limpiarCacheConsumidor`.
- Decisiones: por decisión de Jordin, los demás hallazgos de la auditoría final (H-98 a H-108 y H-111 a H-118) y las 15 dudas quedan para la próxima auditoría.
- Pendiente o aviso para otros:
  - **Dominique:** cambié estos archivos tuyos en `frontend/apps/portal/src/`:
    - `modulos/sesion/CerrarSesionConsumidor.tsx`, `RequiereSesionConsumidor.tsx` y `useIdentidadConsumidor.ts`;
    - el archivo nuevo `modulos/sesion/cacheConsumidor.ts`;
    - las pruebas `Estructura.test.tsx` y `AccesoConsumidor.test.tsx`;
    - tu archivo de tarea DC-03.

    Actualiza tu rama desde `main`.
  - **DC-11 y DC-13:** las consultas de claves, pagos y consumo del portal se borran solas al cerrar sesión, al vencer la sesión y al entrar. Se borran todas las consultas salvo `['portal', 'configuracion']` y `['portal', 'sesion']`: no reutilicen esas dos claves para datos del consumidor.

## 2026-10-05 · JG-09 · Medición en la compuerta y consolidación del consumo
- Hecho: medición de peticiones, rechazos, llamadas descontadas, bytes e histogramas; espera del origen y p95; consolidación cada 10 s con instantáneas atómicas y transacciones idempotentes; recuperación de interrupciones y latidos de compuerta y trabajador con TTL de 30 s; consultas base por API y suscripción con aislamiento de organización.
- Verificación local: plan válido (68 tareas), 102 pruebas de scripts, compilación sin errores ni advertencias, formato sin cambios, 281 pruebas de la compuerta y 97 pruebas de consumo, caché y módulos. La suite completa del backend corresponde al check `backend` de la CI (protocolo B7).
- Revisión en contexto limpio: detectó la posible repetición de llaves en `SCAN`, corregida con deduplicación antes de `HGETALL` y prueba de regresión con fase roja confirmada. También se precisó la clasificación de 413 y respuestas ya iniciadas. Veredicto final LISTO; después de corregir pasaron nuevamente compilación, formato, 284 pruebas de la compuerta y 24 pruebas de consumo.
- Decisiones: precisiones en 08 §7. Cada petición envía sus incrementos y `SADD` juntos en `MULTI/EXEC` sin esperar Redis; el corte del lote es un script atómico de hasta 256 llaves. Las métricas sin API identificada permanecen en Redis con el UUID nulo. Las rutas retiradas se agrupan en la ruta nula. No se agregaron dependencias ni migraciones.
- Revisión automática del PR #79: corregida la recuperación de lotes con referencias eliminadas. Las filas de APIs borradas se descartan definitivamente; las de suscripciones borradas conservan el consumo bajo la API sin atribución a la suscripción. Un lote fallido no detiene los otros ni las métricas nuevas: se conserva para reintentar y el error se informa al terminar el ciclo. Tres regresiones confirmaron la fase roja antes de la corrección.
- Verificación de esta corrección: 28 pruebas de consumo correctas, ninguna fallida ni omitida; formato correcto. La revisión independiente adicional dio LISTO, sin hallazgos obligatorios u opcionales.
- Segunda revisión automática del PR #79: acotado cada ciclo a una lectura de `met:pendientes`, dividida en grupos de 256; el tráfico nuevo espera al siguiente intervalo. Ante el fallo de aplicación de un lote nuevo, se detiene la separación del resto. Regresiones con escritor continuo y fallo real de PostgreSQL comprueban que el ciclo termina, solo crea un lote fallido y luego recupera las 257 llaves sin pérdida ni duplicado.
- Verificación final del ciclo acotado: 30 pruebas de consumo correctas, ninguna fallida ni omitida; ambas regresiones fallaron con el código anterior por plazo agotado y dos lotes creados. Revisión independiente adicional LISTO, sin hallazgos.
- Pendiente o aviso para otros:
  - **JZ-12:** ya están `salud:compuerta:{instancia}` y `salud:trabajador`, cada 10 s con TTL de 30 s. `lote_consolidado.procesado_en` permite comprobar la consolidación. El latido del trabajador es independiente de PostgreSQL y de la duración del lote. La ausencia de un lote reciente no indica atraso cuando no hay métricas pendientes de APIs identificadas; las métricas globales con UUID nulo permanecen en Redis.
  - **JZ-12:** para detectar ausencia del latido, usa la existencia de la llave y su TTL. La marca del trabajador usa `IReloj` (puede estar adelantada en demostración), mientras que la compuerta usa el reloj real; comparar directamente ambas fechas con la hora real daría un resultado incorrecto en ese modo.
  - **JG-11, JG-12 y JG-13:** `IConsultaConsumo`, registrado en el módulo de la API, ofrece `PorApiAsync` y `PorSuscripcionAsync` con organización, periodo inclusivo y entorno opcional. `ConsumoPeriodo` conserva las filas del periodo y ofrece peticiones, llamadas y los dos p95. `Percentil95.Supera2500` indica el texto `> 2500 ms`; sin peticiones, `Milisegundos` es nulo.

## 2026-10-07 · EM-09 · Continuación del PR #81 y correcciones de suscripción de plataforma
- Hecho: continué los commits de Emilio en una rama propia, con su aviso previo confirmado por Jordin, según §E4. Corregí los dos hallazgos obligatorios del PR #81: rechazo de reactivación de Prueba/gratuito sin procesar pagos y regreso de A2.5 a B1.4 después de confirmar un cambio.
- Decisiones: 422 `requiere_plan_de_pago` para Prueba o precio cero, documentado en 09 §5 y el contrato. También corregí fechas de Guatemala, la explicación del cambio de vigencia, la cancelación sin cambio pendiente y el acceso a reactivación desde planes de pago en gracia/suspensión.
- Revisión independiente: se agregaron regresiones y correcciones del reintento de otra tarjeta, el cambio desde planes desactivados, el mensaje de bajada anual, los controles ocultos al lector, el rechazo desde Prueba suspendida y los datos/textos omitidos de los mockups.
- Verificación: compilación y formato de .NET correctos, 3 unitarias de prorrateo y revisión final LISTO. Frontend: lint y typecheck sin errores, 328 pruebas correctas en 21 archivos y compilación del panel y portal; se conservan 3 advertencias previas de lint y el aviso de tamaño del paquete del portal. Capturas de seis escenarios comparadas con los mockups mediante datos de API simulados.
- Backend: 106 pruebas de API de Suscripciones y AutenticacionTests correctas, sin omisiones. Se verificaron las clases en secuencia con una configuración local temporal de xUnit para reducir memoria, conservando las operaciones simultáneas de las pruebas de concurrencia.
- Pendiente o aviso para otros:
  - **Emilio:** este PR conserva y completa tu trabajo en `src/Shapi.Api/{Modulos/SuscripcionesModulo.cs,Suscripciones/}`, `src/Shapi.Dominio/{Pagos,Suscripciones}/`, `contratos/openapi/{planes,suscripciones}.yaml`, `frontend/apps/panel/src/paginas/{A2-*,B1-4-Suscripcion.tsx,Suscripciones.test.tsx}`, las pruebas de suscripciones y `tests/Shapi.Api.Tests/Identidad/AutenticacionTests.cs`, tu tarea EM-09 y su entrada original en tu bitácora. Actualiza tu rama desde `main` cuando se integre.
  - **Dominique:** se conserva y actualiza `frontend/apps/panel/src/tests/rutas.test.tsx`; se regeneraron `frontend/packages/api/src/generado/{planes,suscripciones}.ts` desde sus contratos. Actualiza tu rama desde `main` cuando se integre.
  - **EM-10:** Prueba y los planes de precio cero no se reactivan mediante `/api/suscripcion/pagar`; deben contratar un plan de pago. Conserva esa regla al cerrar la Prueba.

## 2026-10-08 · JG-08 · Convergencia del Avance 2
- Hecho: informe en `docs/plan/convergencia/2026-10-08.md`. De 33 requisitos del avance, 25 están ✅, 7 ⚠️ y 1 ❌ (RNF-14). Se recorrió el guion 2 en el ambiente productivo simulado: funcionan los pasos 2 a 6 y falla el 1, porque `sembrar-demo` se cae en el contenedor (`CultureNotFoundException` por `es-GT` en `SiembraDemo.cs:295`). Se crearon JZ-17 (P1), JZ-18 (P1), JG-19 (P2), DC-17 (P2), EM-19 (P2) y EM-20 (P3), y se regeneró el calendario.
- Decisiones: nadie queda sobrecargado (3 a 3.5 tareas por semana), así que nada baja a P3. JZ-18 es P1 porque RNF-14 es el mínimo del lineamiento de despliegue. DC-09 vuelve a `avance: 3`. Para recorrer el guion se sembró con un parche local (`InvariantCulture`) que no se confirmó.
- Pendiente o aviso para otros:
  - **JZ-17:** es urgente: sin ella, el paso 1 del guion 2 falla en la demostración del viernes 9. Si no llega a tiempo, la demostración siembra en desarrollo.
  - **José Pablo:** JZ-18 (un solo comando con siembra, RNF-14) depende de JZ-17.
  - **Dominique:** corregí `avance: final` → `avance: 3` en el archivo de DC-09; el PR #86 lo había cambiado. DC-17 alinea A5.4, A5.6 y A5.4b con sus mockups (barra lateral con la consumidora) y corrige el texto «la API de API…» de A5.3; precisa en 11 §"Barra de documentación" que, con sesión, las páginas públicas con barra lateral muestran a la consumidora. EM-20 (después de DC-17) tocará `A5-4b-Confirmacion.tsx` para la fila «Medio de pago». Actualiza tu rama desde `main` cuando se integre.
  - **Emilio:** EM-19 completa pruebas de backend del portal para RF-02 a RF-05, y EM-20 (P3) agrega el medio de pago a la respuesta de la contratación, que A5.4b necesita.
  - **Todos:** para el paso 4 del guion, María José ya existe en la siembra; registren a otra consumidora.

## 2026-10-08 · JZ-17 · Siembra de demostración en el ambiente productivo simulado
- Hecho: terminé la tarea de José Pablo, que era P1 para la demostración del 9 de octubre (protocolo §E1). `sembrar-demo` ya no pide la cultura `es-GT`: la fecha del mensaje de CAS-104 se arma con una tabla propia de meses. `tests/Shapi.Api.Tests` corre en modo de globalización invariante, como las imágenes Alpine. El check `ambiente-productivo` ejecuta `node infra/verificar.mjs --sembrar`, que primero comprueba el ambiente sin siembra, después siembra dentro del trabajador y luego llama a `POST /cotizaciones` con la clave de Mercadito Antigua.
- Decisiones: no se agregó ICU a las imágenes: la siembra deja de depender de la cultura, como la compuerta. Al poner en modo invariante todo el proyecto de pruebas de la API, cualquier cultura de país falla en las pruebas y no solo en el contenedor (las 816 pruebas pasan).
- Pendiente o aviso para otros:
  - **José Pablo:** cambié archivos tuyos: `src/Shapi.Infraestructura/Siembra/Demo/SiembraDemo.cs`, `infra/verificar.mjs`, `.github/workflows/publicar-imagenes.yml`, `docs/manual-tecnico.md`, `tests/Shapi.Api.Tests/Siembra/SiembraDemoTests.cs` y tu tarea JZ-17. Actualiza tu rama desde `main` cuando se integre.
  - **JZ-18:** `verificar.mjs --sembrar` ya siembra el ambiente productivo y comprueba la compuerta con una clave fija. Al sembrar con un solo comando, conserva las comprobaciones "sin siembra" antes de sembrar, o ajústalas.
  - **Todos:** las pruebas de `tests/Shapi.Api.Tests` corren sin ICU (`InvariantGlobalization`), igual que los contenedores. No usen `CultureInfo.GetCultureInfo("es-GT")` ni otra cultura de país. Para nombres de meses en español, usen una tabla propia, como `FiltroLimitesYCuotas.Fecha` o `SiembraDemo.DiaYMes`.

## 2026-10-09 · JG-08 · Ensayo del guion 2 y tarea JG-20
- Hecho: se ensayó la demostración del 9 de octubre solo por la interfaz, como pidió la docente, en el ambiente productivo simulado con JZ-17 integrada. `verificar.mjs --sembrar` pasó y los ocho bloques de la interfaz funcionaron sin errores en los registros. El resultado y los nueve hallazgos (H-E1 a H-E9) quedaron en `docs/plan/convergencia/2026-10-08.md` §7. Se creó **JG-20** (P2, jue 15 oct, depende de DC-17) y se regeneró el calendario. `docs/Shapi-Diagrama-de-Gantt.docx` queda en `.gitignore`: es un documento local.
- Decisiones: los hallazgos se dividen por su naturaleza. Lo que la especificación no pide (o pide solo en parte) va a JG-20: H-E1 (vaciar todos los campos de la tarjeta), H-E3 (barra lateral fija y completa desde 1366 × 768), H-E5 (cursor de mano) y H-E6 (mostrar la contraseña, con un ojo). Lo que incumple tareas ya integradas va a la próxima auditoría. H-E4 se movió a la auditoría porque 11 §4 ya pide el aviso de «Acción confirmada».
- Pendiente o aviso para otros:
  - **Próxima auditoría** (además de lo integrado después del 3 de octubre): H-E2 (EM-07, encabezado de la tabla de A4.1), H-E4 (EM-12, aviso al enviar una invitación), H-E7 (EM-07, EM-09 y JZ-10: `actor_nombre` con «Usuario», «Propietario» o el correo), H-E8 (JZ-10, textos de los casos en la bitácora, y el punto final en todas las descripciones) y H-E9 (JZ-10, correo de un caso sin asignado, con decisión pendiente). El detalle, con `archivo:línea`, está en el informe §7.
  - **Dominique:** JG-20 depende de DC-17 y después cambiará `packages/ui` (cursor y campo de contraseña), los layouts del panel y del portal, A5.1 y las pantallas de contraseña y de pago del portal. Conviene que DC-17 se integre en su fecha (mar 13 oct).
  - **Emilio:** JG-20 cambiará los formularios de tarjeta (A2.2, A2.5 y B1.4) y los campos de contraseña de A1.1, A1.3, A1.4b, A8.1 y A8.2.
  - **José Pablo:** JG-20 agregará una prueba de Playwright en `tests/e2e/` para la barra lateral.

## 2026-10-11 · JG-11 · Consumo, latencia y errores por API (B1.1)
- Hecho: se implementaron el endpoint de consumo por API y periodo, su contrato y tipos, los indicadores de B1.1, las llamadas descontadas, barras por día, tablas por ruta y por error, selector y variante vacía. El endpoint aplica permisos y aislamiento y usa los histogramas de JG-09.
- Decisiones: periodo inclusivo de diez días en Guatemala por defecto, máximo de 366 días; porcentajes sobre todas las peticiones, días vacíos con cero y consumo sin ruta conservado; GB decimales y p95 presentado al milisegundo más cercano. Por RF-35 y protocolo §C se corrigió el ejemplo B1.1 (10 ms de compuerta, 93,147 llamadas y origen 250/66/0) y se regeneró su paquete. Las precisiones quedaron en 08 §7.
- Verificación: compilación y formato completos, 48 pruebas de Consumo, 352 pruebas del frontend y 7 dirigidas de B1.1, lint sin errores (11 advertencias previas del portal), tipos, build y plan correctos. Capturas con la siembra y del periodo vacío comparadas con ambos mockups; la revisión independiente corrigió la omisión de llamadas y confirmó el código sin hallazgos.
- Pendiente o aviso para otros:
  - **Dominique:** cambié `frontend/apps/panel/src/tests/rutas.test.tsx`, agregué los tipos generados en `frontend/packages/api/src/generado/consumo.ts` y sincronicé `mockups/B1/Main.dc.html`, `mockups/B1/canvas.json` y `mockups/b1-salidas-proveedor.html` con RF-35 y la siembra. Actualiza tu rama desde `main` cuando se integre.
  - **JG-12 y JG-13:** la respuesta de consumo usa `Percentil95` con `milisegundos` nulo sin muestras y `supera2500` para el rango abierto; la presentación redondea al milisegundo más cercano. El cálculo conserva los decimales y suma histogramas, no percentiles.

## 2026-10-12 · JG-10 · Pantalla de claves del proveedor
- Hecho: lista de claves por consumidor, estados y fechas en Guatemala, variante vacía y confirmación de revocación. Propietario y editor revocan; el lector consulta sin acciones. El listado se actualiza y confirma con un aviso de 4 segundos; errores y paginación tienen reintento y controles.
- Decisiones: A4.3b se muestra dentro de la ruta de claves; páginas de 20 consumidores y orden del contrato. Cambiar de API descarta la confirmación. Se precisó en 11 §4. El contrato de JG-07 ya cubre el comportamiento y sus tipos se regeneraron sin diferencias.
- Verificación: 364 pruebas del frontend en 26 archivos, incluidos 12 casos RF-28; tipos y build correctos, lint sin errores y 11 advertencias previas del portal. Capturas de lista y confirmación con la siembra, y de los tres estados con los datos de referencia, comparadas con sus mockups.
- Pendiente o aviso para otros:
  - **Dominique:** actualicé `frontend/apps/panel/src/tests/rutas.test.tsx` para reconocer A4.3 implementada y simular su listado. Actualiza tu rama desde `main` cuando se integre. No cambió `rutas.tsx`.

## 2026-10-14 · JG-12 · Consumo del ciclo para el consumidor
- Hecho: endpoint del consumo del consumidor aislado por organización, API del portal y suscripción; lectura de la cuota en Redis; agregación del detalle de producción por ruta; contrato y tipos generados; B2.1 con resumen, totales, estado sin suscripción y reintento ante errores.
- Decisiones: las llamadas usadas se leen del contador del inicio del ciclo, y los totales de la tabla de los datos consolidados. No se reconstruyen llamadas multiplicando peticiones por el peso actual. Las rutas retiradas conservan su consumo con peso «—». Hasta que se renueve el ciclo se incluye también el consumo de la gracia. Se precisó en `08-compuerta.md` §7. Un fallo de Redis no se presenta como cero.
- Verificación: build completo sin errores ni advertencias y formato .NET correctos; 69 pruebas de Consumo más una prueba con cookie real aprobadas. Lint sin errores (11 avisos previos), typecheck, 414 pruebas Vitest en 33 archivos y builds de panel/portal aprobados. La primera ejecución concurrente del frontend agotó esperas en 11 pruebas previas; la repetición completa con un solo proceso pasó sin modificar esas pruebas. La compilación general se repitió correctamente al terminar las pruebas, que habían bloqueado sus DLL en Windows. Revisión independiente sin hallazgos de corrección.
- Pantallas: sesión real de demostración comprobada; capturas a 1440 × 900 de los datos reales, las variantes con datos de referencia y el error recuperable. Los textos y datos de las variantes coinciden con ambos mockups y su estructura se comparó visualmente.
- Pendiente o aviso para otros:
  - **Dominique:** agregué `frontend/apps/portal/src/modulos/consumo/useConsumoConsumidor.ts`, las pruebas de B2.1 y el mock de consumo en `frontend/apps/portal/src/tests/Estructura.test.tsx`; regeneré `frontend/packages/api/src/generado/consumo.ts`. Actualiza tu rama desde `main` cuando se integre; las rutas y los layouts no cambiaron.
  - **JZ-13 / José Pablo:** `/cuenta/consumo` y `GET /api/portal/consumo` están listos para las E2E. El resumen refleja el contador de Redis, mientras que los históricos de la siembra pueden incluir llamadas anteriores que ese contador no contiene; las capturas exactas de referencia usan una respuesta controlada del contrato, además de comprobar el flujo real.


## 2026-10-15 · JG-20 · Ajustes de interfaz del ensayo del Avance 2
- Hecho: limpieza completa de los cuatro formularios de tarjeta tras autorización o rechazo, barras laterales completas y fijas a 1366 × 650, cursor común para acciones y ojo accesible en los diez campos de contraseña. Los errores de salida conservan el pie y el reintento. Se completó el flujo real de registro, rechazo, reintento vacío y aprobación.
- Decisiones: filas y grupos compactos por la decisión del 9 oct; solo la lista de documentación puede desplazarse si hay muchas rutas. A2.5 usa el formulario completo para habilitar el pago si no hay tarjeta guardada. Se precisó 11 §1 y §4, sin nuevas dependencias, contratos ni cambios de ADR.
- Verificación: 439 pruebas de frontend en 35 archivos, lint sin errores (11 advertencias previas), tipos y builds correctos; 12 E2E correctas sin omisiones, más repetición dirigida de la captura administrativa; 14 pruebas de reglas y plan válido (76 tareas). Se compararon 18 mockups y las capturas a 1366 × 650 y 1440 × 900. El backend local usó imágenes previas con el frontend actual montado; la reconstrucción completa corresponde a la CI. Las ejecuciones simultáneas agotaron esperas por memoria; se repitió la suite con un solo proceso y Docker cerrado, sin cambiar límites ni omitir pruebas.
- Revisión independiente: detectó expansión del pie por el error al salir y precedencia de la utilidad cursor-pointer en botones deshabilitados; corregidos con regresiones. La prueba de medidas de N.1 ahora exige el espaciado aprobado para esta tarea.
- Pendiente o aviso para otros:
  - **Emilio:** cambié `frontend/apps/panel/src/modulos/identidad/Formularios.tsx`, `paginas/{A2-2-Contratacion,A2-5-CambioPlan,A8-1-Perfil,B1-4-Suscripcion}.tsx`, `paginas/Suscripciones.test.tsx` y las pruebas de identidad, perfil y tarjetas. Actualiza tu rama desde `main` al integrarse.
  - **Dominique:** cambié `frontend/packages/ui/src/{index.tsx,style.css}`, agregué `Contrasena.test.tsx`, compacté `panel/src/layouts/Navegacion.tsx` y `portal/src/layouts/BarraLateralPortal.tsx`, actualicé ambos componentes de cerrar sesión, cuatro pantallas de contraseña y el pago del portal, y sus pruebas de acceso, contratación y estructura. La lista extensa de rutas mantiene el desplazamiento interno sin mover el pie. Actualiza tu rama desde `main` al integrarse.
  - **José Pablo:** agregué `tests/e2e/tests/barras-laterales.spec.ts`; `.github/workflows/e2e.yml` ahora llama al verificador con `--sembrar`, igual que el PR, para preparar las cuentas de demostración una sola vez. Actualiza tu rama desde `main` al integrarse. Los hallazgos H-E2, H-E4 y H-E7 a H-E9 siguen fuera de JG-20, para la próxima auditoría.

## 2026-10-19 · JG-13 · Consumo y facturación por consumidor
- Hecho: endpoint y pantalla B1.2 con consumo consolidado de producción por ciclo, cuota y uso, facturación efectivamente cobrada, estado del último intento y totales. Incluye las suscripciones en gracia y suspendidas, la variante vacía y el enlace a B1.5 con permisos. OpenAPI y tipos generados actualizados.
- Decisiones: 09 §8 precisa los ciclos y la gracia, los pagos autorizados del mismo periodo (incluidos prorrateos), la ausencia de cobros, el orden y el texto del pie. La consulta hace dos lecturas para todas las suscripciones. No hay dependencias nuevas ni migraciones.
- Verificación: fase roja confirmada en interfaz y endpoint; 86 pruebas de Consumo aprobadas sin omisiones, incluidos los 16 casos nuevos de integración. Compilación completa sin errores ni advertencias, formato, tipos generados, typecheck, builds y plan correctos. Pasaron 464 pruebas de frontend en 41 archivos; lint sin errores y con las 11 advertencias previas del portal, y aviso previo de tamaño del build del portal. La primera suite completa se interrumpió tras tres fallos bajo saturación; los 11 casos de los archivos afectados pasaron aislados y luego toda la suite pasó con un hilo, Docker cerrado y aislamiento conservado, sin modificar pruebas ni plazos.
- Pantallas: seis capturas a 1600 × 900 y 1366 × 650 de tabla, variante vacía y error recuperable, comparadas visualmente y por contenido con ambos mockups. La revisión independiente del código y las capturas no encontró hallazgos de corrección; el cierre se comprueba antes del PR.
- Pendiente o aviso para otros:
  - **Dominique:** actualicé `frontend/apps/panel/src/tests/rutas.test.tsx` para reconocer B1.2 y simular el nuevo endpoint, y regeneré `frontend/packages/api/src/generado/consumo.ts`. Actualiza tu rama desde `main` al integrarse. No cambiaron las rutas ni el layout.

## 2026-10-19 · JG-19 · Compuerta sin PostgreSQL ni plano de control
- Hecho: tres casos de integración de RNF-02 y RNF-04 detienen PostgreSQL y verifican el 200 de un origen HTTP real, los rechazos 401 y 403, y el funcionamiento y arranque en frío de la compuerta sin iniciar la API de control ni el trabajador. Los contenedores y datos son exclusivos de las pruebas y se eliminan al terminar.
- Decisiones: se reutiliza el soporte de siembra en Redis y se agrega al proyecto de compuerta la referencia a `Testcontainers.PostgreSql`, ya centralizada. Se conserva la cadena de PostgreSQL antes de detenerlo porque la fábrica configura el host de forma diferida. No se cambiaron comportamiento, contratos, especificaciones ni CI.
- Verificación: la primera compilación de las pruebas señaló la referencia faltante; la primera ejecución pasó dos casos y detectó la lectura del puerto después de detener PostgreSQL en el tercero. Corregido el soporte, pasaron las 288 pruebas de compuerta, sin fallos ni omisiones, con un hilo de pruebas por la memoria disponible. Compilación completa con cero errores y advertencias, formato sin cambios y plan válido. Revisión independiente sin hallazgos de corrección.
- Pendiente o aviso para otros: ninguno.

## 2026-10-20 · JG-15 · Caché de respuestas en la compuerta
- Hecho: filtro 7 para respuestas GET 200 de hasta 1 MB en Redis, con TTL por ruta, HIT/MISS, cuota descontada también en los aciertos y copia acotada mientras YARP transmite al consumidor. Incluye invalidación por versión y pruebas de publicación durante un reenvío.
- Decisiones: la versión se guarda en el hash y un cambio invalida de forma conservadora todas las respuestas de esa API. Cuota, plan y CORS se recalculan por petición. Se omiten cookies, respuestas privadas o que requieren revalidación y variantes por cabecera distintas de Origin; se precisó en 08 §3 y 07 §4. Sin dependencias ni endpoints nuevos.
- Verificación: fase roja confirmada contra la compilación anterior; 22 pruebas nuevas y las 310 pruebas de compuerta aprobadas, sin omisiones y con un hilo por la memoria disponible. Compilación completa con cero errores y advertencias, formato sin cambios y plan válido. Docker estaba detenido al comenzar; tras iniciarlo, la prueba roja falló por la ausencia de caché y luego la implementación pasó. La revisión independiente detectó la reutilización de gzip pese a Vary; corregida con una regresión, la segunda revisión dio LISTO.
- Pendiente o aviso para otros: ninguno.

## 2026-10-23 · JG-14 · Convergencia del Avance 3
- Hecho:
  - Informe en `docs/plan/convergencia/2026-10-22.md`: 46 requisitos (27 ✅, 19 ⚠️, ningún ❌), el guion 3 recorrido en el ambiente productivo simulado y 35 hallazgos (H-C01 a H-C35).
  - Doce tareas nuevas: JG-21 a JG-29, EM-22, DC-18 y JZ-19.
  - Fechas nuevas en `calendario.md`, `lineamientos.md` §9, `01-vision-y-alcance.md` y JG-17.
- Decisiones de Jordin (23 oct):
  - El Avance 3 es un video que se entrega el sábado 31 a las 23:59. Todo termina el jueves 29, incluidos los manuales, y se graba el viernes 30.
  - No hay más auditorías completas: Jordin corrige todos los hallazgos (JG-21 a JG-28).
  - Un elemento de código por persona para el video, hecho por su agente.
- Verificación: `node scripts/tareas.mjs --validar` (plan válido) y `node infra/verificar.mjs --demo` (pasa, salvo la comprobación que depende del volumen de Caddy, H-C31).
- Pendiente o aviso para otros:
  - **Todos:** el Avance 3 ya no es el viernes 23. Es un video de 25 minutos como máximo, el sábado 31 a las 23:59, en el que cada uno narra su parte. Todas las tareas deben estar integradas el jueves 29, y el viernes 30 grabamos.
  - **EM-22:** documenta en `docs/video/elementos/emilio.md` un patrón de diseño y dos características de *Clean Code* de tu código, con el texto que vas a narrar. La hace tu agente. EM-16 sigue el sábado 24.
  - **DC-18:** lo mismo, en `docs/video/elementos/dominique.md`. DC-16 sigue el sábado 24.
  - **JZ-19:** lo mismo, en `docs/video/elementos/jose-pablo.md`, el domingo 25. JG-25 y JG-27 cambian después parte de tu código; JG-29 vuelve a comprobar las líneas citadas el jueves 29.
  - **JZ-15 y JZ-16:** deben terminar el jueves 29. JG-22 a JG-26 cambian pantallas hasta el martes 27, así que toma las capturas del manual de usuario al final. JG-27 (martes 27) precisa `docs/manual-tecnico.md` sobre la siembra y el calentamiento: agrégalo encima de lo que deje JZ-15, sin reescribirlo.
  - **José Pablo:** JG-22 cambia `B3-1-Estado.tsx` y `B3-2-Bitacora.tsx` (sáb 24). JG-25 cambia Soporte, la bitácora y A7.1 (lun 26). JG-26 cambia `A6-2-Organizaciones.tsx` (mar 27). JG-27 cambia `Siembra/Demo/`, `infra/`, `tests/carga/`, los *workflows* y el manual técnico (mar 27). Actualiza tu rama desde `main` antes de tocar esos archivos.
  - **Emilio:** JG-21 cambia `RelojSistema.cs` (vie 23). JG-22 cambia `Sesion.cs`, `RelojDemoApi.cs` y `A2-2` (sáb 24). JG-23 cambia Suscripciones, Pagos, `CierreCiclo.cs` y B1.4 (dom 25). JG-25 cambia `MiembrosApi.cs` y `A4-2` (lun 26). JG-26 cambia `A4-1` (mar 27). Actualiza tu rama desde `main` antes de tocar esos archivos. Si hace falta repartir la carga de Jordin, JG-23 se te puede reasignar.
  - **Dominique:** JG-22 cambia `A5-6`, `B2-4` y `B2-7` (sáb 24). JG-23 cambia B2.3 (dom 25). JG-24 cambia A3.6, la consola, `A5-6`, `B2-3` y `GestionarDominios.cs` (dom 25). JG-26 cambia `A3-3`, `A3-4` y `MarcaPortal.tsx` (mar 27). Actualiza tu rama desde `main` antes de tocar esos archivos. Si hace falta repartir la carga de Jordin, JG-24 se te puede reasignar.

## 2026-10-25 · JG-21 · Timeout de Redis en el reloj y en los trabajos del trabajador
- Hecho: reloj de demostración en memoria, sincronización asíncrona por canal y relectura periódica, y recuperación ante fallos de Redis sin detener el trabajador. El cierre de ciclo recibe la hora actualizada y la siembra carga el desplazamiento antes de iniciar el host.
- Decisiones: el canal lleva el total en segundos, igual que la llave; relectura y reintento de suscripción cada cinco segundos; los mensajes atrasados o duplicados no retroceden el reloj. Fuera de demostración no se abre la sincronización. Se precisaron 09 §9 y 07 §4. Sin dependencias nuevas, cambios de contratos HTTP ni aumentos de los límites de tiempo.
- Verificación: compilación completa con cero errores y advertencias, formato correcto y 1.012 pruebas de la API aprobadas sin omisiones. La prueba con Redis real propagó la hora en 76 ms. Se comprobó que los seis trabajos sobreviven y reintentan ante desconexiones y timeouts, con advertencias del tipo correcto. La regresión de resincronización ahora exige advertencias, dos intentos y ejecución viva, conforme al criterio 3. Ensayo productivo de 627 segundos con navegador, 90 respuestas 200, cero fallos, cero reinicios y cero errores de Redis sin atrapar en la API, incluida una pausa de Redis de 12 segundos. Revisión independiente LISTO tras corregir la inicialización previa a la siembra.
- Pendiente o aviso para otros:
  - **Emilio:** cambié `src/Shapi.Infraestructura/Comun/{RelojSistema,ServiciosComunes}.cs`, `src/Shapi.Api/Suscripciones/RelojDemoApi.cs`, `src/Shapi.Infraestructura/Siembra/Base/SiembraBase.cs`, `TrabajoAvisosCuota.cs` y `TrabajoCierreCiclo.cs`, con sus pruebas. Actualiza tu rama desde `main` al integrarse. El reloj se inicializa antes de sembrar y el canal ahora publica el total en segundos; el cierre de ciclo usa el reloj actualizado y los avisos reintentan ante fallos de Redis. El contrato HTTP no cambia.
  - **José Pablo:** cambié `DespachadorCorreos.cs` y `tests/Shapi.Api.Tests/Siembra/SiembraDemoTests.cs`. Actualiza tu rama desde `main` al integrarse. El despachador registra Warning con el tipo ante fallos de Redis y sigue en el próximo intervalo; la siembra de demostración respeta el desplazamiento existente sin iniciar el host.
  - **Dominique:** cambié `src/Shapi.Trabajador/VerificacionDominios/TrabajoVerificacionDominios.cs` para registrar Warning con el tipo y continuar ante desconexiones y timeouts de Redis. Actualiza tu rama desde `main` al integrarse.
  - **JG-22:** el endpoint del reloj conserva su contrato y actualiza la hora local antes de responder; conserva este orden al modificarlo.

## 2026-10-25 · JG-16 · Generador de PDF y documentos de requisitos y de diseño
- Hecho: generador reutilizable de Markdown a HTML y PDF con React Markdown, Mermaid CLI por npx y Chromium de Playwright, sin LaTeX. PDF de requisitos (37 páginas, 3 diagramas) y diseño (109 páginas, 18 diagramas), carátulas con los integrantes y carnés de la visión, índice navegable, marcadores, encabezado, numeración y tablas con encabezados repetidos.
- Decisiones: se reutilizan las instalaciones existentes del frontend y E2E; Mermaid CLI 11.12.0 fijado y lotes de dos para limitar la memoria. Los diagramas grandes usan A3 horizontal, y UML/MER extensos tienen vista general y detalles solapados. La fecha usa Guatemala; las imágenes de manuales se resuelven desde su Markdown. El runner Linux permite el sandbox del Chromium de Playwright con un perfil AppArmor limitado a la ruta exacta de ese binario, sin desactivar restricciones globales. No se cambiaron dependencias de las aplicaciones, contratos ni decisiones de arquitectura.
- Verificación: 108 pruebas de scripts y 2 integraciones reales aprobadas, sin fallos ni omisiones; plan válido (88 tareas) y ambos comandos de generación correctos. PyMuPDF comprobó contenido, carátulas, numeración y ausencia de texto fuera de página. Se revisaron tres páginas aleatorias por documento, además de carátulas, tablas, diagramas y detalles; las seis capturas y la evidencia están en `docs/pdf/VERIFICACION.md`. La revisión independiente detectó anclas RF/RNF y referencias Markdown perdidas en celdas; corregidas con regresiones, la segunda revisión dio LISTO. El workflow PDF verifica la generación completa en Linux.
- Pendiente o aviso para otros:
  - **JZ-15 y JZ-16:** el generador y las instrucciones de los manuales están en `docs/pdf/README.md`. Usen `node scripts/generar-pdf.mjs docs/pdf/Manual_Tecnico.pdf docs/manual-tecnico.md` y `node scripts/generar-pdf.mjs docs/pdf/Manual_de_Usuario.pdf docs/manual-usuario.md`; las capturas locales se resuelven respecto de la carpeta del Markdown. Actualiza tu rama desde `main` al integrarse.
  - **JG-17:** regenerar ambos documentos y los manuales con el mismo script al congelar el código.


## 2026-10-26 · JG-22 · Modo demostración en la interfaz
- Hecho: tarjeta del reloj en B3.1 solo para administrador/demo; fecha de servidor para la bitácora y las pantallas de contratación, rotación y cambio de plan; sesiones y cookies con hora real en los tres caminos de acceso. Contrato y tipos generados actualizados, sin dependencias nuevas ni migraciones.
- Decisiones: las dos opciones aprobadas por Jordin el 25 oct; avances enteros de 1 a 365 días por operación; previsión de expiración de clave confirmada al rotar. Fechas de negocio con IReloj y vigencia de sesiones con TimeProvider. Precisión en 09 §9, 10 §1, 11 y convenciones §6.
- Verificación: compilación sin errores ni advertencias, formato sin cambios, 344 pruebas de Suscripciones/Identidad y otra ejecución de 110 regresiones aprobadas sin omisiones; 473 pruebas frontend, lint sin errores (11 avisos previos), tipos, builds y plan correctos. La ejecución con recursos saturados agotó esperas; toda la suite frontend pasó después con un hilo y Docker detenido, sin modificar límites ni aislamiento. Ensayo productivo aislado de 34,1 s aprobado: gracia y renovación en 6.600 ms, suspensión/403 en 2.745 ms, pago y 200, con las sesiones abiertas. Once capturas reales y cinco comparaciones de B3 contra sus mockups en `.shapi/jg22/`. Revisión independiente LISTO tras corregir el acceso inicial de cuentas de plataforma y los errores HTTP del reloj.
- Pendiente o aviso para otros:
  - **Emilio:** cambié los manejadores y los helpers de sesión de `src/Shapi.Api/Identidad/`, `Suscripciones/{Endpoints,RelojDemoApi}.cs`, su contrato, tipos y pruebas, y A2.2 y sus fixtures. Las sesiones se crean y validan con TimeProvider, y los planes ya no tienen respaldo con fecha del navegador. Actualiza tu rama desde `main` al integrarse.
  - **Dominique:** cambié `portal/paginas/{A5-6-Pago,B2-3-Suscripcion,B2-4-RotarClave,B2-7-CambioPlan}.tsx`, agregué los módulos de reloj del panel y del portal y actualicé tipos generados y fixtures de rutas/portal. La previsión de la rotación se consulta al servidor y el resultado confirma su fecha definitiva. Actualiza tu rama desde `main` al integrarse.
  - **José Pablo:** cambié B3.1, B3.2, `Administracion/{RepositorioCuentasPlataforma,EndpointsCuentas}.cs` y las pruebas que insertaban sesiones con fecha de negocio. El ensayo está en `tests/e2e/demo/reloj.spec.ts`, con `playwright.demo.config.ts`, separado porque adelanta el reloj global: ejecútalo en un ambiente aislado. Actualiza tu rama desde `main` al integrarse.
  - **JG-29 y JG-17:** el guion 3, paso 4, ya puede recorrerse solo desde la interfaz; las sesiones sobreviven al avance del reloj y la bitácora muestra las acciones de la fecha de demostración.

## 2026-10-26 · JG-23 · Correcciones de cobros y suscripciones
- Hecho: H-C04 a H-C10 corregidos: gracia por reversión con fecha, monto y periodo correctos; aviso de la Prueba para el propietario; consulta y cancelación de bajadas del consumidor; renovaciones con transacciones independientes y compensación del cobro cuyo guardado falla; contratación gratuita sin tarjeta ni pasarela; publicación inmediata de la cuota de plataforma y prueba del precio en la renovación siguiente. Contrato, tipos y especificaciones actualizados.
- Decisiones: Jordin aprobó aplicar la cuota inmediatamente y mostrar el aviso en B1.4 y arriba de las demás páginas del panel durante los últimos siete días, con la fecha del servidor y un enlace a los planes. Sin dependencias nuevas, migraciones ni cambios de ADR. El ensayo nuevo tiene configuración propia porque adelanta el reloj global y necesita una siembra limpia.
- Verificación: compilación completa sin errores ni advertencias, formato correcto, 189 pruebas de integración de los módulos afectados aprobadas sin omisiones, 481 pruebas frontend en 41 archivos, lint sin errores (11 avisos previos), tipos, builds y plan (88 tareas) correctos. La primera ejecución frontend agotó dos esperas en pantallas ajenas y detectó ajustes de MSW en pruebas nuevas; corregidos, la repetición completa pasó sin otras verificaciones pesadas y sin modificar límites ni aislamiento. Ensayo productivo real aprobado en 53,4 s: bajada y cancelación, reversión de Q 173,34, gracia por rechazo, aviso de Prueba y suspensión por reversión. Nueve capturas comparadas con los mockups en `.shapi/jg23/`. Revisión independiente de código LISTO tras corregir dos reversiones del mismo ciclo y separar el ensayo del de JG-22.
- Pendiente o aviso para otros:
  - **Emilio:** cambié `src/Shapi.Api/Suscripciones/{PlataformaSuscripcionesApi,ContratacionApi,Endpoints}.cs`, `Planes/AdministrarPlanesPlataforma.cs`, `src/Shapi.Trabajador/CierreCiclo/CierreCiclo.cs`, sus pruebas, el contrato de suscripciones y A2.2, A2.3 y B1.4. Actualiza tu rama desde `main` al integrarse. Cada renovación confirma y publica por separado; la cuota editada llega inmediatamente a las organizaciones vigentes y la contratación gratuita no usa pasarela.
  - **Dominique:** cambié el layout del panel y su estructura común para el aviso de Prueba, agregué `panel/src/modulos/suscripciones/AvisoPrueba.tsx`, actualicé sus pruebas y B2.3 con el hook de cancelación en `portal/src/modulos/claves/claves.ts`, y regeneré `frontend/packages/api/src/generado/suscripciones.ts`. Actualiza tu rama desde `main` al integrarse. La respuesta de suscripción ahora incluye el cambio programado del consumidor y los datos de reversión y Prueba de plataforma.
  - **José Pablo:** agregué `tests/e2e/demo-suscripciones/suscripciones.spec.ts` y `playwright.suscripciones.config.ts`. Actualiza tu rama desde `main` al integrarse. El ensayo requiere un ambiente recién sembrado y aislado y adelanta el reloj global siete días; no se combina con el ensayo de JG-22. Las capturas del manual pueden mostrar ahora las correcciones de B1.4 y B2.3.
  - **JG-27 y JG-28:** JG-23 deja verificadas las renovaciones y los flujos corregidos; al integrarse queda resuelta esta dependencia.

## 2026-10-26 · JZ-13 · Continuación de las pruebas E2E del PR #119
- Hecho: continuación de los tres commits de José Pablo en una rama propia, según protocolo §E4, con el aviso previo confirmado por Jordin. Se conserva la cobertura de los guiones 2 y 3 y la corrección EM-23, que publica la organización en Redis después de confirmar el registro del proveedor. Se incorporó `main`; el conflicto de `AutenticacionTests.cs` se resolvió conservando `IReloj`, `TimeProvider` y el aislamiento del publicador, sin quitar aserciones.
- Decisiones: el nuevo PR del coordinador sustituye a #119 y conserva su rama. La excepción de propiedad de §E1 y §E5 permite terminar EM-23 en esta misma continuación. Sin dependencias nuevas, migraciones ni cambios de contratos HTTP. El ensayo local usa el proyecto y los volúmenes aislados `shapi-jz13`, porque las E2E adelantan el reloj global 37 días. Se corrigen los comentarios obsoletos sobre la vigencia de las sesiones, que desde JG-22 usa hora real.
- Verificación: compilación completa con 0 errores y 0 advertencias; formato completo correcto; 270 pruebas de Identidad aprobadas, sin fallos ni omisiones; 14 E2E aprobadas en 3,6 minutos, sin fallos, reintentos ni omisiones, sobre las imágenes de esta rama y con siembra terminada en código 0; comprobación estricta de TypeScript de las nuevas E2E y sus ayudantes correcta; plan válido con 89 tareas. Revisión independiente de código y cierre en contexto limpio: LISTO, sin hallazgos de corrección. La publicación con cancelación de la petición queda documentada como observación opcional: si el cliente desconecta después del commit, la resincronización es el respaldo; no se cambia ese comportamiento en esta tarea.
- Pendiente o aviso para otros:
  - **Emilio:** se conserva la corrección de `src/Shapi.Api/Identidad/Endpoints.cs`, la regresión real `tests/Shapi.Api.Tests/Identidad/RegistroCacheTests.cs` y el aislamiento del publicador en `AutenticacionTests.cs` y `RecuperacionTests.cs`. EM-23 queda terminada por el coordinador a partir del trabajo de José Pablo. Actualiza tu rama desde `main` al integrarse.
  - **José Pablo:** continué tus commits del PR #119 en `jordin/JZ-13-flujos-e2e`; se conservan los cambios de `tests/e2e/`, tu entrada histórica de bitácora y el cierre de JZ-13, ahora con la continuación por el coordinador. El PR original se sustituye con un enlace al nuevo, sin modificar ni borrar tu rama. Actualiza tu rama desde `main` al integrarse.
  - **JG-17 y JZ-16:** `pnpm test`, en `tests/e2e`, ejecuta primero los flujos y después el escenario del reloj. Cada ejecución necesita un ambiente recién sembrado y aislado.
