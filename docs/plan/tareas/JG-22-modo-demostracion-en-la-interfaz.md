---
id: JG-22
titulo: Modo demostración en la interfaz (reloj, fechas y bitácora)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: hecha
programada: 2026-10-24
depende_de: [JG-21]
requisitos: [RF-04, RF-22, RF-23, RF-41, RF-44]
pantallas: [B3.1, B3.2, A2.2, A5.6, B2.4, B2.7]
---

# JG-22 · Modo demostración en la interfaz (reloj, fechas y bitácora)

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P1 · **Depende de:** JG-21

## Objetivo
Que el video del Avance 3 pueda mostrar la renovación, la gracia, la suspensión y la reactivación **solo con la interfaz**: el administrador adelanta el reloj desde el panel, y las pantallas muestran las fechas y la bitácora con ese reloj.

## Origen
Hallazgos de la convergencia del 23 oct (`docs/plan/convergencia/2026-10-22.md` §2 y §4):
- **H-C02:** `POST /api/admin/demo/reloj` (09 §9) solo se puede usar con curl o con la consola del navegador. El cliente generado `adelantarRelojDemo` existe en `frontend/packages/api/src/generado/suscripciones.ts`, pero ninguna página lo usa. Además, al adelantar el reloj 30 días **vencen todas las sesiones**, incluida la del administrador que lo adelantó: el segundo avance respondió 401.
- **H-C03:** B3.2 calcula su periodo por omisión con la fecha del navegador (`B3-2-Bitacora.tsx:23-24` y `:82-83`), pero las acciones se guardan con `IReloj`. Después de adelantar el reloj, la suspensión, la reversión y el cierre del caso no aparecían.
- Lo mismo pasa con otras fechas que el navegador calcula con `new Date()`: el periodo que se activa en A2.2 y A5.6, el fin de la clave anterior en B2.4 y las fechas de B2.7.

## Decisiones aprobadas por Jordin (2026-10-25)
1. **Control del reloj:** tarjeta «Modo demostración» al final de B3.1, visible solo para administrador con `SHAPI_MODO_DEMO=true`. Muestra fecha y hora de Guatemala, avances de 30 y 7 días y una cantidad personalizada.
2. **Sesiones:** creación, vigencia, último uso y expiración de sus cookies con hora real (`TimeProvider`), independiente de `IReloj`. Se conservan los límites de inactividad configurables (8 h por defecto) y de 7 días máximos, tanto para personal como para consumidores.

## Contexto que debes leer
- `docs/specs/09-cobros-y-suscripciones.md` §9 (modo demostración)
- `docs/specs/10-identidad-y-seguridad.md` (sesiones) y `docs/specs/03-requisitos.md` RF-04
- `docs/specs/11-interfaz.md` B3.1 y B3.2, y la precisión de B2.3 sobre los días calculados por el servidor con `IReloj`
- `docs/specs/12-decisiones.md` ADR-30 (fechas de los ciclos)
- Mockups: `mockups/B3/*.dc.html`

## Archivos que creas o modificas
- `contratos/openapi/suscripciones.yaml` (modificar: consultar el estado del reloj de demostración, por ejemplo `GET /api/admin/demo/reloj`, y, si hace falta, la fecha actual para el panel y el portal)
- `src/Shapi.Api/Suscripciones/RelojDemoApi.cs` y `Endpoints.cs` (modificar)
- Si se elige la opción 2a: el cálculo de vigencia de `Sesion` y de la cookie (`src/Shapi.Dominio/Identidad/Sesion.cs` y su servicio)
- `frontend/apps/panel/src/paginas/B3-1-Estado.tsx`, `B3-2-Bitacora.tsx` y `A2-2-Contratacion.tsx` (modificar)
- `frontend/apps/portal/src/paginas/A5-6-Pago.tsx`, `B2-4-RotarClave.tsx` y `B2-7-CambioPlan.tsx` (modificar)
- `frontend/packages/api/src/generado/*` (regenerar con `pnpm generar:api`)
- Pruebas del backend y del frontend (crear o modificar)
- `docs/specs/09-cobros-y-suscripciones.md` §9, `11-interfaz.md` (B3.1, B3.2 y la regla de las fechas) y, si cambia la sesión, `10-identidad-y-seguridad.md`

## Criterios de aceptación
1. Con `SHAPI_MODO_DEMO=true`, el administrador adelanta el reloj desde el panel. La pantalla muestra la fecha nueva de la demostración, y en menos de 10 segundos se ven en B1.4, B2.3 y la compuerta la renovación, la gracia o la suspensión que correspondan. El soporte no ve el control. Con `SHAPI_MODO_DEMO=false` no aparece y el endpoint sigue respondiendo 404.
2. Según la decisión 2:
   - con la opción 2a, las sesiones abiertas, incluida la del administrador, siguen vigentes después de adelantar el reloj;
   - con la opción 2b, la tarjeta avisa antes de confirmar que se cerrarán todas las sesiones, y después lleva al administrador a la pantalla de acceso.
3. B3.2 abre en los últimos 7 días de la **fecha del servidor** (`IReloj`), así que las acciones registradas después de adelantar el reloj aparecen sin cambiar el filtro.
4. Ninguna pantalla calcula «hoy» con el reloj del navegador para mostrar fechas de negocio. Los periodos de A2.2 y A5.6, el fin de la clave anterior en B2.4 y las fechas de B2.7 salen del servidor y coinciden con lo que se guarda.
5. El guion 3, paso 4, se puede recorrer de punta a punta solo con la interfaz: 0341 → +30 días → gracia → +7 días → suspensión con 403 → pagar con otra tarjeta → activa.

## Pruebas obligatorias
- Backend: el endpoint de consulta del reloj solo responde al administrador y solo en modo demostración (09 §9).
- Si se elige la opción 2a: una sesión creada antes de adelantar el reloj 30 días sigue vigente (RF-04).
- Frontend: la tarjeta del reloj llama a `adelantarRelojDemo` y muestra la fecha nueva; no aparece para el soporte ni fuera del modo demostración (RF-22).
- Frontend: B3.2 pide por omisión el periodo de los últimos 7 días según la fecha que devuelve el servidor (RF-41).
- Frontend: A5.6, B2.4 y B2.7 muestran las fechas del servidor y no las de `new Date()` (RF-23).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Suscripciones|FullyQualifiedName~Shapi.Api.Tests.Identidad"
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```
Ensayo automatizado aislado (adelanta el reloj global del ambiente):
```
cd tests/e2e
pnpm exec playwright test --config playwright.demo.config.ts
```

Además, recorrer el paso 4 del guion 3 en el ambiente productivo simulado solo con la interfaz (protocolo B8) y comparar B3.1 y B3.2 con sus mockups.

## Fuera de alcance
- Retroceder el reloj.
- El modo demostración fuera del ambiente simulado.


## Resultado

Integrada la tarjeta de demostración en B3.1 y la consulta de fecha del servidor en el panel y el portal. B3.2 abre con los siete días correspondientes a esa fecha. A2.2, A5.6, B2.4 y B2.7 muestran fechas del servidor; B2.3 usa los días restantes de su contrato. La respuesta de rotación confirma la expiración definitiva de la clave anterior.

Los tres caminos de creación de sesiones (personal, consumidor y definición de contraseña inicial de cuentas de plataforma), sus cookies y los dos manejadores de autenticación usan hora real. Las pruebas conservan los límites de 8 horas y 7 días con un proveedor de tiempo controlado separado del reloj de negocio.

El contrato `suscripciones.yaml` documenta las consultas del reloj y los tipos se regeneraron. Los avances aceptan de 1 a 365 días, se bloquean mientras se procesa la solicitud y muestran los errores, incluidos los HTTP sin ProblemDetails. Al terminar, incluso si la publicación falla después del incremento, se vuelve a consultar la fecha. Las precisiones están en 09 §9, 10 §1, 11 y convenciones §6.

### Verificación (2026-10-25 y 2026-10-26)
- Compilación completa sin errores ni advertencias; formato verificado sin cambios pendientes; plan válido (88 tareas).
- 344 pruebas de Suscripciones e Identidad aprobadas; otra ejecución de 110 regresiones de los módulos afectados aprobada, sin fallos ni omisiones. Se confirmó la fase roja de consulta, sesión tras el avance y sesión inicial creada con fecha de negocio.
- Lint sin errores (11 advertencias previas del portal), tipos y builds correctos. 473 pruebas del frontend en 41 archivos aprobadas con un solo hilo, aislamiento conservado y Docker detenido; las primeras ejecuciones con contención de recursos agotaron esperas y el arranque de un proceso, sin cambiar pruebas ni límites.
- Todas las imágenes del ambiente productivo simulado reconstruidas con el código actual. Ensayo aislado de Playwright aprobado en 34,1 s: contratación con 0341, avance de 30 días, gracia y renovación de plataforma en 6.600 ms; avance de 7 días, suspensión y 403 en 2.745 ms; pago con otra tarjeta y 200. Las sesiones del administrador, proveedor y consumidor permanecieron vigentes, y el soporte no vio el control.
- Once capturas del flujo real y comparación de B3.1 normal, degradada y soporte, y B3.2 con datos y vacía, con los datos de sus cinco mockups. Evidencia local en `.shapi/jg22/`. Las esperas del ensayo se ajustaron a la finalización de verificación, carga y pago; se mantuvo el límite global de diez segundos por transición.
- Revisión independiente: corregidas la sesión/cookie del acceso inicial de cuentas de plataforma y la detección de HTTP 502 sin ProblemDetails. Segunda revisión: LISTO, sin hallazgos pendientes.
