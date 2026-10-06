# Bitácora de Emilio Méndez

Cada tarea terminada agrega una entrada **al final** de este archivo (protocolo, paso B10):

```
## AAAA-MM-DD · <ID> · <título>
- Hecho: ...
- Decisiones: ...
- Pendiente o aviso para otros: ...
```

---

## 2026-09-24 · EM-01 · Esquema completo de la base de datos y datos base
- Hecho: Configuré los modelos en C# usando Entity Framework Core, creé las configuraciones, agregué convenciones (snake_case) y filtros globales. Hice la base de `SiembraBase` y configuré las dependencias. Completé pruebas de persistencia para el trigger de inmutabilidad y filtrado.
- Decisiones: Se creó un trigger nativo en Postgres (`bitacora_append_only`) dentro de la migración Inicial para volver la tabla `bitacora` de sólo escritura (inmutable). El uso de `Nullable<Guid>` fue requerido en el Expression Tree de los filtros.
- Pendiente o aviso para otros:
  - **Todos:** la migración `Inicial` fue autogenerada en base a mis entidades y editada manualmente para el trigger. La base se migra y se siembra al arrancar la API en *Development* o con `SHAPI_APLICAR_MIGRACIONES=true`.
  - **Todos:** el contexto de WebApplicationFactory necesita configuración `SHAPI_POSTGRES_CADENA` para no tronar al intentar validar dependencias.
- (Corregido por el coordinador el 26 sep, auditoría H-48: el disparador se llamaba `bitacora_append_only`, no `trg_prevent_update_delete`, y la base sí se migra en *Development*. Desde la migración `AjustesDelEsquemaAuditoria`, los disparadores son `bitacora_solo_inserciones` y `bitacora_sin_vaciado`.)

## 2026-09-25 · EM-01 · Correcciones del PR #9
- Hecho: Se implementó `ColaCorreoBaseDatos` y `BitacoraBaseDatos`. Se ajustaron las configuraciones de EF Core para usar strings precisos según la especificación con la ayuda de conversores explícitos (reemplazando lambdas de `switch` que fallaban en árboles de expresión). Se arregló la migración `Inicial` para incluir llaves foráneas y crear manualmente los índices únicos de `lower(correo)` en `usuario` y `consumidor`.
- Decisiones: Se removió el registro de implementaciones nulas de ServiciosComunes, usando directamente las de BD; además, para los tests con WebApplicationFactory se adoptó `Testcontainers.PostgreSql` en vez de dependencias In-Memory.
- Pendiente o aviso para otros:
  - **Todos:** cualquier otro test que inicialice la app completa mediante `WebApplicationFactory` requerirá testcontainers de BD en sus fixtures si se invoca alguna inyección dependiente de datos.
- (Corregido por el coordinador el 26 sep, auditoría H-48: los índices no eran concurrentes.)

## 2026-09-25 · EM-02 · Registro, verificación de correo e inicio de sesión del personal (backend)
- (Entrada agregada por el coordinador el 26 sep, auditoría H-60, a partir del `## Resultado` de la tarea y de los PR #10 y #17.)
- Hecho: `POST /api/auth/registro`, `verificar-correo`, `reenviar-verificacion`, `entrar` y `salir`, y `GET /api/auth/sesion`, con sesiones del lado del servidor en la tabla `sesion` y la cookie `shapi_sesion`. Bloqueo tras 5 intentos, CSRF, límite de 10 por minuto por IP y las 28 políticas de `Permisos.*`. El 25 sep, el coordinador corrigió la implementación en el PR #17.
- Decisiones: `423 cuenta_bloqueada`, `403 cuenta_desactivada` (solo con la contraseña correcta) y `429 demasiadas_peticiones`. `sesion` y `salir` no llevan el límite por IP.
- Pendiente o aviso para otros:
  - **Todos:** usen `RequireAuthorization(Permisos.X)` en sus endpoints. Desde el 26 sep, un endpoint sin política exige una sesión (04 §4, regla 6).

## 2026-09-25 · EM-06 · Pasarela de pagos simulada
- Hecho: Se implementó `PasarelaSimulada` para procesar cobros ficticios de acuerdo con los números de tarjeta de prueba. Se verificó el algoritmo de Luhn, los rangos de BIN y los códigos CVV.
- Decisiones: Se utilizó un `ConcurrentDictionary` en memoria para retener la asociación de los tokens con los errores específicos de las tarjetas 0002 y 0069, así como 0341, permitiendo a `CobrarAsync` reaccionar correctamente a renovaciones falsas sin persistencia permanente.
- Pendiente o aviso para otros: La variable `SHAPI_PASARELA_FALLA` permite simular una caída total de la pasarela y `Pagos:DemoraMs` añade un tiempo de espera.
- (Corregido por el coordinador el 27 sep, auditoría H-103 a H-105: la tarea la terminó Jordin en el PR que reemplaza al #14. Ya no hay diccionario en memoria: las tarjetas especiales se reconocen por el número completo y las tres llevan su comportamiento en el token (`tok_sim_0002_`, `tok_sim_0069_` y `tok_sim_0341_`). Sin `Pagos:DemoraMs`, la demora es al azar entre 300 y 800 ms; con un valor, es ese valor.)

## 2026-09-29 · EM-04 · Recuperación de contraseña y Mi perfil (A1.4a, A1.4b y A8.1)
- Hecho: Se implementó la recuperación de contraseña (`POST /api/auth/recuperar` y `restablecer`), la edición de perfil y contraseña (`GET/PUT /api/perfil`, `POST /api/perfil/contrasena`). El backend genera tokens de un solo uso de 60 minutos, y revoca otras sesiones al cambiar contraseña. Se crearon las interfaces A1-4a, A1-4b y A8-1 en el frontend con Vitest probando las interacciones. Todo ajustado a los mockups.
- Decisiones: La lógica de recuperación se aisló en `IServicioRecuperacion` (con parámetro de ámbito) para su reutilización en EM-05 con consumidores.
- Pendiente o aviso para otros:
  - **Jordin:** la infraestructura de Docker local estaba caída durante mi ejecución impidiendo que `Shapi.Api.Tests` y `Shapi.Compuerta.Tests` se ejecutaran, pero `Shapi.Dominio.Tests` y todos los test de frontend pasan. Por favor revisar los test con infraestructura.

## 2026-09-29 · EM-04 · Correcciones de la revisión del PR #46
- Hecho: Se conservó y completó la corrección local del mensaje neutral de recuperación. Se agregaron pruebas del texto de CU-03, enlace vencido, errores de contraseña y reintento de sesión. La revisión independiente detectó un rechazo sin manejar después de restablecer; ahora se espera la sesión y el reintento no consume otra vez el token. La prueba falló antes del arreglo y pasa después. Revisión final en contexto limpio: LISTO.
- Decisiones: El estado enviado usa «Recuperación · revise su correo»; «paso 2 de 2» se reserva para definir la contraseña. El comportamiento y el reintento están documentados en 11-interfaz.
- Verificación: Plan válido (66 tareas); compilación sin errores ni advertencias; formato, lint, typecheck y build del frontend correctos; 230 pruebas de frontend pasan con `pnpm test --maxWorkers=1`. La ejecución paralela local tuvo tiempos de espera; no se cambiaron las aserciones ni sus límites. Las pruebas de integración locales encontraron un fallo de DNS al descargar imágenes de Docker Hub; el backend del commit anterior ya pasó en la CI. Se requiere confirmar también la CI del commit corregido antes de integrar.
- Pendiente o aviso para otros: Ninguno sobre contratos; esta corrección mantiene los endpoints y sus tipos.

## 2026-09-30 · EM-05 · Identidad del consumidor (backend del portal)
- Hecho: Se implementaron registro por organización, verificación y reenvío de correo, acceso y salida, consulta de sesión, recuperación y aceptación de invitaciones. La cookie `portal_sesion` valida el ámbito consumidor, la organización y el host del portal. Se agregaron pruebas de integración para correos duplicados entre organizaciones, aislamiento de cookie por host, invitación, bloqueo concurrente, recuperación de correo compartido, salida y límite por IP.
- Decisiones: La marca y los enlaces de verificación/recuperación usan los datos y el host canónico de `IResolutorPortal`. El servicio de recuperación exige token del ámbito esperado. Se limita por IP registro, verificación, reenvío, acceso, recuperación e invitaciones; restablecer mantiene el alcance del flujo personal. `identidad.ts` se regeneró a partir del contrato.
- Verificación: `dotnet build Shapi.slnx --no-restore` y `dotnet format Shapi.slnx --verify-no-changes --no-restore` pasan sin advertencias ni cambios; `node scripts/tareas.mjs --validar` informa 66 tareas válidas. En `frontend/`, `pnpm lint`, `pnpm typecheck`, `pnpm test` (236 pruebas) y `pnpm build` pasan. No hay Docker local (`/var/run/docker.sock`), así que `dotnet test` no pudo correr con Testcontainers. La primera CI encontró dos fallos en la prueba de invitación y en la selección de correo por host; se corrigieron y la suite quedó pendiente de volver a correr en CI.
- Pendiente o aviso para otros:
  - **Jordin:** las pruebas de integración requieren que CI confirme el resultado, porque Docker no está disponible en este entorno local.
  - **Dominique:** los tipos de identidad de `frontend/packages/api/src/generado/identidad.ts` se regeneraron con EM-05; actualiza tu rama desde `main` al integrar cambios dependientes.
## 2026-10-01 · EM-07 · Planes de API (A4.1)
- Hecho: Se implementaron los endpoints para crear, editar, listar y desactivar planes (`/api/apis/{apiId}/planes`). Se creó la interfaz frontend (A4.1) y se conectó al backend con MSW para los tests. Las validaciones de integridad (precio, unicidad, plan gratuito) y la publicación de cachés para suscripciones fueron cubiertas, y se registraron en la bitácora (`TipoActor.Usuario`). Se ajustaron tipos y errores a la especificación, con códigos 404, 400 y 409. (02 de oct): Backend: Refactorización de tests (CS0103 y aserciones) completada exitosamente. Frontend: Arreglo de interceptores MSW (remover "as any", resolver type-checking) y corrección del ciclo de mutación `openapi-fetch`. Todos los tests (locales y CI) de PR #55 pasaron. Correcciones de la revisión con Claude completadas.
- Decisiones: Se resolvió que el nombre del plan se verifique ignorando los que no son parte de la misma API, pero incluyendo inactivos para prevenir conflictos al editar. Para `CodigosError`, se implementaron `PlanDuplicado` y `PlanNoEncontrado`.
- Pendiente o aviso para otros:
  - **Dominique**: Revisa que la interfaz de portales pueda leer los planes activos adecuadamente con los endpoints proporcionados.

## 2026-10-03 · EM-08 · Contratación de un plan de API (backend)
- Hecho: Se implementaron la contratación de planes pagados y gratuitos, el registro de rechazos, la publicación de la suscripción y las claves de JG-07, y la consulta de la suscripción vigente. Se agregó el contrato OpenAPI, las pruebas de integración y la migración `PagoRechazadoSinSuscripcion`. La suscripción y las claves se guardan en una transacción; después del commit se publican ambas en Redis.
- Decisiones: Con autorización del usuario, se amplió el esquema de `pago` para asociar un intento rechazado sin suscripción al consumidor y la API. No se conserva token, tarjeta ni CVV para los rechazos.
- Verificación: `dotnet build Shapi.slnx --no-restore` pasó sin advertencias ni errores. `dotnet format Shapi.slnx --verify-no-changes --no-restore` pasó. `dotnet ef migrations has-pending-model-changes -p src/Shapi.Infraestructura -s src/Shapi.Api` informó que no hay cambios pendientes. `node scripts/tareas.mjs --validar` informó 68 tareas válidas. `dotnet test Shapi.slnx --no-restore` ejecutó 598 pruebas de API (295 pasaron, 303 fallaron al iniciar PostgreSQL/Redis Testcontainers porque este entorno no tiene Docker); también falló por el mismo motivo la ejecución dirigida a EM-08. OrigenesDemo.Tests (48) y Shapi.Dominio.Tests (48) pasaron en la ejecución completa. CI debe confirmar las suites de integración.
- Pendiente o aviso para otros:
  - **DC-09:** ya están disponibles en `contratos/openapi/suscripciones.yaml` `POST /api/portal/suscripciones` y `GET /api/portal/suscripcion`; la respuesta de contratación devuelve las dos claves completas una sola vez.
  - **JG-13:** los pagos de contratación rechazados sin suscripción llevan `consumidor_id` y `api_id`; los demás pagos siguen asociados a su suscripción.
  - **Jordin:** agregué `IServicioClaves.PrepararClavesParaSuscripcion` en `IServicioClaves` y `ServicioClaves`. La contratación lo llama dentro de su transacción y publica sus claves después del commit, conforme al protocolo de Redis.

## 2026-10-05 · EM-09 · Suscripción de plataforma: contratar y cambiar de plan (A2 y B1.4)
- Hecho: Implementé el contrato OpenAPI y los endpoints para consultar planes y suscripción, contratar, cambiar y programar/cancelar una bajada, y reactivar con pago. Completé las pantallas A2.1–A2.5 y B1.4, y agregué pruebas de prorrateo, integración y Vitest.
- Decisiones: El cambio a una vigencia distinta cobra el precio completo y comienza un ciclo nuevo hoy, según 09 §5. La contratación confirma en PostgreSQL antes de publicar la suscripción en Redis; si falla la confirmación, se reembolsa el cobro.
- Verificación: `dotnet build Shapi.slnx` y `dotnet format Shapi.slnx --verify-no-changes` pasan. Las pruebas dirigidas pasan: 16 de API y 3 de dominio. En `frontend/`, `pnpm lint`, `pnpm typecheck`, `pnpm test` (308 pruebas) y `pnpm build` pasan. Capturé y comparé ocho estados de interfaz con sus mockups. `docker compose up` no pudo publicar PostgreSQL porque el puerto local 5432 ya estaba ocupado; Testcontainers sí pudo ejecutar las pruebas dirigidas. La suite completa del backend la verificará CI (B7).
- Pendiente o aviso para otros:
  - **DC-15:** ya están disponibles en `contratos/openapi/suscripciones.yaml` los endpoints y tipos de suscripción de plataforma para continuar la tarea dependiente.
  - **Dominique:** actualicé `frontend/apps/panel/src/tests/rutas.test.tsx` para registrar las pantallas EM-09 y simular los endpoints de suscripción; quedó autorizado por el usuario.
