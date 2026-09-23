---
id: JG-01
titulo: Andamiaje del backend, CI y reglas del repositorio
persona: jordin
responsable: Jordin García
avance: 1
prioridad: P1
estado: hecha
depende_de: []
requisitos: [RNF-14, RNF-15]
pantallas: []
---

# JG-01 · Andamiaje del backend, CI y reglas del repositorio

**Responsable:** Jordin García · **Avance:** 1 · **Prioridad:** P1 · **Sin dependencias**

## Objetivo
Crear la solución .NET 10 con la estructura de las especificaciones, la integración continua con las verificaciones obligatorias y la configuración de GitHub (protección de `main`, auto-merge, invitación a Emilio) para que los otros tres integrantes puedan empezar a trabajar. **Es la primera tarea del proyecto: desbloquea a todos.**

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §3 (procesos) y §9 (estructura y stack definitivo)
- `docs/plan/convenciones.md` §1 a §6 completo
- `docs/specs/07-modelo-de-datos.md` §4 (llaves de Redis)
- `docs/specs/08-compuerta.md` §4 y `docs/specs/03-requisitos.md` (códigos de error)
- `docs/specs/10-identidad-y-seguridad.md` §7 (catálogo de acciones de bitácora)
- `AGENTS.md` y `docs/plan/protocolo.md` §B11

## Archivos que creas o modificas
- `Shapi.slnx` (crear) con `src/Shapi.{Dominio,Aplicacion,Infraestructura,Api,Contratos,Compuerta,Trabajador}` y `tests/Shapi.{Dominio,Api,Compuerta}.Tests`
- `Directory.Build.props` (crear): `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`
- `Directory.Packages.props` (crear): gestión central con **todos** los paquetes del stack (06 §9) con versión fija: Yarp.ReverseProxy, Microsoft.EntityFrameworkCore (+Design, +Relational), Npgsql.EntityFrameworkCore.PostgreSQL, EFCore.NamingConventions, StackExchange.Redis, Microsoft.OpenApi (+ lector YAML), DnsClient, MailKit, FluentValidation (+DependencyInjectionExtensions), Microsoft.Extensions.Identity.Core, Microsoft.AspNetCore.OpenApi, Microsoft.AspNetCore.Mvc.Testing, xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, FluentAssertions **7.x** (licencia Apache), NSubstitute, Testcontainers.PostgreSql, Testcontainers.Redis
- `src/Shapi.Api/Program.cs` (crear): ProblemDetails, `GET /salud`, documento OpenAPI en desarrollo, llamadas a `AgregarModulo<X>` / `MapearModulo<X>` de los 15 módulos
- `src/Shapi.Api/Modulos/<Modulo>Modulo.cs` (crear 15 archivos vacíos, uno por módulo de convenciones §1)
- `src/Shapi.Aplicacion/Comun/` (crear): `Resultado<T>`, `Error(codigo, mensaje)`, interfaces `IReloj`, `IColaCorreo`, `IBitacora` (+ record `EntradaBitacora`), `IPublicadorCache`, `IContextoOrganizacion`, y `AccionesBitacora` con todas las constantes de 10 §7
- `src/Shapi.Infraestructura/Comun/` (crear): `RelojSistema` e implementaciones **nulas** de `IColaCorreo`, `IBitacora` e `IPublicadorCache` (solo registran en el log), registradas por defecto para que ningún módulo dependa del orden en que se terminan las tareas
- `src/Shapi.Contratos/CodigosError.cs` y `src/Shapi.Contratos/Redis/LlavesRedis.cs` (crear)
- `src/Shapi.Compuerta/Program.cs` y `src/Shapi.Trabajador/Program.cs` (crear, mínimos, con `/salud` en la compuerta)
- Carpetas de los 15 módulos en `Dominio`, `Aplicacion`, `Infraestructura` y `Api` (con `.gitkeep`)
- `tests/*/` (crear) con una prueba de humo cada uno
- `.github/workflows/ci.yml` (crear)
- `.gitignore` (modificar si hace falta para `bin/`, `obj/`, `.env`)

## Criterios de aceptación
1. `dotnet build Shapi.slnx` y `dotnet test Shapi.slnx` pasan. La prueba de humo de la API confirma que `GET /salud` responde 200 (con `WebApplicationFactory`).
2. La estructura coincide con `docs/plan/convenciones.md` §1: 15 módulos y 15 archivos `Modulos/<Modulo>Modulo.cs`, todos llamados desde `Program.cs`. Después de esta tarea, `Program.cs` no necesita editarse para agregar módulos.
3. Ningún `.csproj` declara versiones de paquetes: todas están en `Directory.Packages.props`.
4. `LlavesRedis` genera exactamente los formatos de `docs/specs/07-modelo-de-datos.md` §4, y cada uno tiene una prueba unitaria. `CodigosError` contiene todos los códigos de error de las especificaciones (08 §4, 03, 09, convenciones §5).
5. `.github/workflows/ci.yml` se ejecuta en `pull_request` y en `push` a `main`, con tres *jobs* de nombre exacto: `plan` (`node scripts/tareas.mjs --validar`), `backend` (setup de .NET 10, restore, build, `dotnet format --verify-no-changes` y test; Docker ya está disponible en `ubuntu-latest` para Testcontainers) y `frontend` (si existe `frontend/package.json`: Node 24, pnpm con `pnpm/action-setup` leyendo la versión del campo `packageManager`, `pnpm install --frozen-lockfile`, lint, typecheck, test y build; si no existe, termina con éxito y muestra un mensaje). Tiene `concurrency` para cancelar las ejecuciones anteriores del mismo PR.
6. Después de integrar esta tarea en `main`, el agente configura GitHub con `gh` (autenticado como Jordin, que es el administrador): (a) invita a `MiloDou` con permiso de escritura (`gh api -X PUT repos/jordin-garcia/shapi/collaborators/MiloDou -f permission=push`); (b) ejecuta `gh repo edit --enable-auto-merge --delete-branch-on-merge --enable-squash-merge --enable-merge-commit=false --enable-rebase-merge=false`; (c) protege `main` con las verificaciones obligatorias `plan`, `backend` y `frontend`, `strict: true` (la rama debe estar al día), sin aprobaciones obligatorias, sin *force push* y sin borrado.
7. Las implementaciones nulas de `IColaCorreo`, `IBitacora` e `IPublicadorCache` están registradas por defecto. Cada módulo dueño las reemplaza más adelante con `services.Replace(...)` o registrándolas después.

## Pruebas obligatorias
- Humo: `GET /salud` → 200 en la API
- Unitarias: cada formato de `LlavesRedis`
- Humo en `Shapi.Compuerta.Tests` y en `Shapi.Dominio.Tests`, para que los proyectos corran en la CI

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
node scripts/tareas.mjs --validar
gh api repos/jordin-garcia/shapi/branches/main/protection --jq '.required_status_checks.contexts'   # debe mostrar plan, backend y frontend
```

## ⚠️ Pasos que requieren a una persona
- Jordin debe tener `gh` autenticado con su cuenta de administrador (`gh auth status`).
- Emilio tiene que aceptar la invitación que le llega por correo.

## Fuera de alcance
- Lógica de negocio de cualquier módulo
- Frontend (DC-01)
- Docker Compose y Caddy (JZ-01)
- Esquema de base de datos (EM-01)

## Notas
- **Este primer PR todavía no puede usar auto-merge** porque la protección no existe. Cuando la CI pase, intégralo con `gh pr merge --squash --delete-branch` y **después** aplica la configuración del criterio 6 (en un segundo PR, solo si hace falta cambiar archivos; los ajustes de GitHub se hacen con `gh` y no necesitan PR).
- Si DC-01 o JZ-01 se integraron antes que esta tarea, conserva sus archivos. Esta tarea no los toca.

## Resultado
- **Solución:** `Shapi.slnx` con los 7 proyectos de `src/` y los 3 de `tests/`. `Directory.Build.props` fija `net10.0`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` y `AnalysisLevel=latest`. `Directory.Packages.props` declara todos los paquetes del stack con versión fija, y ningún `.csproj` declara versiones. Cada proyecto ya referencia los paquetes que va a usar, para que las tareas siguientes no tengan que editar los `.csproj`.
- **API:** `Program.cs` configura ProblemDetails, `GET /salud` y OpenAPI en desarrollo, y llama a `AgregarModulo<X>()` y `MapearModulo<X>()` de los 15 módulos. Las firmas son `IServiceCollection AgregarModulo<X>(this IServiceCollection services)` y `WebApplication MapearModulo<X>(this WebApplication app)`. Cada dueño edita solo su `Modulos/<X>Modulo.cs`. Puertos de desarrollo: API en 5080 y compuerta en 5090.
- **Comunes** (`Shapi.Aplicacion/Comun`): `Resultado` y `Resultado<T>` (con conversiones implícitas desde el valor y desde `Error`), `Error(Codigo, Mensaje, Detalle?)`, `IReloj.Ahora`, `IColaCorreo.Encolar(plantilla, destinatario, datos)`, `IBitacora.Registrar(EntradaBitacora)`, `IPublicadorCache` (`PublicarApi`, `PublicarClave`, `ExpirarClave`, `EliminarClave`, `PublicarSuscripcion` y `PublicarOrganizacion`), `IContextoOrganizacion.OrganizacionId` y `AccionesBitacora`, con las 31 acciones de 10 §7 y la lista `Todas`.
- **Infraestructura** (`Shapi.Infraestructura/Comun`): `AgregarServiciosComunes()` registra `RelojSistema` (sobre `TimeProvider`) y las implementaciones nulas `ColaCorreoNula`, `BitacoraNula` y `PublicadorCacheNulo`. Program.cs la llama antes que a los módulos, así que el dueño las reemplaza con solo registrar la suya en su módulo, o con `services.Replace(...)`. Las nulas nunca escriben en el log datos del correo ni hashes de claves.
- **Contratos:** `CodigosError` tiene los 12 códigos de 08 §4, los de 03, 09 y convenciones §5, y los que definen los criterios de aceptación de las demás tareas del plan. `LlavesRedis` genera todos los formatos de 07 §4, más `demo:reloj:desplazamiento` (09 §9).
- **Pruebas** (98): humo de `/salud` en la API y la compuerta, humo del Dominio, cada formato de `LlavesRedis`, `Resultado` y los servicios comunes (nulos por defecto, reemplazables). Tres pruebas leen las especificaciones: los códigos de la tabla de 08 §4, las acciones de la tabla de 10 §7 y los 15 módulos llamados desde `Program.cs`. Si la especificación cambia, esas pruebas fallan hasta que el código se actualice.
- **CI** (`.github/workflows/ci.yml`): jobs `plan`, `backend` y `frontend`, con `concurrency` por PR. El job `frontend` termina con éxito y muestra un mensaje mientras no exista `frontend/package.json`.
- **Decisiones:**
  - `Microsoft.OpenApi` y `Microsoft.OpenApi.YamlReader` se fijan en 2.12.2 porque `Microsoft.AspNetCore.OpenApi` 10 exige `[2.12, 3.0)`.
  - Se agrega `Microsoft.Extensions.Hosting`, que necesita el SDK Worker del trabajador (06 §3).
  - En 07 §4 se precisó la normalización de las llaves (UUID, host y hash en minúsculas; el hash de `cache:`; `{aaaammdd}` en America/Guatemala) y se agregó la fila de `demo:reloj:desplazamiento`.
- **Configuración de GitHub** (criterio 6): se aplica con `gh` después de integrar este PR.
