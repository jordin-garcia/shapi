---
id: DC-07
titulo: Portal público: inicio y documentación (A5.0, A5.1 y A5.5)
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: pendiente
depende_de: [DC-03, DC-05]
requisitos: [RF-16, RF-15]
pantallas: [A5.0, A5.1, A5.5]
---

# DC-07 · Portal público: inicio y documentación (A5.0, A5.1 y A5.5)

**Responsable:** Dominique Contreras · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** DC-03, DC-05

## Objetivo
Generar automáticamente el inicio y la documentación del portal de cada API a partir de su especificación y de su configuración.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-15 y RF-16
- `docs/specs/10-identidad-y-seguridad.md` §5 (textos escapados y Markdown sanitizado)
- Mockups: `mockups/A5/Main.dc.html`, `Documentacion.dc.html`, `InicioAgro.dc.html`, `DocumentacionAgro.dc.html`

## Archivos que creas o modificas
- `src/*/Portal/**` (modificar)
- `contratos/openapi/portal.yaml`
- `frontend/apps/portal/src/paginas/A5-0-Inicio.tsx` y `A5-1-Documentacion.tsx`
- `tests/**`

## Criterios de aceptación
1. `GET /api/portal/documentacion` devuelve las rutas **expuestas**: método, patrón, resumen, descripción (Markdown), parámetros (nombre, tipo, obligatorio, descripción), ejemplo de petición y de respuesta, peso en llamadas y URL completa `https://{sub}.api.{dominio_base}{patron}` (o el dominio propio, si está verificado).
2. A5.0 muestra la bienvenida, la descripción, un ejemplo de petición y respuesta (de la primera ruta con ejemplo), las rutas disponibles con "Descuenta N llamadas de su cuota" y un espacio para los planes, que completa DC-09.
3. A5.1 muestra la lista lateral de rutas y el detalle de la ruta elegida, igual que el mockup, con el botón "Probar en la consola".
4. El Markdown se muestra con `react-markdown` + `rehype-sanitize`. Nunca se inserta HTML sin sanitizar.
5. El mismo portal con la marca de Agro Precios se ve como A5.5 (otra marca, otro color y otros datos).

## Pruebas obligatorias
- Integración del endpoint (solo las rutas expuestas)
- Vitest de las pantallas con dos marcas

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Planes en el inicio (DC-09)
- Consola (DC-10)
