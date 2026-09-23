---
id: JG-11
titulo: Consumo, latencia y errores por API (B1.1)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: pendiente
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
