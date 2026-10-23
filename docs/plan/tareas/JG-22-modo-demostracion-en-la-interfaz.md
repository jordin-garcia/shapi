---
id: JG-22
titulo: Modo demostración en la interfaz (reloj, fechas y bitácora)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P1
estado: pendiente
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

## ❓ Decisiones pendientes (preguntarle a Jordin antes de escribir código)
1. **Dónde va el control del reloj.** No hay mockup. Recomiendo una tarjeta «Modo demostración» al final de B3.1 (Estado de los componentes), visible solo para el administrador y solo con `SHAPI_MODO_DEMO=true`. Mostraría la fecha actual de la demostración, los botones «Adelantar 30 días» y «Adelantar 7 días» y un campo de días. La otra opción es una página propia en la barra de la administración.
2. **Sesiones al adelantar el reloj.** (2a, recomendada) Que el vencimiento de las sesiones (RF-04: inactividad y 7 días) use la **hora real** y no `IReloj`, porque es seguridad y no negocio. Así nadie queda fuera al adelantar el reloj. (2b) La otra opción es aceptar que se cierren y avisarlo en la tarjeta («Al adelantar el reloj se cierran todas las sesiones»).

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
Además, recorrer el paso 4 del guion 3 en el ambiente productivo simulado solo con la interfaz (protocolo B8) y comparar B3.1 y B3.2 con sus mockups.

## Fuera de alcance
- Retroceder el reloj.
- El modo demostración fuera del ambiente simulado.
