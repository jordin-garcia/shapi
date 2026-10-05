---
id: JG-07
titulo: Servicio de claves: emisión, rotación y revocación (backend)
persona: jordin
responsable: Jordin García
avance: 2
prioridad: P1
estado: hecha
programada: 2026-09-29
depende_de: [JG-04]
requisitos: [RF-26, RF-27, RF-28, RNF-07]
pantallas: []
---

# JG-07 · Servicio de claves: emisión, rotación y revocación (backend)

**Responsable:** Jordin García · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** JG-04

## Objetivo
Implementar el ciclo de vida completo de las claves de API: emisión (que usará la contratación, EM-08), rotación y revocación, y sus endpoints para el portal y para el panel.

## Contexto que debes leer
- `docs/specs/08-compuerta.md` §2 (formato de la clave)
- `docs/specs/07-modelo-de-datos.md` §3.3 (`clave`) y §5 (estados de la clave)
- `docs/specs/10-identidad-y-seguridad.md` §3 y §7 (acciones `clave.*`)
- `docs/specs/04-roles-y-permisos.md` §3.1 y §3.3
- `docs/specs/05-casos-de-uso.md` CU-13
- `docs/specs/06-arquitectura.md` §5.4
- Mockups: `mockups/A4/Claves.dc.html`, `mockups/B2/Suscripcion.dc.html`, `mockups/B2/RotarClave.dc.html`, `mockups/B2/ClaveNueva.dc.html`

## Archivos que creas o modificas
- `src/Shapi.{Dominio,Aplicacion,Infraestructura,Api}/Claves/**` (crear)
- `src/Shapi.Api/Modulos/ClavesModulo.cs` (modificar)
- `contratos/openapi/claves.yaml` (crear)
- `tests/*/Claves/**` (crear)

## Criterios de aceptación
1. El servicio de aplicación `EmitirClavesParaSuscripcion(suscripcionId)` crea una clave `produccion` y otra `pruebas` con formato `shp_prod_`/`shp_prueba_` + 26 caracteres base62 (`RandomNumberGenerator`). Guarda solo el prefijo, los últimos 4 caracteres y el SHA-256, las publica en Redis y devuelve los valores en claro **una sola vez**.
2. `POST /api/portal/claves/{id}/rotar` (el consumidor dueño) crea una clave nueva activa y deja la anterior como `rotada` con `expira_en = ahora + 24 h` (EXPIREAT en Redis). Devuelve la clave nueva en claro. Rotar una clave que ya está rotada → 422 `clave_no_rotable`.
3. `POST /api/portal/claves/{id}/revocar` (el consumidor dueño) y `POST /api/apis/{apiId}/claves/{id}/revocar` (propietario o editor de la organización) marcan la clave como `revocada`, guardan `revocada_por` y la eliminan de Redis. La compuerta responde 401 en menos de 10 s.
4. `POST /api/portal/claves/emitir` `{tipo}` emite una clave nueva de ese tipo solo si no hay otra activa del mismo tipo.
5. `GET /api/apis/{apiId}/claves` (proveedor: enmascaradas y agrupadas por consumidor, con el plan, el tipo y el estado, igual que A4.3) y `GET /api/portal/claves` (consumidor: enmascaradas, con estado y `expira_en`).
6. Bitácora: `clave.revocada_por_proveedor`, `clave.rotada` y `clave.revocada_por_consumidor`, con los textos de 10 §7.
7. Una clave completa nunca aparece en registros ni en respuestas, salvo en la emisión y en la rotación.
8. Aislamiento: la clave de otro consumidor o de otra organización → 404.

## Pruebas obligatorias
- Unitarias: formato, entropía (longitud y alfabeto) y hash
- Integración de cada endpoint, con Redis y PostgreSQL
- Aislamiento entre organizaciones y entre consumidores

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
```

## Fuera de alcance
- Pantalla A4.3 (JG-10)
- Pantallas B2.3 a B2.6 (DC-11)
- Contratación (EM-08)

## Resultado

**Qué se hizo**
- Dominio: `GeneradorClave` genera `shp_prod_`/`shp_prueba_` + 26 caracteres base62 con `RandomNumberGenerator.GetString`, sin sesgo, y calcula el SHA-256 en hex minúsculas, igual que `ContextoClave.CalcularHash` de la compuerta. `Clave` tiene `Emitir`, `Rotar` (la anterior queda `rotada` con `expira_en = ahora + 24 h`), `Revocar` (desde `activa` o `rotada`), `Enmascarada` y `EsVigente`.
- `IServicioClaves` (en `Shapi.Aplicacion/Claves`) y `ServicioClaves` (en `Shapi.Infraestructura/Claves`):
  - `EmitirClavesParaSuscripcion(suscripcionId)` emite la clave de producción y la de pruebas, solo de los tipos que no tienen una activa. Guarda, publica con `PublicarClave` y devuelve las claves completas una sola vez. Es lo que llama la contratación (EM-08).
  - `Emitir`, `Rotar`, `RevocarPropia` (portal), `ClavesDeApi` y `RevocarDeConsumidor` (panel), y `ClavesDelConsumidor`.
  - Primero se confirma en PostgreSQL y después se publica en Redis. La rotación usa `PublicarClave` para la clave nueva y `ExpirarClave` para la anterior; la revocación, `EliminarClave`.
- Endpoints, según `contratos/openapi/claves.yaml`:
  - `GET /api/apis/{apiId}/claves`, paginado por consumidor (`Permiso.VerClaves`);
  - `POST /api/apis/{apiId}/claves/{id}/revocar` (`Permiso.RevocarClaves`);
  - `GET /api/portal/claves` (`Permiso.ConsumidorVerCuenta`);
  - `POST /api/portal/claves/emitir`, `/{id}/rotar` y `/{id}/revocar` (`Permiso.ConsumidorAdministrarClaves`).
- Bitácora: `clave.rotada`, `clave.revocada_por_consumidor` y `clave.revocada_por_proveedor`, con los textos de 10 §7, el objetivo `clave` y la IP. Se guardan en la misma transacción que el cambio de la clave.
- Nuevo código de error: `clave_activa_existente` (409).
- Tipos TS generados: `frontend/packages/api/src/generado/claves.ts`.

**Decisiones**
- La sesión del consumidor todavía no existe (EM-05). Los endpoints del portal esperan en la sesión el consumidor (`ClaimTypes.NameIdentifier`), su organización (`PoliticasAutorizacion.ClaimOrganizacion`) y el ámbito `Consumidor`. Además resuelven la API con el host (`IResolutorPortal`): si la organización de la sesión no es la del host, responden 404. Quedó en 10 §2. Las pruebas simulan esa sesión con un esquema de autenticación de prueba (`SesionConsumidorDePrueba`).
- En el portal solo se administran las claves de la API del host. Una clave de otra API de la misma organización responde 404.
- Qué claves se listan y en qué orden: las activas, las rotadas mientras duran sus 24 horas y, por cada tipo sin clave activa, la última revocada (como Tienda Sololá en A4.3). No se listan las de suscripciones finalizadas. Primero producción y luego pruebas; dentro de cada tipo, activa, rotada y revocada. A4.3 ordena a los consumidores por la fecha en que contrataron. Quedó en 05 CU-13.
- Emitir con otra clave activa del mismo tipo responde 409 `clave_activa_existente`. Emitir sin una suscripción sin finalizar en la API del portal responde 404. Rotar una clave revocada también responde 422 `clave_no_rotable`, igual que una rotada. Revocar una clave ya revocada responde 200 sin cambiar nada ni registrar otra entrada en la bitácora. Quedó en 05 CU-13.
- Si al rotar hay otra clave rotada del mismo tipo que sigue dentro de sus 24 horas, esa deja de funcionar en ese momento: su `expira_en` pasa a ser la hora actual y se borra de Redis. Así nunca coexisten más de dos claves del mismo tipo (RF-27), y el consumidor puede volver a rotar si pierde la clave nueva, como dice B2.5. Es un hallazgo de la revisión en contexto limpio y quedó en 05 CU-13.
- Rotar y revocar bloquean la fila de la clave (`SELECT … FOR UPDATE`) dentro de la transacción. Así, dos rotaciones simultáneas o una rotación y una revocación no pueden dejar dos claves activas ni revivir una revocada.
- `EmitirClavesParaSuscripcion` respeta el filtro global por organización (10 §2): se llama dentro del contexto de la organización de la suscripción (la petición del consumidor que contrata). Si la suscripción no existe, es de otra organización o está finalizada, lanza `InvalidOperationException`, porque es un error de programación de quien la llama.
- El texto de la bitácora dice "en la API de Cotización de Envíos" con el nombre de la API. Si el nombre no empieza con "API", se le antepone "API".

**Archivos principales:** `src/Shapi.Dominio/Claves/{Clave,GeneradorClave}.cs`, `src/Shapi.Aplicacion/Claves/**`, `src/Shapi.Infraestructura/Claves/ServicioClaves.cs`, `src/Shapi.Api/Claves/Endpoints.cs`, `src/Shapi.Api/Modulos/ClavesModulo.cs`, `contratos/openapi/claves.yaml`, `tests/Shapi.Dominio.Tests/Claves/ClaveTests.cs` y `tests/Shapi.Api.Tests/Claves/ClavesTests.cs`.

### Correcciones de la auditoría (2026-10-04)

Paso 11 de `docs/plan/auditoria-2026-10-03.md`. El PR lleva el ID de JG-04.
- **H-95:** las descripciones de `claves.yaml` ya no mencionan `revocadaPor`, que no está en el esquema: dicen que en la base se guarda `revocada_por`.
- **H-96:** `BloquearSiExiste` hace el `SELECT … FOR UPDATE` solo después de que la consulta con el filtro de la organización (o del consumidor) encuentra la clave. Pedir la clave de otra organización responde 404 sin bloquear su fila. La prueba toma la fila desde otra conexión y comprueba que la petición termina enseguida con 404.
- **H-97:** `RF_27_Rotar_ConLaCookieRealDelPortal_Funciona` rota una clave con la cookie `portal_sesion` real de EM-05. El esquema de prueba, sin su cabecera, sigue la selección real de la aplicación (por la ruta). Por eso `RF_07_Portal_SesionDelPersonal` ahora espera 401, que es lo que responde la aplicación real desde H-05: la sesión del personal no autentica en `/api/portal/*`.
