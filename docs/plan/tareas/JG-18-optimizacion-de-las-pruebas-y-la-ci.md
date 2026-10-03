---
id: JG-18
titulo: Optimización de las pruebas y la CI
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P2
estado: hecha
programada: 2026-10-03
depende_de: []
requisitos: [RNF-15]
pantallas: []
---

# JG-18 · Optimización de las pruebas y la CI

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P2 · **Sin dependencias**

## Objetivo
Que cada tarea tarde menos en verificarse, sin quitar ninguna prueba ni ninguna verificación obligatoria. Se parte de un análisis de los tiempos reales del 3 oct 2026:
- `dotnet test Shapi.slnx` tardaba 15 min 18 s en la PC de Jordin (13 min 18 s solo `Shapi.Api.Tests`), y en la CI 2 min 33 s.
- Las 585 pruebas de `Shapi.Api.Tests` sumaban 7.5 min. El resto se iba en arrancar unos 12 contenedores de PostgreSQL y en migrar una base nueva en cada prueba.
- Los PR de Emilio y Dominique repetían la CI entre 5 y 10 veces. El hallazgo más repetido de la revisión con Claude era mecánico: faltaba `## Resultado`, `estado: hecha` o la bitácora.

## Contexto que debes leer
- `docs/plan/protocolo.md` B7, B10 y B11
- `docs/plan/convenciones.md` §2 y §4
- `.github/workflows/ci.yml`, `titulo-pr.yml` y `publicar-imagenes.yml`
- `tests/Shapi.Api.Tests/Persistencia/BaseDePrueba.cs`

## Archivos que creas o modificas
- `tests/Shapi.Api.Tests/Persistencia/PostgresCompartido.cs` (crear)
- Fixtures de PostgreSQL en `tests/Shapi.Api.Tests/` (modificar): `Persistencia/BaseDePrueba.cs`, `Apis/ApisTests.cs`, `Bitacora/BitacoraTests.cs`, `Identidad/AutenticacionTests.cs`, `Identidad/ConsumidorPortalTests.cs`, `Planes/PlanesTests.cs`, `Portal/PortalTests.cs`, `Correo/EnvioCorreoTests.cs`, `Comun/ServiciosComunesTests.cs` y `SaludTests.cs`
- `scripts/tareas.mjs` y `scripts/tareas.test.mjs` (modificar): `--validar-cierre`
- `scripts/reglas-repositorio.test.mjs` (modificar)
- `.github/workflows/ci.yml`, `.github/workflows/titulo-pr.yml` y `.github/workflows/publicar-imagenes.yml` (modificar)
- `docs/plan/protocolo.md`, `AGENTS.md`, `docs/plan/instalacion.md` y `.github/pull_request_template.md` (modificar)
- `docs/specs/06-arquitectura.md` §7.3 (modificar): las E2E corren en cada PR

## Criterios de aceptación
1. Cuando se ejecuta `Shapi.Api.Tests`, el sistema deberá usar un solo contenedor de PostgreSQL, y la base de cada prueba deberá nacer ya migrada. Cada prueba sigue teniendo su propia base.
2. Cuando un PR no deja su tarea con `estado: hecha` y `## Resultado`, el check `titulo` deberá fallar con el motivo. También deberá fallar si el PR no agrega una entrada en una bitácora, o si es una corrección de auditoría de una tarea ya hecha y le falta `### Correcciones de la auditoría (AAAA-MM-DD)`. Un PR `[<ID>] Bloqueada: …` deberá dejar la tarea bloqueada.
3. Cuando se abre o se actualiza un PR, el job `ambiente-productivo` deberá ejecutar las pruebas E2E en el ambiente que ya levanta.
4. El protocolo deberá permitir que, en local, se ejecuten solo las pruebas de lo que se tocó. La suite completa queda a cargo del check obligatorio `backend`.

## Pruebas obligatorias
- Las 585 pruebas de `Shapi.Api.Tests` pasan con el contenedor compartido.
- `scripts/tareas.test.mjs`: casos de `--validar-cierre`. `scripts/reglas-repositorio.test.mjs`: el paso de cierre del check `titulo` y las E2E en el PR.
- `--validar-cierre` aplicado a los 40 últimos PR integrados no rechaza ninguno.

## Verificación
```
node scripts/tareas.mjs --validar
node --test "scripts/*.test.mjs"
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Borrar o debilitar pruebas. La de RF-08, que espera 5 s reales, se queda: comprueba justamente ese límite.
- Cambiar `dotnet format`: `whitespace` y `style` ahorrarían unos 10 s en la CI, pero dejarían de revisar los analizadores.

## Resultado
- **Pruebas del backend:**
  - `PostgresCompartido` arranca un solo PostgreSQL 16 por proceso, sin `fsync` y con 300 conexiones, y le aplica las migraciones a `template1`. Así, cada base nueva que EF Core crea nace migrada, y `MigrateAsync` solo la crea.
  - Las clases conservan su fixture, ahora de una línea (`: PostgresDePrueba`). `SaludTests`, `ServiciosComunesTests` y el entorno de correo usaban la base por defecto de su propio contenedor; ahora cada uno usa una base propia.
  - En la PC de Jordin (i5-5200U de 2 núcleos), `Shapi.Api.Tests` bajó de 13 min 18 s a 7 min 16 s. Lo que queda es CPU: cada prueba arranca la API. Las pruebas de un solo módulo, que ahora bastan en local, tardan alrededor de 1 min (`Planes`: 57 s).
- **CI:** `ci.yml` corre `dotnet test Shapi.slnx -m:1`, un proyecto a la vez. Con los proyectos en paralelo, `Shapi.Api.Tests`, que ahora ocupa toda la CPU en menos tiempo, dejaba sin CPU a las pruebas de la compuerta que miden tiempos: una espera de menos de 3 s tardó 16 s. Así falló dos veces el PR #58. En la CI, `Shapi.Api.Tests` bajó de 2 min 33 s a 1 min 28 s.
- **Cierre de la tarea:**
  - `node scripts/tareas.mjs --validar-cierre` valida B10 con el título del PR y su diff: el commit de integración contra su primer padre, con `fetch-depth: 2`. Corre en el check `titulo`, después de validar el título.
  - En local (B11, antes del *push*) compara la rama contra `origin/main`, así que revisa lo mismo que la CI, aunque el último commit sea un *merge* de `main`.
  - La subsección de auditoría solo se exige si la tarea ya estaba hecha en `main`. Una tarea que el mismo PR crea y cierra, como EM-17, no la necesita.
  - Aplicado a los 40 últimos PR integrados, no rechaza ninguno.
- **E2E en el PR:** `publicar-imagenes.yml` (`ambiente-productivo`) instala Playwright y corre `tests/e2e` después de `infra/verificar.mjs`. `e2e.yml` sigue corriendo en `main`.
- **Protocolo:**
  - B7 pide `build` y `format` completos, y las pruebas de lo que se tocó. Si se tocó algo compartido, el proyecto completo.
  - Donde la Verificación de una tarea dice `dotnet test Shapi.slnx`, la evidencia es el check `backend`.
  - B11 corre `--validar-cierre` antes del *push*. Se actualizaron también `AGENTS.md` (comandos, "Siempre" y la Definición de terminado), `instalacion.md` y la plantilla del PR.
- **Decisiones de Jordin (3 oct 2026):** tarea nueva JG-18; pruebas dirigidas en local; todo en un solo PR.
- **Pendiente de decisión:** omitir los pasos de los jobs `backend` o `frontend` cuando el PR no toca nada que lean. El clasificador de permisos de Claude Code lo bloqueó por reducir verificaciones de la CI, y no se aplicó. Está descrito en la bitácora de Jordin.
