---
id: DC-13
titulo: Pagos y cambio de plan del consumidor (B2.2 y B2.7)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P2
estado: pendiente
depende_de: [EM-11, EM-13, DC-11]
requisitos: [RF-21, RF-25]
pantallas: [B2.2, B2.7]
---

# DC-13 · Pagos y cambio de plan del consumidor (B2.2 y B2.7)

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** EM-11, EM-13, DC-11

## Objetivo
Mostrarle al consumidor su historial de pagos y permitirle cambiar de plan con prorrateo.

## Contexto que debes leer
- Mockups: `mockups/B2/Pagos.dc.html`, `PagosVacia.dc.html`, `CambioPlan.dc.html`
- `docs/specs/09-cobros-y-suscripciones.md` §5
- `contratos/openapi/{pagos,suscripciones}.yaml`

## Archivos que creas o modificas
- `frontend/apps/portal/src/paginas/B2-2-Pagos.tsx` y `B2-7-CambioPlan.tsx`

## Criterios de aceptación
1. B2.2 lista los pagos (`GET /api/portal/pagos`) con su variante vacía.
2. B2.7 muestra el plan actual y el de destino, y el cálculo del prorrateo (días restantes, crédito, cargo y monto a pagar hoy) igual que el mockup. Al confirmar, llama a `POST /api/portal/suscripcion/cambiar`. Si es una bajada, informa que se aplica en la próxima renovación.

## Pruebas obligatorias
- Vitest + MSW

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Backend (EM-11 y EM-13)
