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

### Correcciones del PR #46 (2026-09-29)
- A1.4a confirma el envío con el texto condicional de CU-03 y reserva «paso 2 de 2» para A1.4b.
- A1.4b espera la consulta de sesión después del restablecimiento; si falla, muestra el error y reintenta únicamente la sesión, sin consumir otra vez el token.
- Pruebas RF-03 del mensaje neutral, enlace vencido, error por campo y recuperación tras un fallo de sesión. La prueba de este último caso falló antes del arreglo con un rechazo sin manejar y sin botón de reintento.

### Correcciones de la auditoría (2026-10-04)

Paso 3 de `docs/plan/auditoria-2026-10-03.md` (H-15 a H-26), hechas por el coordinador:
- **Mismo tiempo exista o no la cuenta (H-15).** `ServicioRecuperacion.Solicitar` terminaba con un solo SELECT si la cuenta no existía, y si existía guardaba el token y el correo (10 §8). Ahora, si no existe, hace una escritura sobre una fila inexistente, en el mismo número de viajes a la base. Vale también para el portal, que usa el mismo servicio. Una prueba cuenta los comandos de las dos rutas, como la de H-51.
- **Límite por IP (H-16).** `restablecer`, del personal y del portal, lleva el límite de 10 por minuto. 10 §1 ya nombra `recuperar` y `restablecer`, y las dos teorías de límite los prueban.
- **A8.1 (H-17, H-18 y H-21):**
  - los bordes usan `border-borde-fila` y `border-borde-inactivo`; las clases anteriores no existían y salían casi negros;
  - la confirmación es el `Toast` de 4 s (11 §4);
  - los errores de red ofrecen «Reintentar» con `AvisoError`, como en A1;
  - no quedan colores hexadecimales;
  - las pruebas cubren organización y rol, «Cerrar sesión», `/admin/perfil`, el aviso, los errores por campo y el reintento.
- **Contraseña actual incorrecta (H-19).** Responde 400 `datos_invalidos` con `errores.contrasenaActual` («La contraseña actual no es correcta.»), que A8.1 muestra debajo del campo. Antes respondía 401 `credenciales_invalidas`, con un mensaje que hablaba de un correo. Se actualizó `identidad.yaml`.
- **Pruebas (H-20 y H-23):**
  - la respuesta de `recuperar` es idéntica en código, cuerpo y cabeceras exista o no la cuenta;
  - un token inventado da 422;
  - un token de consumidor no sirve en `/api/auth/restablecer`;
  - la contraseña actual incorrecta no cambia la contraseña;
  - después de cambiarla, la nueva funciona y la anterior no.
- **`/api/perfil` (H-22).** Usa la política `Permisos.EditarPerfil` (04 §3.1).
- **11 §4 (H-24).** La sección suelta `## §Precisiones` pasó a §4 como «Comportamiento de la recuperación y de Mi perfil (EM-04)», con los textos del enlace inválido de A1.4b y los de A8.1.
- **Código muerto (H-25).** Se quitaron `consultarPerfil`, que nadie usaba, y el `correo` del mock de `/perfil`.
- **Aclaraciones del cierre (H-26):**
  - el criterio 3 dice `{actual, nueva}`, y el contrato y el código usan `contrasenaActual` y `contrasenaNueva`;
  - los bordes de A8.1 no estaban «ajustados al mockup»;
  - el criterio 4 (ámbito del consumidor) lo prueban las pruebas de EM-05 (`ConsumidorPortalTests`).
- **Decisiones del paso 3 (10 §1):**
  - 3 solicitudes de recuperación por hora por cuenta;
  - al restablecer se invalidan los demás enlaces pendientes y se reinicia el bloqueo;
  - una contraseña actual incorrecta en Mi perfil cuenta para el bloqueo de RF-04, y con la cuenta bloqueada se responde 423.
