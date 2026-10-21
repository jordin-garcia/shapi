---
id: DC-12
titulo: Personalización del portal (A3.7)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P2
estado: hecha
programada: 2026-10-20
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

## Resultado
- Se agregaron en `contratos/openapi/apis.yaml` los endpoints `GET /api/apis/{id}/portal`, `PUT /api/apis/{id}/portal`, `GET /api/apis/{id}/portal/logo` y `PUT /api/apis/{id}/portal/logo`, junto con los esquemas `PersonalizacionPortal`, `PeticionPersonalizacionPortal`, `RutaVistaPreviaPortal`, `PlanVistaPreviaPortal` y el código `logo_invalido`, y se regeneraron los tipos de `@shapi/api`.
- Se implementó `PersonalizarPortal` en `Shapi.Aplicacion.Apis` y los métodos `ActualizarPortal` y `ActualizarLogoPortal` en `Api`, validando el color `#RRGGBB`, la bienvenida de hasta 280 caracteres y el logotipo PNG o SVG de hasta 512 KB (422 `logo_invalido`), y sirviendo el SVG con `Content-Security-Policy: sandbox` y `X-Content-Type-Options: nosniff`.
- Se implementó `frontend/apps/panel/src/paginas/A3-7-Portal.tsx` según `mockups/A3/Portal.dc.html` e `InicioPortal.dc.html`, con edición de marca, carga de logotipo, vista previa escalada en vivo antes de guardar y enlace «Abrir mi portal» hacia `https://{sub}.shapi.localhost`.
- Se agregaron las pruebas de integración en `tests/Shapi.Api.Tests/Apis/ApisTests.cs` y las pruebas de Vitest + MSW en `frontend/apps/panel/src/tests/Apis.test.tsx` y `rutas.test.tsx`.
