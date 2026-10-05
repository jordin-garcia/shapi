---
id: JZ-04
titulo: Bitácora de acciones sensibles (B3.2)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 2
prioridad: P1
estado: hecha
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

## Resultado
- Se creó `GET /api/admin/bitacora` para administradores y soporte, con filtros inclusivos por fechas de Guatemala, orden descendente, paginación y el periodo predeterminado de siete días.
- Se implementó B3.2 con los estados poblado, vacío, carga y error, además del selector de fechas, siguiendo ambos mockups.
- Se publicó el contrato `sistema.yaml`, se regeneraron los tipos TypeScript y se agregaron pruebas de integración, permisos, límites de fecha y Vitest.
- Decisiones: la página predeterminada contiene 20 entradas (máximo 100); los límites de fecha que no permiten formar un intervalo completo responden `400 datos_invalidos`; el rol y la organización visibles del actor se obtienen de su membresía actual.
- Archivos principales: `src/Shapi.Api/Bitacora/Endpoints.cs`, `src/Shapi.Aplicacion/Bitacora/ModelosBitacora.cs`, `contratos/openapi/sistema.yaml`, `frontend/apps/panel/src/paginas/B3-2-Bitacora.tsx` y las pruebas de bitácora.

### Correcciones de la auditoría (2026-10-04)

Paso 10 de `docs/plan/auditoria-2026-10-03.md` (`[JZ-04] Correcciones de la auditoría: bitácora`).
- **H-85:** el personal de plataforma se muestra con la organización «Plataforma Shapi», como el mockup, aunque la organización se llame «Shapi» en la siembra base. La prueba compara con el texto literal.
- **H-86:** B3.2 pide la página elegida y muestra un paginador («Página X de Y», «Anterior» y «Siguiente») solo si hay más de una página. Al cambiar el periodo vuelve a la página 1. Mientras carga otra página, la tabla anterior se conserva (`keepPreviousData`).
- **H-87:** prueba de los actores consumidor (con la organización del proveedor), sistema (sin organización), miembro de un proveedor y usuario sin membresía.
- **H-88:** el 400 trae `errores` por parámetro (`desde`, `hasta`, `pagina` y `tamano`).
- **H-89:** `ActorBitacora.rol` es un `enum` en `sistema.yaml`, que incluye `usuario`.
- **H-90:** 10 §7 explica que el rol y la organización del actor salen de su membresía actual.
- **H-91:** Vitest del estado de error con «Reintentar» y del periodo predeterminado de la primera consulta (hoy y los seis días anteriores, en Guatemala).
- **Decidido (3 oct):**
  - (b): el endpoint muestra «Plataforma Shapi» para la organización de tipo `plataforma`, precisado en 10 §7;
  - (a): paginador mínimo que solo aparece con más de una página.
