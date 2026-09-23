---
id: EM-03
titulo: Pantallas de registro, verificación y acceso (A1.1 a A1.3)
persona: emilio
responsable: Emilio Méndez
avance: 1
prioridad: P1
estado: pendiente
depende_de: [EM-02, DC-02]
requisitos: [RF-01, RF-02, RF-04]
pantallas: [A1.1, A1.2, A1.3]
---

# EM-03 · Pantallas de registro, verificación y acceso (A1.1 a A1.3)

**Responsable:** Emilio Méndez · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** EM-02, DC-02

## Objetivo
Implementar en el panel las pantallas de registro del proveedor, aviso de verificación, confirmación del enlace e inicio de sesión, con el enrutamiento por rol.

## Contexto que debes leer
- Mockups: `mockups/A1/Main.dc.html`, `Verificacion.dc.html`, `Sesion.dc.html`
- `docs/specs/11-interfaz.md` §1, §3 (A1) y §4
- `docs/specs/10-identidad-y-seguridad.md` §1 (enrutamiento después de iniciar sesión)
- `contratos/openapi/identidad.yaml`

## Archivos que creas o modificas
- `frontend/apps/panel/src/paginas/A1-1-Registro.tsx`, `A1-2-Verificacion.tsx` y `A1-3-Entrar.tsx` (reemplazar los de relleno)
- `frontend/apps/panel/src/modulos/identidad/**` (crear: `useSesion` con los datos reales, formularios y la página `/verificar?token=`)
- `frontend/packages/api/src/generado/identidad.ts` (generar)

## Criterios de aceptación
1. Los textos, campos y orden coinciden con los mockups. Los errores de validación aparecen debajo de cada campo.
2. Después de registrarse, la persona ve A1.2 con su correo. `/verificar?token=…` llama a la API: si funciona, entra y va al destino según su rol; si falla, ofrece reenviar el enlace.
3. Iniciar sesión lleva al destino según el rol (proveedor → `/panel/apis`, administrador → `/admin/organizaciones`, soporte → `/admin/casos`). La cuenta bloqueada muestra su mensaje.
4. El guardia de rutas de DC-02 usa la sesión real: sin sesión, se redirige a `/entrar`.
5. El enlace "¿Olvidó su contraseña?" lleva a `/recuperar`.

## Pruebas obligatorias
- Vitest + Testing Library + MSW de cada pantalla y del enrutamiento por rol

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Con el entorno levantado: registro → correo en https://correo.shapi.localhost → enlace → panel. Captura de cada pantalla comparada con su mockup

## Fuera de alcance
- Recuperación (EM-04)
