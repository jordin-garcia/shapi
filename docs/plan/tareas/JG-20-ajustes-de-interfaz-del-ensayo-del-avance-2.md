---
id: JG-20
titulo: Ajustes de interfaz del ensayo del Avance 2 (tarjeta, barra lateral, cursor y contraseña)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P2
estado: pendiente
programada: 2026-10-15
depende_de: [DC-17]
requisitos: [RNF-12, RF-20]
pantallas: [N.1, A1.1, A1.3, A1.4b, A2.2, A2.5, A5.1, A5.3, A5.3b, A5.6, A5.7, A5.10, A8.1, A8.2, B1.4, B2.1, B3.2]
---

# JG-20 · Ajustes de interfaz del ensayo del Avance 2 (tarjeta, barra lateral, cursor y contraseña)

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** DC-17

## Objetivo
Corregir cuatro problemas de uso que Jordin encontró al ensayar la demostración del Avance 2 (8 oct). La especificación no los pide (de H-E3 solo pide que el pie de la barra lateral quede fijo), así que la tarea primero los agrega a `11-interfaz.md` y después los implementa en el panel, la administración y el portal.

## Contexto que debes leer
- `docs/specs/11-interfaz.md` §1 "Componentes base", §3 "Navegación" y §4 "Comportamiento de la estructura de navegación (DC-02)" (la línea «La estructura ocupa la altura de la ventana…»)
- `docs/specs/03-requisitos.md` RNF-12 y RF-20
- `docs/specs/09-cobros-y-suscripciones.md` §2 (tarjetas de prueba de la pasarela simulada)
- Mockups: `mockups/Navegacion/Main.dc.html` (N.1), `mockups/A5/Documentacion.dc.html` y los de la cuenta del consumidor (B2)
- `docs/plan/tareas/DC-17-barra-lateral-en-planes-y-pago-del-portal.md`: deja la barra lateral del portal en planes y pago; esta tarea parte de su resultado

## Origen
Hallazgos del ensayo del 8 oct de 2026, que Jordin decidió agrupar en esta tarea:
- **H-E1:** al volver de una tarjeta rechazada (A2.2 → A2.4), el formulario conserva el mes y el año de vencimiento y el titular; solo se borran el número y el CVV (`A2-2-Contratacion.tsx:92-93`).
- **H-E3:** la barra lateral del panel tiene su propia barra de desplazamiento (`Navegacion.tsx:67`, `overflow-y-auto`); en una ventana de ~880 px de alto no se ven todas sus opciones y su pie se desplaza, aunque 11 §4 (DC-02) lo pide fijo.
- **H-E5:** al pasar el cursor sobre los botones no aparece la mano: Tailwind 4 ya no pone `cursor: pointer` en los botones y `packages/ui/src/style.css` no lo restaura.
- **H-E6:** no hay forma de ver la contraseña que se escribe.

Los demás hallazgos del ensayo (H-E2, H-E4, H-E7, H-E8 y H-E9) incumplen tareas ya integradas y van a la próxima auditoría (protocolo §E), no a esta tarea.

## Decisiones de Jordin (9 oct)
- **Barra lateral:** se garantiza completa y sin desplazamiento **desde una pantalla de 1366 × 768**, aunque eso aparte el espaciado del mockup N.1.
- **Contraseña:** el botón usa un **ojo** (abierto para mostrar, tachado para ocultar).

## Archivos que creas o modificas
- `docs/specs/11-interfaz.md` (modificar: §1 "Componentes base" y §4 "Comportamiento de la estructura de navegación")
- `frontend/packages/ui/src/style.css` (modificar: cursor)
- `frontend/packages/ui/src/index.tsx` (modificar: campo de contraseña con el botón del ojo, por ejemplo `CampoContrasena`)
- `frontend/apps/panel/src/layouts/Navegacion.tsx`, `LayoutPanel.tsx` y `LayoutAdmin.tsx` (modificar)
- `frontend/apps/portal/src/layouts/**`, en particular `LayoutCuenta.tsx` y el layout que deje DC-17, y `frontend/apps/portal/src/paginas/A5-1-Documentacion.tsx` (modificar, barra lateral)
- Contraseña: `frontend/apps/panel/src/paginas/A1-1-Registro.tsx`, `A1-3-Sesion.tsx`, `A1-4b-NuevaContrasena.tsx`, `A8-1-Perfil.tsx`, `A8-2-Invitacion.tsx`; `frontend/apps/portal/src/paginas/A5-3-Registro.tsx`, `A5-3b-Invitacion.tsx`, `A5-7-Acceso.tsx`, `A5-10-NuevaContrasena.tsx` (modificar)
- Tarjeta: `frontend/apps/panel/src/paginas/A2-2-Contratacion.tsx`, `A2-5-CambioPlan.tsx`, `B1-4-Suscripcion.tsx`; `frontend/apps/portal/src/paginas/A5-6-Pago.tsx` (modificar)
- `frontend/apps/*/src/tests/**` (crear o modificar)
- `tests/e2e/` (crear: la prueba de la barra lateral)

## Criterios de aceptación
1. **Tarjeta (H-E1).** En los cuatro formularios de tarjeta (A2.2, A2.5, B1.4 y A5.6), cuando la pasarela responde al pago, sea aprobado o rechazado, se vacían **todos** los campos de la tarjeta (número, vencimiento, CVV y titular). Un error de validación o de red no los vacía, para que se pueda corregir y reintentar. Al volver de un rechazo para usar otra tarjeta, todos aparecen vacíos. 11 §1 lo dice en una regla de "Componentes base".
2. **Barra lateral fija (H-E3).** En todas las estructuras con barra lateral (panel del proveedor N.1, administración y soporte, documentación del portal A5.1 y cuenta del consumidor), la barra queda fija: no se desplaza con el contenido y no tiene barra de desplazamiento propia. Si el contenido necesita desplazarse, solo él se desplaza, con su barra a la derecha de la ventana.
3. **Barra lateral completa (H-E3).** Con un área visible de **1366 × 650 px** (una pantalla de 1366 × 768 con Chrome al 100 %), cada barra lateral muestra todas sus opciones y su pie (nombre del usuario y «Cerrar sesión») sin desplazarse, con la API seleccionada en el panel. Para lograrlo se compactan el espaciado y el tamaño de los enlaces, sin quitar opciones ni cambiar su orden. La lista de rutas de la documentación (A5.1) cabe completa con la siembra de demostración; si una API tuviera más rutas de las que caben, solo esa lista se desplaza dentro de la barra (decisión local de esta tarea). 11 §1 "Estructura del panel" y §4 lo precisan, y anotan que el espaciado se aparta del mockup N.1 por decisión del 9 oct.
4. **Cursor (H-E5).** Todo elemento que se puede pulsar (botones, enlaces, enlaces con aspecto de botón, selectores, casillas y opciones de la barra lateral) muestra el cursor de mano en el panel, la administración y el portal. Un botón deshabilitado no muestra la mano: conserva `cursor-not-allowed`, como hoy. La regla está una sola vez en `packages/ui/src/style.css` y 11 §1 la registra.
5. **Contraseña (H-E6).** Los 10 campos de contraseña de 9 pantallas (A1.1, A1.3, A1.4b, A8.1, A8.2, A5.3, A5.3b, A5.7 y A5.10; en A8.1, la actual y la nueva) llevan dentro, a la derecha, un botón con un ojo: al pulsarlo muestra la contraseña y cambia al ojo tachado; al pulsarlo otra vez, la oculta. El botón tiene `aria-label` «Mostrar contraseña» / «Ocultar contraseña» y `aria-pressed`, se puede usar con el teclado y no envía el formulario. Los íconos son SVG en línea, sin dependencias nuevas. 11 §1 registra el componente.

## Pruebas obligatorias
- Vitest y Testing Library: los campos de tarjeta vacíos después de un cobro rechazado en los cuatro formularios (A2.2, A2.5, B1.4 y A5.6) y conservados tras un error de validación; el botón del ojo (mostrar, ocultar, `aria-label` y que no envía el formulario), y que los 10 campos de contraseña usan el componente compartido.
- Playwright (`tests/e2e/`): con el ambiente levantado y la siembra, en un área de 1366 × 650 px, la barra lateral del panel (con una API seleccionada), de la administración y de la cuenta del consumidor no se desplaza (`scrollHeight <= clientHeight`) y su pie es visible; al desplazar una página larga, la barra conserva su posición.
- Capturas de N.1, A5.1 y una pantalla de la administración a 1366 × 650 y a 1440 × 900 (protocolo B8), para compararlas con sus mockups y documentar la diferencia de espaciado.

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
cd tests/e2e && pnpm test
```
Verificación manual con el ambiente levantado:
- Recorrer en Chrome, a 1366 × 768, el bloque 1 del guion de la demostración (registro, suscripción, tarjeta rechazada y aprobada) y comprobar la barra lateral, el cursor, el ojo de la contraseña y los campos de tarjeta vacíos.

## Fuera de alcance
- El encabezado de la tabla de A4.1 (H-E2), el aviso al enviar una invitación (H-E4), el nombre del actor y los textos de los casos en la bitácora (H-E7 y H-E8) y el correo de un caso nuevo (H-E9): van a la próxima auditoría.
- La barra lateral del portal en planes y pago y el texto de A5.3 (DC-17).
- Rediseñar la barra lateral (grupos que se pliegan, íconos u otra estructura).

## Notas
- La tarea toca archivos de Dominique (`packages/ui`, layouts y pantallas del portal), de Emilio (A1, A2, A8, B1.4) y de José Pablo (`tests/e2e/`). Como es del coordinador, no hace falta preguntar (protocolo §E1), pero la bitácora lleva el aviso para cada uno.
