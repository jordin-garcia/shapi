---
id: JG-11
titulo: Consumo, latencia y errores por API (B1.1)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-10
depende_de: [JG-09, DC-02]
requisitos: [RF-35]
pantallas: [B1.1]
---

# JG-11 · Consumo, latencia y errores por API (B1.1)

**Responsable:** Jordin García · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** JG-09, DC-02

## Objetivo
Mostrarle al proveedor el consumo de cada API por periodo: peticiones, latencia p95 total y de la compuerta, datos transferidos, peticiones por día, detalle por ruta y errores por código.

## Contexto que debes leer
- Mockups: `mockups/B1/Main.dc.html`, `mockups/B1/ConsumoVacia.dc.html`
- `docs/specs/03-requisitos.md` RF-35
- `docs/specs/08-compuerta.md` §7 (p95)
- `docs/specs/07-modelo-de-datos.md` §3.5

## Archivos que creas o modificas
- `src/Shapi.{Aplicacion,Api}/Consumo/**` (modificar)
- `contratos/openapi/consumo.yaml` (crear)
- `frontend/apps/panel/src/paginas/B1-1-Consumo.tsx`
- `frontend/apps/panel/src/modulos/consumo/**`
- `tests/*/Consumo/**`

## Criterios de aceptación
1. `GET /api/apis/{apiId}/consumo?desde=&hasta=` devuelve: peticiones totales, llamadas descontadas, p95 total y de la compuerta, bytes de entrada y de salida, una serie por día, un detalle por ruta (peticiones, % del total, p95 total y de la compuerta) y errores: rechazos de la compuerta (401, 403, 429) y del origen (4xx, 5xx, fallas), con su %. Por defecto usa los últimos 10 días.
2. La pantalla reproduce el mockup: 4 tarjetas de indicadores, barras por día, tabla por ruta, tabla de errores separando los de la compuerta de los del origen, y el selector de periodo.
3. Sin datos, se muestra la variante vacía.
4. Solo el personal de la organización dueña (propietario, editor o lector); otra organización → 404.

## Pruebas obligatorias
- Integración del endpoint con filas de `consumo_diario` conocidas (totales, p95 y %)
- Vitest de la pantalla (datos y variante vacía)

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

Verificación manual con el entorno levantado:
- Captura con la siembra de demostración comparada con B1.1

## Fuera de alcance
- Consumo por consumidor (JG-13)

## Resultado

- Se implementó `GET /api/apis/{apiId}/consumo` con el caso de uso `ConsultarConsumoApi`. Consulta las filas consolidadas, incluye ambos entornos, suma los histogramas antes de calcular cada p95 y devuelve totales, llamadas descontadas, bytes, días (incluidos los ceros), rutas y errores separados entre compuerta y origen.
- El periodo es inclusivo, por defecto los últimos diez días en Guatemala con `IReloj`; admite hasta 366 días. Las fechas inválidas responden 400 con errores por campo. Propietario, editor y lector pueden consultar; otra organización o una API inexistente responde 404. Se conserva el consumo sin ruta identificada y los porcentajes usan el total del periodo.
- B1.1 reproduce los cuatro indicadores, las barras, las tablas, el selector y la variante vacía. Muestra las llamadas descontadas en la primera tarjeta y redondea solo la presentación del p95 al milisegundo más cercano. Incluye estados de carga, error y reintento.
- Se creó `contratos/openapi/consumo.yaml` y se generó `frontend/packages/api/src/generado/consumo.ts`. Los archivos principales son `src/Shapi.Aplicacion/Consumo/ConsultarConsumoApi.cs`, `src/Shapi.Api/Consumo/Endpoints.cs`, `ConsumoModulo.cs`, `B1-1-Consumo.tsx` y `modulos/consumo/useConsumo.ts`.
- Se precisó 08 §7 y se sincronizaron `mockups/B1/Main.dc.html` y el paquete regenerado: 93,147 llamadas descontadas, p95 agregado de compuerta de 10 ms y errores de origen 250/66/0. RF-35 y los histogramas prevalecen sobre el ejemplo anterior (protocolo §C); se amplió la altura del mockup para mostrar todas las filas.
- Verificación local: compilación completa (0 errores, 0 advertencias), formato completo sin cambios, 48 pruebas de Consumo (incluidas 18 nuevas del endpoint), 352 pruebas del frontend y 7 pruebas dirigidas de B1.1, lint (sin errores, 11 advertencias previas del portal), tipos, build y plan válidos. No se agregaron dependencias ni se cambió el esquema. La CI verifica la suite completa del backend.
- Verificación visual con la siembra: 105,456 peticiones, 93,147 llamadas, 236/10 ms y 1.84 GB; cuatro rutas y diez barras con las cifras del mockup, errores 476/74/1,476 y 250/66/0. Se eligió el periodo relativo de la siembra (27 sep–6 oct) y un periodo vacío. Capturas y textos en `.shapi/jg11/`, sin versionar.
- Revisión independiente en contexto limpio: se corrigió la omisión de las llamadas descontadas y se confirmó el código sin hallazgos de corrección.
