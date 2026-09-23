---
id: JZ-12
titulo: Estado de los componentes (B3.1)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P2
estado: pendiente
depende_de: [JZ-06, JG-09]
requisitos: [RF-39]
pantallas: [B3.1]
---

# JZ-12 · Estado de los componentes (B3.1)

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P2 · **Depende de:** JZ-06, JG-09

## Objetivo
Mostrarle al administrador y al soporte el estado de cada componente y la latencia que añade la compuerta.

## Contexto que debes leer
- `docs/specs/06-arquitectura.md` §8 (tabla de estados)
- Mockups: `mockups/B3/Main.dc.html`, `Degradado.dc.html`, `Soporte.dc.html`

## Archivos que creas o modificas
- `src/Shapi.Trabajador/Estado/**` (crear: latido `salud:trabajador`)
- `src/Shapi.{Aplicacion,Api}/Estado/**` (crear)
- `contratos/openapi/sistema.yaml` (modificar)
- `frontend/apps/panel/src/paginas/B3-1-Estado.tsx`
- `tests/**`

## Criterios de aceptación
1. El trabajador escribe `salud:trabajador` cada 10 s, con TTL de 30 s.
2. `GET /api/admin/estado` (administrador y soporte) calcula, con caché de 30 s, el estado de los 7 componentes según la tabla de 06 §8, más la latencia p95 de la compuerta en la última hora y la fecha de la comprobación.
3. B3.1 reproduce las tres variantes: normal, un componente degradado (con el aviso) y la vista del soporte.

## Pruebas obligatorias
- Unitarias de las reglas de cada componente
- Vitest

## Verificación
Todos estos comandos deben pasar, además de los generales del protocolo (B7):
```
dotnet build Shapi.slnx
dotnet test Shapi.slnx
dotnet format Shapi.slnx --verify-no-changes
cd frontend && pnpm lint && pnpm typecheck && pnpm test && pnpm build
```

## Fuera de alcance
- Alertas por correo
