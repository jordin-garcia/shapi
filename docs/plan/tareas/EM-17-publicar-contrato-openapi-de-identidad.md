---
id: EM-17
titulo: Publicar el contrato OpenAPI de identidad
persona: emilio
responsable: Emilio Méndez
avance: 1
prioridad: P1
estado: pendiente
depende_de: [EM-02]
requisitos: [RF-04]
pantallas: []
---

# EM-17 · Publicar el contrato OpenAPI de identidad

**Responsable:** Emilio Méndez · **Avance:** 1 · **Prioridad:** P1 · **Depende de:** EM-02

## Objetivo
Publicar el contrato OpenAPI del módulo de identidad implementado en EM-02 para que el frontend genere sus tipos desde la fuente propiedad de Emilio y pueda eliminar su contrato provisional.

## Contexto que debes leer
- `docs/specs/10-identidad-y-seguridad.md` §1
- `docs/plan/convenciones.md` §2, §5 y §7
- `docs/plan/tareas/EM-02-registro-verificacion-de-correo-e-inicio-de-sesion-del-perso.md`

## Archivos que creas o modificas
- `contratos/openapi/identidad.yaml` (crear)
- `frontend/packages/api/src/generado/identidad.ts` (generar)
- `frontend/packages/api/package.json` (exportar los tipos de identidad)
- `frontend/apps/panel/src/modulos/sesion/**` (reemplazar el contrato provisional por el generado)

## Criterios de aceptación
1. El contrato describe los endpoints de identidad implementados en EM-02, incluidos `GET /api/auth/sesion` y `POST /api/auth/salir`, con sus cuerpos y códigos HTTP reales.
2. La respuesta de sesión declara `usuario`, `organizacion`, `rol`, `correoVerificado` y `destino` con la misma forma que devuelve el backend.
3. El panel consume los tipos generados y conserva el comportamiento de guardias y cierre de sesión de DC-02.

## Pruebas obligatorias
- Vitest: sesión activa, ausencia de sesión y cierre de sesión usando el contrato generado.

## Verificación
Todos estos comandos deben pasar:
```bash
cd frontend && pnpm generar:api && pnpm lint && pnpm typecheck && pnpm test && pnpm build
node scripts/tareas.mjs --validar
```

## Fuera de alcance
- Cambiar el comportamiento del backend de EM-02.

## Notas
- DC-02 usa temporalmente `frontend/apps/panel/src/modulos/sesion/contratoSesion.ts` porque el contrato de Identidad pertenece a Emilio.
