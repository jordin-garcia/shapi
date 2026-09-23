---
id: JZ-04
titulo: Bitácora de acciones sensibles (B3.2)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: pendiente
depende_de: [EM-01, DC-02]
requisitos: [RF-41]
pantallas: [B3.2]
---

# JZ-04 · Bitácora de acciones sensibles (B3.2)

**Responsable:** José Pablo Zúñiga · **Avance:** 2 · **Prioridad:** P1 · **Depende de:** EM-01, DC-02

## Objetivo
Permitir que el administrador y el soporte consulten la bitácora de acciones sensibles, filtrada por fechas.

## Contexto que debes leer
- `docs/specs/10-identidad-y-seguridad.md` §7
- `docs/specs/07-modelo-de-datos.md` §3.6
- Mockups: `mockups/B3/Bitacora.dc.html`, `BitacoraVacia.dc.html`

## Archivos que creas o modificas
- `src/Shapi.{Aplicacion,Api}/Bitacora/**` (crear)
- `contratos/openapi/sistema.yaml` (crear)
- `frontend/apps/panel/src/paginas/B3-2-Bitacora.tsx`
- `tests/**/Bitacora/**`

## Criterios de aceptación
1. `GET /api/admin/bitacora?desde=&hasta=&pagina=&tamano=` (administrador y soporte) devuelve fecha, actor (nombre, rol y organización), acción y descripción, de la más reciente a la más antigua. Por defecto, los últimos 7 días.
2. B3.2 reproduce el mockup, con su variante vacía y el selector de fechas.
3. El personal de un proveedor recibe 403.

## Pruebas obligatorias
- Integración
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
- Escribir en la bitácora (cada módulo lo hace con `IBitacora`)
