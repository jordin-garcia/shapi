---
id: JG-05
titulo: Compuerta: organización, suscripción, ruta, secreto, SSRF y CORS
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: pendiente
depende_de: [JG-04, DC-04]
requisitos: [RF-29, RF-31, RF-47, RNF-10]
pantallas: []
---

# JG-05 · Compuerta: organización, suscripción, ruta, secreto, SSRF y CORS

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JG-04, DC-04

## Objetivo
Completar los filtros de validación de la compuerta (0, 3, 4 y 5), las cabeceras hacia el origen, la protección contra SSRF en cada conexión y los errores de reenvío.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §1, §3 (filtros 0 a 5 y 8), §4, §5 (cabeceras hacia el origen) y §6 (CORS)
- `docs/specs/10-identidad-y-seguridad.md` §4 (SSRF)
- `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs` (lo creó DC-04; léelo en su sección Resultado)

## Archivos que creas o modificas
- `src/Shapi.Compuerta/Filtros/{FiltroCors,FiltroOrganizacion,FiltroSuscripcion,FiltroRuta}.cs` (crear)
- `src/Shapi.Compuerta/**` (modificar: tubería, transformaciones y `SocketsHttpHandler`)
- `tests/Shapi.Compuerta.Tests/**`

## Criterios de aceptación
1. Si la organización tiene `estado_efectivo=suspendida` → 403 `api_no_disponible`.
2. Si la suscripción está `suspendida` o `finalizada` → 403 `suscripcion_inactiva`. El filtro **no compara fechas** (08 §3, filtro 4).
3. Si el método y la ruta no existen o están ocultos → 403 `ruta_no_permitida`. La coincidencia usa patrones OpenAPI (`/rastreo/{guia}` coincide con `/rastreo/GT123`) con la regla de especificidad de 08 §1. Las rutas se guardan en memoria 5 segundos como máximo, validando la `version`.
4. Hacia el origen: se quitan `X-Api-Key` y cualquier `X-Shapi-*` que haya enviado el cliente, y se agregan `X-Shapi-Consumidor`, `X-Shapi-Entorno`, `X-Shapi-Secreto` (si la API lo tiene) y `X-Forwarded-For/Proto/Host`.
5. Cada conexión pasa por un `ConnectCallback` que usa `ValidadorDireccionOrigen` y rechaza direcciones internas con 502 `origen_inaccesible`. En modo demostración se permite `SHAPI_ORIGENES_PERMITIDOS` (entradas `host:puerto`). No se siguen redirecciones del origen.
6. Si no se puede conectar al origen → 502 `origen_inaccesible`. Si el origen tarda más de 30 s → 504 `origen_sin_respuesta`. Si el cuerpo pesa más de 10 MB → 413 `cuerpo_demasiado_grande`.
7. CORS según 08 §6: el *preflight* `OPTIONS` se responde sin clave con 204 y `Access-Control-Allow-Origin` igual al `portal_host` de la API. Otros orígenes no reciben esa cabecera. Las peticiones sin `Origin` pasan normal.

## Pruebas obligatorias
- Unitarias por filtro (casos válidos y de rechazo)
- Integración de la tubería completa con Redis (Testcontainers) y un origen falso
- SSRF: origen `127.0.0.1` y `10.0.0.5` → 502; origen permitido en modo demostración → 200

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Límites y cuotas (JG-06)
- Caché de respuestas (JG-15)
