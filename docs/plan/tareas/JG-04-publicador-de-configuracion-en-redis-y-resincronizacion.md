---
id: JG-04
titulo: Publicador de configuración en Redis y resincronización
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: pendiente
depende_de: [JG-02, EM-01]
requisitos: [RNF-02, RNF-04, RNF-05]
pantallas: []
---

# JG-04 · Publicador de configuración en Redis y resincronización

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JG-02, EM-01

## Objetivo
Implementar `IPublicadorCache`, que escribe en Redis la vista que lee la compuerta, y el trabajo de resincronización desde PostgreSQL (al arrancar y cada 5 minutos). Con esto, lo que se configura en el panel llega a la compuerta.

## Contexto que debes leer
- `docs/specs/07-modelo-de-datos.md` §3.1 (estado efectivo de la organización) y §4 completo (llaves y resincronización)
- `docs/specs/06-arquitectura.md` §3 y §5.2 (nota sobre fallas de Redis después del *commit*)
- `docs/specs/08-compuerta.md` §3 (qué lee cada filtro)
- `docs/specs/09-cobros-y-suscripciones.md` §3 (estados de las suscripciones)

## Archivos que creas o modificas
- `src/Shapi.Aplicacion/Cache/**` (crear: armado de los DTO de caché a partir de las entidades)
- `src/Shapi.Infraestructura/Cache/PublicadorCacheRedis.cs` (crear; reemplaza la implementación nula)
- `src/Shapi.Api/Modulos/CacheModulo.cs` (modificar)
- `src/Shapi.Trabajador/Resincronizacion/**` (crear)
- `src/Shapi.Compuerta/**` (modificar: quitar `sembrar-demo`)
- `tests/Shapi.Api.Tests/Cache/**` (crear)

## Criterios de aceptación
1. `PublicarApi(apiId)` escribe `api:{id}` (organizacion_id, estado, url_origen, secreto descifrado, portal_host, version), `api:{id}:rutas` (JSON con todas las rutas y `version` incrementada) y `api:host:{sub}.api.{dominio_base}`, más el dominio propio si está verificado. Si la API está despublicada, deja `estado=despublicada` y la compuerta responde 404.
2. `PublicarClave`, `ExpirarClave(hash, instante)` (EXPIREAT), `EliminarClave`, `PublicarSuscripcion` y `PublicarOrganizacion` escriben los hash de 07 §4. El `estado_efectivo` de la organización es `suspendida` si `estado_admin=suspendida` **o** si su suscripción de plataforma vigente está `suspendida`.
3. Si Redis falla después del *commit* en PostgreSQL, el publicador reintenta 3 veces con espera y registra el error, sin revertir la operación de negocio.
4. Al arrancar el trabajador, y luego cada 5 minutos, `ResincronizarCache` reescribe todas las llaves de configuración (APIs publicadas, claves activas y rotadas vigentes, suscripciones no finalizadas, organizaciones) **sin tocar** los contadores (`cuota:*`, `rl:*`, `met:*`, `cache:*`).
5. Con Redis vacío y PostgreSQL con datos, después de una resincronización la compuerta responde igual que antes (prueba de integración con la compuerta en memoria).
6. Se elimina el comando temporal `sembrar-demo` de la compuerta.

## Pruebas obligatorias
- Integración (Testcontainers PostgreSQL + Redis) de cada método del publicador
- Integración de la resincronización con Redis vacío
- Unitaria del cálculo del estado efectivo

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Invalidar la caché de respuestas (JG-15)
- Los casos de uso que llaman al publicador (los hacen los dueños de cada módulo)
