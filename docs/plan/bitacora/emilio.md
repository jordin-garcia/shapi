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
