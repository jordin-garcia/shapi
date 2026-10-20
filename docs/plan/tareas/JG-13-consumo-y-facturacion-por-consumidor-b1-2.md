---
id: JG-13
titulo: Consumo y facturación por consumidor (B1.2)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P2
estado: hecha
programada: 2026-10-16
depende_de: [JG-09, EM-08]
requisitos: [RF-36]
pantallas: [B1.2]
---

# JG-13 · Consumo y facturación por consumidor (B1.2)

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** JG-09, EM-08

## Objetivo
Mostrarle al proveedor, para cada consumidor, el consumo de su ciclo y lo que se le facturó.

## Contexto que debes leer
- Mockups: `mockups/B1/Consumidores.dc.html`, `mockups/B1/ConsumidoresVacia.dc.html`
- `docs/specs/03-requisitos.md` RF-36
- `docs/specs/09-cobros-y-suscripciones.md` §8

## Archivos que creas o modificas
- `src/Shapi.{Aplicacion,Api}/Consumo/**` (modificar)
- `contratos/openapi/consumo.yaml` (modificar)
- `frontend/apps/panel/src/paginas/B1-2-Consumidores.tsx`

## Criterios de aceptación
1. `GET /api/apis/{apiId}/consumidores` devuelve, por cada suscripción no finalizada: consumidor, plan, ciclo, consumo del ciclo contra la cuota, %, facturación (el precio pagado en el ciclo) y estado del último cobro. Incluye los totales.
2. La pantalla reproduce B1.2. El botón "Invitar consumidores" lleva a `/panel/apis/:id/consumidores/invitar` (B1.5).
3. Sin consumidores, se muestra la variante vacía.

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
- Invitaciones (EM-15)

## Resultado

Se implementó `GET /api/apis/{apiId}/consumidores`, autorizado para propietario, editor y lector y aislado por organización y API. Devuelve las suscripciones no finalizadas con empresa, plan, ciclo inclusivo en Guatemala, llamadas de producción consolidadas, cuota, porcentaje, pagos autorizados del periodo y estado del último cobro; incluye los totales. La consulta reúne todas las filas en dos lecturas, sin consultas por consumidor, y responde sin caché HTTP.

B1.2 reemplaza la pantalla pendiente por la tabla, los totales y la variante vacía de los mockups. Propietario y editor pueden abrir B1.5 mediante «Invitar consumidores». Se conservaron la ruta y el layout existentes y se agregaron estados de carga, error recuperable y falta de permiso.

Decisiones precisadas en 09 §8: solo producción; gracia hasta hoy cuando aún no se renovó el ciclo; facturación como suma de pagos autorizados del mismo periodo, incluidos prorrateos, sin usar el precio actual del plan; último cobro por fecha e ID; sin pagos, cero y «Sin cobros»; filas por empresa e ID; el pie distingue suscripciones activas de las demás vigentes. No se agregaron dependencias ni migraciones.

Pruebas: 16 casos nuevos de integración RF-36/RNF-08 y cinco de interfaz. Cubren varias suscripciones y ciclos, producción frente a pruebas, rutas retiradas, pagos históricos y rechazados, cero consumo y cobros ausentes, gracia, exclusión de finalizadas, totales, permisos, aislamiento, textos y datos del mockup, enlace de invitación, variante vacía, carga y reintento.

Archivos principales: `src/Shapi.{Aplicacion,Api,Infraestructura}/Consumo/**`, `src/Shapi.Api/Modulos/ConsumoModulo.cs`, `contratos/openapi/consumo.yaml`, los tipos generados, `frontend/apps/panel/src/paginas/B1-2-Consumidores.tsx` y sus pruebas.

Verificación: 86 pruebas del módulo de consumo y 464 pruebas del frontend en 41 archivos aprobadas sin omisiones; compilación completa .NET sin errores ni advertencias, formato, tipos generados, lint, typecheck, builds de panel y portal y validador del plan correctos. El lint conserva 11 advertencias previas y el build del portal su aviso de tamaño. La primera suite frontend en procesos separados se interrumpió tras tres fallos bajo saturación; los 11 casos de los archivos afectados y la suite completa posterior pasaron con un solo hilo y aislamiento entre archivos, sin cambiar pruebas ni plazos. Se compararon visualmente los dos mockups con la tabla, su variante vacía y el error recuperable; seis capturas a 1600 × 900 y 1366 × 650 en `.shapi/jg13/`.
