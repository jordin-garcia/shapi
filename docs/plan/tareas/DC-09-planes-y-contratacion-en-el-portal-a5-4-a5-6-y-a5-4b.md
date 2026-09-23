---
id: DC-09
titulo: Planes y contratación en el portal (A5.4, A5.6 y A5.4b)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P1
estado: pendiente
depende_de: [DC-08, EM-08, EM-07]
requisitos: [RF-19, RF-20, RF-26]
pantallas: [A5.4, A5.6, A5.4b]
---

# DC-09 · Planes y contratación en el portal (A5.4, A5.6 y A5.4b)

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** DC-08, EM-08, EM-07

## Objetivo
Permitir que el consumidor vea los planes, contrate uno con tarjeta (o sin ella si es gratuito) y reciba sus claves una sola vez.

## Contexto que debes leer
- Mockups: `mockups/A5/Planes.dc.html`, `PlanesVacia.dc.html`, `Pago.dc.html`, `Confirmacion.dc.html`
- `contratos/openapi/{planes,suscripciones}.yaml`
- `docs/specs/09-cobros-y-suscripciones.md` §2 (tarjetas de prueba)

## Archivos que creas o modificas
- `frontend/apps/portal/src/paginas/A5-4-Planes.tsx`, `A5-6-Pago.tsx` y `A5-4b-Confirmacion.tsx`
- `frontend/apps/portal/src/paginas/A5-0-Inicio.tsx` (modificar: sección de planes)
- `frontend/apps/portal/src/modulos/suscripcion/**`

## Criterios de aceptación
1. A5.4 lista los planes activos (`GET /api/portal/planes`), con su variante vacía. En el inicio (A5.0) aparece la misma sección de planes.
2. Si el correo no está verificado, el botón "Contratar" lleva a un aviso para verificarlo. Sin sesión, lleva a `/entrar`.
3. A5.6 muestra el resumen del plan y el formulario de la tarjeta (número, titular, vencimiento, CVV) con validación básica, y el texto sobre el token. Si la tarjeta se rechaza, el error aparece en el mismo formulario.
4. A5.4b muestra la suscripción y **las dos claves completas una sola vez**, con el aviso "Cópielas ahora" y los botones Copiar. Al salir de la pantalla, ya no se vuelven a mostrar.
5. Un plan gratuito se contrata sin pasar por A5.6.

## Pruebas obligatorias
- Vitest + MSW: plan de pago aprobado, rechazado, plan gratuito y correo sin verificar

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Contratar el plan Comercio con 4242 4242 4242 4242 y confirmar que la clave funciona con curl contra la compuerta

## Fuera de alcance
- Suscripción y claves en la cuenta (DC-11)
