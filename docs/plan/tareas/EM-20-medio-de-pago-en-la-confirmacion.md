---
id: EM-20
titulo: Medio de pago en la confirmación de la contratación (A5.4b)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P3
estado: pendiente
programada: 2026-10-17
depende_de: [DC-17]
requisitos: [RF-20]
pantallas: [A5.4b]
---

# EM-20 · Medio de pago en la confirmación de la contratación (A5.4b)

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P3 · **Depende de:** DC-17

## Objetivo
Que la respuesta de la contratación incluya el medio de pago y que A5.4b muestre la fila «Medio de pago» de su mockup, que DC-09 tuvo que omitir.

## Contexto que debes leer
- `docs/specs/11-interfaz.md` (nota de A5.4b después del catálogo de A5)
- `docs/specs/09-cobros-y-suscripciones.md` §2
- Mockup: `mockups/A5/Confirmacion.dc.html`
- `docs/plan/bitacora/dominique.md` (entrada de DC-09, aviso a EM-08)

## Archivos que creas o modificas
- `contratos/openapi/suscripciones.yaml` (modificar `Contratacion`)
- `src/Shapi.Api/Suscripciones/**` (modificar)
- `frontend/packages/api/**` (regenerar con `pnpm generar:api`)
- `frontend/apps/portal/src/paginas/A5-4b-Confirmacion.tsx` (modificar)
- `docs/specs/11-interfaz.md` (quitar la nota de A5.4b)
- `tests/**`

## Criterios de aceptación
1. En un plan de pago, `POST /api/portal/suscripciones` devuelve el medio de pago: la marca y los últimos 4 dígitos. Nunca el número completo ni el token. En un plan gratuito, no devuelve medio de pago.
2. A5.4b muestra la fila «Medio de pago» como en el mockup («Mastercard •••• 3057»). En un plan gratuito, la fila no aparece.
3. Se quita de `11-interfaz.md` la nota que justificaba omitir la fila.

## Pruebas obligatorias
- Integración (criterio 1)
- Vitest (criterio 2)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Suscripciones"
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm generar:api && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- La barra lateral de A5.4b (DC-17)
