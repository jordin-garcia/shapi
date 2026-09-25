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
- Decisiones: Se creó un trigger nativo en Postgres (`trg_prevent_update_delete`) dentro de la migración Inicial para volver la tabla `bitacora` de sólo escritura (inmutable). El uso de `Nullable<Guid>` fue requerido en el Expression Tree de los filtros.
- Pendiente o aviso para otros: La migración `Inicial` fue autogenerada en base a mis entidades y editada manualmente para el trigger. No se levanta la BD en tiempo de desarrollo por defecto; requiere `SHAPI_APLICAR_MIGRACIONES=true`. El contexto de WebApplicationFactory necesita configuración `SHAPI_POSTGRES_CADENA` para no tronar al intentar validar dependencias.

## 2026-09-25 · EM-01 · Correcciones del PR #9
- Hecho: Se implementó `ColaCorreoBaseDatos` y `BitacoraBaseDatos`. Se ajustaron las configuraciones de EF Core para usar strings precisos según la especificación con la ayuda de conversores explícitos (reemplazando lambdas de `switch` que fallaban en árboles de expresión). Se arregló la migración `Inicial` para incluir llaves foráneas y crear manualmente índices concurrentes `lower(correo)` en `usuario` y `consumidor`. 
- Decisiones: Se removió el registro de implementaciones nulas de ServiciosComunes, usando directamente las de BD; además, para los tests con WebApplicationFactory se adoptó `Testcontainers.PostgreSql` en vez de dependencias In-Memory.
- Pendiente o aviso para otros: Cualquier otro test que inicialice la app completa mediante `WebApplicationFactory` requerirá testcontainers de BD en sus fixtures si se invoca alguna inyección dependiente de datos.

## 2026-09-25 · EM-06 · Pasarela de pagos simulada
- Hecho: Se implementó `PasarelaSimulada` para procesar cobros ficticios de acuerdo con los números de tarjeta de prueba. Se verificó el algoritmo de Luhn, los rangos de BIN y los códigos CVV.
- Decisiones: Se utilizó un `ConcurrentDictionary` en memoria para retener la asociación de los tokens con los errores específicos de las tarjetas 0002 y 0069, así como 0341, permitiendo a `CobrarAsync` reaccionar correctamente a renovaciones falsas sin persistencia permanente.
- Pendiente o aviso para otros: La variable `SHAPI_PASARELA_FALLA` permite simular una caída total de la pasarela y `Pagos:DemoraMs` añade un tiempo de espera.
