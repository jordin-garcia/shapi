---
id: EM-04
titulo: Recuperación de contraseña y Mi perfil (A1.4a, A1.4b y A8.1)
persona: emilio
responsable: Emilio Méndez
avance: 2
prioridad: P1
estado: hecha
programada: 2026-09-28
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

## Resultado
- `POST /api/auth/recuperar` responde 200 exista o no la cuenta; si existe, encola `recuperacion` con un token de 60 minutos en formato hash.
- `POST /api/auth/restablecer` valida la política de contraseña (10-128 chars, distinta del correo) antes de consumir el token, marca el token como usado dentro de una transacción, revoca todas las sesiones e inicia una nueva.
- `GET /api/perfil` y `PUT /api/perfil` (nombre, máx. 120 chars con `errores.nombre`).
- `POST /api/perfil/contrasena` valida la política (10-128 chars, distinta del correo), verifica la contraseña actual y revoca las demás sesiones (mantiene la activa).
- La lógica de recuperación está en `IServicioRecuperacion` con parámetro `ambito` para reutilización en EM-05.
- A1.4a, A1.4b y A8.1 implementadas según mockups. A1.4b muestra enlace (via `<Link>`) a `/recuperar` cuando el token es inválido o está vencido.
- **Precisión (§C):** A1.4b no muestra el correo porque el enlace solo lleva `?token=`. Usa el texto fijo "Defina una contraseña nueva para su cuenta." Documentado en `docs/specs/11-interfaz.md §Precisiones`.
- Contrato OpenAPI actualizado: `POST /api/auth/restablecer` 400 → `DatosInvalidos` (con `errores.contrasena`). Tipos TypeScript regenerados.
- Pruebas Vitest de las 3 pantallas y pruebas de integración backend (criterios 1-4, incluye validación contraseña=correo).
