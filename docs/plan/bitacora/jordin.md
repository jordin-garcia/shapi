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
  - **JZ-06:** la API y el trabajador deben montar el mismo volumen `dpkeys` con el mismo `SHAPI_DPKEYS_DIR`. Sin eso, el trabajador no descifra los secretos y la resincronización omite esas APIs (lo registra como error).
  - **JG-05 y JG-07:** lean `susc:` y `org:` con `ContextoSuscripcion.DesdeCampos` y `ContextoOrganizacion.DesdeCampos`, y las rutas con `RutaCache.Deserializar`. Una `susc:` que no existe equivale a una suscripción inactiva. JG-07 publica con `PublicarClave`, `ExpirarClave` y `EliminarClave`.
