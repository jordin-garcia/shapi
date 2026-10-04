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

### Correcciones de la auditoría (2026-10-04)

Paso 7 de `docs/plan/auditoria-2026-10-03.md`. El mismo PR corrige también H-55 a H-57 de DC-04.
- **H-48:** `GET /api/apis/{id}/rutas` exige `VerApis`, así que el lector puede abrir A3.3 y A3.4 (04 §3.1). Las dos escrituras siguen exigiendo `ConfigurarApis`, con prueba de 403 para el lector. El criterio 4 decía «(propietario o editor)», pero manda la spec.
- **H-49:** `ListaRutas` lleva `especificacion` (título, versión, versión de OpenAPI, formato, fecha de carga y tamaño en bytes), o `null` si no hay. Al volver a A3.3 se ve la tarjeta del archivo con «Cargado». Si se rechaza un archivo nuevo, vuelve a verse la especificación que la API conserva. Como el nombre del archivo no se guarda, se muestra el título (precisado en 11 §4).
- **H-50:** `CargarEspecificacion` bloquea la fila de la API (`SELECT … FOR UPDATE`) al empezar la transacción. Así, dos cargas simultáneas ya no chocan con el UNIQUE `(api_id, metodo, patron)`. Después del bloqueo se vuelve a leer la entidad `api`, para que la especificación guardada corresponda a las rutas que quedaron (lo encontró la revisión). La exposición de rutas toma el mismo bloqueo, para que una recarga no le borre una ruta a mitad del guardado. Las pruebas lanzan cargas a la vez; la carrera no se reproduce siempre sin la corrección.
- **H-51:** `CambioExposicionRuta` tiene `rutaId` y `expuesta` anulables. Un elemento nulo, sin uno de los dos campos o con una ruta repetida responde 400 `datos_invalidos`, con `errores` por posición (`[0].expuesta`). Antes, un elemento sin `expuesta` ocultaba la ruta sin aviso, y uno nulo daba 500.
- **H-52:** sin la parte `archivo`, la carga responde 400 `datos_invalidos` con `errores.archivo`. El contrato documenta ese 400 y el 404 de una `rutaId` que no es de la API, con su prueba.
- **H-53:** los radios de A3.4 son los del mockup: 16 px, borde `borde-campo` (#C9D2E1) y, marcados, un punto de 8 px en el color principal, separados 28 px.
- **H-54:** 07 §3.2 precisa la forma de `ruta.definicion` y 07 §3.5, que el consumo de una ruta retirada se consolida con `ruta_id` nulo. Hay avisos para JG-09, DC-07 y DC-10 en la bitácora de Jordin.
- **Decidido (3 oct):**
  - un documento Swagger 2.0, uno sin `info.version` y una bomba de alias YAML responden 422, con prueba;
  - el `mensaje` del 422 va en español («El documento tiene un error de formato o de estructura.») cuando el error viene de Microsoft.OpenApi o de SharpYaml. El texto original va aparte, en `detalle.detalleTecnico`, y A3.3 lo muestra como «Detalle técnico: …» (RNF-12).
