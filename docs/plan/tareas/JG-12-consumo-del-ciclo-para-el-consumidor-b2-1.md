---
id: JG-12
titulo: Consumo del ciclo para el consumidor (B2.1)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P1
estado: pendiente
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
