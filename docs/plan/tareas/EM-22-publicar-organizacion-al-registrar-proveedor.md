---
id: EM-22
titulo: Publicar la organización al registrar un proveedor
persona: emilio
responsable: Emilio Méndez
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-23
depende_de: [EM-02, JG-04]
requisitos: [RF-01, RF-11, RNF-04]
pantallas: []
---

# EM-22 · Publicar la organización al registrar un proveedor

**Responsable:** Emilio Méndez · **Avance:** final · **Prioridad:** P1

## Objetivo
Eliminar el 403 `api_no_disponible` de la primera API de un proveedor nuevo en el plan Prueba: su organización debe estar en Redis sin esperar la resincronización de cinco minutos.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-01 y RF-11
- `docs/specs/07-modelo-de-datos.md` §3.1 y §4
- `docs/specs/10-identidad-y-seguridad.md` §1 a §3
- `docs/plan/tareas/EM-02-registro-verificacion-de-correo-e-inicio-de-sesion-del-perso.md`

## Archivos que creas o modificas
- `src/Shapi.Api/Identidad/Endpoints.cs`
- `tests/Shapi.Api.Tests/Identidad/RegistroCacheTests.cs`
- `tests/Shapi.Api.Tests/Identidad/AutenticacionTests.cs` y `RecuperacionTests.cs` (aislar la dependencia del publicador en sus pruebas existentes)

## Criterios de aceptación
1. Tras guardar el registro, se publica la organización mediante `IPublicadorCache.PublicarOrganizacion`, fuera de la transacción de negocio. Redis recibe su estado efectivo activo, cuota y ciclo del plan Prueba.
2. Una prueba de integración con PostgreSQL y Redis reales confirma esa publicación antes de que responda el registro, sin ejecutar el trabajador.
3. La E2E de JZ-13 registra un proveedor en Prueba, publica su primera API y recibe 200 con la clave del consumidor en menos de diez segundos, sin contratar un plan de plataforma ni forzar la resincronización.

## Pruebas obligatorias
- `RegistroCacheTests`: fase roja por ausencia de `org:{id}` y fase verde tras la corrección.
- Pruebas dirigidas de Identidad y recorrido E2E de JZ-13.

## Verificación
```
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests --filter FullyQualifiedName~Shapi.Api.Tests.Identidad
cd tests/e2e && pnpm test
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Cambiar los permisos, el contrato HTTP o la periodicidad de la resincronización.

## Notas
- Detectada al ejecutar JZ-13: la compuerta devolvió `403 api_no_disponible` durante toda la ventana de diez segundos y la regresión de integración encontró el contexto de Redis nulo.
- José Pablo autorizó expresamente corregir el endpoint y agregar la regresión dentro del PR de JZ-13 el 23 de octubre de 2026.

## Resultado

- José Pablo corrigió la falla durante JZ-13, con autorización expresa: `POST /api/auth/registro` llama a `IPublicadorCache.PublicarOrganizacion` después de guardar el registro, sin cambiar su contrato HTTP ni esperar al trabajador.
- `RegistroCacheTests` verifica en PostgreSQL y Redis reales el estado activo, la cuota y los extremos del ciclo del plan Prueba antes de que termine el registro. La prueba falló por contexto nulo antes de la corrección y pasó después.
- Las pruebas existentes de autenticación y recuperación aíslan el publicador con NSubstitute, conservando sus aserciones; la regresión específica y las E2E comprueban la publicación real.
- Pasaron las 268 pruebas dirigidas de Identidad, la compilación completa y las 14 E2E, sin omisiones. La primera petición de la API de un proveedor nuevo funciona dentro de diez segundos sin contratar plataforma ni forzar la resincronización.
- Revisión independiente: LISTO, sin hallazgos de corrección. La precisión sobre el registro se agregó a `docs/specs/07-modelo-de-datos.md` §3.1.
- El formato completo pasó sin cambios y el plan de 77 tareas es válido.
