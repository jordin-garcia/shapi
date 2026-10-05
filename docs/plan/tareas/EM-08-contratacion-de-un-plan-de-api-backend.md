---
id: EM-08
titulo: Contratación de un plan de API (backend)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-03
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

## Resultado

**Qué se hizo**
- Se implementó `POST /api/portal/suscripciones` con verificación de correo, validación de plan activo, serialización de contrataciones concurrentes, tokenización y cobro simulado. Los rechazos quedan registrados sin crear suscripción ni guardar datos de tarjeta; los cobros aprobados guardan el medio tokenizado, el pago y la suscripción en una transacción.
- Los planes gratuitos activan la suscripción sin tarjeta ni pago. El servicio de JG-07 guarda las dos claves dentro de la misma transacción; después del commit se publican la suscripción y las claves en Redis, y las claves completas se devuelven una sola vez.
- Se implementó `GET /api/portal/suscripcion` con plan, estado, periodo visible, próxima renovación, tarjeta enmascarada y límites.
- Se añadió contrato OpenAPI, pruebas de integración con PostgreSQL y Redis, configuración de pagos, filtro por organización y migración `PagoRechazadoSinSuscripcion`. El servicio de claves ahora ofrece `PrepararClavesParaSuscripcion` para permitir la escritura atómica y diferir la publicación hasta el commit.

**Decisiones tomadas**
- La restricción original de `pago` requería una suscripción para cada fila, lo que impedía registrar el rechazo sin activar una suscripción. Con autorización de Emilio, el esquema admite además intentos rechazados sin suscripción y los asocia con `consumidor_id` y `api_id`. Estos intentos no guardan medio de pago. La regla quedó documentada en 07 §3.4 y 09 §2.
- El plan inactivo responde 422 con el código existente `plan_no_encontrado`, al no haber un código más específico en el contrato de errores.

**Archivos principales:** `src/Shapi.Api/Suscripciones/**`, `src/Shapi.Dominio/{Pagos,Suscripciones}/**`, `src/Shapi.Infraestructura/{Persistencia,Claves}/**`, `src/Shapi.Aplicacion/Claves/IServicioClaves.cs`, `contratos/openapi/suscripciones.yaml` y `tests/Shapi.Api.Tests/Suscripciones/ContratacionTests.cs`.

### Correcciones de la auditoría (2026-10-04)

Paso 8 de `docs/plan/auditoria-2026-10-03.md`. El PR lleva el ID de EM-07 y corrige también H-60 y H-64 en la contratación.
- **H-60:** un plan de pago con precio 0 ya no se puede crear (400 y `ck_plan_api_pago`), así que no se llega al 500 de `ck_pago_monto` sin reembolso.
- **H-64:** el 400 de un plan de pago sin tarjeta trae `errores.tarjeta`.
- **H-67:** si algo falla después de autorizar el cobro y antes del commit, se reembolsa y se vuelve a lanzar la excepción. La prueba simula una falla al preparar las claves.
- **H-68:** la prueba del cobro aprobado, con el reloj a las 21:30 de Guatemala (03:30 UTC del día siguiente), comprueba:
  - `susc:{id}` en Redis;
  - el ciclo a la medianoche de Guatemala, con `fin = inicio + 30 días`;
  - el monto y el periodo del pago;
  - la marca, los últimos 4, el titular y el vencimiento del medio de pago.
- **H-69:** el rechazo no deja `medio_pago` ni `clave`. Dos contrataciones simultáneas dejan una sola suscripción, un solo pago y dos claves.
- **H-70:** un vencimiento o un titular inválidos responden 400 `datos_invalidos`, con `errores` en `tarjeta.vencimiento` y `tarjeta.titular`.
- **H-71:** si la pasarela responde `pasarela_no_disponible` al cobrar, se responde 503 sin registrar un pago, como en la tokenización (09 §2).
- **H-72:** la prueba de la consulta dice RF-20. En 07 están `consumidor_id` y `api_id` en el diagrama ER de `PAGO`, con sus FK y sus índices.
- **Decidido (3 oct):**
  - se acepta `PrepararClavesParaSuscripcion` en `IServicioClaves`;
  - un plan inactivo responde 422 `plan_no_encontrado`, documentado en `suscripciones.yaml`;
  - con una suscripción `suspendida`, el consumidor recibe 409 `suscripcion_existente`. Le corresponde a EM-13 resolver el caso «suspendida → finalizada: el consumidor contrata otro plan» de 09 §3 (aviso en la bitácora).
