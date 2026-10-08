---
id: EM-09
titulo: Suscripción de plataforma: contratar y cambiar de plan (A2 y B1.4)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-05
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
2. `POST /api/suscripcion/contratar` `{planId, tarjeta | usarRegistrada}`, desde Prueba o desde un plan gratuito: cobra el precio completo, finaliza la suscripción anterior y crea la nueva con el ciclo desde hoy. Después del *commit*, publica en Redis la organización (`PublicarSuscripcion` de la nueva, que publica `org:{id}`). Si se rechaza → 402 y todo queda igual (A2.4).
3. `POST /api/suscripcion/cambiar` `{planId}`: si es una subida (precio diario mayor), aplica la fórmula de 09 §5. Con el ejemplo de A2.5 (Lanzamiento → Producto con 13 de 30 días restantes) el crédito es 86.23, el cargo 259.57 y el monto a pagar **173.34**. Si es una bajada, queda programada en `plan_siguiente_id`, o se rechaza con 422 `excede_limites_del_plan` si la organización excede los límites. `DELETE /api/suscripcion/cambio-programado` la cancela. Una subida publica en Redis la suscripción y la organización después del *commit* (`PublicarSuscripcion`, que publica `org:{id}`), porque cambian la cuota y los límites.
4. `POST /api/suscripcion/pagar` `{tarjeta}`: si la suscripción de un plan de pago está en gracia o suspendida, cobra y reactiva con un ciclo nuevo desde hoy. En los dos casos se publica la organización en Redis. Prueba o un plan de precio cero responde 422 `requiere_plan_de_pago` sin cobrar ni modificar datos: se contrata un plan de pago por A2.1 y A2.2 (RF-44, 09 §5).
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

## Resultado

- Se implementaron `GET /api/planes-plataforma`, la consulta de suscripción y los flujos de contratación, cambio, cancelación del cambio programado y reactivación por pago. Las operaciones registran cobros autorizados y rechazados, y publican la suscripción en Redis después del commit.
- Se implementaron las pantallas A2.1–A2.5 y B1.4 con los estados vigente, en gracia y suspendida, incluidos el prorrateo, los límites, la tarjeta registrada, las fechas en formato de Guatemala y los enlaces de cambio para una organización con plan de pago.
- Se agregó el contrato OpenAPI de planes y suscripciones y se generaron sus tipos TypeScript. Las pruebas cubren los montos del prorrateo, los endpoints, los rechazos de tarjeta y cambio, invitaciones vigentes en el límite de miembros, reactivación desde gracia y suspensión, concurrencia en contratación/cambio/reactivación, bitácora, permiso del propietario y aislamiento de organizaciones.
- Evidencia local: compilación y formato de .NET; 17 pruebas de integración de suscripciones y 3 unitarias de prorrateo; lint, typecheck, 308 pruebas de frontend y build; ocho capturas de los estados de las pantallas comparadas con sus mockups. La suite completa del backend queda a cargo de CI según el protocolo B7.

### Continuación por el coordinador (2026-10-07)

- Jordin terminó la tarea a partir de los commits de Emilio en el PR #81, con aviso previo confirmado al dueño, en una rama y un PR nuevos según protocolo §E4.
- Se impide reactivar Prueba o un plan gratuito mediante un cobro de cero: la API rechaza antes de preparar la tarjeta, cobrar o cambiar el ciclo. B1.4 lleva a contratar un plan de pago y explica el vencimiento sin atribuirlo a un cobro rechazado.
- Tras una subida o bajada confirmada, A2.5 vuelve a B1.4 con el plan actualizado o el aviso del cambio programado.
- Se corrigieron también las observaciones opcionales de la revisión: fechas de Guatemala, texto del ciclo nuevo al cambiar vigencia, cancelación idempotente sin bitácora ficticia, enlace de reactivación para planes de pago no activos y documentación de errores.
- Se agregaron pruebas de regresión de Prueba y gratuito en gracia/suspensión, conservación de datos ante el rechazo, navegación después de ambos cambios, zona horaria, cambio de vigencia y cancelación sin cambios.
- Se corrigieron los hallazgos de la revisión independiente: reintento con otra tarjeta, cambio desde un plan desactivado, explicación de la bajada anual, controles ocultos al lector, rechazo desde Prueba suspendida y contenido omitido de los mockups. Revisión final LISTO, sin hallazgos pendientes.
- Verificación de la continuación: compilación y formato de .NET correctos, 3 unitarias de prorrateo; frontend con lint y typecheck sin errores, 328 pruebas correctas en 21 archivos y compilación del panel y portal. El lint conserva 3 advertencias del portal existentes en main; la compilación avisa del tamaño del paquete del portal. Se compararon con los mockups las capturas de subida/bajada antes y después y Prueba/pago en gracia/suspensión, con datos de API simulados en navegador.
- Integración local: 106 pruebas de API correctas, sin omisiones, de Suscripciones y AutenticacionTests. Se ejecutaron las clases en secuencia mediante una configuración temporal de xUnit para reducir la presión de memoria; las pruebas de concurrencia mantienen sus operaciones simultáneas. La suite completa del backend se valida en CI según B7.
