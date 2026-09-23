---
id: EM-11
titulo: Historial de pagos (B1.3 y la API de B2.2)
persona: emilio
responsable: Emilio Méndez
avance: 3
prioridad: P1
estado: pendiente
depende_de: [EM-09, EM-08]
requisitos: [RF-21]
pantallas: [B1.3]
---

# EM-11 · Historial de pagos (B1.3 y la API de B2.2)

**Responsable:** Emilio Méndez · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-09, EM-08

## Objetivo
Mostrar el historial de pagos de la organización en el panel y exponer el del consumidor para el portal.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-21
- Mockups: `mockups/B1/Pagos.dc.html`, `PagosVacia.dc.html`, `mockups/B2/Pagos.dc.html`

## Archivos que creas o modificas
- `src/*/Pagos/**` (modificar)
- `contratos/openapi/pagos.yaml` (crear)
- `frontend/apps/panel/src/paginas/B1-3-Pagos.tsx`
- `tests/*/Pagos/**`

## Criterios de aceptación
1. `GET /api/pagos?pagina=&tamano=` (propietario y lector) devuelve los pagos de la suscripción de plataforma: fecha, concepto, descripción, periodo, monto, marca, últimos 4 y estado.
2. `GET /api/portal/pagos` hace lo mismo para el consumidor en la API del portal.
3. B1.3 reproduce el mockup, con su variante vacía.
4. Aislamiento entre organizaciones y entre consumidores.

## Pruebas obligatorias
- Integración
- Vitest de B1.3

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Pantalla B2.2 (DC-13)
