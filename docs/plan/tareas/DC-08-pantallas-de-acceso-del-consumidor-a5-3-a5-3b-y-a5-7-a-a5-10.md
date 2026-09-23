---
id: DC-08
titulo: Pantallas de acceso del consumidor (A5.3, A5.3b y A5.7 a A5.10)
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: pendiente
depende_de: [DC-03, EM-05]
requisitos: [RF-02, RF-03, RF-04, RF-05]
pantallas: [A5.3, A5.3b, A5.7, A5.8, A5.9, A5.10]
---

# DC-08 · Pantallas de acceso del consumidor (A5.3, A5.3b y A5.7 a A5.10)

**Responsable:** Dominique Contreras · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** DC-03, EM-05

## Objetivo
Implementar en el portal el registro (directo y por invitación), la verificación, el inicio de sesión y la recuperación de contraseña del consumidor.

## Contexto que debes leer
- Mockups: `mockups/A5/Registro.dc.html`, `RegistroInvitacion.dc.html`, `Acceso.dc.html`, `Verificacion.dc.html`, `Recuperacion.dc.html`, `NuevaContrasena.dc.html`
- `contratos/openapi/identidad.yaml` (`/api/portal/auth/*`)
- `docs/specs/10-identidad-y-seguridad.md` §1 (destino después de entrar)

## Archivos que creas o modificas
- `frontend/apps/portal/src/paginas/A5-3-Registro.tsx`, `A5-3b-RegistroInvitacion.tsx`, `A5-7-Entrar.tsx`, `A5-8-Verificacion.tsx`, `A5-9-Recuperacion.tsx` y `A5-10-NuevaContrasena.tsx`
- `frontend/apps/portal/src/modulos/sesion/**` (sesión real)

## Criterios de aceptación
1. Cada pantalla reproduce su mockup con la marca del portal.
2. Después del registro se muestra A5.8. El enlace `/verificar?token=` confirma el correo y entra.
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
