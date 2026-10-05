---
id: EM-07
titulo: Planes de API (A4.1)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-01
depende_de: [EM-02, DC-04, JG-04]
requisitos: [RF-18, RF-19]
pantallas: [A4.1]
---

# EM-07 · Planes de API (A4.1)

**Responsable:** Emilio Méndez · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-02, DC-04, JG-04

## Objetivo
Permitir que el proveedor cree, edite y desactive los planes de su API, y exponer al portal los planes activos.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-18, RF-19
- `docs/specs/07-modelo-de-datos.md` §3.3 (`plan_api`)
- `docs/specs/05-casos-de-uso.md` CU-09
- Mockups: `mockups/A4/Main.dc.html`, `PlanesVacia.dc.html`, `EditarPlan.dc.html`, `NuevoPlan.dc.html`

## Archivos que creas o modificas
- `src/*/Planes/**` (crear)
- `src/Shapi.Api/Modulos/PlanesModulo.cs`
- `contratos/openapi/planes.yaml` (crear)
- `frontend/apps/panel/src/paginas/A4-1-Planes.tsx` (lista, nuevo y editar)
- `tests/*/Planes/**`

## Criterios de aceptación
1. `GET /api/apis/{apiId}/planes`, `POST`, `PUT /{planId}` y `POST /{planId}/desactivar` (propietario o editor; el lector solo lee). Validaciones de 07 §3.3: un plan gratuito tiene precio 0, la vigencia va de 1 a 366, la cuota y el límite son mayores que 0, y el nombre es único en la API.
2. Un plan con suscripciones no se elimina: se desactiva. Los cambios de cuota y de límite se publican **de inmediato** en Redis para las suscripciones vigentes (`IPublicadorCache.PublicarSuscripcion`). El precio aplica desde la siguiente renovación.
3. `GET /api/portal/planes` (público, según el host) devuelve los planes activos de la API del portal.
4. Bitácora: `plan_api.creado`, `plan_api.editado` y `plan_api.desactivado`.
5. La pantalla A4.1 reproduce la lista, la variante vacía, el formulario de edición y el de plan nuevo gratuito. El límite se muestra en "peticiones" y la cuota en "llamadas".

## Pruebas obligatorias
- Integración de los endpoints, con la publicación en Redis
- Vitest de la pantalla

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Contratación (EM-08)
- Planes en el portal (DC-09)


## Resultado
- **Hecho:** Se implementaron los endpoints y validaciones, la publicación de planes en caché, y se integró la interfaz A4.1 (mockups y pruebas de MSW). Se ajustó la bitácora con los eventos requeridos (`plan_api.creado`, `plan_api.editado`, `plan_api.desactivado`).
- **Decisiones:** Se implementó una lógica de nombre único considerando la API (incluso inactivos), y las validaciones de límite y precio usan 400 y 409 con la especificación `ErrorApi` en frontend.
- **Archivos:** `src/Shapi.Api/Planes/GestionarPlanes.cs`, `src/Shapi.Api/Planes/Endpoints.cs`, `tests/Shapi.Api.Tests/Planes/PlanesTests.cs`, `frontend/apps/panel/src/paginas/A4-1-PlanesApi.tsx`, `frontend/apps/panel/src/paginas/A4-1-PlanesApi.test.tsx`, `contratos/openapi/planes.yaml`.

### Correcciones de la auditoría (2026-10-04)

Paso 8 de `docs/plan/auditoria-2026-10-03.md`, en el mismo PR que EM-08 (`[EM-07] Correcciones de la auditoría: planes y contratación`).
- **H-58:** los planes se listan por precio y nombre, en el panel y en el portal.
- **H-59:** un precio con más de 2 decimales o mayor que Q 9,999,999,999.99 responde 400. Solo el UNIQUE `(api_id, nombre)` se traduce a 409 `plan_duplicado`; cualquier otro error de la base ya no se disfraza de nombre repetido.
- **H-60:** un plan de pago con precio 0 responde 400. La migración `PlanDePagoConPrecio` agrega `ck_plan_api_pago` (`es_gratuito OR precio > 0`), precisado en 07 §3.3.
- **H-61:** `PlanesRedisTests` edita un plan con Redis real y comprueba `cuota_llamadas` y `limite_minuto` en `susc:{id}` (criterio 2).
- **H-62:** en A4.1, el precio se edita con 2 decimales («149.00»), y un plan gratuito se muestra como «Q 0.00».
- **H-63:** la bitácora dice «…en la API de Cotización de Envíos», con `TextoBitacora.LaApi` (que ahora también usa el servicio de claves) y las constantes `AccionesBitacora.PlanApi*`.
- **H-64:** los 400 traen `errores` por campo, y A4.1 los muestra.
- **H-65:** las respuestas llevan `moneda: "GTQ"` y ya no `creadoEn` ni `actualizadoEn`. El contrato documenta el 404 de `/api/portal/planes`.
- **H-66:** los requisitos de las pruebas quedaron corregidos (RF-18). Vitest comprueba el cuerpo del PUT, «peticiones» y el 409.
- **Decidido (3 oct):**
  - el formulario del plan nuevo empieza con «Plan gratuito» marcado, como el mockup;
  - A4.1 no tiene botón para desactivar planes, porque el mockup solo muestra «Editar», y la vigencia solo ofrece 30 y 365 días, aunque la API acepta de 1 a 366. Se aceptó así;
  - pasar un plan con suscripciones vigentes de gratuito a pago (o al revés) responde 422 `plan_con_suscripciones`, precisado en 09 §5;
  - la lógica sigue en `Shapi.Api/Planes`. Si EM-13 la necesita, la moverá a `Shapi.Aplicacion` (aviso en la bitácora).
