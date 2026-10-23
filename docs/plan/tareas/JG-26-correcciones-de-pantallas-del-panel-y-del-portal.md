---
id: JG-26
titulo: Correcciones de pantallas del panel y del portal (fechas, tablas, permisos y estilos)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P3
estado: pendiente
programada: 2026-10-27
depende_de: []
requisitos: [RF-09, RF-18, RF-37, RF-38, RNF-12]
pantallas: [A3.3, A3.4, A4.1, A6.2, B2.1, A5.0]
---

# JG-26 · Correcciones de pantallas del panel y del portal

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P3 · **Sin dependencias**

## Objetivo
Corregir los detalles de pantallas que quedaron pendientes en el ensayo del 8 oct, en la auditoría del 3 oct y en la convergencia del 23 oct (`docs/plan/convergencia/2026-10-22.md` §4), para que la interfaz se vea correcta en el video. Jordin decidió corregirlos él (protocolo §E1). Los archivos son de EM-07, JZ-08, JG-12, DC-03 y DC-05.

Es P3: si no cabe antes del jueves 29, es la primera que queda fuera (calendario §"Qué hacer si vamos atrasados").

## Hallazgos que corrige
- **H-C25 (baja, EM-07; H-E2):** el encabezado de la tabla de A4.1 queda pegado al borde de la tarjeta (`A4-1-PlanesApi.tsx:81`, `pt-0`) y no se ve como el de A3.5.
- **H-C26 (baja, JZ-08, ADR-30):** A6.2 muestra el ciclo con `fin` («9 nov – 9 dic 2026»), mientras A6.4b y B1.4 muestran el último día con servicio («9 nov – 8 dic 2026»), como pide ADR-30.
- **H-C28 (baja, JG-12, RF-37):** ninguna prueba compara `X-Cuota-Restante` de una petición real a la compuerta con `restante` de `/api/portal/consumo`, ni prueba que B2.1 consulte de nuevo cada 10 s (`useConsumoConsumidor.ts:21`). Tampoco se prueba que B3.1 consulte cada 30 s (`B3-1-Estado.tsx:44`).
- **H-108 (baja, EM-07):** `A4-1-PlanesApi.tsx:237`: el precio de un plan de pago acepta 0 (`min="0"`); el servidor ya responde 400.
- **H-111 (baja, DC-05):** `A3-3-Especificacion.tsx:48-51`: al cargar con éxito no se invalida `['rutas-api', id]`, así que A3.4 puede abrir con las rutas viejas.
- **H-113 (baja, DC-04 y DC-05):** el lector ve en A3.3 y A3.4 «Registrar una API», la zona de carga y «Guardar rutas expuestas». 04 §3 dice que el frontend oculta las opciones no permitidas.
- **H-114 (baja, DC-05):** `A3-3-Especificacion.tsx:97`: la clase `bg-principal-claro` no existe y la zona de arrastre no se tiñe.
- **H-115 (baja, DC-03):** `frontend/apps/portal/src/layouts/MarcaPortal.tsx:10,12`: la insignia del encabezado tiene 8 px de radio, y los mockups de A5 y B2, 6 px.

## ❓ Decisiones pendientes (preguntarle a Jordin antes de escribir código)
1. **Controles de escritura para el lector (H-113, duda 9 del 3 oct).** Recomiendo ocultarlos en todo el panel, como dice 04 §3, y que DC-16 lo revise. La otra opción es dejar el 403 y precisarlo en 04 §3.

## Contexto que debes leer
- `docs/specs/12-decisiones.md` ADR-30
- `docs/specs/04-roles-y-permisos.md` §3
- `docs/specs/03-requisitos.md` RF-37
- `docs/specs/11-interfaz.md` A3.3, A3.4, A4.1, A6.2 y B2.1
- Mockups correspondientes en `mockups/A3`, `mockups/A4`, `mockups/A6`, `mockups/A5` y `mockups/B2`

## Archivos que creas o modificas
- `frontend/apps/panel/src/paginas/A4-1-PlanesApi.tsx`, `A6-2-Organizaciones.tsx`, `A3-3-Especificacion.tsx` y `A3-4-*.tsx` (modificar)
- `frontend/apps/portal/src/layouts/MarcaPortal.tsx` (modificar)
- Pruebas del frontend y una prueba de integración de RF-37 en `tests/Shapi.Api.Tests` o `tests/Shapi.Compuerta.Tests` (crear)

## Criterios de aceptación
1. Cada hallazgo queda corregido como lo describe su línea, contra su mockup.
2. RF-37 tiene una prueba que hace peticiones reales con una clave, consolida y comprueba que `restante` de `/api/portal/consumo` coincide con el último `X-Cuota-Restante`.

## Pruebas obligatorias
- Cada hallazgo tiene una prueba que falla sin la corrección, salvo H-C25, H-114 y H-115, que son solo de estilo: esos tres se comprueban con una captura contra el mockup (protocolo B8).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Consumo"
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- La revisión visual completa contra los mockups (DC-16).
