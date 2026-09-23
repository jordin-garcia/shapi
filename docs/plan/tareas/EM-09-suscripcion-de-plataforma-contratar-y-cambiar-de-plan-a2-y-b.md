---
id: EM-09
titulo: Suscripción de plataforma: contratar y cambiar de plan (A2 y B1.4)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P1
estado: pendiente
depende_de: [EM-06, EM-03, JG-04]
requisitos: [RF-19, RF-20, RF-25]
pantallas: [A2.1, A2.2, A2.3, A2.4, A2.5, B1.4]
---

# EM-09 · Suscripción de plataforma: contratar y cambiar de plan (A2 y B1.4)

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-06, EM-03, JG-04

## Objetivo
Permitir que el propietario vea los planes de plataforma, contrate uno de pago, cambie de plan con prorrateo y vea el estado de su suscripción.

## Contexto que debes leer
- `docs/specs/09-cobros-y-suscripciones.md` §3, §4 y §5 **completos**
- `docs/specs/05-casos-de-uso.md` CU-10
- Mockups: `mockups/A2/*.dc.html`, `mockups/B1/Suscripcion*.dc.html`
- `docs/specs/04-roles-y-permisos.md` (solo el propietario contrata)

## Archivos que creas o modificas
- `src/*/Suscripciones/**` (modificar)
- `contratos/openapi/{suscripciones,planes}.yaml` (modificar)
- `frontend/apps/panel/src/paginas/A2-1-Planes.tsx`, `A2-2-Contratacion.tsx`, `A2-3-Confirmacion.tsx`, `A2-4-Rechazo.tsx`, `A2-5-CambioPlan.tsx` y `B1-4-Suscripcion.tsx`
- `tests/*/Suscripciones/**`

## Criterios de aceptación
1. `GET /api/planes-plataforma` (público: planes activos por orden) y `GET /api/suscripcion` (plan, estado, periodo que se muestra, renovación, tarjeta, `graciaHasta`, días restantes, cambio programado).
2. `POST /api/suscripcion/contratar` `{planId, tarjeta | usarRegistrada}`, desde Prueba o desde un plan gratuito: cobra el precio completo, finaliza la suscripción anterior y crea la nueva con el ciclo desde hoy. Si se rechaza → 402 y todo queda igual (A2.4).
3. `POST /api/suscripcion/cambiar` `{planId}`: si es una subida (precio diario mayor), aplica la fórmula de 09 §5. Con el ejemplo de A2.5 (Lanzamiento → Producto con 13 de 30 días restantes) el crédito es 86.23, el cargo 259.57 y el monto a pagar **173.34**. Si es una bajada, queda programada en `plan_siguiente_id`, o se rechaza con 422 `excede_limites_del_plan` si la organización excede los límites. `DELETE /api/suscripcion/cambio-programado` la cancela.
4. `POST /api/suscripcion/pagar` `{tarjeta}`: si la suscripción está en gracia o suspendida, cobra y reactiva con un ciclo nuevo desde hoy. En los dos casos se publica la organización en Redis.
5. Bitácora: `suscripcion_plataforma.contratada` y `suscripcion_plataforma.cambiada`. Solo el propietario puede hacerlo; los demás roles reciben 403.
6. Las pantallas A2.1 a A2.5 y B1.4 (vigente, en gracia y suspendida) reproducen sus mockups, con las fechas en el formato de 09 §4.

## Pruebas obligatorias
- Unitarias del prorrateo (el ejemplo de A2.5, el cambio a un plan con otra vigencia y la bajada)
- Integración de cada endpoint
- Vitest de las pantallas

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Renovación automática (EM-10)
- Límites del plan (EM-13)
