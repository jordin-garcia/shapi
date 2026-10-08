---
id: DC-17
titulo: Barra lateral del portal en planes y pago, y texto de A5.3
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P2
estado: pendiente
programada: 2026-10-13
depende_de: []
requisitos: [RF-16, RF-19]
pantallas: [A5.3, A5.4, A5.6, A5.4b]
---

# DC-17 · Barra lateral del portal en planes y pago, y texto de A5.3

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P2 · **Sin dependencias**

## Objetivo
Que A5.4, A5.6 y A5.4b se vean como sus mockups, con la barra lateral del portal (secciones «API» y «Mi cuenta» y, con sesión, el nombre de la consumidora), y corregir el texto repetido de A5.3.

## Contexto que debes leer
- `docs/plan/convergencia/2026-10-08.md` (pasos 3 y 4 del guion)
- `docs/specs/11-interfaz.md` §"Comportamiento del portal y de sus pantallas de acceso" (barra de documentación y barra de la cuenta)
- Mockups: `mockups/A5/Planes.dc.html`, `mockups/A5/PlanesVacia.dc.html`, `mockups/A5/Pago.dc.html`, `mockups/A5/Confirmacion.dc.html`, `mockups/A5/Registro.dc.html`

## Archivos que creas o modificas
- `frontend/apps/portal/src/rutas.tsx` (modificar)
- `frontend/apps/portal/src/layouts/**` (modificar)
- `frontend/apps/portal/src/paginas/A5-3-Registro.tsx` (modificar)
- `frontend/apps/portal/src/paginas/A5-4-Planes.tsx`, `A5-4b-Confirmacion.tsx` y `A5-6-Pago.tsx` (modificar, si hace falta)
- `frontend/apps/portal/src/tests/**`
- `docs/specs/11-interfaz.md` §"Comportamiento del portal y de sus pantallas de acceso" (modificar, criterio 2)

## Criterios de aceptación
1. `/planes` y `/contratar/:plan`, incluida la confirmación A5.4b, usan la misma barra lateral que A5.1, con las secciones «API» y «Mi cuenta» de sus mockups, en lugar del encabezado público con «Entrar» y «Crear cuenta».
2. **Regla nueva, que precisa 11 §"Barra de documentación":** en las páginas públicas del portal con barra lateral (A5.1, A5.4, A5.6 y A5.4b), si hay una sesión de consumidor, la barra muestra su nombre, su empresa y la acción de cerrar sesión, como en `Planes.dc.html` y `Documentacion.dc.html`. Sin sesión, los enlaces de cuenta se muestran sin datos del consumidor, como hoy en A5.1. El PR actualiza esa precisión de `11-interfaz.md`, que hoy dice que esos datos aparecen únicamente dentro del área autenticada.
3. A5.3 dice «obtiene sus claves de la {nombre de la API}», como el mockup. Hoy la siembra llama a la API «API de Cotización de Envíos» y la pantalla muestra «la API de API de Cotización de Envíos». Si el nombre no empieza por «API», se antepone «la API», como hace `TextoBitacora.cs`.
4. Al verificar el correo desde el portal (A5.8 → `/planes`), la consumidora ve su nombre en la barra sin recargar la página.

## Pruebas obligatorias
- Vitest y Testing Library (criterios 1 a 4)
- Capturas de A5.4, A5.6 y A5.4b con sesión, comparadas con sus mockups (B8)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- La fila «Medio de pago» de A5.4b (EM-20)
- La consola de pruebas (DC-10)
