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

## 2026-09-25 · EM-03 · Pantallas de registro, verificación y acceso (A1.1 a A1.3)
- Hecho: Se implementaron las pantallas del panel para el registro (A1.1), verificación de correo (A1.2) e inicio de sesión (A1.3) según los mockups. Se conectaron a la API de identidad mediante hooks de \`react-query\` (\`useIdentidad.ts\`) manejando la lógica de éxito y los mensajes de error de ProblemDetails.
- Decisiones: La generación de la API de TypeScript (\`pnpm generar:api\`) no pudo acceder al registro remoto por un bloqueo de red en el entorno actual. Se creó manualmente el archivo \`identidad.ts\` que tipa las rutas de OpenApi para que la compilación estática y el cliente openapi-fetch puedan funcionar correctamente.
- Pendiente o aviso para otros: El componente A1-3-Sesion resuelve su redirección tras login invocando de forma manual \`clienteSesion.GET\`. La ejecución de las pruebas y linter debe hacerse en un entorno con acceso a \`npmjs.org\`.
