---
id: DC-05
titulo: Especificación OpenAPI y rutas expuestas (A3.3 y A3.4)
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: hecha
programada: 2026-10-01
depende_de: [DC-04]
requisitos: [RF-09, RF-10]
pantallas: [A3.3, A3.4]
---

# DC-05 · Especificación OpenAPI y rutas expuestas (A3.3 y A3.4)

**Responsable:** Dominique Contreras · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** DC-04

## Objetivo
Cargar y validar la especificación OpenAPI de una API, extraer sus rutas conservando la configuración cuando se vuelve a cargar, y permitir exponer u ocultar cada ruta.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-09 y RF-10
- `docs/specs/12-decisiones.md` ADR-26
- `docs/specs/07-modelo-de-datos.md` §3.2 (`ruta.definicion`)
- Mockups: `mockups/A3/Especificacion.dc.html`, `Rutas.dc.html`
- Archivos de ejemplo: `origenes-demo/envios-xelaju/cotizacion-envios.yaml` (las 5 rutas de A3.3) y `origenes-demo/agro-precios/openapi.yaml` (de JZ-02)

## Archivos que creas o modificas
- `src/*/Apis/**` (modificar)
- `contratos/openapi/apis.yaml` (modificar)
- `frontend/apps/panel/src/paginas/A3-3-Especificacion.tsx` y `A3-4-Rutas.tsx`
- `tests/*/Apis/**`

## Criterios de aceptación
1. `PUT /api/apis/{id}/especificacion` (multipart, de hasta 2 MB, en JSON o YAML) valida OpenAPI 3.0 o 3.1 con Microsoft.OpenApi. Si no es válida → 422 `especificacion_invalida`, con la línea o la sección del error.
2. Extrae cada operación como una ruta (método, patrón, resumen, descripción y una `definicion` en jsonb con los parámetros, el cuerpo y los ejemplos) y guarda el título, la descripción y la versión de `info`.
3. Al volver a cargarla, las rutas que ya existían (mismo método y patrón) **conservan** su configuración, las nuevas quedan **ocultas** y las que ya no aparecen se eliminan.
4. `GET /api/apis/{id}/rutas` y `PUT /api/apis/{id}/rutas/exposicion` `[{rutaId, expuesta}]` (propietario o editor). Bitácora `ruta.expuesta` y `ruta.ocultada`. Si la API está publicada, se llama a `IPublicadorCache.PublicarApi`.
5. A3.3 (carga, archivo cargado y rutas encontradas) y A3.4 (expuesta u oculta por ruta, con el resumen) reproducen sus mockups.

## Pruebas obligatorias
- Integración con las especificaciones de los orígenes de demostración y con una inválida
- La recarga conserva la configuración
- Vitest de las pantallas

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Configuración por ruta y publicación (DC-06)

## Resultado

- Se publicaron los contratos y endpoints para cargar especificaciones OpenAPI, listar rutas y guardar su exposición por lote.
- El lector acepta JSON o YAML de OpenAPI 3.0/3.1, informa ubicación y mensaje de validación, e incorpora parámetros, cuerpos, respuestas y ejemplos en una definición JSON estable por operación.
- La recarga reconcilia por método y patrón: conserva identificador, exposición, límites, caché y peso; crea rutas ocultas; consolida como consumo sin ruta las métricas históricas y elimina las retiradas dentro de una transacción.
- Las APIs publicadas actualizan su caché después de confirmar los cambios, y los cambios de exposición registran `ruta.expuesta` o `ruta.ocultada` en la bitácora.
- A3.3 y A3.4 implementan la carga automática, sus estados y errores, la tabla de rutas y la selección de exposición según los mockups.
- Las pruebas cubren las dos especificaciones de demostración, archivos inválidos y grandes, recargas, aislamiento, permisos, bitácora, publicación de caché y las dos pantallas.
