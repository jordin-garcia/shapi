---
id: JG-24
titulo: Correcciones del portal y del dominio propio (A3.6, consola y marca)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P2
estado: pendiente
programada: 2026-10-25
depende_de: []
requisitos: [RF-12, RF-15, RF-16, RF-25, RF-43, RF-47]
pantallas: [A3.6, A5.2, A5.6, B2.3]
---

# JG-24 · Correcciones del portal y del dominio propio

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P2 · **Sin dependencias**

## Objetivo
Que A3.6 funcione con las APIs de la siembra, que la consola funcione con un dominio propio y que el portal no muestre la marca de Shapi. Son hallazgos de la convergencia del 23 oct (`docs/plan/convergencia/2026-10-22.md` §4), que Jordin decidió corregir él (protocolo §E1). Los archivos son de DC-10, DC-14, DC-09, DC-11, EM-09 y JZ-17.

## Hallazgos que corrige
- **H-C11 (alta, DC-14 y JZ-17; H-E10 del ensayo del 18 oct):** `GET /api/apis/{id}/dominio` responde 500 en las dos APIs sembradas, así que A3.6 no abre. Sin `SHAPI_SECRETO_ORIGEN_*`, la siembra cifra un secreto vacío (`SiembraDemo.CrearApi`), y `GestionarDominios.EnmascararSecreto` falla en `secreto[^4..]` (`GestionarDominios.cs:206`, `ArgumentOutOfRangeException`). Se comprobó otra vez el 23 oct.
- **H-C12 (media, DC-10; H-E11):** con un dominio propio verificado, la consola llama a `https://{dominio}/…` (`urlCompleta`, que usa `clienteCompuerta.ts:44`). La CSP del portal solo permite `connect-src 'self' https://*.api.shapi.localhost` (`infra/caddy/Caddyfile.prod:19`), así que el navegador la bloquea y la consola no muestra ningún error. RF-16 dice que la consola envía la petición a `{sub}.api.{dominio_base}`.
- **H-C13 (baja, DC-09 y DC-11, RF-15):** A5.6 (`A5-6-Pago.tsx:25`) y B2.3 (`B2-3-Suscripcion.tsx:206`) dicen «Shapi no guarda el número completo…». RF-15 dice que el portal no muestra la marca de Shapi. El texto viene del mockup (`mockups/A5/Pago.dc.html:157`), pero la especificación tiene más rango (protocolo §C).
- **H-C14 (baja, DC-14):** en `frontend/apps/panel/src/paginas/A3-6-Dominio.tsx`:
  - :69: «Incluido en su plan Producto.» sale aunque el plan no lo incluya, y la insignia usa siempre el color de advertencia, también con «Verificado»;
  - :67: «Certificado TLS: Vigente» no depende del estado real;
  - :62 y :70: `dominio || datos.dominio` no deja vaciar el campo;
  - :34: ante `datos_invalidos` solo se muestra «Revise el dominio.», sin el motivo.
- **H-C15 (baja, DC-14, RF-12):** faltan pruebas de RF-12: un CNAME que apunta a otro destino deja el dominio pendiente; un dominio que no termina en `.localhost` se rechaza; y, en el frontend, «Conectar» y el error `plan_sin_dominio_propio`. Además, las pruebas del dominio propio se llaman `RF_11_*` (`tests/Shapi.Api.Tests/Apis/ApisTests.cs:1095-1172` y `tests/Shapi.Dominio.Tests/Apis/DominioPropioTests.cs:7-23`).
- **H-C16 (media, EM-09 y DC-14):** bajar a un plan de plataforma sin dominio propio no tiene en cuenta un dominio verificado (`PlataformaSuscripcionesApi.cs:274-282`). `AutorizarTls` y el host en Redis siguen activos, así que el dominio sigue funcionando en un plan que no lo incluye. 09 no lo define.

## ❓ Decisiones pendientes (preguntarle a Jordin antes de escribir código)
1. **Texto de la tarjeta en el portal (H-C13).** Recomiendo «No se guardan el número completo ni el código de seguridad: la pasarela entrega un token con el que se cobran las renovaciones. Usted solo verá la marca y los últimos cuatro dígitos.», y anotar en 11 que el mockup se corrige por RF-15.
2. **Bajar de plan con un dominio propio (H-C16).** Recomiendo tratar el dominio como un límite del plan (RF-43). Bajar a un plan sin dominio propio mientras haya uno conectado se rechaza con 422 `excede_limites_del_plan` (con el límite `dominio_propio` en el detalle), como las demás bajadas que exceden los límites (09 §5 y `CodigosError.ExcedeLimitesDelPlan`) y como dice RF-25 («no se puede bajar si excede sus límites»), y B1.4 y A2.5 lo explican. La otra opción es desconectar el dominio al aplicar la bajada.
3. **Qué URL muestra la documentación (H-C12).** Recomiendo que la documentación siga mostrando la URL del dominio propio, que es la que usará el consumidor en su sistema, y que la consola use siempre `{sub}.api.{dominio_base}`, como dice RF-16.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-12, RF-15, RF-16, RF-25, RF-43 y RF-47
- `docs/specs/08-compuerta.md` (dominio propio y TLS a pedido)
- `docs/specs/09-cobros-y-suscripciones.md` §5 (cambio de plan y límites)
- `docs/specs/11-interfaz.md` A3.6, A5.2, A5.6 y B2.3
- Mockups: `mockups/A3/Dominio*.dc.html`, `mockups/A5/Consola*.dc.html` y `mockups/A5/Pago.dc.html`
- Los `## Resultado` de DC-10, DC-14 y JZ-17

## Archivos que creas o modificas
- `src/Shapi.Aplicacion/Apis/GestionarDominios.cs` (modificar)
- `src/Shapi.Infraestructura/Siembra/Demo/SiembraDemo.cs` (modificar: generar un secreto si falta la variable)
- `frontend/apps/portal/src/modulos/consola/clienteCompuerta.ts` y la página A5.2 (modificar)
- `frontend/apps/portal/src/paginas/A5-6-Pago.tsx` y `B2-3-Suscripcion.tsx` (modificar)
- `frontend/apps/panel/src/paginas/A3-6-Dominio.tsx` (modificar)
- `src/Shapi.Api/Suscripciones/PlataformaSuscripcionesApi.cs` (modificar, según la decisión 2)
- Pruebas del backend y del frontend (crear, modificar o renombrar)
- `docs/specs/11-interfaz.md` y `09-cobros-y-suscripciones.md` (precisar lo que se decida)

## Criterios de aceptación
1. **H-C11.** Con la siembra de demostración, sin las variables `SHAPI_SECRETO_ORIGEN_*`, A3.6 abre en las dos APIs y muestra el secreto enmascarado. Un secreto de menos de 4 caracteres no produce un 500.
2. **H-C12.** Con un dominio propio verificado, la consola envía la petición a `{sub}.api.{dominio_base}` y muestra la respuesta. Si el navegador bloquea una petición, la consola muestra un error en vez de quedarse callada.
3. **H-C13.** Ninguna página del portal, A5 o B2, muestra la marca «Shapi» en sus textos (RF-15). Las URL con el dominio base `*.shapi.localhost` no cuentan.
4. **H-C14.** A3.6 muestra el plan, el estado del dominio y el del certificado según los datos reales. El campo del dominio se puede vaciar, y un `datos_invalidos` muestra su motivo.
5. **H-C15.** Las pruebas de RF-12 cubren el CNAME a otro destino, el dominio sin `.localhost`, «Conectar» y `plan_sin_dominio_propio` en el frontend, y se llaman `RF_12_*`.
6. **H-C16.** Según la decisión 2, con una prueba (RF-25 y RF-43).

## Pruebas obligatorias
- Cada hallazgo tiene una prueba que falla sin la corrección, con el código del requisito en el nombre: H-C11 (RF-47), H-C12 (RF-16), H-C13 (RF-15, en todas las páginas del portal, no solo en `/`), H-C14 (RF-12), H-C15 (RF-12) y H-C16 (RF-25).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Apis|FullyQualifiedName~Shapi.Api.Tests.Suscripciones|FullyQualifiedName~Shapi.Api.Tests.Siembra"
dotnet test tests/Shapi.Dominio.Tests
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```
Además, en el ambiente productivo simulado con la siembra (protocolo B8): abrir A3.6 de las dos APIs, verificar el dominio de Envíos Xelajú y usar la consola de su portal.

## Fuera de alcance
- Cambiar la CSP de Caddy para permitir dominios propios.
