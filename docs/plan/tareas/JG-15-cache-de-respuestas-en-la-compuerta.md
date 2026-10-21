---
id: JG-15
titulo: Caché de respuestas en la compuerta
persona: jordin
responsable: Jordin García
avance: final
prioridad: P3
estado: hecha
programada: 2026-10-26
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

## Resultado

- Se implementó `FiltroCache` como filtro 7, después de límites y cuotas. Un acierto transmite desde Redis el cuerpo binario y las cabeceras del origen con `X-Shapi-Cache: HIT`, sin reenviar; sigue descontando el peso y la cuota de plataforma. Un fallo marca `MISS` y captura una copia acotada mientras YARP transmite.
- Solo GET con caché activada guarda respuestas 200 completas de hasta 1,048,576 bytes. La llave conserva el formato de `LlavesRedis.Cache` y la transacción reemplaza cuerpo, cabeceras, estado, versión y TTL. Las respuestas mayores y los errores siguen transmitiéndose sin guardarse.
- La versión de la API se almacena en cada entrada: al cambiar, ninguna respuesta anterior produce un acierto, incluso si terminó de guardarse después de una publicación. La invalidación es conservadora para todas las rutas de esa API y las entradas antiguas desaparecen por TTL.
- Las cabeceras de cuota, plan y CORS se calculan por petición. No se guardan respuestas con cookies, `Cache-Control: private`, `no-store` o `no-cache`, ni con `Vary` distinto de `Origin`, porque la llave no incluye variantes por cabecera. Estas decisiones quedaron en 08 §3 y la estructura de Redis en 07 §4.
- Pruebas: 22 casos nuevos de integración con Redis real y YARP, incluidos cuotas, consumidores distintos, cuerpo binario y tamaño límite, TTL, query y camino, versión durante un reenvío, cookies y negociación gzip. La suite de compuerta pasó sus 310 pruebas sin omisiones. La revisión independiente detectó el caso de `Vary: Accept-Encoding`; se corrigió con una regresión y la segunda revisión dio LISTO.
- Archivos principales: `src/Shapi.Compuerta/Filtros/FiltroCache.cs`, `ContextoPeticion.cs`, `TuberiaCompuerta.cs`, `tests/Shapi.Compuerta.Tests/Tuberia/CacheRespuestasTests.cs` y la prueba del orden de filtros. No se añadieron dependencias ni endpoints.
