# Convenciones técnicas

Las reglas comunes para que el código de las cuatro personas, y de sus agentes, encaje sin conflictos. Complementan `docs/specs/06-arquitectura.md`.

## 1. Estructura y capas

```
Shapi.slnx
Directory.Build.props          ← net10.0, nullable, ImplicitUsings, TreatWarningsAsErrors, analizadores
Directory.Packages.props       ← versiones centralizadas de TODOS los paquetes NuGet (archivo caliente)
src/
  Shapi.Dominio/<Modulo>/            entidades, objetos de valor, reglas, máquinas de estado (sin dependencias)
  Shapi.Aplicacion/<Modulo>/         casos de uso (una clase por caso de uso), interfaces, DTO, validadores
  Shapi.Infraestructura/
    Persistencia/ShapiDbContext.cs   aplica todas las configuraciones del ensamblado (no se edita por módulo)
    Persistencia/Configuraciones/    una clase IEntityTypeConfiguration<T> por entidad
    Persistencia/Migraciones/        migraciones de EF Core (archivo caliente: el snapshot)
    <Modulo>/                        implementaciones del módulo (repositorios, servicios externos)
    Siembra/Base/                    datos base (plataforma, planes, admin inicial)
    Siembra/Demo/                    siembra de demostración (07 §6)
  Shapi.Api/
    Program.cs                       NO se edita por módulo: llama a los Agregar/Mapear de Modulos/
    Modulos/<Modulo>Modulo.cs        AgregarModulo<X>(services) y MapearModulo<X>(app); cada dueño edita el suyo
    <Modulo>/Endpoints.cs            Minimal APIs del módulo (MapGroup)
  Shapi.Contratos/                   llaves de Redis, códigos de error, DTO de la caché, ValidadorDireccionOrigen
  Shapi.Compuerta/                   YARP + filtros (depende solo de Shapi.Contratos y StackExchange.Redis)
  Shapi.Trabajador/<Trabajo>/        un BackgroundService por trabajo
tests/
  Shapi.Dominio.Tests/<Modulo>/
  Shapi.Api.Tests/<Modulo>/          integración con WebApplicationFactory + Testcontainers (PostgreSQL y Redis)
  Shapi.Compuerta.Tests/
  e2e/                               Playwright (TypeScript)
  carga/                             k6
frontend/  (pnpm workspace)
  packages/ui/                       tokens de la variante 4 y componentes base
  packages/api/                      cliente HTTP base + tipos generados en src/generado/<modulo>.ts
  apps/panel/src/
    rutas.tsx                        router con TODAS las rutas del catálogo (creado en DC-02; no se edita por página)
    paginas/<ID>-<Nombre>.tsx        una pantalla por archivo, por ejemplo paginas/A3-2-RegistrarApi.tsx
    modulos/<modulo>/                componentes y hooks del módulo
  apps/portal/src/                   igual que panel
contratos/openapi/<modulo>.yaml      un contrato por módulo (ver contratos/openapi/README.md)
origenes-demo/{envios-xelaju,agro-precios}/
infra/{compose.yml,compose.prod.yml,caddy/}
scripts/
```

**Módulos del backend:** `Identidad`, `Organizaciones`, `Planes`, `Suscripciones`, `Pagos`, `Apis`, `Portal`, `Claves`, `Consumo`, `Cache`, `Administracion`, `Soporte`, `Bitacora`, `Correo`, `Estado`.

## 2. Qué pertenece a quién

Cada persona modifica solo lo suyo. Si una tarea necesita tocar algo ajeno, la tarea lo dice explícitamente; si no lo dice, se aplica "Preguntar antes". La excepción es el coordinador (Jordin): en cualquier PR suyo puede modificar cualquier archivo de esta tabla y de §3, y avisa en su bitácora a la persona dueña (`protocolo.md` §E1). La excepción cubre solo quién edita; las reglas técnicas de §3 (no editar el snapshot ni el lockfile a mano, no editar `Program.cs`, etc.) se siguen aplicando igual.

| Persona | Backend (`src/*/<Modulo>/`, `tests/*/<Modulo>/`) | Frontend | Otros |
|---|---|---|---|
| **Jordin** (JG) | `Claves`, `Consumo`, `Cache`, todo `Shapi.Compuerta/`, `Shapi.Contratos/Redis/` | `panel/paginas/A4-3*`, `panel/paginas/B1-1*`, `panel/paginas/B1-2*`, `portal/paginas/B2-1*` | `.github/workflows/`, `Shapi.slnx`, `Directory.*.props`, `docs/specs/` (coordina) |
| **Emilio** (EM) | `Identidad`, `Organizaciones`, `Planes`, `Suscripciones`, `Pagos`, `Persistencia/` (esquema y migraciones), `Siembra/Base/` | `panel/paginas/A1-*`, `A2-*`, `A4-1*`, `A4-2*`, `A6-1*`, `A6-3*`, `A8-*`, `B1-3*`, `B1-4*`, `B1-5*` | `contratos/openapi/{identidad,organizaciones,planes,suscripciones,pagos}.yaml` |
| **Dominique** (DC) | `Apis`, `Portal`, `Shapi.Contratos/Red/` (ValidadorDireccionOrigen) | `packages/ui/`, `packages/api/` (base), estructura de `apps/panel` y `apps/portal` (layouts, `rutas.tsx`), `panel/paginas/A0-*`, `A3-*`, todo `portal/paginas/` salvo `B2-1*` | `contratos/openapi/{apis,portal}.yaml`, `frontend/package.json` y el lockfile |
| **José Pablo** (JZ) | `Administracion`, `Soporte`, `Bitacora`, `Correo`, `Estado`, `Siembra/Demo/` | `panel/paginas/A6-2*`, `A6-4*`, `A6-5*`, `A7-*`, `B3-*` | `infra/`, `origenes-demo/`, `tests/e2e/`, `tests/carga/`, `docs/manual-*.md`, Dockerfiles, `contratos/openapi/{administracion,soporte,sistema}.yaml` |

Todos pueden modificar su archivo de tarea, su bitácora y las secciones de `docs/specs/` que tienen que ver con su tarea.

## 3. Archivos calientes y cómo resolver sus conflictos

Las restricciones de quién puede tocar cada archivo ("Solo José Pablo", "Solo Jordin", "Cada quien edita los suyos") no aplican al coordinador (§2). Las reglas técnicas sí.

| Archivo | Regla |
|---|---|
| `Persistencia/Migraciones/*ModelSnapshot.cs` | Si al rebasar hay conflicto: **borra tu migración**, toma el snapshot de `main`, haz el rebase y **vuelve a generar tu migración** con `dotnet ef migrations add`. Nunca edites el snapshot a mano |
| `Directory.Packages.props` | JG-01 ya declara todos los paquetes del stack. Si necesitas uno nuevo (primero "Preguntar antes"), agrégalo en orden alfabético. Si hay conflicto, conserva ambas líneas |
| `frontend/pnpm-lock.yaml` | Si hay conflicto, toma la versión de `main` y ejecuta `pnpm install` para regenerarlo. Nunca lo edites a mano |
| `frontend/apps/*/src/rutas.tsx` | Lo crea DC-02 con todas las rutas del catálogo, apuntando a páginas de relleno. **Para implementar una pantalla se reemplaza el contenido de su archivo en `paginas/`, no se toca `rutas.tsx`** |
| `src/Shapi.Api/Program.cs` | No se edita. Cada módulo se registra en su `Modulos/<Modulo>Modulo.cs`, que ya existe desde JG-01 |
| `Shapi.slnx` | Solo lo modifica JG-01. Los proyectos de demostración los agrega JZ-02 |
| `infra/caddy/Caddyfile`, `infra/compose*.yml` | Solo José Pablo. Si otra tarea necesita un cambio, la tarea lo indica |
| `.github/workflows/*` | Solo Jordin, salvo `publicar-imagenes.yml` (JZ-06) y `e2e.yml` (JZ-07), que son de José Pablo |
| `docs/plan/tareas/*`, `docs/plan/bitacora/*` | Cada quien edita los suyos. Las tareas de convergencia de Jordin pueden crear tareas para otros |

## 4. Git, *commits* y PR

- **Ramas:** `<persona>/<ID>-<descripcion>`, por ejemplo `jose-pablo/JZ-01-infraestructura-local`.
- ***Commits*** en español, con el formato `tipo(modulo): descripción (ID)`. Los tipos son `feat`, `fix`, `test`, `docs`, `refactor`, `chore` y `ci`.
- **Un PR por tarea**, con el título `[<ID>] <título de la tarea>` y el cuerpo según la plantilla. La CI rechaza los títulos sin `[<ID>]` o con un ID que no existe.
- **Integración:** *squash*, con auto-merge cuando pasan las verificaciones obligatorias (`plan`, `backend`, `frontend`). No se exigen aprobaciones humanas. La rama debe estar al día con `main`.
- `main` siempre tiene que compilar y pasar todas las pruebas.

## 5. Convenciones de la API de control

- **Rutas** en español y en minúsculas, con guiones: `/api/apis/{apiId}/configuracion-rutas`. Los IDs son UUID.
- **JSON** en `camelCase` y en español: `{ "nombreEmpresa": "...", "cuotaLlamadas": 5000 }`. Las fechas van en ISO 8601 UTC y el dinero como número decimal con 2 cifras, más el campo `moneda: "GTQ"` donde haga falta.
- **Errores** en formato `application/problem+json` (ProblemDetails):
  ```json
  { "type": "about:blank", "title": "El plan no permite más APIs", "status": 422, "codigo": "limite_del_plan", "detalle": { "limite": "apis" } }
  ```
  Los códigos (`codigo`) son los que aparecen en las especificaciones: `correo_no_verificado`, `limite_del_plan`, `pago_rechazado`, `origen_inaccesible`, etc. Validación de datos → 400 `datos_invalidos`, con `errores` por campo (nombre del campo en camelCase → lista de mensajes). Sin sesión → 401. Sin permiso → 403. Recurso de otra organización o inexistente → 404. Regla de negocio → 422. Conflicto (un duplicado) → 409.
- **Listas paginadas:** `?pagina=1&tamano=20`, con la respuesta `{ "elementos": [...], "total": 123 }`.
- **Autorización:** cada endpoint declara su política (`RequireAuthorization("Permiso.X")`) según `docs/specs/04-roles-y-permisos.md`. Los endpoints del portal usan la sesión del ámbito `consumidor` y la organización se resuelve por el host.
- **CSRF:** todo método distinto de GET exige la cabecera `X-Requested-With: shapi`. El cliente del frontend la agrega siempre.

## 6. Convenciones del backend

- **Casos de uso:** una clase por caso de uso, en `Shapi.Aplicacion/<Modulo>/` (por ejemplo `RegistrarProveedor`), con un validador de FluentValidation y un resultado `Resultado<T>` (éxito o error con un `codigo`). Los endpoints solo traducen HTTP a caso de uso.
- **Dominio:** las reglas y las transiciones de estado viven en las entidades (`Suscripcion.EntrarEnGracia()`), no en los endpoints.
- **Tiempo:** siempre con `IReloj`, nunca `DateTime.UtcNow` directo, para poder probar y para el modo demostración.
- **Correo:** siempre con `IColaCorreo.Encolar(plantilla, destinatario, datos)`, que escribe en `correo_saliente`. Nunca se envía por SMTP desde la API.
- **Bitácora:** `IBitacora.Registrar(...)` en toda acción de la lista de `docs/specs/10-identidad-y-seguridad.md` §7.
- **Redis:** solo a través de `IPublicadorCache`, en el plano de control, y de `LlavesRedis`, en `Shapi.Contratos`. Nunca se escriben llaves "a mano".
- **Registros (logs)** estructurados con `ILogger`. Nunca se registran claves, contraseñas, tokens ni datos de tarjetas.
- **Configuración:** en `appsettings.json` y variables de entorno `SHAPI_*`. Los secretos, en `.env`, que no se versiona.

## 7. Convenciones del frontend

- React 19 con TypeScript estricto, Vite, React Router, TanStack Query para los datos y Tailwind 4 con los tokens de `packages/ui`.
- **Una pantalla por archivo** en `paginas/`, nombrada con su ID del catálogo. Los estados que no tienen mockup propio (cargando, error, sin permiso) se hacen con los componentes base de `packages/ui`.
- Los datos se piden con el cliente de `packages/api`, que usa los tipos generados desde los contratos. Nunca se escribe `fetch` suelto.
- Toda la interfaz está en español y trata al usuario de *usted*. Los textos se copian del mockup.
- **Pruebas:** Vitest y Testing Library por pantalla. Cada prueba verifica los textos y los datos clave del mockup y usa MSW para simular la API.

## 8. Pruebas

- **Backend:** xUnit con FluentAssertions. La integración usa Testcontainers (PostgreSQL 16 y Redis 7.4) y no simula la base de datos.
- **Nombres:** `Metodo_Escenario_ResultadoEsperado`, con el requisito en un comentario (`// RF-28`).
- **Datos:** cada prueba crea sus propios datos. No se depende de la siembra de demostración, salvo en E2E.
- **Aislamiento entre organizaciones (RNF-08):** todo endpoint de un módulo nuevo lleva una prueba que confirma que otra organización recibe 404.

## 9. Puertos y hosts de desarrollo

| Servicio | Local | A través de Caddy |
|---|---|---|
| API de control | `http://localhost:5080` | `https://shapi.localhost/api/*` y `https://{sub}.shapi.localhost/api/portal/*` |
| Compuerta | `http://localhost:5090` | `https://{sub}.api.shapi.localhost` |
| Panel (Vite) | `http://localhost:5173` | `https://shapi.localhost` |
| Portal (Vite) | `http://localhost:5174` | `https://{sub}.shapi.localhost` |
| PostgreSQL | `localhost:5432` (usuario `shapi`, contraseña de `.env.example`, base `shapi`) | — |
| Redis | `localhost:6379` | — |
| Mailpit | SMTP `localhost:1025`, web `http://localhost:8025` | `https://correo.shapi.localhost` |
| Origen Envíos (demo) | `http://localhost:5101` | (solo a través de la compuerta) |
| Origen Agro (demo) | `http://localhost:5102` | (solo a través de la compuerta) |
