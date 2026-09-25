---
id: EM-01
titulo: Esquema completo de la base de datos y datos base
persona: emilio
responsable: Emilio Méndez
avance: 1
prioridad: P1
estado: hecha
depende_de: [JG-01]
requisitos: [RNF-08]
pantallas: []
---

# EM-01 · Esquema completo de la base de datos y datos base

**Responsable:** Emilio Méndez · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** JG-01

## Objetivo
Crear de una sola vez todas las entidades, las configuraciones de EF Core y la migración inicial con el esquema completo de `docs/specs/07`, más los datos base y las implementaciones que escriben en la base de datos para la cola de correo y la bitácora. Así nadie tiene que tocar el esquema en paralelo.

## Contexto que debes leer
- `docs/specs/07-modelo-de-datos.md` §1, §2, §3 **completo** (tablas, columnas, restricciones, índices) y §6
- `docs/specs/01-vision-y-alcance.md` §6 (planes de plataforma exactos)
- `docs/specs/02-glosario.md` (formatos)
- `docs/plan/convenciones.md` §1, §3 (migraciones) y §6

## Archivos que creas o modificas
- `src/Shapi.Dominio/<Modulo>/*.cs` (crear las clases de entidad de **todos** los módulos, solo con propiedades, constructores y los enumerados de estado; los dueños de cada módulo agregarán el comportamiento en sus tareas)
- `src/Shapi.Infraestructura/Persistencia/ShapiDbContext.cs` (crear): `ApplyConfigurationsFromAssembly`, nombres `snake_case` (EFCore.NamingConventions) y filtro global por organización para toda entidad que implemente `IPerteneceAOrganizacion`
- `src/Shapi.Infraestructura/Persistencia/Configuraciones/*.cs` (crear, una por entidad)
- `src/Shapi.Infraestructura/Persistencia/Migraciones/*` (crear la migración `Inicial`)
- `src/Shapi.Infraestructura/Siembra/Base/**` (crear)
- `src/Shapi.Infraestructura/Correo/ColaCorreoBaseDatos.cs` y `src/Shapi.Infraestructura/Bitacora/BitacoraBaseDatos.cs` (crear; reemplazan las implementaciones nulas)
- `src/Shapi.Infraestructura/Comun/ContextoOrganizacion.cs` (crear)
- `src/Shapi.Api/Program.cs` o un módulo de infraestructura: aplicar migraciones y datos base al iniciar en *Development* o con `SHAPI_APLICAR_MIGRACIONES=true`
- `tests/Shapi.Api.Tests/Persistencia/**` (crear)

## Criterios de aceptación
1. `dotnet ef migrations has-pending-model-changes -p src/Shapi.Infraestructura -s src/Shapi.Api` indica que no hay cambios pendientes: el modelo coincide con la migración.
2. La base creada tiene **todas** las tablas, columnas, tipos, CHECK, UNIQUE (incluidos los índices únicos parciales y `NULLS NOT DISTINCT`) e índices de 07 §3. Si EF no soporta algo, se hace con `migrationBuilder.Sql`. Hay pruebas que confirman al menos: un solo propietario por organización, una suscripción vigente por organización y por consumidor en cada API, una clave activa por tipo, la unicidad de `consumo_diario` con nulos, `num_nonnulls` en `pago` y en `medio_pago`, y un solo plan `es_prueba`.
3. Un disparador rechaza UPDATE y DELETE sobre `bitacora`, y hay una prueba de ello. La secuencia de `caso.numero` empieza en 100.
4. El filtro global por organización funciona: con el contexto de la organización A, una consulta no devuelve filas de la organización B (prueba). `IgnoreQueryFilters` queda disponible para los servicios de administración.
5. Los datos base son idempotentes: la organización de plataforma, los 5 planes de plataforma exactos de 01 §6 (Prueba, Lanzamiento, Producto, Escala mensual y Escala anual con 120,000,000 peticiones) y el administrador inicial, tomado de `SHAPI_ADMIN_CORREO`, `SHAPI_ADMIN_NOMBRE` y `SHAPI_ADMIN_CONTRASENA`. Si esas variables no existen, no se crea y se registra un aviso.
6. `ColaCorreoBaseDatos.Encolar(plantilla, destinatario, datos)` inserta en `correo_saliente` con estado `pendiente`. `BitacoraBaseDatos.Registrar(...)` inserta en `bitacora`. Ambas se registran en lugar de las nulas.

## Pruebas obligatorias
- Integración con Testcontainers (PostgreSQL 16) de cada restricción del criterio 2
- Disparador de la bitácora
- Filtro por organización
- Idempotencia de los datos base (ejecutarlos dos veces)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet ef migrations has-pending-model-changes -p src/Shapi.Infraestructura -s src/Shapi.Api
```

## Fuera de alcance
- Casos de uso y endpoints
- Siembra de demostración (JZ-05)
- Envío real de correos (JZ-03)

## Notas
- Esta tarea toca carpetas de otros módulos solo para crear las entidades. Es la excepción acordada en convenciones §3: el esquema se crea una sola vez.
