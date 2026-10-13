---
id: JZ-08
titulo: Administración de organizaciones (A6.2 y A6.2b)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P1
estado: hecha
programada: 2026-10-10
depende_de: [JZ-04, JG-04]
requisitos: [RF-38]
pantallas: [A6.2, A6.2b]
---

# JZ-08 · Administración de organizaciones (A6.2 y A6.2b)

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** JZ-04, JG-04

## Objetivo
Permitir que el administrador liste las organizaciones y las suspenda o reactive por motivos administrativos, con efecto inmediato en la compuerta.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-38
- `docs/specs/05-casos-de-uso.md` CU-18
- `docs/specs/07-modelo-de-datos.md` §3.1 (estado efectivo)
- Mockups: `mockups/A6/Organizaciones.dc.html`, `OrganizacionesVacia.dc.html`, `Suspender.dc.html`

## Archivos que creas o modificas
- `src/Shapi.{Aplicacion,Api}/Administracion/**` (crear)
- `src/Shapi.Dominio/Organizaciones/Organizacion.cs` (modificar **solo** para agregar `Suspender(motivo)` y `Reactivar()`; es de Emilio, cambio acordado)
- `contratos/openapi/administracion.yaml` (crear)
- `frontend/apps/panel/src/paginas/A6-2-Organizaciones.tsx` y `A6-2b-Suspender.tsx`
- `tests/**`

## Criterios de aceptación
1. `GET /api/admin/organizaciones` (administrador y soporte) devuelve el nombre, el correo del propietario, el número de APIs, el plan, el ciclo y el estado efectivo (activa, en gracia o suspendida).
2. `POST /api/admin/organizaciones/{id}/suspender` `{motivo}` y `/reactivar` (solo el administrador) cambian `estado_admin` y llaman a `PublicarOrganizacion`. La compuerta responde 403 `api_no_disponible` en menos de 10 s. Se encola `organizacion_suspendida` y se registra en la bitácora.
3. Reactivar solo quita la suspensión administrativa. Si la suscripción sigue suspendida por falta de pago, la organización sigue suspendida.
4. A6.2 (con su variante vacía) y A6.2b (con el número de consumidores afectados) reproducen sus mockups.

## Pruebas obligatorias
- Integración: suspender → llave de Redis → 403 en la compuerta (en memoria)
- Permisos del soporte
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
- Suspensión por falta de pago (EM-10)

## Resultado

- Se agregaron los endpoints administrativos para listar, suspender y reactivar organizaciones, con permisos diferenciados para administrador y soporte y contrato OpenAPI generado para el frontend.
- Los casos de uso cambian únicamente `estado_admin`, registran la acción sensible, encolan `organizacion_suspendida` al suspender y publican el estado efectivo en Redis después de confirmar la transacción.
- La integración comprueba que una suspensión llega a la compuerta y produce 403 `api_no_disponible`; la reactivación conserva la suspensión efectiva cuando la suscripción sigue suspendida por falta de pago.
- A6.2 incluye los estados poblado, vacío, carga y error; A6.2b muestra el impacto, exige el motivo administrativo y conserva los textos y la composición del mockup. La precisión del campo de motivo quedó registrada en `docs/specs/11-interfaz.md`.
- Se verificaron compilación y formato .NET, pruebas dirigidas de dominio y API, lint, tipos, las 345 pruebas de frontend, el build de ambos frontends y capturas locales a 1440 × 900.

Archivos principales: `src/Shapi.Aplicacion/Administracion/OrganizacionesAdministracion.cs`, `src/Shapi.Api/Administracion/RepositorioOrganizacionesAdministracion.cs`, `src/Shapi.Api/Administracion/Endpoints.cs`, `contratos/openapi/administracion.yaml`, `frontend/apps/panel/src/paginas/A6-2-Organizaciones.tsx` y `frontend/apps/panel/src/paginas/A6-2b-Suspender.tsx`.
