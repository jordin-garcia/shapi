---
id: EM-03
titulo: Pantallas de registro, verificación y acceso (A1.1 a A1.3)
persona: emilio
responsable: Emilio Méndez
avance: 1
prioridad: P1
estado: hecha
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
- `frontend/apps/panel/src/paginas/A1-1-Registro.tsx`, `A1-2-Verificacion.tsx` y `A1-3-Sesion.tsx` (reemplazar los de relleno)
- `frontend/apps/panel/src/modulos/identidad/**` (crear: `useSesion` con los datos reales, formularios y la página `/verificar-correo?token=`)
- `frontend/packages/api/src/generado/identidad.ts` (generar)

## Criterios de aceptación
1. Los textos, campos y orden coinciden con los mockups. Los errores de validación aparecen debajo de cada campo.
2. Después de registrarse, la persona ve A1.2 con su correo. `/verificar-correo?token=…` (el enlace del correo, 10 §1) llama a la API: si funciona, entra y va al destino según su rol; si falla, ofrece reenviar el enlace.
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

## Resultado

Lo terminó Jordin (coordinador, protocolo §E4) el 26 de septiembre, a partir del PR #16 de Emilio Méndez, que tenía la CI en rojo y hallazgos obligatorios de la revisión. Se conservaron los *commits* de Emilio y el PR #16 se cerró con un enlace al nuevo.

- **Pantallas:** A1.1 (`/registro`), A1.2 (`/verificar-correo`, con y sin `?token=`) y A1.3 (`/entrar`) tienen los textos, campos y orden de los mockups. Se comprobaron con el entorno levantado (registro → correo en Mailpit → enlace → panel → entrar) y con capturas contra los mockups. Los estados sin mockup quedaron descritos en 11 §4, "Comportamiento de las pantallas de acceso".
- **Módulo `modulos/identidad`:**
  - `useIdentidad.ts` tiene las cuatro operaciones del contrato, con la cabecera CSRF que declara el contrato; `interpretarError` para el `codigo`, el mensaje y los errores por campo; y `useIrAlDestino`, que consulta la sesión recién creada y lleva al destino según el rol (10 §1).
  - `Formularios.tsx` tiene la tarjeta de A1, el encabezado, el campo con su etiqueta asociada (`label for`, `aria-invalid`) y el aviso de error con «Reintentar» (11 §4).
- **Sesión:** `useSesion.ts` expone `consultarSesion` y `claveSesion`, para que `entrar` y `verificar-correo`, que responden sin cuerpo, lleven al destino sin una consulta más. El guardia de rutas de DC-02 ya usaba la sesión real (criterio 4).
- **Contrato:** `packages/api/src/generado/identidad.ts` se generó con `pnpm generar:api` desde `contratos/openapi/identidad.yaml`. Reemplaza la versión escrita a mano del PR #16, que no coincidía con el contrato. Se exporta como `@shapi/api/identidad`.
- **Correcciones sobre el PR #16:**
  - el `useEffect` de A1.2 volvía a llamar a `verificar-correo` en cada render: ahora llama una sola vez por token, también con StrictMode;
  - tras verificar, la pantalla no consultaba la sesión nueva;
  - el enlace inválido ofrecía reenviar sin tener el correo;
  - no se manejaba `cuenta_desactivada`;
  - las etiquetas no estaban asociadas a los campos;
  - había dos `any`;
  - no había ninguna prueba;
  - `rutas.test.tsx` buscaba los textos de relleno.
- **Fuera de A1, necesario para que las pantallas coincidan con el mockup:**
  - `packages/ui/src/style.css` declara `@source "./"`. Tailwind v4 solo buscaba clases en la app, así que las que usan únicamente los componentes base (`h-11`, `text-white`…) no se generaban y los campos y botones salían sin tamaño ni color en el navegador (hallazgo H-115 de la auditoría, de DC-01);
  - `LayoutPublico` ahora deja que la tarjeta se centre (`main` flexible, sin el relleno de 32 px) y el logotipo tiene el `gap` de 11 px y el `letter-spacing` del mockup.
- **Pruebas:** 24 en `apps/panel/src/tests/Identidad.test.tsx` (RF-01, RF-02 y RF-04, con MSW y StrictMode), una de regresión en `packages/ui/src/style.test.tsx`, y en `rutas.test.tsx` las pantallas implementadas se reconocen por su título y ninguna de las tres páginas públicas consulta la sesión. Frontend: 127 pruebas.
- **Decisiones:**
  - Manda 10 §1: el enlace es `/verificar-correo?token=`, no `/verificar?token=`, y se corrigió el texto de la tarea. El archivo de A1.3 es `A1-3-Sesion.tsx`, el que creó DC-02.
  - Los formularios usan `noValidate`, así que los mensajes de validación son los de la API, en español y debajo de cada campo, y no las burbujas del navegador.
  - El mensaje del reenvío no revela si la cuenta existe (10 §1).
