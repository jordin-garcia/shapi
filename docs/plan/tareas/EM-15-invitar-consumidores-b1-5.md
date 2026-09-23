---
id: EM-15
titulo: Invitar consumidores (B1.5)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P2
estado: pendiente
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
