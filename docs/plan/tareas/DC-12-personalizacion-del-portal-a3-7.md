---
id: DC-12
titulo: Personalización del portal (A3.7)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P2
estado: pendiente
depende_de: [DC-07]
requisitos: [RF-15]
pantallas: [A3.7]
---

# DC-12 · Personalización del portal (A3.7)

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** DC-07

## Objetivo
Permitir que el proveedor personalice el portal de cada API (logotipo, color, nombre y bienvenida) con vista previa en vivo.

## Contexto que debes leer
- Mockups: `mockups/A3/Portal.dc.html` y `mockups/A3/InicioPortal.dc.html`
- `docs/specs/07-modelo-de-datos.md` §3.2 (campos `portal_*`)
- `docs/specs/10-identidad-y-seguridad.md` §5 (SVG)

## Archivos que creas o modificas
- `src/*/Apis/**` (modificar)
- `contratos/openapi/apis.yaml`
- `frontend/apps/panel/src/paginas/A3-7-Portal.tsx`
- `tests/**`

## Criterios de aceptación
1. `PUT /api/apis/{id}/portal` `{nombre, color, bienvenida}` valida el color `#RRGGBB` y la bienvenida de hasta 280 caracteres. `PUT /api/apis/{id}/portal/logo` acepta PNG o SVG de hasta 512 KB (422 `logo_invalido`).
2. La vista previa muestra el inicio del portal con los valores sin guardar. "Abrir mi portal" abre `https://{sub}.shapi.localhost`.
3. Después de guardar, el portal publicado muestra los cambios.

## Pruebas obligatorias
- Integración: validaciones y tipos de archivo
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
- Dominio propio (DC-14)
