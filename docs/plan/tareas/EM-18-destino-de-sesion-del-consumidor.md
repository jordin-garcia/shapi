---
id: EM-18
titulo: Publicar el destino de la sesión del consumidor
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: hecha
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

## Resultado
- **Origen:** Dominique creó esta tarea en el PR #56 (DC-08), según el protocolo §C, al ver que la sesión del consumidor no traía el destino. Como ese PR no se había integrado, la tarea no estaba en `main`. La implementó el coordinador el 2026-10-03 (protocolo §E1), antes de terminar DC-08.
- **Hecho:**
  - `GET /api/portal/auth/sesion` devuelve `destino`: `/cuenta/suscripcion` si el consumidor tiene una suscripción vigente (no `finalizada`) a la API del portal actual; si no, `/planes` (10 §1, CU-02 paso 3). Las suscripciones `activa`, `en_gracia` y `suspendida` llevan a la suscripción, porque desde ahí el consumidor puede pagar o cambiar de plan.
  - Solo cuenta la API del host, no otra API del mismo proveedor (criterio 1).
  - `SesionConsumidor` en `contratos/openapi/identidad.yaml` declara `destino` como obligatorio, con sus dos valores. `frontend/packages/api/src/generado/identidad.ts` está regenerado.
- **Pruebas:** en `tests/Shapi.Api.Tests/Identidad/ConsumidorPortalTests.cs`:
  - `RF_04_Sesion_DestinoSegunLaSuscripcionVigenteEnLaApiDelPortal`, con los cinco casos: sin suscripción y con cada estado;
  - `RF_04_Sesion_UnaSuscripcionDeOtraApiDeLaOrganizacionNoCuenta`.
- **Decisiones:** "tiene suscripción" se interpreta como "tiene una suscripción vigente", que es la definición de 07 (índice único parcial `estado <> 'finalizada'`) y de ADR-19.
- **Archivos:** `docs/specs/10-identidad-y-seguridad.md` §1 (precisión de "suscripción vigente"), `src/Shapi.Api/Identidad/EndpointsPortal.cs`, `contratos/openapi/identidad.yaml`, `frontend/packages/api/src/generado/identidad.ts` y `tests/Shapi.Api.Tests/Identidad/ConsumidorPortalTests.cs`.

### Correcciones de la auditoría (2026-10-04)

Paso 2 de `docs/plan/auditoria-2026-10-03.md` (H-14): `SesionActual` ya no usa `IgnoreQueryFilters()` para leer el consumidor ni sus suscripciones. La sesión del consumidor trae su organización, así que el filtro global de 10 §2 ya aplica, y 10 §2 reserva `IgnoreQueryFilters` a la administración, al trabajador y a la compuerta. Las pruebas de EM-18 siguen pasando sin cambios.
