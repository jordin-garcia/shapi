---
id: JG-23
titulo: Correcciones de cobros y suscripciones (gracia por reversión, aviso de la Prueba, cambio programado y cierre de ciclo)
persona: jordin
responsable: Jordin García
avance: final
prioridad: P2
estado: pendiente
programada: 2026-10-25
depende_de: [JG-21]
requisitos: [RF-17, RF-20, RF-22, RF-23, RF-24, RF-25, RF-44]
pantallas: [B1.4, B2.3]
---

# JG-23 · Correcciones de cobros y suscripciones

**Responsable:** Jordin García · **Avance:** final · **Prioridad:** P2 · **Depende de:** JG-21

## Objetivo
Corregir los hallazgos de cobros y suscripciones de la convergencia del 23 oct (`docs/plan/convergencia/2026-10-22.md` §4), para que B1.4 y B2.3 digan lo que pasó y para que el cierre de ciclo no pueda cobrar dos veces. Jordin decidió el 23 oct corregir él todos los hallazgos, sin auditoría completa. Los archivos son de EM-09, EM-10, EM-13, EM-14 y DC-11 (protocolo §E1).

## Hallazgos que corrige
- **H-C04 (media, EM-09 y EM-14):** en la gracia por reversión de un pago (RF-24), B1.4 se equivoca, como se vio en el guion 3, paso 5:
  - dice «El cobro de la renovación fue rechazado» (`B1-4-Suscripcion.tsx:71`), aunque el pago se revirtió;
  - marca «Periodo vencido» aunque el ciclo sigue en curso;
  - muestra «Cobro rechazado —»;
  - el periodo de gracia sale del 1 al 5 dic (`:96`, de `proximaRenovacion` a `graciaHasta − 1`), aunque empezó el día de la reversión.

  `ultimoRechazoEn` (`PlataformaSuscripcionesApi.cs:72`) solo toma los pagos `rechazado`. El portal ya lo resuelve en `ContratacionApi.cs:247-253`.
- **H-C05 (media, EM-10, RF-44):** el panel no avisa 7 días antes de que venza la Prueba. Solo se envía el correo `prueba_por_vencer`. B1.4 y los layouts no tienen el aviso y la API no expone el dato.
- **H-C06 (media, EM-13 y DC-13, RF-25):** después de programar una bajada de plan en B2.7, B2.3 no la muestra ni deja cancelarla. `GET /api/portal/suscripcion` no devuelve el plan siguiente (`ContratacionApi.cs:259-274`), y el portal no tiene un endpoint para cancelar el cambio, como `/api/suscripcion/cambio-programado` del panel. RF-25 pide que «la pantalla lo indica», y 09 §5 dice que la interfaz muestra «A partir del {fin} su plan será {X}» y **permite cancelarla**.
- **H-C07 (alta, EM-10):** `src/Shapi.Trabajador/CierreCiclo/CierreCiclo.cs:48-97` (plataforma) y `:139-166` (API) renuevan todas las suscripciones en **una sola transacción** y cobran a la pasarela dentro de ella. Si una falla, se revierten también los pagos que la pasarela ya autorizó para las demás, sin reembolsarlos, y se vuelven a cobrar al minuto siguiente.
- **H-C08 (baja, EM-09, RF-20):** `PlataformaSuscripcionesApi.cs:128` llama a `PrepararMedio` con `requerido = true` aunque el plan cueste Q 0.00, y cobra Q 0 a la pasarela. Un plan de plataforma gratuito que no sea Prueba pediría tarjeta. Hoy no se siembra ninguno.
- **H-C09 (baja, EM-14):** al editar la cuota de un plan de plataforma, `AdministrarPlanesPlataforma.cs:100` no vuelve a publicar `org:{id}`. La compuerta sigue con la cuota anterior hasta la resincronización.
- **H-C10 (baja, EM-14, RF-17):** ninguna prueba comprueba que un precio editado se cobra en la siguiente renovación sin tocar el ciclo vigente. El código ya lo hace (`CierreCiclo.cs:118`).

## Decisiones aprobadas por Jordin (2026-10-26)
1. **Cuota de un plan de plataforma editado (H-C09).** El cambio llega a la compuerta de inmediato, como en los planes de API (RF-18). El precio aplica desde la siguiente renovación.
2. **Dónde va el aviso de la Prueba (H-C05).** Un aviso de advertencia en B1.4 y en la parte superior del panel, para el propietario, durante los 7 días anteriores al fin de la Prueba, con la fecha y un enlace a los planes.

## Contexto que debes leer
- `docs/specs/09-cobros-y-suscripciones.md` §3 a §5 y §8
- `docs/specs/03-requisitos.md` RF-17, RF-20, RF-22 a RF-25 y RF-44
- `docs/specs/12-decisiones.md` ADR-30
- `docs/specs/11-interfaz.md` B1.4 y B2.3
- Mockups: `mockups/B1/Suscripcion*.dc.html` y `mockups/B2/Suscripcion*.dc.html`
- Los `## Resultado` de EM-09, EM-10, EM-13, EM-14 y DC-13

## Archivos que creas o modificas
- `src/Shapi.Api/Suscripciones/PlataformaSuscripcionesApi.cs` y `ContratacionApi.cs` (modificar)
- `src/Shapi.Trabajador/CierreCiclo/CierreCiclo.cs` (modificar)
- `src/Shapi.Api/Planes/AdministrarPlanesPlataforma.cs` (modificar)
- `contratos/openapi/suscripciones.yaml` (modificar) y `frontend/packages/api/src/generado/*` (regenerar)
- `frontend/apps/panel/src/paginas/B1-4-Suscripcion.tsx` y el layout del panel, si el aviso va arriba (modificar)
- `frontend/apps/panel/src/paginas/A2-2-Contratacion.tsx` y `A2-3-Confirmacion.tsx` (contratar y confirmar un plan gratuito sin tarjeta ni cobro)
- `frontend/apps/portal/src/paginas/B2-3-Suscripcion.tsx` (modificar)
- Pruebas del backend y del frontend (crear o modificar)
- `docs/specs/09-cobros-y-suscripciones.md` y `11-interfaz.md` (precisar lo que se decida)

## Criterios de aceptación
1. **H-C04.** En la gracia por reversión, B1.4 dice que el pago se revirtió, con su fecha y su monto; muestra el periodo vigente como vigente; y el rango de gracia va del día de la reversión al último día con servicio (ADR-30). En la gracia por un cobro rechazado, todo queda como hoy.
2. **H-C05.** Desde 7 días antes del fin de la Prueba, el panel muestra un aviso con la fecha en que vence y un enlace para contratar un plan. La fecha la calcula el servidor con `IReloj` (RF-44).
3. **H-C06.** Con una bajada programada, B2.3 muestra «A partir del {fin} su plan será {X}» y permite cancelarla, igual que B1.4 en el panel (RF-25 y 09 §5). El portal tiene un endpoint para cancelar el cambio, con su contrato.
4. **H-C07.** Cada suscripción se renueva en su propia transacción. Si una falla, las demás conservan su pago y su ciclo nuevo, y no se les vuelve a cobrar (RF-22).
5. **H-C08.** Contratar un plan de plataforma de Q 0.00 no pide tarjeta ni llama a la pasarela (RF-20).
6. **H-C09.** Según la decisión 1.
7. **H-C10.** Hay una prueba de RF-17 para el precio editado.

## Pruebas obligatorias
- Cada hallazgo tiene una prueba que falla sin la corrección, con el código del requisito en el nombre:
  - H-C04: B1.4 después de revertir el pago del ciclo vigente (RF-24);
  - H-C05: el aviso aparece a los 7 días del fin de la Prueba y no antes (RF-44);
  - H-C06: `GET /api/portal/suscripcion` y B2.3 con una bajada programada (RF-25);
  - H-C07: dos suscripciones que vencen y la segunda lanza una excepción; la primera conserva su pago y la siguiente corrida no la vuelve a cobrar (RF-22);
  - H-C08 (RF-20), H-C09 (RF-17) y H-C10 (RF-17).

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
node scripts/tareas.mjs --validar
dotnet build Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
dotnet test tests/Shapi.Api.Tests --filter "FullyQualifiedName~Shapi.Api.Tests.Suscripciones|FullyQualifiedName~Shapi.Api.Tests.Planes|FullyQualifiedName~Shapi.Api.Tests.Pagos"
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```
Además, recorrer en el ambiente productivo simulado la reversión del guion 3, paso 5, y ver B1.4 (protocolo B8).

El ensayo automatizado de JG-23 se ejecuta desde `tests/e2e/` con `pnpm exec playwright test --config playwright.suscripciones.config.ts`, sobre un ambiente de demostración recién sembrado y aislado. También comprueba la bajada y cancelación del consumidor y el aviso de la Prueba; adelanta el reloj global siete días. Su configuración está separada del ensayo del reloj de JG-22 para que cada uno parta de sus propios datos iniciales.

## Fuera de alcance
- Reembolsos de pagos de API.
