---
id: EM-18
titulo: Publicar el destino de la sesión del consumidor
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: pendiente
programada: 2026-10-02
depende_de: [EM-05]
requisitos: [RF-04]
pantallas: []
---

# EM-18 · Publicar el destino de la sesión del consumidor

**Responsable:** Emilio Méndez · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-05

## Objetivo
Permitir que el portal lleve al consumidor a su suscripción cuando ya tiene una y a los planes cuando todavía no la tiene.

## Contexto que debes leer
- `docs/specs/10-identidad-y-seguridad.md` §1 (enrutamiento después de iniciar sesión)
- `docs/specs/05-casos-de-uso.md` CU-02, paso 3
- `docs/plan/tareas/DC-08-pantallas-de-acceso-del-consumidor-a5-3-a5-3b-y-a5-7-a-a5-10.md`

## Archivos que creas o modificas
- `src/Shapi.Api/Identidad/EndpointsPortal.cs`
- `contratos/openapi/identidad.yaml`
- `frontend/packages/api/src/generado/identidad.ts` (regenerar)
- `tests/Shapi.Api.Tests/Identidad/ConsumidorPortalTests.cs`

## Criterios de aceptación
1. `GET /api/portal/auth/sesion` devuelve `destino: "/cuenta/suscripcion"` cuando el consumidor tiene una suscripción de la API del portal actual.
2. Sin una suscripción de esa API, devuelve `destino: "/planes"`.
3. El esquema `SesionConsumidor` declara ambos valores y los tipos TypeScript quedan regenerados.

## Pruebas obligatorias
- Integración: sesión con suscripción y sesión sin suscripción.

## Verificación
Todos estos comandos deben pasar:
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm generar:api && pnpm typecheck
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Pantallas del portal (DC-08).

## Notas
- DC-08 detectó que EM-05 publica `correoVerificado`, pero no el destino exigido por RF-04 y 10 §1. El frontend no debe duplicar la regla de suscripciones ni asumir siempre `/planes`.
