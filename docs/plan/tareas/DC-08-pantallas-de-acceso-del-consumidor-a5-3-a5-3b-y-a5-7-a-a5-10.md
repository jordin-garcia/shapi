---
id: DC-08
titulo: Pantallas de acceso del consumidor (A5.3, A5.3b y A5.7 a A5.10)
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-02
depende_de: [DC-03, EM-05, EM-18]
requisitos: [RF-02, RF-03, RF-04, RF-05]
pantallas: [A5.3, A5.3b, A5.7, A5.8, A5.9, A5.10]
---

# DC-08 · Pantallas de acceso del consumidor (A5.3, A5.3b y A5.7 a A5.10)

**Responsable:** Dominique Contreras · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** DC-03, EM-05, EM-18

## Objetivo
Implementar en el portal el registro (directo y por invitación), la verificación, el inicio de sesión y la recuperación de contraseña del consumidor.

## Contexto que debes leer
- Mockups: `mockups/A5/Registro.dc.html`, `RegistroInvitacion.dc.html`, `Acceso.dc.html`, `Verificacion.dc.html`, `Recuperacion.dc.html`, `NuevaContrasena.dc.html`
- `contratos/openapi/identidad.yaml` (`/api/portal/auth/*`)
- `docs/specs/10-identidad-y-seguridad.md` §1 (destino después de entrar)

## Archivos que creas o modificas
- `frontend/apps/portal/src/paginas/A5-3-Registro.tsx`, `A5-3b-Invitacion.tsx`, `A5-7-Acceso.tsx`, `A5-8-Verificacion.tsx`, `A5-9-Recuperacion.tsx` y `A5-10-NuevaContrasena.tsx`
- `frontend/apps/portal/src/modulos/sesion/**` (sesión real)

## Criterios de aceptación
1. Cada pantalla reproduce su mockup con la marca del portal.
2. Después del registro se muestra A5.8. El enlace `/verificar-correo?token=` confirma el correo y entra.
3. Iniciar sesión lleva a `/cuenta/suscripcion` si hay suscripción y, si no, a `/planes`.
4. `/invitacion?token=` precarga el correo invitado y crea la cuenta ya verificada.
5. La recuperación responde lo mismo exista o no el correo.

## Pruebas obligatorias
- Vitest + MSW de cada pantalla

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Con el entorno levantado: registro en `https://envios.shapi.localhost`, correo en Mailpit con la marca de Envíos Xelajú, verificación y acceso

## Fuera de alcance
- Backend (EM-05)

## Avance 2026-10-02

- Las seis pantallas, la sesión real, los flujos con tokens y sus pruebas Vitest + MSW están implementados en la rama `dominique/DC-08-acceso-consumidor`.
- Lint, typecheck, 250 pruebas y build del frontend pasan.
- Pendiente para cerrar: EM-18 debe agregar `destino` a la sesión del consumidor; sin ese dato el frontend no puede distinguir una cuenta con suscripción de una cuenta nueva.
- La comprobación manual sigue pendiente: la infraestructura terminó de descargarse, pero otro proyecto ocupa el puerto local 5432. No se cerrará la tarea hasta integrar EM-18 y verificar el flujo completo con el entorno levantado.

## Resultado
- **Quién:** Dominique implementó las seis pantallas, la sesión real del portal y sus pruebas (PR #56). El coordinador terminó la tarea el 2026-10-03 a partir de ese trabajo (protocolo §E4), con la rama `jordin/DC-08-acceso-consumidor`, que parte de la de Dominique y conserva sus *commits*.
- **Hecho por Dominique:** A5.3, A5.3b, A5.7, A5.8, A5.9 y A5.10, con la marca dinámica del portal.
  - Consumen `/api/portal/auth/*` con los tipos generados.
  - Los enlaces de un solo uso no se reenvían si solo falla la consulta posterior de la sesión.
  - Pruebas de Vitest y MSW de cada pantalla.
  - Creó EM-18 (protocolo §C), porque la sesión no traía el destino.
- **Hecho por el coordinador:**
  - **Criterio 3 (destino):** integró antes EM-18 (PR #60). El portal usa ahora el tipo generado `SesionConsumidor`, sin la extensión local ni el `as`, y navega a `sesion.destino`.
  - **A5.3b:** si la invitación ya se aceptó y solo falló la consulta de la sesión, «Reintentar» ya no la vuelve a aceptar. Antes el backend respondía `token_invalido` aunque la cuenta ya existiera.
  - **Marca del portal:** el botón principal salía azul (Shapi) en las seis pantallas, porque `bg-principal` de `Boton` le ganaba a la clase de la página. `MarcoAcceso` redefine ahora `--principal`, `--principal-hover` y `--anillo-foco` con `--marca-principal`, y así el botón y el campo enfocado usan la marca, como en los mockups. En A5.3b, el correo invitado se ve de solo lectura, como en el mockup.
  - **A5.3 (CU-11 2a, revisión con Claude del PR #61):** con el correo ya registrado, el mensaje sale debajo del correo con los enlaces «Entrar» y «Recuperar la contraseña», como A1.1. En A5.3b, si la invitación se usó mientras se llenaba el formulario, se muestra «El enlace ya no sirve».
  - **A5.8:**
    - con el enlace vencido o usado se puede pedir otro escribiendo el correo, como en A1.2;
    - un fallo de red al reenviar ofrece «Reintentar»;
    - sin correo en la dirección, el campo ya no desaparece al escribir el primer carácter.
  - Las páginas se reformatearon en varias líneas, como el resto del frontend, sin cambiar textos ni comportamiento.
  - **Pruebas nuevas:** reintento después de aceptar, confirmar y restablecer, con la sesión fallando una vez (la de A5.3b fallaba antes de la corrección); enlace de verificación vencido; reintento del reenvío; campo de correo de A5.8; colores de la marca.
- **Verificación manual (B8)**, en el ambiente productivo simulado con la API de Envíos Xelajú, contra el backend real:
  - registro, correo en Mailpit con el enlace al host del portal, verificación y entrada;
  - destino `/planes` sin suscripción y `/cuenta/suscripcion` con una suscripción vigente;
  - enlace ya usado, credenciales inválidas, recuperación neutral, restablecimiento e invitación (cuenta ya verificada).
  - Las capturas de las seis pantallas coinciden con sus mockups en textos, datos, orden y estados. Diferencia aceptada: A5.10 no muestra el correo (11 §Precisiones).
- **Pendiente ajeno a DC-08:** el correo de verificación llega sin la marca de Envíos Xelajú. Es la plantilla básica de JZ-03, y ponerle la marca del portal es el criterio 2 de JZ-11 (José Pablo). El backend ya manda `nombrePortal` y `colorPortal` en los datos del correo.
- **Decisiones:** se mantienen las de Dominique: `/verificar-correo?token=` (10 §1), A5.10 sin el correo (11 §Precisiones) y el encabezado de las pantallas de acceso solo con la marca. La que se agregó: los colores de la marca se aplican en `MarcoAcceso`, no en cada botón.
- **Archivos principales:**
  - `frontend/apps/portal/src/paginas/A5-{3,3b,7,8,9,10}-*.tsx`
  - `frontend/apps/portal/src/modulos/sesion/` (`useSesionConsumidor.ts`, `useIdentidadConsumidor.ts`, `FormulariosAcceso.tsx`, `CerrarSesionConsumidor.tsx`)
  - `frontend/apps/portal/src/layouts/LayoutPublico.tsx`
  - `frontend/apps/portal/src/tests/AccesoConsumidor.test.tsx`

### Correcciones de la auditoría (2026-10-04)

Paso 9 de `docs/plan/auditoria-2026-10-03.md`. El PR lleva el ID de DC-03.
- **H-80:** si aceptar la invitación responde 409 `correo_ya_registrado`, A5.3b muestra el error con «Entrar» y «Recuperar la contraseña» (CU-11 2a), con prueba.
- **H-81:** después de enviar, A5.9 muestra la respuesta neutral una sola vez.
- **H-82:** en A5.7, «Contraseña» es un `<label>` del campo.
- **H-83:** «Reintentar» como botón secundario quedó como criterio 4 de DC-16, para el panel y el portal.
- **H-84:** 11 §4 tiene ahora la sección «Comportamiento del portal y de sus pantallas de acceso (DC-03 y DC-08)».
- **Decidido (3 oct):** (a). A5.10 dice «Defina una contraseña nueva para su cuenta en el portal de {nombrePortal}.».
- La prueba de la marca de las pantallas de acceso ahora mira la raíz del portal, adonde se movieron los colores (H-74).
