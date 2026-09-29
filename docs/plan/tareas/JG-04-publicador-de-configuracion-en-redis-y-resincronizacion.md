---
id: JG-04
titulo: Publicador de configuración en Redis y resincronización
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: hecha
programada: 2026-09-28
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
1. `PublicarApi(apiId)` escribe `api:{id}` (organizacion_id, estado, url_origen, secreto descifrado, portal_host, version) con `DEL` y luego `HSET` en una misma transacción, porque `ContextoApi.ACampos()` omite los campos vacíos y un secreto borrado seguiría en Redis; `api:{id}:rutas` (JSON con todas las rutas y `version` incrementada) y `api:host:{sub}.api.{dominio_base}`, más el dominio propio si está verificado. Si la API está despublicada, deja `estado=despublicada` y la compuerta responde 404.
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

## Resultado

**Qué se hizo**
- `PublicadorCacheRedis` reemplaza a `PublicadorCacheNulo`. Lee el estado actual de PostgreSQL, sin el filtro por organización, y escribe en Redis:
  - `api:{id}` con `DEL` y luego `HSET` en una transacción, con la `version` siguiente;
  - `api:{id}:rutas`, con todas las rutas;
  - `api:host:{sub}.api.{dominio_base}` y el dominio propio verificado;
  - `clave:{sha256}` con `EXPIREAT` si está rotada, y la borra si está revocada;
  - `susc:{id}`, que borra si la suscripción está finalizada;
  - `org:{id}` con el estado efectivo, la cuota y el ciclo de plataforma.
- Si Redis falla, el publicador reintenta 3 veces (200 ms, 500 ms y 1 s), registra el error y no lanza (criterio 3). La conexión usa `BacklogPolicy.FailFast`, para que los reintentos no alarguen la respuesta HTTP, y `IncludeDetailInExceptions = false`, para que los errores no lleven el hash de una clave.
- `ResincronizarCache` y `TrabajoResincronizacion` corren en el trabajador, al arrancar y cada 5 minutos. Reescriben toda la configuración y borran las llaves de configuración que sobran. No tocan los contadores.
- Contratos nuevos en `Shapi.Contratos/Redis`: `ContextoSuscripcion`, `ContextoOrganizacion` y `RutaCache`, con JSON en snake_case, más `LlavesRedis.PatronesConfiguracion`. `Aplicacion/Cache/ArmadoCache` arma los DTO y calcula el estado efectivo.
- `IProtectorSecretoOrigen` usa ASP.NET Data Protection con el propósito `Shapi.SecretoOrigen`, el nombre de aplicación `Shapi` y las llaves en `SHAPI_DPKEYS_DIR`. Se registra en `AgregarServiciosComunes`. Por eso `Shapi.Infraestructura` usa el marco compartido `Microsoft.AspNetCore.App` (no es un paquete NuGet nuevo) y deja de referenciar `Microsoft.Extensions.Identity.Core`, que ya viene incluido.
- Se eliminó `sembrar-demo` de la compuerta, con su prueba.

**Decisiones**
- El `EXPIREAT` de una clave rotada se traslada a la hora real: hora real + (`expira_en` − `IReloj.Ahora`). En el modo demostración, el reloj adelantado haría que la clave rotada funcionara 24 h más los días adelantados. Quedó en 07 §4.
- `api:{id}:rutas` es un arreglo JSON, como dice 07 §4. La "`version` incrementada" del criterio 1 es la de `api:{id}`, que se escribe en la misma transacción que las rutas. Para que la versión solo suba aunque publiquen dos procesos a la vez, la transacción lleva una condición sobre la versión leída.
- `PublicarSuscripcion` de una suscripción de plataforma publica `org:{id}`, porque su estado cambia el estado efectivo.
- La resincronización también borra las llaves de configuración sobrantes. Sin eso, una clave cuya revocación falló en Redis seguiría funcionando. Lee las llaves existentes con `SCAN` antes de leer PostgreSQL, para no borrar una llave publicada durante la resincronización, y al final corrige las claves que cambiaron mientras tanto.
- Si el secreto de una API no se puede descifrar, la API no se publica y se registra el error.
- `Shapi.Api.Tests` referencia `Shapi.Compuerta` con el alias `compuerta`, para la prueba del criterio 5. El `Program` del trabajador se declara `internal`: con el marco de ASP.NET Core, .NET 10 lo haría público y chocaría con el de la API.

**Archivos principales:** `src/Shapi.Infraestructura/Cache/{PublicadorCacheRedis,EscritorCacheRedis,LectorCacheBaseDatos,ReintentosRedis,ServiciosCache}.cs`, `src/Shapi.Trabajador/Resincronizacion/**`, `src/Shapi.Aplicacion/Cache/ArmadoCache.cs`, `src/Shapi.Contratos/Redis/{ContextoSuscripcion,ContextoOrganizacion,RutaCache}.cs`, `src/Shapi.Infraestructura/Comun/ProtectorSecretoOrigen.cs` y `tests/Shapi.Api.Tests/Cache/**`.
