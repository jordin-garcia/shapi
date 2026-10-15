---
id: JG-12
titulo: Consumo del ciclo para el consumidor (B2.1)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-14
depende_de: [JG-09, DC-03]
requisitos: [RF-37]
pantallas: [B2.1]
---

# JG-12 · Consumo del ciclo para el consumidor (B2.1)

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** JG-09, DC-03

## Objetivo
Mostrarle al consumidor, en el portal, el consumo de su ciclo vigente desglosado por ruta.

## Contexto que debes leer
- Mockups: `mockups/B2/Main.dc.html`, `mockups/B2/ConsumoVacia.dc.html`
- `docs/specs/03-requisitos.md` RF-37
- `docs/specs/07-modelo-de-datos.md` §4 (`cuota:susc:*`)

## Archivos que creas o modificas
- `src/Shapi.{Aplicacion,Api}/Consumo/**` (modificar)
- `contratos/openapi/consumo.yaml` (modificar)
- `frontend/apps/portal/src/paginas/B2-1-Consumo.tsx`
- `frontend/apps/portal/src/modulos/consumo/**`

## Criterios de aceptación
1. `GET /api/portal/consumo` (sesión de consumidor) devuelve: ciclo vigente, llamadas usadas en tiempo real (leídas de `cuota:susc:*`), cuota, restante, % y un detalle por ruta (peticiones, peso, llamadas descontadas, %) sacado de `consumo_diario`.
2. La pantalla reproduce el mockup B2.1, incluida la fila de totales.
3. Sin suscripción, se muestra la variante vacía con el botón "Ver los planes".

## Pruebas obligatorias
- Integración del endpoint
- Vitest de la pantalla

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Suscripción y claves (DC-11)

## Resultado

- Se implementó `GET /api/portal/consumo`, con sesión de consumidor y aislamiento por organización, consumidor y API del host. Devuelve el ciclo inclusivo para presentación, la cuota del plan, las llamadas usadas de Redis, el restante y el porcentaje.
- El detalle suma las peticiones y llamadas realmente descontadas de producción en `consumo_diario`; conserva rutas retiradas, informa el peso actual y mantiene los totales consolidados separados del contador en tiempo real. La misma suscripción conserva su consumo durante la gracia hasta renovarse.
- B2.1 muestra el resumen, la barra de cuota, el detalle por ruta y su fila de totales; reproduce la variante sin suscripción con «Ver los planes» y ofrece estados de carga y error recuperable. Consulta de nuevo cada diez segundos.
- Se actualizó `contratos/openapi/consumo.yaml`, se regeneraron sus tipos y se precisó el comportamiento en `08-compuerta.md` §7. Una suscripción ausente se identifica con `suscripcion_no_encontrada`; un fallo de Redis responde 503 en lugar de inventar consumo cero.
- Verificación: compilación completa sin errores ni advertencias, formato .NET correcto, 69 pruebas del módulo Consumo y una prueba adicional con cookie real aprobadas; lint sin errores (11 avisos preexistentes), typecheck, 414 pruebas del frontend en 33 archivos y builds de panel/portal aprobados. La fase roja comprobó el endpoint ausente y la pantalla de relleno antes de implementar.
- Se capturaron la pantalla con datos reales de demostración, las dos variantes con los datos de referencia y el error recuperable a 1440 × 900. Los textos y datos de referencia coinciden con `B2/Main.dc.html` y `B2/ConsumoVacia.dc.html`; también se compararon visualmente sus estructuras. La revisión independiente no encontró hallazgos de corrección.
- Archivos principales: `src/Shapi.{Aplicacion,Infraestructura,Api}/Consumo/**`, `ConsumoModulo.cs`, `frontend/apps/portal/src/{paginas/B2-1-Consumo.tsx,modulos/consumo/**}`, el contrato y sus tipos, y las pruebas de integración y Vitest de consumo.
