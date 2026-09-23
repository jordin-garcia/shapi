---
id: DC-04
titulo: Registrar una API y lista de APIs (A3.1 y A3.2)
persona: dominique
responsable: Dominique Contreras
avance: 2
prioridad: P1
estado: pendiente
depende_de: [DC-02, EM-02]
requisitos: [RF-08, RF-14, RF-47, RNF-10]
pantallas: [A3.1, A3.2]
---

# DC-04 · Registrar una API y lista de APIs (A3.1 y A3.2)

**Responsable:** Dominique Contreras · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** DC-02, EM-02

## Objetivo
Permitir que el proveedor registre una API (nombre, URL de origen y subdominio), validando la URL contra SSRF y probando la conexión, y ver la lista de sus APIs.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-08, RF-14, RF-47 y RNF-10
- `docs/specs/10-identidad-y-seguridad.md` §3 (secreto) y §4 (SSRF)
- `docs/specs/06-arquitectura.md` §4 (subdominios reservados) y §5.3
- `docs/specs/02-glosario.md` (subdominio)
- `docs/specs/07-modelo-de-datos.md` §3.2
- Mockups: `mockups/A3/Main.dc.html`, `ListaVacia.dc.html`, `Registro.dc.html`

## Archivos que creas o modificas
- `src/Shapi.Contratos/Red/ValidadorDireccionOrigen.cs` (crear; la compuerta lo usará en JG-05)
- `src/Shapi.{Dominio,Aplicacion,Infraestructura,Api}/Apis/**` (crear)
- `src/Shapi.Api/Modulos/ApisModulo.cs`
- `contratos/openapi/apis.yaml` (crear)
- `frontend/apps/panel/src/paginas/A3-1-Apis.tsx` y `A3-2-RegistrarApi.tsx`
- `tests/*/Apis/**`

## Criterios de aceptación
1. `POST /api/apis` `{nombre, urlOrigen, subdominio}` (propietario o editor). El subdominio cumple `^[a-z0-9][a-z0-9-]{1,28}[a-z0-9]$`, no está en la lista de reservados de 06 §4 y es único (409 `subdominio_ocupado`).
2. `ValidadorDireccionOrigen` exige `http` o `https` sin credenciales, resuelve el DNS y rechaza cualquier dirección de los rangos de 10 §4 (422 `origen_no_permitido`). Con `SHAPI_MODO_DEMO=true` permite las entradas `host:puerto` de `SHAPI_ORIGENES_PERMITIDOS`.
3. La prueba de conexión (GET con 5 s de espera, sin seguir redirecciones) se aprueba con cualquier respuesta HTTP. Si falla → 422 `origen_inaccesible`, con el detalle, **sin guardar nada**.
4. La API se guarda en estado `borrador` con un secreto de origen `shps_` + 32 base62 **cifrado** con Data Protection (llaves en el directorio `SHAPI_DPKEYS_DIR`). La respuesta 201 incluye el secreto en claro **una sola vez**. Bitácora `api.registrada`.
5. `GET /api/apis` devuelve las APIs de la organización (nombre, subdominio, estado), el número de APIs y el `max_apis` del plan.
6. A3.1 (con su variante vacía y el texto "Usa N de M APIs de su plan") y A3.2 (con el resultado de la prueba de conexión) reproducen sus mockups. Después de registrar, se muestra el secreto una vez y se va a la pantalla de especificación.

## Pruebas obligatorias
- Unitarias de `ValidadorDireccionOrigen` (IPv4 e IPv6 privadas, loopback, link-local y lista permitida)
- Integración de los endpoints (subdominio reservado u ocupado, origen caído, secreto cifrado en la base de datos)
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
- Especificación y rutas (DC-05)
- Publicar (DC-06)
- Límite de APIs del plan (EM-13)
