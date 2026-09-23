---
id: JG-13
titulo: Consumo y facturación por consumidor (B1.2)
persona: jordin
responsable: Jordin García
avance: 3
prioridad: P2
estado: pendiente
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
