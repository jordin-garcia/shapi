---
id: JZ-09
titulo: Cuentas de administración y soporte (A6.5)
persona: jose-pablo
responsable: José Pablo Zúñiga
avance: 3
prioridad: P1
estado: pendiente
depende_de: [EM-04]
requisitos: [RF-42]
pantallas: [A6.5]
---

# JZ-09 · Cuentas de administración y soporte (A6.5)

**Responsable:** José Pablo Zúñiga · **Avance:** 3 · **Prioridad:** P1 · **Depende de:** EM-04

## Objetivo
Permitir que el administrador cree, desactive y reactive las cuentas de administración y de soporte, que definen su contraseña con un enlace.

## Contexto que debes leer
- `docs/specs/03-requisitos.md` RF-42
- `docs/specs/05-casos-de-uso.md` CU-21
- `docs/specs/10-identidad-y-seguridad.md` §1 (enlace `definir_contrasena`)
- Mockups: `mockups/A6/Cuentas.dc.html`, `CuentasVacia.dc.html` y `mockups/A1/NuevaContrasena.dc.html` (para definir la contraseña)

## Archivos que creas o modificas
- `src/*/Administracion/**` (modificar)
- `contratos/openapi/administracion.yaml`
- `frontend/apps/panel/src/paginas/A6-5-Cuentas.tsx`
- Página pública `/definir-contrasena?token=` (crear `paginas/A6-5b-DefinirContrasena.tsx`, basada en el diseño de A1.4b)
- `tests/**`

## Criterios de aceptación
1. `GET` y `POST /api/admin/cuentas` `{nombre, correo, rol: administrador|soporte}` crean el usuario sin contraseña, con su membresía en la organización de plataforma y un token `definir_contrasena` de 7 días, y encolan el correo.
2. `POST /api/auth/definir-contrasena` `{token, contrasena}` define la contraseña, verifica el correo e inicia sesión.
3. `POST /api/admin/cuentas/{id}/desactivar` y `/activar`. Un administrador no puede desactivar su propia cuenta (422). Una cuenta desactivada no puede entrar. Bitácora `cuenta_plataforma.*`.
4. A6.5 (con su variante vacía) reproduce el mockup.

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
- Miembros de proveedores (EM-12)
