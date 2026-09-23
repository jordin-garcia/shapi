---
id: EM-04
titulo: Recuperación de contraseña y Mi perfil (A1.4a, A1.4b y A8.1)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: pendiente
depende_de: [EM-03]
requisitos: [RF-03, RF-04]
pantallas: [A1.4a, A1.4b, A8.1]
---

# EM-04 · Recuperación de contraseña y Mi perfil (A1.4a, A1.4b y A8.1)

**Responsable:** Emilio Méndez · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-03

## Objetivo
Permitir recuperar la contraseña con un enlace de un solo uso, cerrando las demás sesiones, y editar el perfil (nombre y contraseña).

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-03, RF-04
- `docs/specs/10-identidad-y-seguridad.md` §1
- `docs/specs/05-casos-de-uso.md` CU-03
- Mockups: `mockups/A1/Recuperacion.dc.html`, `NuevaContrasena.dc.html`, `mockups/A8/Perfil.dc.html`

## Archivos que creas o modificas
- `src/*/Identidad/**` (modificar)
- `contratos/openapi/identidad.yaml` (modificar)
- `frontend/apps/panel/src/paginas/A1-4a-Recuperacion.tsx`, `A1-4b-NuevaContrasena.tsx` y `A8-1-Perfil.tsx`
- `tests/*/Identidad/**`

## Criterios de aceptación
1. `POST /api/auth/recuperar` `{correo}` responde siempre lo mismo (200) y, si la cuenta existe, encola `recuperacion` con un token de 60 minutos.
2. `POST /api/auth/restablecer` `{token, contrasena}` cambia la contraseña, marca el token como usado, **revoca todas las sesiones** de la cuenta e inicia una nueva. Token inválido → 422 `token_invalido`.
3. `GET` y `PUT /api/perfil` (nombre) y `POST /api/perfil/contrasena` `{actual, nueva}`, que valida la contraseña actual y revoca las demás sesiones.
4. La lógica de recuperación queda en un servicio que recibe el ámbito como parámetro, para que EM-05 la reutilice con los consumidores.
5. Las pantallas A1.4a, A1.4b y A8.1 reproducen sus mockups. A8.1 se usa tanto en `/panel/perfil` como en `/admin/perfil`.

## Pruebas obligatorias
- Integración: la respuesta idéntica exista o no el correo, el token de un solo uso y la revocación de las sesiones
- Vitest de las 3 pantallas

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Consumidores (EM-05)
