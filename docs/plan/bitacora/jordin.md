# Bitácora de Jordin García

Cada tarea terminada agrega una entrada **al final** de este archivo (protocolo, paso B10):

```
## AAAA-MM-DD · <ID> · <título>
- Hecho: ...
- Decisiones: ...
- Pendiente o aviso para otros: ...
```

---

## 2026-09-23 · JG-01 · Andamiaje del backend, CI y reglas del repositorio
- Hecho:
  - Solución .NET 10 con 7 proyectos y 3 de pruebas, y paquetes centralizados.
  - `Program.cs` con los 15 módulos.
  - Tipos comunes (`Resultado`, `Error`, `IReloj`, `IColaCorreo`, `IBitacora`, `IPublicadorCache`, `IContextoOrganizacion`, `AccionesBitacora`) e implementaciones nulas.
  - `CodigosError` y `LlavesRedis`.
  - CI con `plan`, `backend` y `frontend`.
  - 98 pruebas.
- Decisiones:
  - `Microsoft.OpenApi` 2.12.2, por compatibilidad con `Microsoft.AspNetCore.OpenApi` 10.
  - `Microsoft.Extensions.Hosting` para el trabajador.
  - La normalización de las llaves de Redis quedó escrita en 07 §4.
- Pendiente o aviso para otros:
  - **Todos:** registren su módulo solo en `src/Shapi.Api/Modulos/<X>Modulo.cs`, nunca en `Program.cs`. Los paquetes del stack ya están referenciados en los `.csproj`. Los códigos de error se toman de `Shapi.Contratos.CodigosError` y las acciones de la bitácora de `AccionesBitacora`.
  - **EM-01:** registrar `ColaCorreoBaseDatos` y `BitacoraBaseDatos` en su módulo reemplaza a las nulas.
  - **JG-04:** reemplaza a `PublicadorCacheNulo`.

## 2026-09-23 · JG-02 · Compuerta mínima: host → API, clave y reenvío
- Hecho:
  - Tubería de filtros (`IFiltroCompuerta`, `TuberiaCompuerta.Orden`), `FiltroApi`, `FiltroClave` y reenvío con YARP.
  - Errores 404 y 401 en JSON (08 §4).
  - `ContextoApi` y `ContextoClave` en `Shapi.Contratos/Redis`.
  - Comando temporal `sembrar-demo`.
  - 33 pruebas nuevas, con Testcontainers y un origen en memoria.
- Decisiones:
  - El origen recibe el `Host` de `url_origen` (precisado en 08 §5).
  - `/salud` solo responde con el host `localhost` (08 §1).
  - Redis se configura con `SHAPI_REDIS`.
- Pendiente o aviso para otros:
  - **JZ-01:** en `.env.example`, la variable de Redis de la compuerta es `SHAPI_REDIS=localhost:6379`.
  - **JZ-06:** el *healthcheck* de la compuerta debe llamar a `http://localhost:<puerto>/salud`, con el host `localhost`.
  - **JG-04 y JG-07:** escriban `api:{id}` y `clave:{hash}` con `ContextoApi.ACampos()` y `ContextoClave.ACampos()`, y calculen el hash con `ContextoClave.CalcularHash`.
  - **JG-05:** 502, 504 y 413 en JSON, `X-Forwarded-*`, `X-Shapi-Secreto`, quitar las demás `X-Shapi-*` y las cookies del portal, y leer el contexto en un solo *pipeline*.

