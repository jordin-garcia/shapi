---
id: DC-11
titulo: Suscripción y claves del consumidor (B2.3 a B2.6)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-10
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

## Resultado
- B2.3 consulta la suscripción y las claves reales, muestra los estados vigente, en gracia, suspendido, proveedor no disponible y vacío, y respeta los periodos inclusivos que entrega EM-10.
- «Pagar con otra tarjeta» usa `POST /api/portal/suscripcion/pagar`, conserva el formulario ante un rechazo y actualiza la suscripción al autorizarse el cobro. El cambio de plan enlaza a B2.7.
- B2.4, B2.5 y B2.6 implementan la confirmación de rotación, la exposición única de la clave nueva, la revocación y la emisión posterior de una clave del mismo tipo.
- Las pruebas Vitest + MSW cubren los estados, el pago aprobado y rechazado, la rotación, la exposición única, la revocación y la emisión. También se actualizaron las pruebas estructurales que todavía esperaban la página de relleno.
- La comparación visual cubrió las cuatro variantes de B2.3 y los tres pasos de claves. Como el backend actual no publica la suspensión del proveedor ni los datos de una gracia iniciada por reversión, se creó EM-21; B2.3 ya acepta de forma compatible `apiDisponible`, `graciaHasta`, `ultimoRechazoEn` y `diasRestantes`, y conserva los fallbacks actuales.
- Archivos principales: `frontend/apps/portal/src/modulos/claves/claves.ts`, `frontend/apps/portal/src/paginas/B2-3-Suscripcion.tsx`, `B2-4-RotarClave.tsx`, `B2-5-ClaveNueva.tsx`, `B2-6-RevocarClave.tsx` y `frontend/apps/portal/src/tests/SuscripcionClaves.test.tsx`.
