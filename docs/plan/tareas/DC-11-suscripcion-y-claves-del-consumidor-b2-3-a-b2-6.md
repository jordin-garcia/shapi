---
id: DC-11
titulo: Suscripción y claves del consumidor (B2.3 a B2.6)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P1
estado: pendiente
depende_de: [DC-09, JG-07]
requisitos: [RF-22, RF-23, RF-26, RF-27, RF-28]
pantallas: [B2.3, B2.4, B2.5, B2.6]
---

# DC-11 · Suscripción y claves del consumidor (B2.3 a B2.6)

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** DC-09, JG-07

## Objetivo
Mostrarle al consumidor su suscripción y sus claves, y permitirle rotar, revocar y emitir claves nuevas.

## Contexto que debes leer
- Mockups: `mockups/B2/Suscripcion.dc.html`, `SuscripcionGracia.dc.html`, `SuscripcionNoDisponible.dc.html`, `SuscripcionVacia.dc.html`, `RotarClave.dc.html`, `ClaveNueva.dc.html`, `RevocarClave.dc.html`
- `contratos/openapi/{suscripciones,claves}.yaml`
- `docs/specs/09-cobros-y-suscripciones.md` §3 (qué muestra cada estado)

## Archivos que creas o modificas
- `frontend/apps/portal/src/paginas/B2-3-Suscripcion.tsx`, `B2-4-RotarClave.tsx`, `B2-5-ClaveNueva.tsx` y `B2-6-RevocarClave.tsx`
- `frontend/apps/portal/src/modulos/claves/**`

## Criterios de aceptación
1. B2.3 muestra el plan, el estado, el periodo, la renovación y la tarjeta, y las claves **enmascaradas** (`shp_prod_••••7c2e`) con los botones Rotar y Revocar. Tiene las cuatro variantes: vigente, en gracia (aviso con los días restantes y "Pagar con otra tarjeta", que usa `POST /api/portal/suscripcion/pagar`), "API no disponible" (organización suspendida) y vacía ("Ver los planes").
2. Rotar → B2.4 (confirmación con la fecha en que deja de funcionar la anterior) → B2.5 (la clave nueva completa una sola vez, con "Cópiela ahora").
3. Revocar → B2.6 (confirmación). Después de revocar, aparece "Emitir una clave nueva" para ese tipo.
4. El botón "Cambiar de plan" lleva a B2.7.

## Pruebas obligatorias
- Vitest + MSW de cada estado y de cada flujo

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Pagos y cambio de plan (DC-13)
