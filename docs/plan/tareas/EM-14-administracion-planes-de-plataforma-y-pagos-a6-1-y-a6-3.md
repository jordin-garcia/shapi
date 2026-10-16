---
id: EM-14
titulo: Administración: planes de plataforma y pagos (A6.1 y A6.3)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-16
depende_de: [EM-09, DC-02]
requisitos: [RF-17, RF-24]
pantallas: [A6.1, A6.3, A6.3b]
---

# EM-14 · Administración: planes de plataforma y pagos (A6.1 y A6.3)

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-09, DC-02

## Objetivo
Permitir que el administrador administre los planes de plataforma y revise y revierta los pagos de plataforma.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-17 y RF-24
- `docs/specs/09-cobros-y-suscripciones.md` §7
- `docs/specs/05-casos-de-uso.md` CU-17 y CU-19
- Mockups: `mockups/A6/Main.dc.html`, `NuevoPlan`, `EditarPlan`, `Pagos`, `PagosVacia`, `RevertirPago`

## Archivos que creas o modificas
- `src/*/Planes/**` y `src/*/Pagos/**` (modificar)
- `contratos/openapi/{planes,pagos}.yaml`
- `frontend/apps/panel/src/paginas/A6-1-PlanesPlataforma.tsx`, `A6-3-Pagos.tsx` y `A6-3b-RevertirPago.tsx`
- `tests/*/**`

## Criterios de aceptación
1. `/api/admin/planes-plataforma` (GET, POST, PUT y `POST /{id}/desactivar`), solo para el administrador. Un plan con suscripciones se desactiva en lugar de borrarse. Bitácora `plan_plataforma.*`.
2. `GET /api/admin/pagos` lista **solo** los pagos de plataforma, de todas las organizaciones.
3. `POST /api/admin/pagos/{id}/revertir` (solo pagos `autorizado`): llama a `ReembolsarAsync`, deja el pago `revertido` con `revertido_en` y `revertido_por` y, si cubría el ciclo vigente, pasa la suscripción a `en_gracia` con `graciaHasta = ahora + 7 días`. Bitácora `pago.revertido`.
4. El soporte no ve estas pantallas (403). A6.1 (lista con Escala mensual y Escala anual, nuevo y editar), A6.3 y A6.3b reproducen sus mockups.

## Pruebas obligatorias
- Integración, incluidos los permisos
- Vitest

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Organizaciones (JZ-08)

## Resultado
- Se agregaron los endpoints administrativos para listar, crear, editar y desactivar planes de plataforma, y para consultar y revertir pagos. La reversión exige un pago autorizado, registra la bitácora y pone en gracia por siete días la suscripción cuyo ciclo vigente se reembolsó.
- Se implementaron A6.1, A6.3 y la confirmación A6.3b, con permisos, errores, estados vacíos y formularios conectados a la API. Los contratos OpenAPI y los tipos TypeScript se actualizaron.
- A6.1 ofrece la acción de desactivar desde la edición para que el administrador pueda aplicar la operación del criterio 1 sin borrar planes. La columna de límites mantiene singular/plural y el texto combinado de límites ilimitados del mockup.
- Verificación local: `dotnet build Shapi.slnx` y `dotnet format Shapi.slnx --verify-no-changes` pasan; `pnpm lint`, `pnpm typecheck`, `pnpm test` (451 pruebas) y `pnpm build` pasan; `node scripts/tareas.mjs --validar` informa 76 tareas válidas.
- Revisión en contexto limpio: revisión independiente LISTO, sin hallazgos pendientes. Se corrigió el nombre mostrado en los pagos por cambio de plan, reactivación y renovación, con cobertura para los tres formatos.
- Las pruebas de integración con Testcontainers no se pudieron ejecutar localmente porque Docker no está disponible; se ejecutarán en la CI del PR.
- La primera CI encontró que el nombre del plan nuevo usado por una prueba de integración ya existía en los datos sembrados. Lo cambié a un nombre exclusivo; la CI del nuevo commit volverá a ejecutar la suite backend completa.
