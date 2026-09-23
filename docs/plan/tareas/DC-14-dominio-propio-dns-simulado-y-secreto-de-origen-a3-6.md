---
id: DC-14
titulo: Dominio propio, DNS simulado y secreto de origen (A3.6)
persona: dominique
responsable: Dominique Contreras
avance: 3
prioridad: P2
estado: pendiente
depende_de: [DC-06, JZ-01]
requisitos: [RF-11, RF-12, RF-47]
pantallas: [A3.6]
---

# DC-14 · Dominio propio, DNS simulado y secreto de origen (A3.6)

**Responsable:** Dominique Contreras · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** DC-06, JZ-01

## Objetivo
Permitir conectar y verificar un dominio propio con el DNS simulado, autorizar la emisión de su certificado en el borde y regenerar el secreto de origen.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §4 (`/interno/tls/autorizar`), §5.6 y §6
- `docs/specs/12-decisiones.md` ADR-10 y ADR-11
- `docs/specs/07-modelo-de-datos.md` §3.2 (`dominio_propio`, `registro_dns_simulado`) y §5
- Mockups: `mockups/A3/Dominio.dc.html`

## Archivos que creas o modificas
- `src/*/Apis/**` (modificar: dominio, secreto y el endpoint interno)
- `src/Shapi.Infraestructura/Apis/ResolutorDns*.cs` (crear las versiones simulada y real)
- `src/Shapi.Trabajador/VerificacionDominios/**` (crear)
- `contratos/openapi/apis.yaml`
- `frontend/apps/panel/src/paginas/A3-6-Dominios.tsx`
- `tests/**`

## Criterios de aceptación
1. `IResolutorDns` tiene la implementación `simulado` (tabla `registro_dns_simulado`) y la `real` (DnsClient.NET), elegidas con `SHAPI_DNS_MODO`.
2. `POST /api/apis/{id}/dominio` `{dominio}`: si el plan no incluye dominio propio → 422 `plan_sin_dominio_propio` (usa `IVerificadorLimitesPlan` si EM-13 ya existe; si no, consulta el plan directamente). En modo simulado, el dominio debe terminar en `.localhost`. Queda `pendiente`, con el destino `{sub}.api.{dominio_base}`.
3. `POST .../dominio/simular-registro` (solo en modo simulado) inserta el CNAME. `POST .../dominio/verificar` resuelve el CNAME: si coincide, queda `verificado`, se llama a `PublicarApi` (que agrega `api:host:{dominio}`) y se registra `dominio.verificado` en la bitácora; si no coincide, sigue `pendiente` con el motivo. `DELETE .../dominio` lo desconecta.
4. `GET /interno/tls/autorizar?domain=` responde 200 si el dominio está verificado y 404 en cualquier otro caso, y solo acepta peticiones desde direcciones privadas o de loopback.
5. Cada 5 minutos, el trabajador intenta verificar los dominios pendientes. Después de 72 h, quedan `fallido`.
6. `POST /api/apis/{id}/secreto/regenerar` devuelve el secreto nuevo una sola vez, republica la API y registra `secreto_origen.regenerado` en la bitácora.
7. A3.6 reproduce el mockup: las direcciones asignadas, el secreto enmascarado con "Regenerar", el dominio propio con el registro que hay que crear, "Simular la creación del registro" y "Verificar registro DNS".

## Pruebas obligatorias
- Integración del flujo completo con el DNS simulado
- Endpoint interno
- Vitest

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Conectar `api.enviosxelaju.localhost`, simular, verificar y hacer `curl` a `https://api.enviosxelaju.localhost/cotizaciones`: el certificado se emite en ese momento y responde

## Fuera de alcance
- Cambios en el Caddyfile (ya los deja JZ-01)
