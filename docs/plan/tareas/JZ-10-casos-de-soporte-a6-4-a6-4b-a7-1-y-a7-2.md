---
id: JZ-10
titulo: Casos de soporte (A6.4, A6.4b, A7.1 y A7.2)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P1
estado: pendiente
depende_de: [JZ-03, DC-02]
requisitos: [RF-40]
pantallas: [A6.4, A6.4b, A7.1, A7.2]
---

# JZ-10 · Casos de soporte (A6.4, A6.4b, A7.1 y A7.2)

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** JZ-03, DC-02

## Objetivo
Permitir que los proveedores abran casos de soporte y conversen en ellos, y que el soporte y el administrador los atiendan con acceso de solo lectura a los datos de la organización.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-40
- `docs/specs/05-casos-de-uso.md` CU-20
- `docs/specs/04-roles-y-permisos.md` §3 y §4 (el soporte no modifica nada)
- Mockups: `mockups/A7/Casos.dc.html`, `Caso.dc.html`, `mockups/A6/Casos.dc.html`, `CasosVacia.dc.html`, `Caso.dc.html`

## Archivos que creas o modificas
- `src/Shapi.{Dominio,Aplicacion,Infraestructura,Api}/Soporte/**` (crear)
- `contratos/openapi/soporte.yaml` (crear)
- `frontend/apps/panel/src/paginas/A7-1-Casos.tsx`, `A7-2-Caso.tsx`, `A6-4-Casos.tsx` y `A6-4b-Caso.tsx`
- `src/Shapi.Infraestructura/Correo/Plantillas/respuesta_caso.*`
- `tests/**`

## Criterios de aceptación
1. Proveedor: `GET /api/casos` (los de su organización), `POST /api/casos` `{asunto, apiId?, descripcion}` (el primer mensaje es la descripción), `GET /api/casos/{numero}` (con sus mensajes) y `POST /api/casos/{numero}/mensajes`. Lo pueden hacer el propietario, el editor y el lector.
2. Soporte y administrador: `GET /api/admin/casos`, `POST /api/admin/casos` (a nombre de una organización), `POST /{numero}/asignar`, `POST /{numero}/mensajes` y `POST /{numero}/cerrar`. Un caso cerrado no admite más mensajes (422 `caso_cerrado`).
3. `GET /api/admin/casos/{numero}/organizacion` devuelve, en **solo lectura**, el resumen de A6.4b: organización, API afectada, plan, ciclo, estado, número de APIs y de consumidores, y el dominio propio con su verificación.
4. Cada mensaje nuevo encola `respuesta_caso` para la otra parte. La bitácora registra `caso.abierto` y `caso.cerrado`. Los casos se muestran como `CAS-{numero}`.
5. Las cuatro pantallas reproducen sus mockups, incluida la conversación.

## Pruebas obligatorias
- Integración: permisos por rol, caso cerrado y aislamiento entre organizaciones
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
- Adjuntos
