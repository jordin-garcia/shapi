---
id: EM-08
titulo: Contratación de un plan de API (backend)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: pendiente
depende_de: [EM-05, EM-06, EM-07, JG-07]
requisitos: [RF-20, RF-26, RF-19]
pantallas: []
---

# EM-08 · Contratación de un plan de API (backend)

**Responsable:** Emilio Méndez · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-05, EM-06, EM-07, JG-07

## Objetivo
Permitir que un consumidor con el correo verificado contrate un plan de API, pague con la pasarela simulada o sin tarjeta si es gratuito, y reciba sus claves una sola vez.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §5.2 (secuencia de contratación)
- `docs/specs/09-cobros-y-suscripciones.md` §3, §4 y §8
- `docs/specs/05-casos-de-uso.md` CU-12
- `docs/specs/07-modelo-de-datos.md` §3.3 y §3.4
- Sección Resultado de JG-07 (servicio `EmitirClavesParaSuscripcion`)

## Archivos que creas o modificas
- `src/*/Suscripciones/**` y `src/*/Pagos/**` (crear o modificar)
- `contratos/openapi/suscripciones.yaml` (crear)
- `tests/*/Suscripciones/**`

## Criterios de aceptación
1. `POST /api/portal/suscripciones` `{planId, tarjeta?}`: si el correo no está verificado → 422 `correo_no_verificado`. Si ya hay una suscripción vigente en esa API → 409 `suscripcion_existente` (se usa el cambio de plan). Si el plan está inactivo → 422.
2. Un plan de pago tokeniza y cobra. Si se rechaza, se registra el pago `rechazado` y se responde 402 `pago_rechazado` con el motivo, **sin crear la suscripción**.
3. Si se autoriza, en una transacción se guardan el `medio_pago` (token, marca, últimos 4 y vencimiento), el `pago` (`autorizado`, concepto `contratacion`, periodo) y la `suscripcion_api` activa con su ciclo. Luego se emiten las claves (JG-07), se publican en Redis y se responde 201 con la suscripción y las claves en claro.
4. Un plan gratuito se activa sin tarjeta ni pago.
5. `GET /api/portal/suscripcion` devuelve la suscripción del consumidor en la API del portal: plan, estado, periodo que se muestra (`inicio – fin−1 día`), próxima renovación, tarjeta enmascarada, cuota y límite (para B2.3).

## Pruebas obligatorias
- Integración: cobro aprobado, rechazado (tarjeta 0002), plan gratuito, correo sin verificar y duplicado
- Las claves aparecen en Redis después de contratar

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Pantallas del portal (DC-09)
- Renovación (EM-10)
- Cambio de plan (EM-13)
