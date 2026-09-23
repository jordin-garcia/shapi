---
id: EM-12
titulo: Miembros e invitaciones (A4.2 y A8.2)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P2
estado: pendiente
depende_de: [EM-04]
requisitos: [RF-06, RF-07, RF-43]
pantallas: [A4.2, A8.2]
---

# EM-12 · Miembros e invitaciones (A4.2 y A8.2)

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** EM-04

## Objetivo
Permitir que el propietario invite miembros, cambie su rol y los quite, y que las personas invitadas acepten la invitación.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-06 y RF-43
- `docs/specs/05-casos-de-uso.md` CU-04
- `docs/specs/04-roles-y-permisos.md` §4
- Mockups: `mockups/A4/Miembros*.dc.html`, `QuitarMiembro.dc.html`, `mockups/A8/Invitacion.dc.html`

## Archivos que creas o modificas
- `src/*/Organizaciones/**` (crear)
- `contratos/openapi/organizaciones.yaml` (crear)
- `frontend/apps/panel/src/paginas/A4-2-Miembros.tsx` y `A8-2-Invitacion.tsx`
- `tests/*/Organizaciones/**`

## Criterios de aceptación
1. `GET /api/miembros`, `POST /api/miembros/invitaciones` `{correo, rol}` (editor o lector), `PUT /api/miembros/{id}/rol` y `DELETE /api/miembros/{id}`, solo para el propietario.
2. Invitar un correo que ya pertenece a otra organización → 422 `correo_en_otra_organizacion`. Superar `max_miembros` (contando las invitaciones vigentes) → 422 `limite_del_plan` con `limite: miembros`. El propietario no se puede quitar ni cambiar de rol.
3. La invitación encola `invitacion_miembro` con un token de 7 días. `GET /api/invitaciones/{token}` y `POST /api/invitaciones/{token}/aceptar` `{nombre, contrasena}` crean el usuario, con el correo verificado, y su membresía.
4. Bitácora: `miembro.invitado`, `miembro.rol_cambiado` y `miembro.quitado`.
5. A4.2 (con sus variantes y la confirmación para quitar) y A8.2 reproducen sus mockups, incluido el texto "Su plan Producto permite N miembros; tiene M".

## Pruebas obligatorias
- Integración de cada regla
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
- Cuentas de plataforma (JZ-09)
