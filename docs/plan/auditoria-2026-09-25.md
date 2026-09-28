# Plan de correcciones de la auditoría del 25 de septiembre de 2026

El 25 de septiembre de 2026 se auditaron las 10 tareas integradas en `main` (JG-01, JG-02, JG-03, EM-01, EM-02, DC-01, DC-02, JZ-01, JZ-02 y JZ-03), la documentación y el plan. Cada tarea se revisó contra su archivo de tarea y contra las especificaciones. Este documento enumera los hallazgos y el orden en que se corrigen.

**Estado de partida.** La compilación, el formato, el lint, el *typecheck* y todas las pruebas pasan: 348 pruebas del backend, 100 del frontend y 15 de `scripts/`. Los hallazgos son incumplimientos de las especificaciones o de los criterios de aceptación que las pruebas no cubrían.

**Cómo se trabaja.**
- Se avanza un paso a la vez. Al terminar cada paso, el agente informa a Jordin y espera su indicación para seguir con el siguiente.
- Cada paso es uno o más PR titulados `[<ID de la tarea original>] Correcciones de la auditoría: <tema>`, igual que el PR #17. Si un paso toca varias tareas, cada PR lleva el ID de la tarea que corrige. Los pasos que terminan un PR abierto (2 y 14) usan el título normal de la tarea (protocolo §E4).
- Cada paso sigue `protocolo.md` §E3, o §E4 si termina un PR abierto:
  1. Crea la rama `jordin/<ID>-auditoria-<tema>` desde `main` actualizado.
  2. Escribe primero las pruebas.
  3. Ejecuta todos los comandos de verificación.
  4. Hace la revisión en contexto limpio con el subagente `revisor`.
  5. Abre el PR y lo integra una vez que la CI está en verde.
- En el mismo PR:
  - se agrega al archivo de la tarea original una subsección `### Correcciones de la auditoría (AAAA-MM-DD)`, con la fecha del PR, dentro de `## Resultado`, que dice qué cambió y por qué;
  - se agrega una entrada en `docs/plan/bitacora/jordin.md`, con un aviso en negrita para la persona dueña del código.
- **Excepción para los pasos 2 y 14 (§E4), que terminan un PR abierto de Emilio:**
  - la rama parte de la del PR de Emilio, no de `main`;
  - el cierre es el normal: `estado: hecha` y un `## Resultado` completo, sin subsección de correcciones;
  - se cierra el PR original con un enlace al nuevo.
- Las dudas marcadas como **❓ Decisión pendiente** se le preguntan a Jordin al empezar ese paso, antes de escribir código.

**Decisiones ya tomadas por Jordin (25 sep):**
- **Integración:** un PR por tarea original, con su mismo ID.
- **Marca en los correos:**
  - los correos del personal (proveedores, administración y soporte) llevan la marca de Shapi;
  - los de los consumidores llevan solo la marca del portal;
  - se corrige 10 §6 (`10-identidad-y-seguridad.md:85`).
- **Alcance:** se incluyen lo integrado en `main`, los PR abiertos #16 (EM-03) y #14 (EM-06), y los huecos de requisitos. La reasignación de DC-03 y DC-04 queda fuera.
- **Automatización:** la revisión con Claude pasa a ser bloqueante.

Cada hallazgo tiene un código (`H-xx`) y una casilla que se marca en el PR que lo corrige.

---

## Paso 1 · [JG-01] Autorización del coordinador y este plan

Autoriza a Jordin a corregir directamente el trabajo de cualquier integrante.

- [x] **H-01** `docs/plan/protocolo.md` §C: agregar la excepción del coordinador a la fila "No lo arregles en su código". Queda así:
  - **Autorización permanente:** Jordin, el coordinador, puede modificar el código, las pruebas, los contratos, los archivos de tarea, las bitácoras y la documentación de cualquier persona. No necesita preguntar antes ni crear una tarea nueva.
  - **Motivo:** la auditoría que hace después de cada integración en `main`.
  - **Forma de hacerlo:** un PR `[<ID>] Correcciones de la auditoría: …`, la subsección de correcciones en el `## Resultado` de la tarea y un aviso en negrita para la persona dueña en `docs/plan/bitacora/jordin.md`.
  - Se agrega una sección nueva, **§E. Auditoría del coordinador después de cada integración** (E1 a E5), con ese procedimiento.
- [x] **H-02** `AGENTS.md`, en "Preguntar antes" y en "Nunca": agregar la excepción. Si la persona es `jordin`, puede modificar archivos ajenos e implementar o corregir tareas de otros, según el protocolo §E.
- [x] **H-03** `docs/plan/convenciones.md` §2 ("Cada persona modifica solo lo suyo") y §3 (archivos calientes): agregar la misma excepción, solo para quién edita; las reglas técnicas de §3 se mantienen. También se alinean `docs/plan/prompts/revision.md` (alcance y cierre) y la plantilla del PR.
- [x] **H-04** `docs/plan/README.md`, regla de oro correspondiente: agregar la misma excepción.
- [x] Integrar este archivo, `docs/plan/auditoria-2026-09-25.md`.

## Paso 2 · [EM-03] Terminar el PR #16: pantallas de registro, verificación y acceso

Este paso afecta el paso 2 del guion de la demostración del Avance 1.

- [x] **H-05** Pruebas con Vitest, Testing Library y MSW de A1.1, A1.2, A1.3 y del destino según el rol, nombradas con RF-01, RF-02 y RF-04. Deben cubrir:
  - errores por campo;
  - navegación a `/verificar-correo?correo=…`;
  - token válido e inválido;
  - reenvío del correo;
  - destino por rol: `proveedor` → `/panel/apis`, `administrador` → `/admin/organizaciones`, `soporte` → `/admin/casos`;
  - cuenta bloqueada;
  - enlace a `/recuperar`.
- [x] **H-06** `A1-2-Verificacion.tsx`: el `useEffect` llama a `mutate` en cada render. El token es de un solo uso, así que se envía varias veces y la pantalla muestra "Enlace no válido" aunque la verificación funcionó. Debe llamarse una sola vez, también con StrictMode.
- [x] **H-07** Quitar los `error as any` (`no-explicit-any`), que hoy ponen en rojo el job `frontend` de la CI.
- [x] **H-08** Actualizar `rutas.test.tsx`, que busca el texto de relleno "A1-x ·", para que busque el contenido real de cada pantalla, sin debilitar la prueba.
- [x] Comparar las pantallas con los mockups de A1 (textos, orden y estados), actualizar la rama desde `main` y agregar la evidencia de `pnpm lint && pnpm typecheck && pnpm test && pnpm build`.
- [x] **H-115** (encontrado en este paso, de DC-01) `packages/ui/src/style.css` no declaraba `@source`. Tailwind v4 solo buscaba clases en la app, así que las que usan únicamente los componentes base (`h-11`, `text-white`…) no se generaban, y en el navegador los campos y botones salían sin tamaño ni color, aunque las pruebas con jsdom pasaban. Se corrigió con una prueba de regresión.
- Según §E4 (decisión de Jordin del 26 sep): se trabaja en una rama nueva, `jordin/EM-03-…`, que parte de `emilio/EM-03-pantallas-registro-acceso` y conserva sus *commits*. Se abre un PR nuevo y se cierra el #16 con un comentario que enlaza al nuevo.
- **❓ Decisión pendiente:** si Jordin ya le avisó a Emilio que no siga trabajando en el #16.

## Paso 3 · [JG-03] La revisión con Claude pasa a ser bloqueante

- [x] **H-09** `revision-claude.yml`: si la revisión tiene hallazgos en "CORRECCIÓN (obligatorio corregir)", el job termina con error. Si solo hay hallazgos opcionales, pasa.
- [x] **H-10** Agregar el job `revision-claude` a los checks obligatorios de la protección de `main`. Aplicado el 26 de septiembre, al integrar el #23: `plan`, `backend`, `frontend` y `revision-claude`.
- [x] **H-11** Actualizar el criterio 5 de JG-03, `protocolo.md` B11 y la guía, y dejar el aviso en la bitácora para todos.
- **Decidido por Jordin (26 sep):**
  - Si la revisión no se completa (cuota, caída o tiempo), el check falla y el PR se bloquea hasta reintentarla.
  - Un falso positivo solo lo desbloquea Jordin, con `gh pr merge <n> --admin --squash`.
- [x] **H-118** (encontrado el 27 sep, en el paso 9) Falso positivo en el #30. La revisión decía `CORRECCIÓN: Ninguno` y `VEREDICTO: LISTO`, pero agregó una sección "Comprobado:" antes de OPCIONAL, y `veredicto-revision.mjs` contó esas líneas como hallazgos. Jordin integró el #30 con `--admin`.
  - **Decidido por Jordin (27 sep):** corregirlo en un PR aparte. Si la sección de corrección empieza con "Ninguno", una sección posterior deja de contar. Con cualquier otro comienzo todo sigue contando. `revision.md` pide poner lo comprobado antes de CORRECCIÓN.

## Paso 4 · [JG-01] Coherencia de las especificaciones y del plan

- [x] **H-12** 10 §5 (`10-identidad-y-seguridad.md:59`), CSP: agregar `'self'` a `font-src`. Las fuentes de `@fontsource` se sirven desde el propio sitio.
- [x] **H-13** Ruta de salud:
  - 06 §4 (`06-arquitectura.md:104`) define `/interno/salud` para la API y 06 §8 (`06-arquitectura.md:379`) define `/salud` para cada proceso;
  - JZ-06:42 espera `https://shapi.localhost/api/salud`, que da 404 a través de Caddy;
  - se unifica la especificación, se corrige JZ-06 y, si hace falta, se agrega `/interno/salud` a la API.
- [x] **H-14** 10 §6 (`10-identidad-y-seguridad.md:85`): los correos del personal llevan la marca de Shapi y los de los consumidores solo la del portal (decisión de Jordin). Hay que alinear JZ-11.
- [x] **H-15** `12-decisiones.md`: el encabezado dice "D1 a D34", pero existe ADR-35. Además, `03-requisitos.md:125,137` citan "D29" en vez de ADR-29.
- [x] **H-16** Versión de Node: `12-decisiones.md:181` dice "Node 22 o posterior", mientras que `instalacion.md`, `manual-tecnico.md`, DC-01 y `package.json` exigen 24.
  - **Decidido por Jordin (26 sep):** manda Node 24; se corrigió ADR-35.
- [x] **H-17** Nombre del Caddyfile: 06:429 y `convenciones.md:73` dicen `infra/caddy/Caddyfile`, pero los archivos reales son `Caddyfile.dev` y `Caddyfile.prod`.
- [x] **H-18** Citas equivocadas: EM-05:34 y `jordin.md:97` citan 10 §6 cuando la regla está en 10 §1.
- [x] **H-19** `contratos/openapi/README.md` asigna `claves.yaml` y `consumo.yaml` a Jordin, pero `convenciones.md` §2 no. Hay que alinearlos.
- [x] **H-20** DC-02:22 dice que el catálogo de 11 §3 trae los responsables, pero no los trae. Se agrega la columna o se corrige la frase.
- [x] **H-21** `specs/README.md:26` ("se agrega aquí primero") contradice protocolo §C ("en el mismo PR"). Hay que alinearlos.
- [x] **H-22** El ejemplo `/rastreo/{guia}` de 08:9 y del glosario no coincide con el origen de demostración, que usa `GET /rastreo?guia=`.
- [x] **H-23** Textos que quedaron atrás:
  - el `## Resultado` de JG-01:86-87 dice que las implementaciones nulas se registran por defecto;
  - JZ-03:59 y `jose-pablo.md:29` dicen "quinto fallo", pero ahora es el sexto;
  - `11-interfaz.md:195` todavía habla de un contrato provisional de sesión;
  - `emilio.md:17` dice que no migra en Development, y sí migra;
  - la línea "Algunos todavía no existen" de `AGENTS.md` (sección Comandos) todavía dice que algunos comandos no existen.
- [x] **H-24** EM-17 dice "crear `identidad.yaml`", pero ya existe; debe decir "modificar". Además, EM-17 no aparece en la tabla del Avance 1 de `calendario.md`.
- [x] **H-25** 07:695 exige que el rol de la aplicación solo tenga INSERT y SELECT sobre `bitacora`, pero ninguna tarea lo pide. Asignado a H-40 (paso 5).
- [x] **H-26** Avisos sin atender que se pasan a los criterios de sus tareas:
  - JG-05: quitar las cookies del portal y leer el contexto en un solo *pipeline* (08 §8); resuelto como dos *pipelines* más el script Lua, por decisión de Jordin del 26 sep;
  - JZ-06: el *healthcheck* con host `localhost` y no publicar el puerto de la API.
- [x] **H-27** JG-04: documentar que la publicación hace `DEL` y luego `HSET` en una transacción. `ContextoApi.ACampos()` omite los campos vacíos, así que sin el `DEL` un secreto borrado seguiría en Redis.
- [x] **H-28** 06 §9, stack: agregar los paquetes que las tareas ya autorizan.
  - NuGet: `EFCore.NamingConventions`, `Microsoft.Extensions.Identity.Core`, `NSubstitute` y `Microsoft.Extensions.Hosting`.
  - npm: `openapi-fetch`, `openapi-typescript`, `msw`, `react-markdown`, `rehype-sanitize`, `@fontsource/*`, `eslint` y `jsdom`.
- [x] **H-29** Colores de la lámina: `mockups/A0/Lamina.dc.html` y unos 20 mockups usan `#1F8A5B` y `#C2481F`, mientras que 11 §1 solo tenía `#146542` y `#8E3315`. **Decidido por Jordin (26 sep):** los colores de los mockups son correctos; 11 §1 agrega el tono base (`--correcto-base` y `--alerta-base`) y conserva el de etiqueta. Los mockups no cambian.

## Paso 5 · [EM-01] Esquema de la base de datos

Todos los cambios del esquema van en una **migración nueva**, porque `Inicial` ya está aplicada en las bases locales.

- [x] **H-30 (crítico)** `caso.numero`: la secuencia debe empezar en 100 e incrementar de 1 en 1, con `DEFAULT nextval`, en lugar del HiLo con bloques de 10 que empieza en 1 (`CasoConfiguracion.cs:41`). Incluye una prueba de que el primer caso es CAS-100. Así se evita el choque con la siembra de JZ-05, que inserta CAS-100 a CAS-104.
- [x] **H-31 (crítico)** Pruebas de las 7 restricciones del criterio 2:
  - propietario único;
  - una suscripción vigente única;
  - una clave activa por tipo;
  - `consumo_diario` con nulos;
  - `num_nonnulls` en `pago`;
  - `num_nonnulls` en `medio_pago`;
  - `es_prueba` único.
- [x] **H-32** Agregar la FK de `plan_siguiente_id` en `suscripcion_plataforma` y `suscripcion_api`.
- [x] **H-33** `Activo` usa `HasDefaultValue(true)` en `PlanApi` y `PlanPlataforma`. Por el *sentinel* de EF, un plan que se inserte con `false` queda guardado como `true`. Se corrige y se agrega una prueba.
- [x] **H-34** `consumo_diario`: renombrar las columnas a `rechazos_401…429` y `origen_2xx…5xx`, como dice 07 §3.
- [x] **H-35** CHECK faltantes:
  - `medio_pago.mes_vencimiento` entre 1 y 12;
  - `ruta.cache_segundos = 0 OR metodo = 'GET'`;
  - `organizacion.nombre` con al menos 2 caracteres;
  - largo de `hist_latencia_*`;
  - tamaño de `portal_logo`.
- [x] **H-36** Los estados deben tener DEFAULT en la base: `'activa'`, `'activo'` y `'borrador'`.
- [x] **H-37** Columnas de auditoría:
  - agregar `creado_en` y `actualizado_en` donde faltan;
  - agregar un `SaveChangesInterceptor` que actualice `actualizado_en`, que hoy nunca cambia.
  - **Decidido por Jordin (26 sep):** según el uso real. Llevan `creado_en` y `actualizado_en` `usuario`, `membresia`, `consumidor`, `token`, `correo_saliente`, `consumo_diario` y `registro_dns_simulado`, y `sesion` suma `actualizado_en`. En `sesion`, `bitacora` y `lote_consolidado`, `creada_en`, `fecha` y `procesado_en` hacen de `creado_en`. Así se precisó en 07 §3.
- [x] **H-38** Ampliar el filtro global por organización a todas las entidades que pertenecen a una organización (10 §1), con sus pruebas. Hoy solo cubre 5.
  - **Decidido por Jordin (26 sep):** entidades directas e indirectas: las que tienen `organizacion_id` y las que pertenecen a una organización por su padre, con subconsultas. Las tablas globales o previas a la sesión quedan sin filtro. La lista está en 10 §2.
- [x] **H-39** Los identificadores deben ser UUID v7 (`Guid.CreateVersion7()`), como pide 07 §3. Hoy los constructores del dominio usan `Guid.NewGuid()`.
- [x] **H-40** `bitacora`:
  - bloquear también TRUNCATE;
  - traducir al español el disparador y su mensaje;
  - cumplir los permisos de 07:695.
  - **Decidido por Jordin (26 sep):** opción (b), solo los disparadores: se precisó 07 §3.6 y no cambian las cadenas de conexión, `compose.yml` ni `.env.example`. La otra opción era (a), dos roles de PostgreSQL.
- [x] **H-41** `EntradaBitacora` usa `DateTimeOffset.UtcNow`. Debe recibir la fecha desde `IReloj` (convenciones §6 y el modo demostración de 09 §9).
- [x] **H-42** Pruebas de la siembra: comprobar los valores de los 5 planes contra 01 §6 y el aviso cuando faltan las variables `SHAPI_ADMIN_*`.
- [x] **H-43** Pruebas de los servicios comunes: comprobar que se inserta la fila en `correo_saliente` y en `bitacora`. Renombrar la prueba `ServiciosComunes_SinImplementacionDelModuloDueno_ResuelvenLasNulas` y decidir qué se hace con `ColaCorreoNula` y `BitacoraNula`, que ya no se registran.
  - **Decidido por Jordin (26 sep):** se borran; eran código muerto.
- [x] **H-44** Borrar los 15 scripts Python de la raíz: `add_fks.py`, los 11 `fix_*.py`, `modificar_migracion.py`, `update_bitacora.py` y `update_correo_saliente.py`.
- [x] **H-45** Quitar `Microsoft.EntityFrameworkCore.InMemory`, que no está autorizada y no se usa.
- [x] **H-46** Quitar `#pragma warning disable CS0618` y usar el constructor no obsoleto de `PostgreSqlBuilder`. Las pruebas de persistencia deben llevar el código de su requisito.
- [x] **H-47** Los nombres de las restricciones usan el prefijo `CK_` en mayúsculas y el resto del esquema usa `snake_case`. Se unifican.
- [x] **H-48** Agregar el `## Resultado` de EM-01, que falta, y corregir la bitácora de Emilio: el nombre del disparador, la afirmación falsa sobre la migración en Development y el formato de los avisos.

## Paso 6 · [EM-02] Seguridad de la identidad del personal

- [x] **H-49** Denegar por defecto (04): `SetFallbackPolicy(RequireAuthenticatedUser)` y `AllowAnonymous` explícito en `registro`, `verificar-correo`, `reenviar-verificacion`, `entrar` y `/salud`, con su prueba.
  - **Decidido por Jordin (26 sep):** se edita `Program.cs` (excepción puntual de convenciones §3) para poner `AllowAnonymous` en `/salud` y en `/openapi`.
- [x] **H-50** Incrementar el contador de intentos fallidos de forma atómica, con `ExecuteUpdate` o un token de concurrencia `xmin`. Incluye una prueba de intentos en paralelo.
- [x] **H-51** Mismo tiempo de respuesta cuando la cuenta existe y cuando no (10 §1). Se evita el `SaveChanges` extra en la ruta de la contraseña incorrecta.
- [x] **H-52** `X-Forwarded-For`: confiar solo en la red del borde (Caddy), no en cualquier red privada.
  - **Decidido por Jordin (26 sep):** variable `SHAPI_REDES_BORDE`. Por defecto incluye la máquina y la red `shapi`, que ahora tiene la subred fija `172.30.0.0/24` en `infra/compose.yml` para que funcione en Linux sin configurar nada.
- [x] **H-53** Límite de reenvíos de verificación por cuenta, además del límite por IP.
  - **Decidido por Jordin (26 sep):** 3 reenvíos por hora; al pasarse responde el mismo 200 sin enviar.
- [x] **H-54** CSRF: validar el esquema y el puerto del `Origin`, no solo el host.
- [x] **H-55** `salir` con la sesión vencida: borrar la cookie y aplicar la política de 04 §3.1.
  - **Decidido por Jordin (26 sep):** `salir` es público y siempre responde 200 y borra la cookie. Cerrar sesión está permitido a todos los roles.
- [x] **H-56** Validar el correo de forma más estricta (hoy acepta `a@b`) y hacer el rehash cuando `SuccessRehashNeeded`.
- [x] **H-57** JSON mal formado: responder 400 con `codigo` y documentarlo en `identidad.yaml`. Alinear también los campos `required` de `PeticionEntrar` y `PeticionReenviar` con lo que hace el backend.
- [x] **H-58** Nombres de las pruebas de CSRF y del límite con su requisito. Pruebas faltantes:
  - 4 fallos, un acierto y otro fallo no bloquean;
  - verificación y registro simultáneos;
  - sesión de una cuenta desactivada;
  - token reenviado;
  - `InactividadHoras` configurable;
  - `salir`;
  - `HashContrasena` nula;
  - `ultimo_uso_en`.
- [x] **H-59** Mover los endpoints de `Modulos/IdentidadModulo.cs` a `Identidad/Endpoints.cs`, como pide `convenciones.md`.
- [x] **H-60** Agregar la entrada de EM-02 que falta en la bitácora de Emilio.

## Paso 7 · [JZ-03] Envío de correos

- [x] **H-61** Tomar los correos pendientes con `FOR UPDATE SKIP LOCKED` para que dos trabajadores no envíen el mismo correo. Incluye una prueba con dos procesadores.
  - **Decidido por Jordin (26 sep):** una transacción por correo. El bloqueo dura solo el envío (el tiempo de espera de SMTP es de 5 s). Si el proceso muere después de enviar y antes de confirmar, el correo se reenvía: la entrega es "al menos una vez".
- [x] **H-62** Quitar el `token` de `correo_saliente.datos` cuando el correo pasa a `enviado` o `fallido` (10 §3 y §8).
- [x] **H-63** Marcar el correo `enviado` en cuanto `SendAsync` termina. Ignorar los errores del `QUIT` y guardar con `CancellationToken.None`, para no reenviar.
- [x] **H-64** Usar `SecureSocketOptions.Auto` para aceptar SMTPS implícito.
  - **Decidido por Jordin (26 sep):** no se usa `Auto`, porque con `SHAPI_SMTP_TLS=true` enviaría en claro si el servidor no ofrece STARTTLS. El cifrado es obligatorio: SMTPS implícito (`SslOnConnect`) en el puerto 465 y STARTTLS en los demás. Así se precisó en 10 §6.
- [x] **H-65** `hostPortal` debe rechazar los subdominios reservados de 06 §4.
  - **Decidido por Jordin (26 sep):** la lista va en `Shapi.Dominio/Apis/SubdominiosReservados.cs`, como única definición, para que DC-04 la reutilice.
- [x] **H-66** Agregar un índice por `(estado, proximo_intento_en)` y la columna `creado_en` en `correo_saliente`. Se coordina con H-37. (`creado_en` ya se agregó en el paso 5 con H-37; aquí se agregó el índice.)
- [x] **H-67** Pruebas faltantes:
  - los 6 intentos hasta `fallido` en el procesador;
  - un `hostPortal` inválido cuenta como intento;
  - el cuerpo entregado (enlace, HTML escapado y parte de texto);
  - autenticación y TLS.

## Paso 8 · [JZ-01] Infraestructura local

- [x] **H-68** Que Compose lea el `.env` de la raíz (con `--env-file .env` en los comandos o de otra forma) y actualizar el manual y `instalacion.md`.
  - **Decidido por Jordin (27 sep):** `--env-file .env` en todos los comandos. Se descartó `include` con `env_file`, porque el `compose.prod.yml` de JZ-06 no vería el `.env` y el proyecto dejaría de llamarse `shapi`.
- [x] **H-69** Hacer configurable el puerto de PostgreSQL con `${SHAPI_POSTGRES_PUERTO:-5432}` y agregar `SHAPI_SECRETO_ORIGEN_ENVIOS` y `SHAPI_SECRETO_ORIGEN_AGRO` a `.env.example`.
- [x] **H-70** Fijar la versión de Mailpit (`v1.27`, igual que en las pruebas) en lugar de `latest`.
- [x] **H-71** Publicar los puertos solo en `127.0.0.1`.
  - **Decidido por Jordin (27 sep):** todos, incluidos el 80 y el 443 de Caddy, porque `*.shapi.localhost` solo resuelve a la propia máquina.
- [x] **H-72** Agregar *healthchecks* a `origen-envios` y `origen-agro` e incluirlos en `infra/verificar.mjs`.
  - **Decidido por Jordin (27 sep):** se instala `curl` en la imagen de los orígenes. El *healthcheck* manda `X-Shapi-Secreto`, porque `/salud` lo exige cuando hay secreto.
- [x] **H-73** Agregar un `.dockerignore` en la raíz y `.shapi/` al `.gitignore`.

## Paso 9 · [JZ-02] Orígenes de demostración

- [x] **H-74** Quitar `/salud` y el parámetro `X-Shapi-Secreto` de `cotizacion-envios.yaml` y de `agro-precios/openapi.yaml`, para que coincidan con A3.3 y A5.1.
  - **Decidido por Jordin (27 sep):** también se quitan las respuestas 401 que solo documentaban el secreto de origen. Se alinearon además los textos que muestran A5.1 e InicioAgro.
- [x] **H-75** Corregir "Secreto de origen invalido" a "inválido".
- [x] **H-76** Hacer que `/precios` de Agro sea coherente con `/historial` para fechas distintas del 10 de septiembre.
  - **Decidido por Jordin (27 sep):** `/precios` y `/historial` leen la misma tabla. Una fecha sin precio responde 404, una mal escrita 400, y la fecha sale siempre como "8 sep 2026".
- [x] **H-77** Pruebas que comparen los ejemplos del OpenAPI con las respuestas reales, que revisen los cuerpos de `/tarifas`, `/rastreo` y `/cobertura`, y que prueben "sin `SECRETO_ORIGEN` acepta todo".
- [x] **H-116** (encontrado en este paso) `/productos` y `/mercados` de Agro listaban tomate, banano, La Terminal y La Democracia, pero solo frijol negro en CENMA tenía precio; lo demás respondía 404. El mockup describe `/productos` como "Lista los productos que tienen precio publicado".
  - **Decidido por Jordin (27 sep):** se agregan precios de demostración para los 3 productos en los 3 mercados y las 3 fechas. Frijol negro en CENMA conserva Q 505, Q 508 y Q 510.
- [x] **H-117** (encontrado en este paso) `POST /cotizaciones` de Envíos respondía siempre Q 38.50 (o Q 57.75 urgente), sin importar el peso.
  - **Decidido por Jordin (27 sep):** la tarifa es `precio_base + precio_por_kg × peso_kg`, con la misma tabla de `/tarifas`. El ejemplo del mockup no cambia: 2.5 kg normal da Q 38.50.

## Paso 10 · [JG-02] Compuerta mínima

- [x] **H-78** Rechazar la clave enviada en la query string con 401, sin reenviarla (08 §1). Silenciar el registro de la URL de destino de YARP.
  - **Decidido por Jordin (27 sep):** se rechaza cualquier nombre o valor de la query con el formato de clave de 08 §2, con 401 `clave_en_url`, aunque también venga `X-Api-Key`. Los demás parámetros se reenvían.
- [x] **H-79** Si Redis no está disponible, responder JSON y no la página de excepciones en HTML.
  - **Decidido por Jordin (27 sep):** 503 `servicio_no_disponible` con `Retry-After: 5`. Se agregó a 08 §4.
- [x] **H-80** Tiempo de espera total de 30 s, no solo de inactividad, y precisar el `ConnectTimeout` en 08 §1.
  - **Decidido por Jordin (27 sep):** la conexión tiene 10 s, antes eran 15 s.
- [x] **H-81** Pruebas faltantes:
  - varias `X-Api-Key`;
  - un 4xx o 5xx del origen se devuelve tal cual;
  - datos incompletos en Redis;
  - `sembrar-demo` desde `Program.cs`;
  - la configuración real del invocador de YARP.

  Además, nombrar las pruebas con su requisito.
- [x] **H-82** En el `## Resultado` de JG-02, dejar escrito que RF-31 quedó parcial: `X-Shapi-Secreto` y la limpieza de `X-Shapi-*` y `X-Forwarded-*` se completan en JG-05.

## Paso 11 · [EM-17] Publicar el contrato de identidad en el frontend

- [x] **H-83** Generar `packages/api/src/generado/identidad.ts` y exportar `"./*"` en `packages/api/package.json`. (Paso 2: `identidad.ts` ya está generado y se exporta como `./identidad`. Falta el `"./*"`.)
- [x] **H-84** Reemplazar el contrato provisional `modulos/sesion/contratoSesion.ts` por los tipos generados, y cerrar EM-17.

## Paso 12 · [DC-01] Sistema de diseño

- [x] **H-85** Quitar `@fontsource/instrument-sans`, que no se usa.
- [x] **H-86** Tokens completos según 11 §1:
  - escala de espaciado y escala tipográfica (32, 22, 16, 15, 12 y 11);
  - tema oscuro sin valores inventados;
  - los tonos base `--correcto-base` (`#1F8A5B`) y `--alerta-base` (`#C2481F`) de 11 §1 (decisión del 26 sep), y `/_ui` con las muestras de la lámina;
  - sin la paleta por defecto de Tailwind (quitar `hover:bg-gray-50`).
  - **Decidido por Jordin (27 sep):** el tema oscuro solo redefine los valores de la columna oscura de 11 §1, y los demás tokens heredan el claro. Si el paso 13 (H-98) necesita más colores oscuros, se toman de los mockups y se agregan a 11 §1.
- [x] **H-87** Medidas de `/_ui` y de los componentes según la lámina:
  - logotipo;
  - tabla: peso, borde y relleno;
  - campo: 48 px de alto, 15 px de texto, anillo de foco y texto guía;
  - botón: texto de 15 px;
  - tarjeta: relleno de 20 px;
  - descripciones de las muestras de color.
- [x] **H-88** Accesibilidad:
  - `Campo`: `aria-invalid`, `aria-describedby` y etiqueta propia;
  - `DialogoConfirmacion`: textos y contenido configurables, foco atrapado, cierre con Escape y `aria-labelledby`;
  - `Selector`: `aria-label` e `id`.
  - **Decidido por Jordin (27 sep):** solo se corrige `packages/ui`. `CampoEtiquetado` y `AvisoError` de A1 (EM-03) se quedan; unificarlos le toca a DC-16.
- [x] **H-89** `EstadoError` y `Aviso` no deben heredar `nowrap`, `uppercase` ni `text-xs`. "Reintentar" debe ser un `Boton` (11 §4).
- [x] **H-90** `generar:api` no debe fallar si la carpeta de contratos no existe.
- [x] **H-91** MSW con `onUnhandledRequest: 'error'`. Los patrones de Vitest también deben incluir `*.test.ts`. Quitar la configuración duplicada de Vitest.
- [x] **H-92** Pruebas de `EstadoCargando`, `EstadoError` y `EstadoSinPermiso`, y de la paleta y los estados en `/_ui`.
- [x] **H-93** Identificadores y comentarios en español según el glosario: `isAdmin`, `navItem`, `headers`, `rows`, `options`, `open`, `onClose`, `onConfirm`, `Suspensify`, `ProblemDetailsError` y los comentarios de `style.css`. El portal no debe mostrar "Portal Base".
  - **Decidido por Jordin (27 sep):** `ProblemDetailsError` pasa a ser la clase `ErrorApi`, con `codigo`, `titulo`, `estado` y `errores` como propiedades directas. El portal muestra "El portal todavía no está disponible." hasta DC-03.

## Paso 13 · [DC-02] Estructura del panel y del sitio público

- [x] **H-94** Sacar A0.1 (`/`) de `LayoutPublico`, como ya se hizo con `/_ui`.
- [x] **H-95** `errorElement`: distinguir un 404 (`isRouteErrorResponse`) de los demás errores y ofrecer "Reintentar".
- [x] **H-96** Rutas índice en `/panel`, `/admin` y `/panel/apis/:id`. El 403 entre áreas debe dar una salida (cerrar sesión o ir a su área). Un 404 dentro del panel debe conservar el layout.
  - **Decidido por Jordin (27 sep):** el 403 entre áreas ofrece "Ir a su panel" (el destino de 10 §1) y "Cerrar sesión".
- [x] **H-97** (Paso 2: el encabezado de A1 ya tiene el `gap` y el `letter-spacing` del mockup.) Medidas de N.1 según el mockup: rellenos de la barra superior y de la lateral, altura de línea, selector con flecha y *hover* del botón de salir. Medidas del encabezado de A1: `gap` de 11 px y `letter-spacing`.
- [x] **H-98** Colores de los layouts con tokens en lugar de hex escritos a mano.
  - Según la decisión del paso 12, los colores oscuros que faltaban se tomaron de N.1, A6 y B3 y se agregaron a 11 §1: `--borde-barra`, `--tinta-rotulo`, `--tinta-navegacion` y `--fondo-activo`.
- [x] **H-99** Usar `h-screen` para que el pie de la barra lateral quede fijo, y hacer que el HMR de Vite funcione también sin Caddy.
- [x] **H-100** Un solo cliente de sesión (hoy `useSesion` y `CerrarSesion` tienen uno cada uno). `SelectorApi` debe reutilizar `Selector`. Agregar `staleTime` a la consulta de sesión.
  - **Decidido por Jordin (27 sep):** 5 minutos.
- [x] **H-101** Limitar `/_ui` al entorno de desarrollo.
- [x] **H-102** Pruebas:
  - el orden completo de N.1 y A6;
  - los textos de B3;
  - un 501 en el selector se trata como "Sin APIs";
  - A0.1 sin el encabezado de A1;
  - un error que no es 404.

## Paso 14 · [EM-06] Terminar el PR #14: pasarela de pagos simulada

- [x] **H-103** Las pruebas obligatorias en `tests/`, con NSubstitute, nombradas con RF-20. Deben cubrir:
  - cada fila de la tabla de 09 §2;
  - Luhn;
  - longitudes de 13 a 19;
  - las tres marcas y `marca_no_soportada`;
  - vencimientos;
  - `cvv_invalido`;
  - los formatos `ch_sim_` y `re_sim_`;
  - `pasarela_no_disponible`.
- [x] **H-104** Detectar las tarjetas especiales por el número completo, no por los últimos 4 dígitos.
- [x] **H-105** Quitar `generar_pagos.py` y el resto de los hallazgos obligatorios de la revisión automática, y actualizar la rama desde `main`.
- Igual que en el paso 2 (§E4): una rama nueva, `jordin/EM-06-…`, que parte de `emilio/EM-06-pasarela-de-pagos`, un PR nuevo y se cierra el #14 con un comentario que enlaza al nuevo.
- **Decidido (27 sep):** Jordin ya le avisó a Emilio que no siga trabajando en el #14.
- [x] **H-119** (encontrado en este paso, de la especificación y de los mockups) Las tarjetas de ejemplo de 09 §2, que están en A2 y A5 (`4024 0071 2244 4821` y `5412 7534 1209 3057`), no pasan Luhn, así que la pasarela las rechazaría con `numero_invalido`. Se cambió un dígito del medio y se conservaron los últimos 4 (`4024 0071 2284 4821` y `5412 7534 1203 3057`), en 09 §2 y en los mockups.
- **Decidido en este paso (27 sep), y agregado a 09 §2:**
  - las tres tarjetas especiales llevan su comportamiento en el token (`tok_sim_0002_`, `tok_sim_0069_` y `tok_sim_0341_`), porque el Trabajador cobra las renovaciones en otro proceso;
  - sin `Pagos:DemoraMs`, la demora es al azar entre 300 y 800 ms; con un valor, es ese valor;
  - el mes actual del vencimiento es el de America/Guatemala;
  - `0341` se rechaza en las renovaciones con `fondos_insuficientes`.

## Paso 15 · [JG-01] CI y reglas del repositorio

- [x] **H-106** En el job `backend`: `dotnet ef migrations has-pending-model-changes`.
- [x] **H-107** En el job `frontend`: `pnpm generar:api` y luego `git diff --exit-code` sobre `generado/`.
- [x] **H-108** `timeout-minutes` en los jobs. El evento `edited` solo debe volver a ejecutar la CI cuando cambia el título.
- [x] **H-109** Fijar por SHA las acciones de terceros que reciben secretos.
- [x] **H-110** `.claude/settings.json`: negar también `git push origin HEAD:main` y sus variantes.
  - **Decidido en el paso 3 (26 sep):** `enforce_admins` queda desactivado, porque Jordin desbloquea los falsos positivos de `revision-claude` con `--admin`.
- [x] **H-111** La plantilla de PR debe pedir la evidencia de `dotnet format`, `pnpm build` y `tareas.mjs --validar`. Completar el `README.md` con las carpetas y los comandos de arranque.
- [x] **H-112** Ejecutar las pruebas en paralelo es inestable con Docker en Windows (se vio en la auditoría). Se evalúa limitar el paralelismo entre proyectos o documentarlo.
- **Decidido en este paso (27 sep):**
  - H-108: el título lo valida `titulo-pr.yml` (check obligatorio `titulo`) y `ci.yml` deja de escuchar `edited`. No se usa `if` en los jobs, porque un check obligatorio omitido cuenta como aprobado.
  - H-112: se documenta `dotnet test Shapi.slnx -m:1` para Windows; la CI no cambia.

## Paso 16 · [JG-01] Huecos de requisitos (tareas nuevas según §C)

- [x] **H-113** Crear tareas para RNF-06 (disponibilidad del 99.5 %) y RNF-11 (publicar en menos de 5 minutos). (RNF-06 y RNF-11 quedaron sin tarea: ver la decisión de abajo.) Agregar RNF-13 a los metadatos de JG-02.
- [x] **H-114** `calendario.md:102` dice que las tareas P1 cubren los lineamientos obligatorios. Hoy solo los cubren tareas P2:
  - RF-06: EM-12;
  - RF-11 y RF-12: DC-14;
  - RF-36: JG-13;
  - RF-39: JZ-12;
  - RF-43: EM-12 y EM-13.
- **Decidido (27 sep):**
  - RNF-06 no lleva tarea: queda como objetivo de diseño para un despliegue real, respaldado por JZ-06, RNF-04 y JZ-12, y no se mide en el ambiente simulado (03 §2 y §4). Se descartó una sonda de disponibilidad, porque el ambiente no corre de forma continua y la medición no cabía antes del PDF;
  - RNF-11 tampoco lleva tarea: queda como objetivo de diseño, respaldado por un flujo sin pasos manuales ni aprobaciones (03 §2 y §4). La tarea JZ-18, una prueba E2E cronometrada, se creó en el PR #38 y se borró en el PR siguiente, porque no es un lineamiento y podía atrasar el avance final;
  - no se sube ninguna prioridad: `calendario.md` precisa que las P1 cubren el mínimo de cada lineamiento y nombra las P2 que completan la trazabilidad.

## Paso 17 · [JG-01] Auditoría final

- [x] Ejecutar la verificación completa: build, formato, pruebas, lint, typecheck, build del frontend, `scripts/` y `--validar`.
- [x] Volver a auditar todas las tareas hechas contra su archivo de tarea y contra las especificaciones, en contexto limpio.
- [x] Anotar el resultado final en este documento y en la bitácora de Jordin.

**Verificación antes de corregir (27 sep).** Todo pasó: `--validar` (66 tareas), 55 pruebas de `scripts/`, compilación sin advertencias, formato, migraciones al día, 584 pruebas del backend (`-m:1`), tipos generados al día, lint, *typecheck*, 189 pruebas del frontend y su *build*.

**Auditoría.** Se auditaron en contexto limpio las 13 tareas hechas, con 9 subagentes (EM-01 · EM-02 y EM-17 · EM-03 · EM-06 · DC-01 y DC-02 · JG-01 y JG-03 · JG-02 · JZ-01 y JZ-02 · JZ-03). No volvió a aparecer ninguno de los hallazgos H-01 a H-119. Salieron 25 hallazgos nuevos (H-120 a H-144), ninguno de severidad alta, y 21 dudas. Las que Jordin decidió resolver quedaron como H-145 a H-151; las demás se quedan como están.

**Decidido (27 sep):**
- Se corrige todo en un solo paso y en un solo PR, con el ID de JG-01, en vez de un PR por tarea original. Cada archivo de tarea lleva su subsección de correcciones y la bitácora de Jordin lleva los avisos.
- Las dudas se resuelven con las recomendaciones del agente, salvo la de Vite (ver abajo).
- **Se quedan como están:**
  - Vite escucha en `0.0.0.0`: en Linux, Caddy llega a Vite por el puente de Docker, y con `127.0.0.1` se rompería el panel detrás de Caddy (manual técnico);
  - los textos de las páginas de relleno de DC-02;
  - `api.especificacion` no lleva un CHECK de 2 MB: la valida la aplicación (DC-05);
  - `/historial` de Agro responde 200 con `precios: []` para un producto o un mercado sin datos;
  - la pasarela simulada acepta tokens que no reconoce, porque 09 §2 no lo define.

### Hallazgos

- [x] **H-120** (media, EM-03) Con `correo_ya_registrado`, A1.1 no ofrecía los enlaces a entrar y a recuperar la contraseña de CU-01 2a (`A1-1-Registro.tsx:30`). Ahora aparecen debajo del correo, y 11 §4 lo precisa.
- [x] **H-121** (media, EM-03) Faltaban pruebas de estos casos:
  - los estados de A1.2: «Confirmando su correo», «Enlace no válido» y «No se pudo confirmar su correo»;
  - los textos del correo enviado y del plan Prueba;
  - el fallo del reenvío en las dos variantes;
  - un error con código que no es de campo en A1.1 (429).
- [x] **H-122** (media, JZ-02) La descripción de las dos APIs y la de /guias, /rastreo y /cobertura no coincidían con A5.0 ni con InicioAgro. Se copiaron del mockup, con prueba.
- [x] **H-123** (baja, EM-01) `organizacion.nombre`, `caso.asunto` y `api.portal_bienvenida` eran `varchar(n)`, y 07 §3 pide `text`. Ahora son `text`, con el largo en un CHECK (migración `TextoConLargoEnCheck`).
- [x] **H-124** (baja, EM-01) `lote_consolidado.procesado_en`, que hace de `creado_en`, no tenía `DEFAULT now()`.
- [x] **H-125** (baja, EM-01) La siembra guardaba al administrador y su membresía en dos `SaveChanges`. Si fallaba entre los dos, el administrador quedaba sin organización para siempre. Ahora:
  - usa una transacción;
  - completa la membresía que falte, con la contraseña y la verificación del correo si también faltan;
  - nunca convierte en administrador a un usuario de otra organización, y avisa en el registro si el correo ya es de otra;
  - crea las entidades con los constructores del dominio, así que los identificadores son UUID v7.
- [x] **H-126** (baja, EM-02) Con peticiones simultáneas se podía pasar el límite de 3 reenvíos por hora. Ahora el reenvío bloquea la fila del usuario (`FOR UPDATE`) dentro de una transacción.
- [x] **H-127** (baja, EM-02) Solo se probaba el límite por IP de `entrar`. Ahora una teoría cubre los 4 endpoints.
- [x] **H-128** (baja, EM-02) Faltaba la prueba del 403 `cuenta_desactivada` de `verificar-correo`.
- [x] **H-129** (baja, EM-17) La prueba de cerrar sesión decía RF-07. Ahora dice RF-04.
- [x] **H-130** (baja, EM-06) El aviso de la bitácora decía que EM-09 registraba la pasarela en el Trabajador, pero las renovaciones las cobra EM-10. Se corrigió el aviso y se agregó a EM-10.
- [x] **H-131** (baja, EM-06) `PasarelaSimulada` escribía los códigos de error como texto. Ahora usa `CodigosError`.
- [x] **H-132** (baja, DC-01) Los botones de `DialogoConfirmacion` no llevaban `type="button"`, así que dentro de un formulario también lo enviaban.
- [x] **H-133** (baja, DC-01) `/_ui` no mostraba el campo enfocado de la lámina, y los rótulos y códigos de la superficie oscura no usaban `--tinta-rotulo`.
- [x] **H-134** (baja, JG-01) El `## Resultado` decía «12 códigos» de 08 §4, y hoy son 14.
- [x] **H-135** (baja, JG-03) El criterio 1 decía `@v1`, pero desde H-109 la acción está fijada por SHA.
- [x] **H-136** (baja, JG-01) 06 §9 no listaba `tablero.mjs` ni `veredicto-revision.mjs` en `scripts/`.
- [x] **H-137** (baja, JG-03) Ninguna prueba vigilaba el cableado del check obligatorio `revision-claude`. Ahora una prueba comprueba:
  - que el job no tiene `if`;
  - que la acción tiene `continue-on-error`;
  - que el veredicto se decide con `!cancelled()`.
- [x] **H-138** (baja, JG-02) Un comentario de prueba citaba 10 §7 en vez de convenciones §6.
- [x] **H-139** (baja, JG-02) Ninguna prueba comprobaba que una clave enviada en la query no quedara en los registros.
- [x] **H-140** (baja, JZ-02) `/cotizaciones` aceptaba cualquier `tipo_servicio`. Ahora responde 400 si no es normal ni urgente.
- [x] **H-141** (baja, JZ-02) `/cobertura` devolvía `dias_habiles: 2` para un municipio sin cobertura. Ahora devuelve `null`, y está documentado en el contrato.
- [x] **H-142** (baja, JZ-01) El manual técnico pedía `hmr.clientPort: 443`, lo que contradecía H-99.
- [x] **H-143** (baja, JZ-01) El manual, `instalacion.md` y el README no decían cómo pasar el `.env` a `dotnet run`. Ahora tienen los comandos de PowerShell y de bash, y los dos se probaron.
- [x] **H-144** (baja, JZ-03) DC-04 no decía que tenía que reutilizar `SubdominiosReservados`.
- [x] **H-145** (media, JG-01) 06 §8 pide registros JSON en los tres procesos, y ninguno lo hacía. Ahora `Logging:Console:FormatterName` es `json` en los tres `appsettings.json`, con prueba.
- [x] **H-146** (baja, JZ-03) Los correos del personal salían sin nombre de remitente. Ahora salen como «Shapi», y 10 §6 lo precisa.
- [x] **H-147** (baja, JZ-11) JZ-11 precisa dos cosas:
  - que también se agrega la marca a `verificacion_correo` y `recuperacion`;
  - de dónde salen el color y el logotipo del portal.
- [x] **H-148** (baja, especificaciones) Se precisaron tres textos:
  - CSRF, en 10 §1, convenciones §5 y el criterio 5 de EM-02: «método no seguro», como hace el código;
  - 08 §8: un invocador de YARP con *pooling* por destino;
  - 06 §4: se quitó `caddy trust` dentro del contenedor, que no sirve para el navegador.
- [x] **H-149** (baja, EM-03) CU-02 paso 3 decía «o a la página desde la que llegó», y ninguna tarea lo pide. Ahora remite al destino del rol de 10 §1.
- [x] **H-150** (baja, DC-04) DC-04 completa `apis.yaml`: `security`, 401 y `required`.
- [ ] **H-151** (baja, JG-03) Negar `gh pr merge *--admin*` a los agentes en `.claude/settings.json`, para que solo Jordin integre un falso positivo. **No se aplicó en este PR:** el modo automático de Claude Code bloquea que un agente edite sus propios permisos. Lo aplica Jordin a mano.

**Verificación después de corregir (27 sep).** Todo pasó: `--validar` (66 tareas), 56 pruebas de `scripts/`, compilación sin advertencias, formato, migraciones al día, 605 pruebas del backend (`-m:1`: Api 403, Compuerta 120, OrígenesDemo 48 y Dominio 34), tipos generados al día, lint, *typecheck*, 196 pruebas del frontend y su *build*. La revisión en contexto limpio (subagente `revisor`) dio `VEREDICTO: LISTO`, y también se aplicaron sus 5 sugerencias opcionales.

**Resultado final.** Con este PR, lo integrado cumple sus tareas y las especificaciones, salvo H-151, que queda en manos de Jordin.
