---
id: EM-21
titulo: Disponibilidad del proveedor en la cuenta del consumidor
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P1
estado: pendiente
programada: 2026-10-19
depende_de: [EM-10, JZ-08]
requisitos: [RF-23]
pantallas: [B2.3]
---

# EM-21 · Disponibilidad del proveedor en la cuenta del consumidor

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-10, JZ-08

## Objetivo
Publicar en la consulta de suscripción del consumidor si la API del proveedor está disponible, para que B2.3 pueda distinguir una suspensión del proveedor de una suspensión de la suscripción del propio consumidor sin ocultar su plan ni sus claves.

## Contexto que debes leer
- `docs/specs/09-cobros-y-suscripciones.md` §3
- `docs/specs/03-requisitos.md` (RF-23)
- Mockup: `mockups/B2/SuscripcionNoDisponible.dc.html`
- `docs/plan/bitacora/dominique.md` (entrada de DC-11)

## Archivos que creas o modificas
- `contratos/openapi/suscripciones.yaml` (modificar `SuscripcionActual`)
- `src/Shapi.Api/Suscripciones/**` (modificar)
- `frontend/packages/api/**` (regenerar con `pnpm generar:api`)
- `tests/Shapi.Api.Tests/Suscripciones/**` (modificar)

## Criterios de aceptación
1. `GET /api/portal/suscripcion` incluye el booleano requerido `apiDisponible` sin dejar de devolver el plan, el periodo, el medio de pago y los límites de la suscripción.
2. `apiDisponible` es `false` cuando la organización está suspendida administrativamente o su suscripción de plataforma está suspendida; en otro caso es `true`. El periodo de gracia del proveedor todavía se considera disponible.
3. La consulta conserva el aislamiento por organización de RNF-08 y no cambia el estado de la suscripción del consumidor.
4. B2.3 de DC-11 ya consume la propiedad de forma compatible y muestra el aviso junto al plan y las claves, como en el mockup; esta tarea no modifica la pantalla.

## Pruebas obligatorias
- Integración para organización suspendida, suscripción de plataforma suspendida y proveedor disponible.
- Integración de aislamiento entre organizaciones.

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Suscripciones"
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm generar:api && pnpm typecheck
```

## Fuera de alcance
- Cambiar la disponibilidad en la compuerta o las transiciones de suscripción.
- Modificar B2.3 o sus pruebas de interfaz.

## Notas
- DC-11 no puede inferir este estado desde un 403: el mockup exige conservar visibles los datos reales de la suscripción y las claves. La propiedad explícita evita inventarlos o guardarlos fuera de la API.
