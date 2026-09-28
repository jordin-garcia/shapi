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
