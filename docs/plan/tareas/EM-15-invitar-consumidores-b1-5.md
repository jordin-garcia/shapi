---
id: EM-15
titulo: Invitar consumidores (B1.5)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P2
estado: hecha
programada: 2026-10-18
depende_de: [EM-05]
requisitos: [RF-05]
pantallas: [B1.5]
---

# EM-15 · Invitar consumidores (B1.5)

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** EM-05

## Objetivo
Permitir que el proveedor invite consumidores a su portal y administre las invitaciones pendientes.

## Contexto que debes leer
- `docs/specs/05-casos-de-uso.md` CU-23
- Mockups: `mockups/B1/InvitarConsumidores.dc.html`

## Archivos que creas o modificas
- `src/*/Organizaciones/**` (modificar)
- `src/Shapi.Infraestructura/Correo/MotorPlantillasCorreo.cs` y `tests/Shapi.Api.Tests/Correo/MotorPlantillasCorreoTests.cs` (modificar con autorización explícita del usuario para renderizar y probar el enlace de la invitación)
- `contratos/openapi/organizaciones.yaml`
- `frontend/apps/panel/src/paginas/B1-5-InvitarConsumidores.tsx`
- `tests/**`

## Criterios de aceptación
1. `POST /api/apis/{apiId}/invitaciones` `{correo}` (propietario o editor) crea un token `invitacion_consumidor` de 7 días y encola `invitacion_consumidor` con el enlace `https://{sub}.shapi.localhost/invitacion?token=…`.
2. `GET` lista las pendientes (correo, enviada, vence). `POST /{id}/reenviar` genera un token nuevo. `DELETE /{id}` la cancela.
3. Un correo que ya es consumidor de esa organización → 409 `consumidor_existente`.
4. B1.5 reproduce el mockup.

## Pruebas obligatorias
- Integración
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
- Aceptación en el portal (EM-05 y DC-08)

## Resultado
- Se implementaron el contrato OpenAPI y los endpoints para crear, listar, reenviar y cancelar invitaciones a consumidores, con tokens de siete días y correo encolado al subdominio del portal.
- Se completó B1.5 con gestión de invitaciones pendientes, estados de carga/error/vacío y acciones accesibles; se regeneraron los tipos de la API.
- Se agregaron pruebas de integración para ciclo de vida, permisos, consumidor existente y aislamiento RNF-08 entre organizaciones, además de pruebas Vitest.
- Se documentó CU-23: las invitaciones son de la organización y su lista se comparte entre sus APIs; los correos ya invitados o con una invitación vigente responden 409 con códigos distintos.
- Decisiones: la lista devuelve el nombre de la API y del portal para reproducir el texto dinámico del mockup. El motor de correo arma el enlace del portal al renderizarlo desde el token y el host, y elimina el token de los datos al terminar.
- Archivos principales: `src/Shapi.Api/Organizaciones/InvitacionesConsumidoresApi.cs`, `contratos/openapi/organizaciones.yaml`, `frontend/apps/panel/src/paginas/B1-5-InvitarConsumidores.tsx` y sus pruebas.
