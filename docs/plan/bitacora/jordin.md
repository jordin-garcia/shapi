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
