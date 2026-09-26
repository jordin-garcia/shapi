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

- [ ] **H-05** Pruebas con Vitest, Testing Library y MSW de A1.1, A1.2, A1.3 y del destino según el rol, nombradas con RF-01, RF-02 y RF-04. Deben cubrir:
  - errores por campo;
  - navegación a `/verificar-correo?correo=…`;
  - token válido e inválido;
  - reenvío del correo;
  - destino por rol: `proveedor` → `/panel/apis`, `administrador` → `/admin/organizaciones`, `soporte` → `/admin/casos`;
  - cuenta bloqueada;
  - enlace a `/recuperar`.
- [ ] **H-06** `A1-2-Verificacion.tsx`: el `useEffect` llama a `mutate` en cada render. El token es de un solo uso, así que se envía varias veces y la pantalla muestra "Enlace no válido" aunque la verificación funcionó. Debe llamarse una sola vez, también con StrictMode.
- [ ] **H-07** Quitar los `error as any` (`no-explicit-any`), que hoy ponen en rojo el job `frontend` de la CI.
- [ ] **H-08** Actualizar `rutas.test.tsx`, que busca el texto de relleno "A1-x ·", para que busque el contenido real de cada pantalla, sin debilitar la prueba.
- [ ] Comparar las pantallas con los mockups de A1 (textos, orden y estados), actualizar la rama desde `main` y agregar la evidencia de `pnpm lint && pnpm typecheck && pnpm test && pnpm build`.
- Según §E4 (decisión de Jordin del 26 sep): se trabaja en una rama nueva, `jordin/EM-03-…`, que parte de `emilio/EM-03-pantallas-registro-acceso` y conserva sus *commits*. Se abre un PR nuevo y se cierra el #16 con un comentario que enlaza al nuevo.
- **❓ Decisión pendiente:** si Jordin ya le avisó a Emilio que no siga trabajando en el #16.

## Paso 3 · [JG-03] La revisión con Claude pasa a ser bloqueante

- [ ] **H-09** `revision-claude.yml`: si la revisión tiene hallazgos en "CORRECCIÓN (obligatorio corregir)", el job termina con error. Si solo hay hallazgos opcionales, pasa.
- [ ] **H-10** Agregar el job `revision-claude` a los checks obligatorios de la protección de `main`.
- [ ] **H-11** Actualizar el criterio 5 de JG-03, `protocolo.md` B11 y la guía, y dejar el aviso en la bitácora para todos.
- **❓ Decisión pendiente:**
  - Qué pasa cuando la revisión falla por un motivo externo (cuota agotada o caída del servicio): ¿bloquea o deja pasar con un aviso?
  - Quién puede saltarse el bloqueo ante un falso positivo, y cómo.

## Paso 4 · [JG-01] Coherencia de las especificaciones y del plan

- [ ] **H-12** 10 §5 (`10-identidad-y-seguridad.md:59`), CSP: agregar `'self'` a `font-src`. Las fuentes de `@fontsource` se sirven desde el propio sitio.
- [ ] **H-13** Ruta de salud:
  - 06 §4 (`06-arquitectura.md:104`) define `/interno/salud` para la API y 06 §8 (`06-arquitectura.md:379`) define `/salud` para cada proceso;
  - JZ-06:42 espera `https://shapi.localhost/api/salud`, que da 404 a través de Caddy;
  - se unifica la especificación, se corrige JZ-06 y, si hace falta, se agrega `/interno/salud` a la API.
- [ ] **H-14** 10 §6 (`10-identidad-y-seguridad.md:85`): los correos del personal llevan la marca de Shapi y los de los consumidores solo la del portal (decisión de Jordin). Hay que alinear JZ-11.
- [ ] **H-15** `12-decisiones.md`: el encabezado dice "D1 a D34", pero existe ADR-35. Además, `03-requisitos.md:125,137` citan "D29" en vez de ADR-29.
- [ ] **H-16** Versión de Node: `12-decisiones.md:181` dice "Node 22 o posterior", mientras que `instalacion.md`, `manual-tecnico.md`, DC-01 y `package.json` exigen 24.
  - **❓ Decisión pendiente:** cambiar una decisión de `12-decisiones.md` está en "Preguntar antes". Se confirma que manda Node 24.
- [ ] **H-17** Nombre del Caddyfile: 06:429 y `convenciones.md:73` dicen `infra/caddy/Caddyfile`, pero los archivos reales son `Caddyfile.dev` y `Caddyfile.prod`.
- [ ] **H-18** Citas equivocadas: EM-05:34 y `jordin.md:97` citan 10 §6 cuando la regla está en 10 §1.
- [ ] **H-19** `contratos/openapi/README.md` asigna `claves.yaml` y `consumo.yaml` a Jordin, pero `convenciones.md` §2 no. Hay que alinearlos.
- [ ] **H-20** DC-02:22 dice que el catálogo de 11 §3 trae los responsables, pero no los trae. Se agrega la columna o se corrige la frase.
- [ ] **H-21** `specs/README.md:26` ("se agrega aquí primero") contradice protocolo §C ("en el mismo PR"). Hay que alinearlos.
- [ ] **H-22** El ejemplo `/rastreo/{guia}` de 08:9 y del glosario no coincide con el origen de demostración, que usa `GET /rastreo?guia=`.
- [ ] **H-23** Textos que quedaron atrás:
  - el `## Resultado` de JG-01:86-87 dice que las implementaciones nulas se registran por defecto;
  - JZ-03:59 y `jose-pablo.md:29` dicen "quinto fallo", pero ahora es el sexto;
  - `11-interfaz.md:195` todavía habla de un contrato provisional de sesión;
  - `emilio.md:17` dice que no migra en Development, y sí migra;
  - la línea "Algunos todavía no existen" de `AGENTS.md` (sección Comandos) todavía dice que algunos comandos no existen.
- [ ] **H-24** EM-17 dice "crear `identidad.yaml`", pero ya existe; debe decir "modificar". Además, EM-17 no aparece en la tabla del Avance 1 de `calendario.md`.
- [ ] **H-25** 07:695 exige que el rol de la aplicación solo tenga INSERT y SELECT sobre `bitacora`, pero ninguna tarea lo pide. Se asigna en el paso 5.
- [ ] **H-26** Avisos sin atender que se pasan a los criterios de sus tareas:
  - JG-05: quitar las cookies del portal y leer el contexto en un solo *pipeline* (08 §8);
  - JZ-06: el *healthcheck* con host `localhost` y no publicar el puerto de la API.
- [ ] **H-27** JG-04: documentar que la publicación hace `DEL` y luego `HSET` en una transacción. `ContextoApi.ACampos()` omite los campos vacíos, así que sin el `DEL` un secreto borrado seguiría en Redis.
- [ ] **H-28** 06 §9, stack: agregar los paquetes que las tareas ya autorizan.
  - NuGet: `EFCore.NamingConventions`, `Microsoft.Extensions.Identity.Core`, `NSubstitute` y `Microsoft.Extensions.Hosting`.
  - npm: `openapi-fetch`, `openapi-typescript`, `msw`, `react-markdown`, `rehype-sanitize`, `@fontsource/*`, `eslint` y `jsdom`.
- [ ] **H-29** Colores de la lámina: `mockups/A0/Lamina.dc.html` usa Correcto `#1F8A5B` y Alerta `#C2481F`, mientras que 11 §1 usa `#146542` y `#8E3315`. Según §C manda la especificación, así que se documenta la contradicción en 11 §1 y se corrige el mockup si su formato lo permite.

## Paso 5 · [EM-01] Esquema de la base de datos

Todos los cambios del esquema van en una **migración nueva**, porque `Inicial` ya está aplicada en las bases locales.

- [ ] **H-30 (crítico)** `caso.numero`: la secuencia debe empezar en 100 e incrementar de 1 en 1, con `DEFAULT nextval`, en lugar del HiLo con bloques de 10 que empieza en 1 (`CasoConfiguracion.cs:41`). Incluye una prueba de que el primer caso es CAS-100. Así se evita el choque con la siembra de JZ-05, que inserta CAS-100 a CAS-104.
- [ ] **H-31 (crítico)** Pruebas de las 7 restricciones del criterio 2:
  - propietario único;
  - una suscripción vigente única;
  - una clave activa por tipo;
  - `consumo_diario` con nulos;
  - `num_nonnulls` en `pago`;
  - `num_nonnulls` en `medio_pago`;
  - `es_prueba` único.
- [ ] **H-32** Agregar la FK de `plan_siguiente_id` en `suscripcion_plataforma` y `suscripcion_api`.
- [ ] **H-33** `Activo` usa `HasDefaultValue(true)` en `PlanApi` y `PlanPlataforma`. Por el *sentinel* de EF, un plan que se inserte con `false` queda guardado como `true`. Se corrige y se agrega una prueba.
- [ ] **H-34** `consumo_diario`: renombrar las columnas a `rechazos_401…429` y `origen_2xx…5xx`, como dice 07 §3.
- [ ] **H-35** CHECK faltantes:
  - `medio_pago.mes_vencimiento` entre 1 y 12;
  - `ruta.cache_segundos = 0 OR metodo = 'GET'`;
  - `organizacion.nombre` con al menos 2 caracteres;
  - largo de `hist_latencia_*`;
  - tamaño de `portal_logo`.
- [ ] **H-36** Los estados deben tener DEFAULT en la base: `'activa'`, `'activo'` y `'borrador'`.
- [ ] **H-37** Columnas de auditoría:
  - agregar `creado_en` y `actualizado_en` donde faltan;
  - agregar un `SaveChangesInterceptor` que actualice `actualizado_en`, que hoy nunca cambia.
- [ ] **H-38** Ampliar el filtro global por organización a todas las entidades que pertenecen a una organización (10 §1), con sus pruebas. Hoy solo cubre 5.
- [ ] **H-39** Los identificadores deben ser UUID v7 (`Guid.CreateVersion7()`), como pide 07 §3. Hoy los constructores del dominio usan `Guid.NewGuid()`.
- [ ] **H-40** `bitacora`:
  - bloquear también TRUNCATE;
  - traducir al español el disparador y su mensaje;
  - cumplir los permisos de 07:695.
  - **❓ Decisión pendiente:** separar el rol que migra del rol de la aplicación afecta a las cadenas de conexión, a `compose.yml` y a `.env.example`. Opciones:
    - (a) dos roles de PostgreSQL;
    - (b) solo los disparadores, y precisar la especificación.
- [ ] **H-41** `EntradaBitacora` usa `DateTimeOffset.UtcNow`. Debe recibir la fecha desde `IReloj` (convenciones §6 y el modo demostración de 09 §9).
- [ ] **H-42** Pruebas de la siembra: comprobar los valores de los 5 planes contra 01 §6 y el aviso cuando faltan las variables `SHAPI_ADMIN_*`.
- [ ] **H-43** Pruebas de los servicios comunes: comprobar que se inserta la fila en `correo_saliente` y en `bitacora`. Renombrar la prueba `ServiciosComunes_SinImplementacionDelModuloDueno_ResuelvenLasNulas` y decidir qué se hace con `ColaCorreoNula` y `BitacoraNula`, que ya no se registran.
- [ ] **H-44** Borrar los 15 scripts Python de la raíz: `add_fks.py`, los 11 `fix_*.py`, `modificar_migracion.py`, `update_bitacora.py` y `update_correo_saliente.py`.
- [ ] **H-45** Quitar `Microsoft.EntityFrameworkCore.InMemory`, que no está autorizada y no se usa.
- [ ] **H-46** Quitar `#pragma warning disable CS0618` y usar el constructor no obsoleto de `PostgreSqlBuilder`. Las pruebas de persistencia deben llevar el código de su requisito.
- [ ] **H-47** Los nombres de las restricciones usan el prefijo `CK_` en mayúsculas y el resto del esquema usa `snake_case`. Se unifican.
- [ ] **H-48** Agregar el `## Resultado` de EM-01, que falta, y corregir la bitácora de Emilio: el nombre del disparador, la afirmación falsa sobre la migración en Development y el formato de los avisos.

## Paso 6 · [EM-02] Seguridad de la identidad del personal

- [ ] **H-49** Denegar por defecto (04): `SetFallbackPolicy(RequireAuthenticatedUser)` y `AllowAnonymous` explícito en `registro`, `verificar-correo`, `reenviar-verificacion`, `entrar` y `/salud`, con su prueba.
- [ ] **H-50** Incrementar el contador de intentos fallidos de forma atómica, con `ExecuteUpdate` o un token de concurrencia `xmin`. Incluye una prueba de intentos en paralelo.
- [ ] **H-51** Mismo tiempo de respuesta cuando la cuenta existe y cuando no (10 §1). Se evita el `SaveChanges` extra en la ruta de la contraseña incorrecta.
- [ ] **H-52** `X-Forwarded-For`: confiar solo en la red del borde (Caddy), no en cualquier red privada.
- [ ] **H-53** Límite de reenvíos de verificación por cuenta, además del límite por IP.
- [ ] **H-54** CSRF: validar el esquema y el puerto del `Origin`, no solo el host.
- [ ] **H-55** `salir` con la sesión vencida: borrar la cookie y aplicar la política de 04 §3.1.
- [ ] **H-56** Validar el correo de forma más estricta (hoy acepta `a@b`) y hacer el rehash cuando `SuccessRehashNeeded`.
- [ ] **H-57** JSON mal formado: responder 400 con `codigo` y documentarlo en `identidad.yaml`. Alinear también los campos `required` de `PeticionEntrar` y `PeticionReenviar` con lo que hace el backend.
- [ ] **H-58** Nombres de las pruebas de CSRF y del límite con su requisito. Pruebas faltantes:
  - 4 fallos, un acierto y otro fallo no bloquean;
  - verificación y registro simultáneos;
  - sesión de una cuenta desactivada;
  - token reenviado;
  - `InactividadHoras` configurable;
  - `salir`;
  - `HashContrasena` nula;
  - `ultimo_uso_en`.
- [ ] **H-59** Mover los endpoints de `Modulos/IdentidadModulo.cs` a `Identidad/Endpoints.cs`, como pide `convenciones.md`.
- [ ] **H-60** Agregar la entrada de EM-02 que falta en la bitácora de Emilio.

## Paso 7 · [JZ-03] Envío de correos

- [ ] **H-61** Tomar los correos pendientes con `FOR UPDATE SKIP LOCKED` para que dos trabajadores no envíen el mismo correo. Incluye una prueba con dos procesadores.
- [ ] **H-62** Quitar el `token` de `correo_saliente.datos` cuando el correo pasa a `enviado` o `fallido` (10 §3 y §8).
- [ ] **H-63** Marcar el correo `enviado` en cuanto `SendAsync` termina. Ignorar los errores del `QUIT` y guardar con `CancellationToken.None`, para no reenviar.
- [ ] **H-64** Usar `SecureSocketOptions.Auto` para aceptar SMTPS implícito.
- [ ] **H-65** `hostPortal` debe rechazar los subdominios reservados de 06 §4.
- [ ] **H-66** Agregar un índice por `(estado, proximo_intento_en)` y la columna `creado_en` en `correo_saliente`. Se coordina con H-37.
- [ ] **H-67** Pruebas faltantes:
  - los 6 intentos hasta `fallido` en el procesador;
  - un `hostPortal` inválido cuenta como intento;
  - el cuerpo entregado (enlace, HTML escapado y parte de texto);
  - autenticación y TLS.

## Paso 8 · [JZ-01] Infraestructura local

- [ ] **H-68** Que Compose lea el `.env` de la raíz (con `--env-file .env` en los comandos o de otra forma) y actualizar el manual y `instalacion.md`.
- [ ] **H-69** Hacer configurable el puerto de PostgreSQL con `${SHAPI_POSTGRES_PUERTO:-5432}` y agregar `SHAPI_SECRETO_ORIGEN_ENVIOS` y `SHAPI_SECRETO_ORIGEN_AGRO` a `.env.example`.
- [ ] **H-70** Fijar la versión de Mailpit (`v1.27`, igual que en las pruebas) en lugar de `latest`.
- [ ] **H-71** Publicar los puertos solo en `127.0.0.1`.
- [ ] **H-72** Agregar *healthchecks* a `origen-envios` y `origen-agro` e incluirlos en `infra/verificar.mjs`.
- [ ] **H-73** Agregar un `.dockerignore` en la raíz y `.shapi/` al `.gitignore`.

## Paso 9 · [JZ-02] Orígenes de demostración

- [ ] **H-74** Quitar `/salud` y el parámetro `X-Shapi-Secreto` de `cotizacion-envios.yaml` y de `agro-precios/openapi.yaml`, para que coincidan con A3.3 y A5.1.
- [ ] **H-75** Corregir "Secreto de origen invalido" a "inválido".
- [ ] **H-76** Hacer que `/precios` de Agro sea coherente con `/historial` para fechas distintas del 10 de septiembre.
- [ ] **H-77** Pruebas que comparen los ejemplos del OpenAPI con las respuestas reales, que revisen los cuerpos de `/tarifas`, `/rastreo` y `/cobertura`, y que prueben "sin `SECRETO_ORIGEN` acepta todo".

## Paso 10 · [JG-02] Compuerta mínima

- [ ] **H-78** Rechazar la clave enviada en la query string con 401, sin reenviarla (08 §1). Silenciar el registro de la URL de destino de YARP.
- [ ] **H-79** Si Redis no está disponible, responder JSON y no la página de excepciones en HTML.
  - **❓ Decisión pendiente:** es un hueco de la especificación. Hay que definir el código y el `codigo` de error; se propone 503 `servicio_no_disponible`.
- [ ] **H-80** Tiempo de espera total de 30 s, no solo de inactividad, y precisar el `ConnectTimeout` en 08 §1.
- [ ] **H-81** Pruebas faltantes:
  - varias `X-Api-Key`;
  - un 4xx o 5xx del origen se devuelve tal cual;
  - datos incompletos en Redis;
  - `sembrar-demo` desde `Program.cs`;
  - la configuración real del invocador de YARP.

  Además, nombrar las pruebas con su requisito.
- [ ] **H-82** En el `## Resultado` de JG-02, dejar escrito que RF-31 quedó parcial: `X-Shapi-Secreto` y la limpieza de `X-Shapi-*` y `X-Forwarded-*` se completan en JG-05.

## Paso 11 · [EM-17] Publicar el contrato de identidad en el frontend

- [ ] **H-83** Generar `packages/api/src/generado/identidad.ts` y exportar `"./*"` en `packages/api/package.json`.
- [ ] **H-84** Reemplazar el contrato provisional `modulos/sesion/contratoSesion.ts` por los tipos generados, y cerrar EM-17.

## Paso 12 · [DC-01] Sistema de diseño

- [ ] **H-85** Quitar `@fontsource/instrument-sans`, que no se usa.
- [ ] **H-86** Tokens completos según 11 §1:
  - escala de espaciado y escala tipográfica (32, 22, 16, 15, 12 y 11);
  - tema oscuro sin valores inventados;
  - sin la paleta por defecto de Tailwind (quitar `hover:bg-gray-50`).
- [ ] **H-87** Medidas de `/_ui` y de los componentes según la lámina:
  - logotipo;
  - tabla: peso, borde y relleno;
  - campo: 48 px de alto, 15 px de texto, anillo de foco y texto guía;
  - botón: texto de 15 px;
  - tarjeta: relleno de 20 px;
  - descripciones de las muestras de color.
- [ ] **H-88** Accesibilidad:
  - `Campo`: `aria-invalid`, `aria-describedby` y etiqueta propia;
  - `DialogoConfirmacion`: textos y contenido configurables, foco atrapado, cierre con Escape y `aria-labelledby`;
  - `Selector`: `aria-label` e `id`.
- [ ] **H-89** `EstadoError` y `Aviso` no deben heredar `nowrap`, `uppercase` ni `text-xs`. "Reintentar" debe ser un `Boton` (11 §4).
- [ ] **H-90** `generar:api` no debe fallar si la carpeta de contratos no existe.
- [ ] **H-91** MSW con `onUnhandledRequest: 'error'`. Los patrones de Vitest también deben incluir `*.test.ts`. Quitar la configuración duplicada de Vitest.
- [ ] **H-92** Pruebas de `EstadoCargando`, `EstadoError` y `EstadoSinPermiso`, y de la paleta y los estados en `/_ui`.
- [ ] **H-93** Identificadores y comentarios en español según el glosario: `isAdmin`, `navItem`, `headers`, `rows`, `options`, `open`, `onClose`, `onConfirm`, `Suspensify`, `ProblemDetailsError` y los comentarios de `style.css`. El portal no debe mostrar "Portal Base".

## Paso 13 · [DC-02] Estructura del panel y del sitio público

- [ ] **H-94** Sacar A0.1 (`/`) de `LayoutPublico`, como ya se hizo con `/_ui`.
- [ ] **H-95** `errorElement`: distinguir un 404 (`isRouteErrorResponse`) de los demás errores y ofrecer "Reintentar".
- [ ] **H-96** Rutas índice en `/panel`, `/admin` y `/panel/apis/:id`. El 403 entre áreas debe dar una salida (cerrar sesión o ir a su área). Un 404 dentro del panel debe conservar el layout.
- [ ] **H-97** Medidas de N.1 según el mockup: rellenos de la barra superior y de la lateral, altura de línea, selector con flecha y *hover* del botón de salir. Medidas del encabezado de A1: `gap` de 11 px y `letter-spacing`.
- [ ] **H-98** Colores de los layouts con tokens en lugar de hex escritos a mano.
- [ ] **H-99** Usar `h-screen` para que el pie de la barra lateral quede fijo, y hacer que el HMR de Vite funcione también sin Caddy.
- [ ] **H-100** Un solo cliente de sesión (hoy `useSesion` y `CerrarSesion` tienen uno cada uno). `SelectorApi` debe reutilizar `Selector`. Agregar `staleTime` a la consulta de sesión.
- [ ] **H-101** Limitar `/_ui` al entorno de desarrollo.
- [ ] **H-102** Pruebas:
  - el orden completo de N.1 y A6;
  - los textos de B3;
  - un 501 en el selector se trata como "Sin APIs";
  - A0.1 sin el encabezado de A1;
  - un error que no es 404.

## Paso 14 · [EM-06] Terminar el PR #14: pasarela de pagos simulada

- [ ] **H-103** Las pruebas obligatorias en `tests/`, con NSubstitute, nombradas con RF-20. Deben cubrir:
  - cada fila de la tabla de 09 §2;
  - Luhn;
  - longitudes de 13 a 19;
  - las tres marcas y `marca_no_soportada`;
  - vencimientos;
  - `cvv_invalido`;
  - los formatos `ch_sim_` y `re_sim_`;
  - `pasarela_no_disponible`.
- [ ] **H-104** Detectar las tarjetas especiales por el número completo, no por los últimos 4 dígitos.
- [ ] **H-105** Quitar `generar_pagos.py` y el resto de los hallazgos obligatorios de la revisión automática, y actualizar la rama desde `main`.
- Igual que en el paso 2 (§E4): una rama nueva, `jordin/EM-06-…`, que parte de `emilio/EM-06-pasarela-de-pagos`, un PR nuevo y se cierra el #14 con un comentario que enlaza al nuevo.
- **❓ Decisión pendiente:** si Jordin ya le avisó a Emilio que no siga trabajando en el #14.

## Paso 15 · [JG-01] CI y reglas del repositorio

- [ ] **H-106** En el job `backend`: `dotnet ef migrations has-pending-model-changes`.
- [ ] **H-107** En el job `frontend`: `pnpm generar:api` y luego `git diff --exit-code` sobre `generado/`.
- [ ] **H-108** `timeout-minutes` en los jobs. El evento `edited` solo debe volver a ejecutar la CI cuando cambia el título.
- [ ] **H-109** Fijar por SHA las acciones de terceros que reciben secretos.
- [ ] **H-110** `.claude/settings.json`: negar también `git push origin HEAD:main` y sus variantes.
  - **❓ Decisión pendiente:** si se activa `enforce_admins`. Con eso, Jordin tampoco podría saltarse la CI.
- [ ] **H-111** La plantilla de PR debe pedir la evidencia de `dotnet format`, `pnpm build` y `tareas.mjs --validar`. Completar el `README.md` con las carpetas y los comandos de arranque.
- [ ] **H-112** Ejecutar las pruebas en paralelo es inestable con Docker en Windows (se vio en la auditoría). Se evalúa limitar el paralelismo entre proyectos o documentarlo.

## Paso 16 · [JG-01] Huecos de requisitos (tareas nuevas según §C)

- [ ] **H-113** Crear tareas para RNF-06 (disponibilidad del 99.5 %) y RNF-11 (publicar en menos de 5 minutos). Agregar RNF-13 a los metadatos de JG-02.
- [ ] **H-114** `calendario.md:102` dice que las tareas P1 cubren los lineamientos obligatorios. Hoy solo los cubren tareas P2:
  - RF-06: EM-12;
  - RF-11 y RF-12: DC-14;
  - RF-36: JG-13;
  - RF-39: JZ-12;
  - RF-43: EM-12 y EM-13.
- **❓ Decisión pendiente:**
  - a quién se asigna cada tarea nueva;
  - si se suben a P1 las tareas existentes o se corrige la afirmación del calendario.

## Paso 17 · [JG-01] Auditoría final

- [ ] Ejecutar la verificación completa: build, formato, pruebas, lint, typecheck, build del frontend, `scripts/` y `--validar`.
- [ ] Volver a auditar todas las tareas hechas contra su archivo de tarea y contra las especificaciones, en contexto limpio.
- [ ] Anotar el resultado final en este documento y en la bitácora de Jordin.
