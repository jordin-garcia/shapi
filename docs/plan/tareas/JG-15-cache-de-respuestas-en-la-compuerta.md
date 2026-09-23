---
id: JG-15
titulo: Caché de respuestas en la compuerta
persona: jordin
responsable: Jordin García
avance: final
prioridad: P3
estado: pendiente
depende_de: [JG-06]
requisitos: [RF-13]
pantallas: []
---

# JG-15 · Caché de respuestas en la compuerta

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P3 · **Depende de:** JG-06

## Objetivo
Implementar el filtro 7 de la compuerta: guardar en Redis las respuestas GET de las rutas que tienen caché activada.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §3 (filtro 7) y §5 (`X-Shapi-Cache`)
- `docs/specs/12-decisiones.md` ADR-26
- `docs/specs/07-modelo-de-datos.md` §4 (`cache:*`)

## Archivos que creas o modificas
- `src/Shapi.Compuerta/Filtros/FiltroCache.cs` (crear)
- `tests/Shapi.Compuerta.Tests/**`

## Criterios de aceptación
1. Solo aplica a GET en rutas con `cache_segundos > 0`. La llave es `cache:{api}:{ruta}:{sha256(metodo+ruta+query)}`, con TTL igual a `cache_segundos`, y solo guarda respuestas 200 de hasta 1 MB.
2. Un acierto responde desde Redis sin llegar al origen, **descuenta cuota** y agrega `X-Shapi-Cache: HIT`. Un fallo agrega `MISS`.
3. Si la configuración de la ruta cambia (sube la `version` de las rutas), la caché de esa ruta deja de usarse.

## Pruebas obligatorias
- Integración: la segunda petición no llega al origen y sí descuenta cuota

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Caché por consumidor
