---
id: DC-10
titulo: Consola de pruebas del portal (A5.2)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-12
depende_de: [DC-07, JG-05]
requisitos: [RF-16, RF-45]
pantallas: [A5.2]
---

# DC-10 · Consola de pruebas del portal (A5.2)

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** DC-07, JG-05

## Objetivo
Permitir que el visitante o el consumidor pruebe cualquier ruta expuesta desde el navegador, pegando su clave de pruebas.

## Contexto que debes leer
- Mockups: `mockups/A5/Consola.dc.html`
- `docs/specs/08-compuerta.md` §6 (CORS)
- `docs/specs/12-decisiones.md` ADR-01 y ADR-24

## Archivos que creas o modificas
- `frontend/apps/portal/src/paginas/A5-2-Consola.tsx`
- `frontend/apps/portal/src/modulos/consola/**`

## Criterios de aceptación
1. Se elige una ruta expuesta y se muestran sus parámetros (de `definicion`) con su tipo y si son obligatorios.
2. El campo "Su clave de pruebas" guarda el valor **solo** en `sessionStorage` y lo muestra enmascarado.
3. "Enviar petición" hace un `fetch` real a `https://{sub}.api.{dominio_base}{ruta}` con `X-Api-Key`, y muestra el código, el cuerpo formateado y las cabeceras `X-RateLimit-*` y `X-Cuota-*`.
4. Los errores de la compuerta (401, 403, 429) se muestran con su código y su mensaje.

## Pruebas obligatorias
- Vitest + MSW: petición exitosa, error 401 y la clave que no se persiste en `localStorage`

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Probar `POST /cotizaciones` con la clave de pruebas de la siembra

## Fuera de alcance
- Documentación (DC-07)

## Resultado
- Se implementó A5.2 con selección de ruta, edición y validación de parámetros, clave de pruebas enmascarada conservada únicamente en `sessionStorage`, envío real a la compuerta y presentación del estado, cuerpo, errores y cabeceras de límites/cuota.
- La documentación del portal conserva la ubicación de cada parámetro y distingue cuándo un valor representa el cuerpo JSON completo, para construir correctamente ruta, consulta, cabeceras y cuerpos objeto, arreglo o escalares.
- Las pruebas cubren éxito, 401, 403, 429, ruta y consulta, `POST` con consulta más cuerpo no objeto, objeto libre sin envoltorio y ausencia de persistencia en `localStorage`.
- Archivos principales: `A5-2-Consola.tsx`, `modulos/consola/`, `contratos/openapi/portal.yaml`, `ConsultorDocumentacionPortal.cs`, tipos generados y pruebas de Portal/Consola.
