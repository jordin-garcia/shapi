---
id: JG-19
titulo: Prueba de la compuerta sin PostgreSQL, API de control ni trabajador (RNF-02 y RNF-04)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P2
estado: pendiente
programada: 2026-10-18
depende_de: []
requisitos: [RNF-02, RNF-04]
pantallas: []
---

# JG-19 · Prueba de la compuerta sin PostgreSQL, API de control ni trabajador (RNF-02 y RNF-04)

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P2 · **Sin dependencias**

## Objetivo
Verificar con una prueba automatizada lo que RNF-02 y RNF-04 piden medir: la compuerta valida y enruta con PostgreSQL apagado, y sigue enrutando aunque la API de control y el trabajador estén fuera de servicio.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RNF-02 y RNF-04
- `docs/specs/08-compuerta.md` §3
- `docs/plan/convergencia/2026-10-08.md` (clasificación de RNF-02 y RNF-04)

## Archivos que creas o modificas
- `tests/Shapi.Api.Tests/**` o `tests/Shapi.Compuerta.Tests/**` (crear)
- `infra/verificar.mjs` (modificar, solo si se elige comprobarlo en el ambiente productivo)

## Criterios de aceptación
1. Con una API publicada y una clave vigente ya escritas en Redis, se **detiene PostgreSQL** y una petición válida a la compuerta responde 200 desde el origen. Una clave inválida sigue respondiendo 401 `clave_invalida` y una ruta oculta, 403 `ruta_no_permitida` (RNF-02).
2. Con la API de control y el trabajador detenidos, o sin ellos en marcha, la compuerta sigue enrutando con Redis disponible (RNF-04).
3. Las pruebas llevan el código del requisito en su nombre (`RNF_02_…`, `RNF_04_…`) y corren en la CI.

## Pruebas obligatorias
- Integración con Testcontainers (detener el contenedor de PostgreSQL), o un paso del verificador del ambiente productivo. En `Shapi.Compuerta.Tests` hace falta referenciar `Testcontainers.PostgreSql`, que ya está en `Directory.Packages.props`

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test tests/Shapi.Compuerta.Tests   # o, si la prueba queda en la API: dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~RNF_0"
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Las pruebas de carga (JZ-14)
