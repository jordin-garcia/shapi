---
id: DC-06
titulo: Configuración por ruta y publicación (A3.5)
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: pendiente
depende_de: [DC-05, JG-04]
requisitos: [RF-13, RF-14]
pantallas: [A3.5]
---

# DC-06 · Configuración por ruta y publicación (A3.5)

**Responsable:** Dominique Contreras · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** DC-05, JG-04

## Objetivo
Permitir configurar el límite por minuto, la caché y el peso en llamadas de cada ruta, y publicar o despublicar la API. Al publicarla, la configuración llega a la compuerta.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-13 y RF-14
- `docs/specs/06-arquitectura.md` §5.3
- `docs/specs/05-casos-de-uso.md` CU-05 y CU-06
- Mockups: `mockups/A3/ConfigRutas.dc.html` y `mockups/A3/Main.dc.html` (botones publicar y despublicar)

## Archivos que creas o modificas
- `src/*/Apis/**` (modificar)
- `contratos/openapi/apis.yaml`
- `frontend/apps/panel/src/paginas/A3-5-ConfigRutas.tsx` y `A3-1-Apis.tsx` (modificar)
- `tests/*/Apis/**`

## Criterios de aceptación
1. `PUT /api/apis/{id}/configuracion-rutas` `[{rutaId, limiteMinuto?, cacheSegundos, pesoLlamadas}]` valida: caché solo en GET y entre 0 y 86400, peso entre 1 y 1000, límite mayor que 0 o vacío. Si la API está publicada, se republica.
2. `POST /api/apis/{id}/publicar` exige el correo verificado (422 `correo_no_verificado`), al menos una ruta expuesta y al menos un plan activo (422 `publicacion_incompleta` con `faltan`). Luego deja el estado en `publicada`, guarda `publicada_en`, llama a `PublicarApi` y registra `api.publicada` en la bitácora.
3. `POST /api/apis/{id}/despublicar` deja el estado en `despublicada` y republica. La compuerta responde 404.
4. A3.5 reproduce el mockup (columnas: límite en peticiones, caché y peso en llamadas). En A3.1 funcionan los botones Publicar y Despublicar, con los errores de publicación visibles.

## Pruebas obligatorias
- Integración: validaciones, publicación incompleta y llaves de Redis después de publicar
- Vitest

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Planes (EM-07)
