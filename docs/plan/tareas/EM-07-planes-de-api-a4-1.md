---
id: EM-07
titulo: Planes de API (A4.1)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: pendiente
depende_de: [EM-02, DC-04, JG-04]
requisitos: [RF-18, RF-19]
pantallas: [A4.1]
---

# EM-07 · Planes de API (A4.1)

**Responsable:** Emilio Méndez · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-02, DC-04, JG-04

## Objetivo
Permitir que el proveedor cree, edite y desactive los planes de su API, y exponer al portal los planes activos.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-18, RF-19
- `docs/specs/07-modelo-de-datos.md` §3.3 (`plan_api`)
- `docs/specs/05-casos-de-uso.md` CU-09
- Mockups: `mockups/A4/Main.dc.html`, `PlanesVacia.dc.html`, `EditarPlan.dc.html`, `NuevoPlan.dc.html`

## Archivos que creas o modificas
- `src/*/Planes/**` (crear)
- `src/Shapi.Api/Modulos/PlanesModulo.cs`
- `contratos/openapi/planes.yaml` (crear)
- `frontend/apps/panel/src/paginas/A4-1-Planes.tsx` (lista, nuevo y editar)
- `tests/*/Planes/**`

## Criterios de aceptación
1. `GET /api/apis/{apiId}/planes`, `POST`, `PUT /{planId}` y `POST /{planId}/desactivar` (propietario o editor; el lector solo lee). Validaciones de 07 §3.3: un plan gratuito tiene precio 0, la vigencia va de 1 a 366, la cuota y el límite son mayores que 0, y el nombre es único en la API.
2. Un plan con suscripciones no se elimina: se desactiva. Los cambios de cuota y de límite se publican **de inmediato** en Redis para las suscripciones vigentes (`IPublicadorCache.PublicarSuscripcion`). El precio aplica desde la siguiente renovación.
3. `GET /api/portal/planes` (público, según el host) devuelve los planes activos de la API del portal.
4. Bitácora: `plan_api.creado`, `plan_api.editado` y `plan_api.desactivado`.
5. La pantalla A4.1 reproduce la lista, la variante vacía, el formulario de edición y el de plan nuevo gratuito. El límite se muestra en "peticiones" y la cuota en "llamadas".

## Pruebas obligatorias
- Integración de los endpoints, con la publicación en Redis
- Vitest de la pantalla

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Contratación (EM-08)
- Planes en el portal (DC-09)
